using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace CHDMounter.Core.Services;

/// <summary>
///     Provides thread-safe logging with UI-bound observable log entries and Serilog integration.
/// </summary>
public class LoggingService : ILoggingService
{
    private const int MaxEntries = 5000;
    private readonly Lock _dedupLock = new();
    private readonly Dispatcher? _dispatcher;
    private string _lastMessage = "";
    private DateTime _lastMessageTime;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LoggingService" /> class.
    /// </summary>
    /// <param name="dispatcher">
    ///     The WPF dispatcher for thread-safe UI updates. If <c>null</c>, uses the current application
    ///     dispatcher.
    /// </param>
    public LoggingService(Dispatcher? dispatcher = null)
    {
        _dispatcher = dispatcher ?? Application.Current?.Dispatcher;
    }

    /// <summary>
    ///     Gets the observable collection of log entries for data binding.
    /// </summary>
    public ObservableCollection<LogEntry> LogEntries { get; } = [];

    /// <summary>
    ///     Logs an informational message and writes it to Serilog.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Log(string message)
    {
        AppendEntry(message, false);
        Serilog.Log.Information(message);
    }

    /// <summary>
    ///     Logs an error message and writes it to Serilog.
    /// </summary>
    /// <param name="message">The error message to log.</param>
    public void LogError(string message)
    {
        AppendEntry(message, true);
        Serilog.Log.Error(message);
    }

    /// <summary>
    ///     Logs an error with its exception, preserving the stack trace for bug reports.
    /// </summary>
    /// <param name="message">The error message to log.</param>
    /// <param name="ex">The exception whose details and stack trace to preserve.</param>
    public void LogError(string message, Exception ex)
    {
        AppendEntry($"{message} ({ex.GetType().Name}: {ex.Message})", true);
        Serilog.Log.Error(ex, "{Message}", message);
    }

    /// <summary>
    ///     Logs a user-facing error without filing a bug report. The UI entry is still
    ///     marked as an error, but Serilog receives it at Information level so the
    ///     bug-report sink (Warning and above) ignores it. Use for expected failures:
    ///     wrong console type, unparsable disc, missing Dokan/WinFsp driver, busy mount
    ///     point, etc.
    /// </summary>
    /// <param name="message">The user-facing error message to log.</param>
    public void LogUserError(string message)
    {
        AppendEntry(message, true);
        Serilog.Log.Information("User error (no bug report): {Message}", message);
    }

    private void AppendEntry(string message, bool isError)
    {
        var dispatcher = _dispatcher ?? Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
            DoAppend(message, isError);
        else
            _ = dispatcher.BeginInvoke(() => DoAppend(message, isError));
    }

    private void DoAppend(string message, bool isError)
    {
        lock (_dedupLock)
        {
            if (string.Equals(message, _lastMessage, StringComparison.Ordinal) &&
                (DateTime.Now - _lastMessageTime).TotalMilliseconds < 100)
                return;

            _lastMessage = message;
            _lastMessageTime = DateTime.Now;

            LogEntries.Add(new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = message,
                IsError = isError
            });

            if (LogEntries.Count > MaxEntries)
            {
                var excess = LogEntries.Count - MaxEntries;
                for (var i = 0; i < excess; i++)
                    LogEntries.RemoveAt(0);
            }
        }
    }
}