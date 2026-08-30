using CHDMounter.Core.Logging;
using Serilog.Events;

namespace CHDMounter.Core.Tests.Logging;

public class BugReportSinkTests
{
    [Fact]
    public void EmitDoesNotThrowForInformationLevel()
    {
        var sink = new BugReportSink();
        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            MessageTemplate.Empty,
            []);

        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitDoesNotThrowForDebugLevel()
    {
        var sink = new BugReportSink();
        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Debug,
            null,
            MessageTemplate.Empty,
            []);

        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitDoesNotThrowForVerboseLevel()
    {
        var sink = new BugReportSink();
        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Verbose,
            null,
            MessageTemplate.Empty,
            []);

        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitDoesNotThrowForWarningLevel()
    {
        var sink = new BugReportSink();
        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Warning,
            null,
            MessageTemplate.Empty,
            []);

        // Warning level will try to send a report (async), but should not throw
        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitDoesNotThrowForErrorLevel()
    {
        var sink = new BugReportSink();
        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Error,
            null,
            MessageTemplate.Empty,
            []);

        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }

    [Fact]
    public void EmitDoesNotThrowForFatalLevel()
    {
        var sink = new BugReportSink();
        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Fatal,
            null,
            MessageTemplate.Empty,
            []);

        var exception = Record.Exception(() => sink.Emit(logEvent));
        Assert.Null(exception);
    }
}