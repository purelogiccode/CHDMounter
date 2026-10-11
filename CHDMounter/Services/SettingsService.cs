using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using CHDMounter.Interfaces;
using CHDMounter.Models;
using Serilog;

namespace CHDMounter.Services;

/// <summary>
///     Manages loading and saving of application settings. On Windows the file is encrypted
///     with DPAPI; on Linux and macOS it is stored as plain UTF-8 JSON (settings contain no
///     sensitive data and keep the file readable across the platforms the app supports).
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SettingsService" /> class and loads settings from disk.
    /// </summary>
    /// <param name="appName">The application name used to determine the settings folder path.</param>
    public SettingsService(string appName)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName);
        _settingsFilePath = Path.Combine(folder, "settings.dat");
        Load();
    }

    /// <summary>
    ///     Gets the current application settings.
    /// </summary>
    public AppSettings Settings { get; private set; } = new();

    /// <summary>
    ///     Saves the current settings to disk with DPAPI encryption.
    /// </summary>
    public void Save()
    {
        try
        {
            var folder = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            Settings.MountBackend = NormalizeBackend(Settings.MountBackend);
            var json = JsonSerializer.Serialize(Settings);
            var data = Encoding.UTF8.GetBytes(json);
            File.WriteAllBytes(_settingsFilePath, Protect(data));
        }
        catch (Exception ex)
        {
            // Save failures (disk full, permissions, DPAPI) are environmental, not app bugs.
            Log.Information(ex, "SettingsService: Failed to save settings");
            Trace.TraceError("SettingsService: Failed to save settings to '{0}'. Error: {1}", _settingsFilePath,
                ex.Message);
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
                return;

            var encrypted = File.ReadAllBytes(_settingsFilePath);
            var json = Encoding.UTF8.GetString(Unprotect(encrypted));
            Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            Settings.MountBackend = NormalizeBackend(Settings.MountBackend);
        }
        catch (Exception ex)
        {
            // A corrupt or foreign settings file is environmental data corruption,
            // not a bug in this application. Log below Warning to keep it out of
            // bug reports, and delete the unreadable file so the failure does not
            // recur on every launch (the app already resets to defaults).
            Log.Information(ex, "SettingsService: Failed to load settings, resetting to defaults: {Message}",
                ex.Message);
            Trace.TraceWarning("SettingsService: Failed to load settings from '{0}', resetting to defaults. Error: {1}",
                _settingsFilePath, ex.Message);
            Settings = new AppSettings();

            try
            {
                if (File.Exists(_settingsFilePath))
                    File.Delete(_settingsFilePath);
            }
            catch
            {
                // Best-effort cleanup; the corrupt file will simply be retried next launch.
            }
        }
    }

    /// <summary>
    ///     Restricts the mount backend to what the current operating system supports
    ///     (FUSE on Linux/macOS; Dokan or WinFsp on Windows).
    /// </summary>
    /// <param name="backend">The backend stored in the settings file.</param>
    /// <returns>A backend that is valid for the current platform.</returns>
    private static MountBackend NormalizeBackend(MountBackend backend)
    {
        if (OperatingSystem.IsWindows())
            return backend == MountBackend.Fuse ? MountBackend.Dokan : backend;

        return MountBackend.Fuse;
    }

    private static byte[] Protect(byte[] data)
    {
        return OperatingSystem.IsWindows()
            ? ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser)
            : data;
    }

    private static byte[] Unprotect(byte[] data)
    {
        return OperatingSystem.IsWindows()
            ? ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser)
            : data;
    }
}