namespace CHDMounter.Core.Models;

/// <summary>
///     Represents a single log entry displayed in the application's log view.
/// </summary>
public class LogEntry
{
    /// <summary>
    ///     Gets or sets the date and time when the log entry was created.
    ///     Defaults to <see cref="DateTime.Now" /> (local time) for display in the UI.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    ///     Gets or sets the log message text.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether this log entry represents an error.
    /// </summary>
    public bool IsError { get; set; }
}