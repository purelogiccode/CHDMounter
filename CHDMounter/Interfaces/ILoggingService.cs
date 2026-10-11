using System.Collections.ObjectModel;

namespace CHDMounter.Core.Interfaces;

/// <summary>
///     Defines a service for logging application messages and errors.
/// </summary>
public interface ILoggingService
{
    /// <summary>
    ///     Gets the collection of log entries displayed in the UI.
    /// </summary>
    ObservableCollection<LogEntry> LogEntries { get; }

    /// <summary>
    ///     Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void Log(string message);

    /// <summary>
    ///     Logs an error message.
    /// </summary>
    /// <param name="message">The error message to log.</param>
    void LogError(string message);

    /// <summary>
    ///     Logs an error with its exception, preserving the stack trace in the bug report.
    ///     The UI shows <paramref name="message" />; Serilog receives the exception so
    ///     <see cref="Logging.BugReportSink" /> forwards full exception details.
    /// </summary>
    /// <param name="message">The error message to log.</param>
    /// <param name="ex">The exception whose details and stack trace to preserve.</param>
    void LogError(string message, Exception ex);

    /// <summary>
    ///     Logs a user-facing error (wrong console type, missing driver, mount point busy,
    ///     missing WinFsp/Dokan native library, unparsable disc, etc.) without filing a
    ///     bug report. The entry still appears as an error in the UI, but it is written
    ///     to Serilog at Information level so <see cref="Logging.BugReportSink" /> ignores it.
    /// </summary>
    /// <param name="message">The user-facing error message to log.</param>
    void LogUserError(string message);
}