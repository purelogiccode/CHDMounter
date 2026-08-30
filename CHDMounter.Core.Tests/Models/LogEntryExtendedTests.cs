namespace CHDMounter.Core.Tests.Models;

public class LogEntryExtendedTests
{
    [Fact]
    public void LogEntryTimestampIsSetToRecentTime()
    {
        var before = DateTime.Now;
        var entry = new LogEntry();
        var after = DateTime.Now;

        Assert.True(entry.Timestamp >= before && entry.Timestamp <= after);
    }

    [Fact]
    public void LogEntryMessageDefaultsToEmpty()
    {
        var entry = new LogEntry();
        Assert.Equal("", entry.Message);
    }

    [Fact]
    public void LogEntryIsErrorDefaultsToFalse()
    {
        var entry = new LogEntry();
        Assert.False(entry.IsError);
    }

    [Fact]
    public void LogEntryCanSetMessage()
    {
        var entry = new LogEntry { Message = "test message" };
        Assert.Equal("test message", entry.Message);
    }

    [Fact]
    public void LogEntryCanSetIsError()
    {
        var entry = new LogEntry { IsError = true };
        Assert.True(entry.IsError);
    }

    [Fact]
    public void LogEntryCanSetTimestamp()
    {
        var customTime = new DateTime(2020, 1, 1, 12, 0, 0);
        var entry = new LogEntry { Timestamp = customTime };
        Assert.Equal(customTime, entry.Timestamp);
    }

    [Fact]
    public void LogEntryCanSetAllPropertiesTogether()
    {
        var time = DateTime.Now;
        var entry = new LogEntry
        {
            Timestamp = time,
            Message = "error occurred",
            IsError = true
        };

        Assert.Equal(time, entry.Timestamp);
        Assert.Equal("error occurred", entry.Message);
        Assert.True(entry.IsError);
    }

    [Fact]
    public void LogEntryWithSpecialCharactersInMessage()
    {
        var entry = new LogEntry { Message = "line1\nline2\ttab\0null" };
        Assert.Equal("line1\nline2\ttab\0null", entry.Message);
    }

    [Fact]
    public void LogEntryWithUnicodeMessage()
    {
        var entry = new LogEntry { Message = "\u00e9\u00e8\u00ea \u4e16\u754c" };
        Assert.Equal("\u00e9\u00e8\u00ea \u4e16\u754c", entry.Message);
    }

    [Fact]
    public void LogEntryWithEmptyMessage()
    {
        var entry = new LogEntry { Message = "" };
        Assert.Equal("", entry.Message);
    }

    [Fact]
    public void LogEntryWithLongMessage()
    {
        var longMessage = new string('x', 10000);
        var entry = new LogEntry { Message = longMessage };
        Assert.Equal(10000, entry.Message.Length);
    }
}