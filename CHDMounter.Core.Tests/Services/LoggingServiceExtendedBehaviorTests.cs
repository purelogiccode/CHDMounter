using System.Collections.ObjectModel;

namespace CHDMounter.Core.Tests.Services;

public class LoggingServiceExtendedBehaviorTests
{
    [Fact]
    public void LogEntriesIsObservableCollection()
    {
        var service = new LoggingService();
        Assert.IsAssignableFrom<ObservableCollection<LogEntry>>(service.LogEntries);
    }

    [Fact]
    public void LogWithVeryLongMessageDoesNotThrow()
    {
        var service = new LoggingService();
        var longMessage = new string('x', 10000);
        var exception = Record.Exception(() => service.Log(longMessage));
        Assert.Null(exception);
    }

    [Fact]
    public void LogErrorWithVeryLongMessageDoesNotThrow()
    {
        var service = new LoggingService();
        var longMessage = new string('y', 10000);
        var exception = Record.Exception(() => service.LogError(longMessage));
        Assert.Null(exception);
    }

    [Fact]
    public void LogWithSpecialCharactersDoesNotThrow()
    {
        var service = new LoggingService();
        var exception = Record.Exception(() => service.Log("line1\nline2\ttab\0null\r\n"));
        Assert.Null(exception);
    }

    [Fact]
    public void LogErrorWithSpecialCharactersDoesNotThrow()
    {
        var service = new LoggingService();
        var exception = Record.Exception(() => service.LogError("error\nwith\nnewlines"));
        Assert.Null(exception);
    }

    [Fact]
    public void LogWithUnicodeCharactersDoesNotThrow()
    {
        var service = new LoggingService();
        var exception = Record.Exception(() => service.Log("\u00e9\u00e8\u00ea \u4e16\u754c \u2603"));
        Assert.Null(exception);
    }

    [Fact]
    public void LogAndLogErrorInterleaveCorrectly()
    {
        var service = new LoggingService();
        service.Log("info1");
        service.LogError("error1");
        service.Log("info2");
        service.LogError("error2");

        Assert.Equal(4, service.LogEntries.Count);
        Assert.False(service.LogEntries[0].IsError);
        Assert.True(service.LogEntries[1].IsError);
        Assert.False(service.LogEntries[2].IsError);
        Assert.True(service.LogEntries[3].IsError);
    }

    [Fact]
    public void LogEntriesPreservesMessageContent()
    {
        var service = new LoggingService();
        service.Log("info message");
        service.LogError("error message");

        Assert.Equal("info message", service.LogEntries[0].Message);
        Assert.Equal("error message", service.LogEntries[1].Message);
    }

    [Fact]
    public void LogEntriesTimestampIsRecent()
    {
        var before = DateTime.Now;
        var service = new LoggingService();
        service.Log("test");
        var after = DateTime.Now;

        Assert.True(service.LogEntries[0].Timestamp >= before);
        Assert.True(service.LogEntries[0].Timestamp <= after);
    }

    [Fact]
    public void LogEntriesCountIsCorrectAfterMultipleLogs()
    {
        var service = new LoggingService();
        for (var i = 0; i < 50; i++) service.Log($"message {i}");

        Assert.Equal(50, service.LogEntries.Count);
    }

    [Fact]
    public void LogEntriesMaxEntriesIsEnforced()
    {
        var service = new LoggingService();
        // Log more than 5000 entries with unique messages to bypass dedup
        for (var i = 0; i < 5100; i++)
        {
            Thread.Sleep(1); // Ensure each message is unique by timing
            service.Log($"unique message {i}");
        }

        Assert.True(service.LogEntries.Count <= 5000);
    }

    [Fact]
    public void LogDuplicateMessageAfter100MsIsNotDeduplicated()
    {
        var service = new LoggingService();
        service.Log("duplicate");
        Thread.Sleep(150);
        service.Log("duplicate");

        Assert.Equal(2, service.LogEntries.Count);
    }

    [Fact]
    public void LogEntriesAreOrderedByTime()
    {
        var service = new LoggingService();
        service.Log("first");
        Thread.Sleep(10);
        service.Log("second");
        Thread.Sleep(10);
        service.Log("third");

        Assert.True(service.LogEntries[0].Timestamp <= service.LogEntries[1].Timestamp);
        Assert.True(service.LogEntries[1].Timestamp <= service.LogEntries[2].Timestamp);
    }
}