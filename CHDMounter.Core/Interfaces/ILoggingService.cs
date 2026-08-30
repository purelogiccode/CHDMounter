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
}