using CHDMounter.Core.Logging;
using Serilog.Events;
using Serilog.Parsing;

namespace CHDMounter.Core.Tests.Logging;

public class BugReportSinkExtendedTests
{
    private static LogEvent CreateLogEvent(LogEventLevel level, Exception? exception = null, string message = "")
    {
        var template = new MessageTemplateParser().Parse(message);
        return new LogEvent(DateTimeOffset.Now, level, exception, template, []);
    }

    [Fact]
    public void EmitBelowWarningDoesNotTriggerAnyReport()
    {
        var sink = new BugReportSink();

        // Information level should be below Warning threshold
        var logEvent = CreateLogEvent(LogEventLevel.Information, message: "info message");

        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitDebugLevelDoesNotTriggerReport()
    {
        var sink = new BugReportSink();
        var logEvent = CreateLogEvent(LogEventLevel.Debug, message: "debug message");

        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitVerboseLevelDoesNotTriggerReport()
    {
        var sink = new BugReportSink();
        var logEvent = CreateLogEvent(LogEventLevel.Verbose, message: "verbose message");

        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitWarningLevelTriggersSendWarning()
    {
        var sink = new BugReportSink();
        var logEvent = CreateLogEvent(LogEventLevel.Warning, message: "warning message");

        // Should not throw; async task will try to send warning
        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitErrorLevelWithoutExceptionTriggersSendError()
    {
        var sink = new BugReportSink();
        var logEvent = CreateLogEvent(LogEventLevel.Error, message: "error message");

        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitFatalLevelWithoutExceptionTriggersSendError()
    {
        var sink = new BugReportSink();
        var logEvent = CreateLogEvent(LogEventLevel.Fatal, message: "fatal message");

        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitWarningWithExceptionTriggersSendException()
    {
        var sink = new BugReportSink();
        var ex = new InvalidOperationException("test exception");
        var logEvent = CreateLogEvent(LogEventLevel.Warning, ex, "warning with exception");

        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitErrorWithExceptionTriggersSendException()
    {
        var sink = new BugReportSink();
        var ex = new ArgumentException("arg error");
        var logEvent = CreateLogEvent(LogEventLevel.Error, ex, "error with exception");

        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitFatalWithExceptionTriggersSendException()
    {
        var sink = new BugReportSink();
        var ex = new OutOfMemoryException("oom");
        var logEvent = CreateLogEvent(LogEventLevel.Fatal, ex, "fatal with exception");

        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitWarningWithEmptyMessageDoesNotThrow()
    {
        var sink = new BugReportSink();
        var logEvent = CreateLogEvent(LogEventLevel.Warning, message: "");

        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitErrorWithFormattedMessageTemplateDoesNotThrow()
    {
        var sink = new BugReportSink();
        var template = new MessageTemplateParser().Parse("Error in {Operation} with value {Value}");
        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Error,
            null,
            template,
            [
                new LogEventProperty("Operation", new ScalarValue("TestOp")),
                new LogEventProperty("Value", new ScalarValue(42))
            ]);

        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitMultipleEventsSequentiallyDoesNotThrow()
    {
        var sink = new BugReportSink();

        for (var i = 0; i < 5; i++)
        {
            var logEvent = CreateLogEvent(LogEventLevel.Warning, message: $"warning {i}");
            sink.Emit(logEvent);
        }

        // No assertion needed - just verifying no exceptions
    }
}