using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Serilog;

namespace CHDMounter.Core.Services;

/// <summary>
///     Manages loading and saving of application settings using DPAPI encryption.
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

            var json = JsonSerializer.Serialize(Settings);
            var data = Encoding.UTF8.GetBytes(json);
            var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_settingsFilePath, encrypted);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "SettingsService: Failed to save settings");
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
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(decrypted);
            Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
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
}