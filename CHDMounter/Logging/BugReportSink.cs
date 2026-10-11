using System.Globalization;
using Serilog.Core;
using Serilog.Events;

namespace CHDMounter.Core.Logging;

/// <summary>
///     A Serilog sink that forwards warning-level and above log events to the bug report client.
/// </summary>
public class BugReportSink : ILogEventSink
{
    /// <summary>
    ///     Processes a log event, forwarding warnings and errors to the remote bug report API.
    /// </summary>
    /// <param name="logEvent">The log event to process.</param>
    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < LogEventLevel.Warning)
            return;

        if (logEvent.Exception is not null)
        {
            var context = logEvent.RenderMessage(CultureInfo.InvariantCulture);
            _ = Task.Run(() => BugReportClient.SendException(logEvent.Exception, context));
        }
        else if (logEvent.Level >= LogEventLevel.Error)
        {
            var message = logEvent.RenderMessage(CultureInfo.InvariantCulture);
            _ = Task.Run(() => BugReportClient.SendError(message, null));
        }
        else
        {
            var message = logEvent.RenderMessage(CultureInfo.InvariantCulture);
            _ = Task.Run(() => BugReportClient.SendWarning(message));
        }
    }
}