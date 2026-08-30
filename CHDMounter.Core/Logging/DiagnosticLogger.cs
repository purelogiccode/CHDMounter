using SerilogLog = Serilog.Log;

namespace CHDMounter.Core.Logging;

/// <summary>
///     Provides diagnostic logging initialization, log file path management, and log cleanup functionality.
/// </summary>
public static class DiagnosticLogger
{
    /// <summary>
    ///     Gets the path to the current log file, or <c>null</c> if not initialized.
    /// </summary>
    public static string? LogFilePath { get; private set; }

    /// <summary>
    ///     Gets the application data folder path (e.g. %LOCALAPPDATA%/CHDMounter).
    /// </summary>
    public static string AppDataFolder { get; private set; } = string.Empty;

    /// <summary>
    ///     Gets the application data folder path where log files are stored.
    /// </summary>
    public static string AppDataLogFolder { get; private set; } = string.Empty;

    /// <summary>
    ///     Gets the application data folder path for the specified application name.
    /// </summary>
    /// <param name="appName">The application name used as a subfolder under LocalApplicationData.</param>
    /// <returns>The full path to the application data folder.</returns>
    public static string GetAppDataFolder(string appName)
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName);
    }

    /// <summary>
    ///     Initializes the diagnostic logger, creating the log directory and configuring Serilog.
    /// </summary>
    /// <param name="appName">The application name used for the log folder path. Defaults to "CHDMounter".</param>
    public static void Initialize(string appName = "CHDMounter")
    {
        AppDataFolder = GetAppDataFolder(appName);
        AppDataLogFolder = Path.Combine(AppDataFolder, "logs");
        Directory.CreateDirectory(AppDataLogFolder);
        LogFilePath = Path.Combine(AppDataLogFolder, $"debug_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        AppLogger.Initialize(LogFilePath);
    }

    /// <summary>
    ///     Deletes log files older than 7 days from the application data log folder.
    /// </summary>
    public static void CleanupOldLogs()
    {
        try
        {
            var logDir = AppDataLogFolder;
            if (string.IsNullOrEmpty(logDir) || !Directory.Exists(logDir))
                return;

            var oldLogs = Directory.GetFiles(logDir, "debug_*.log");
            foreach (var log in oldLogs)
                try
                {
                    var fi = new FileInfo(log);
                    if (fi.CreationTime < DateTime.Now.AddDays(-7))
                        File.Delete(log);
                }
                catch (Exception ex)
                {
                    SerilogLog.Warning(ex, "Failed to delete old log file: {LogPath}", log);
                }
        }
        catch (Exception ex)
        {
            SerilogLog.Warning(ex, "Failed to cleanup old logs");
        }
    }

    /// <summary>
    ///     Gets the application data folder path for the current application.
    /// </summary>
    /// <returns>The full path to the application data folder.</returns>
    public static string GetAppDataFolderForCurrentApp()
    {
        return AppDataFolder;
    }

    /// <summary>
    ///     Writes a visually distinct section header to the diagnostic log.
    /// </summary>
    /// <param name="section">The section title to display.</param>
    public static void LogSection(string section)
    {
        var line = new string('=', 60);
        SerilogLog.Debug(line);
        SerilogLog.Debug("  {Section}", section);
        SerilogLog.Debug(line);
    }

    /// <summary>
    ///     Writes a message to the Serilog diagnostic log.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Log(string message)
    {
        SerilogLog.Debug(message);
    }
}