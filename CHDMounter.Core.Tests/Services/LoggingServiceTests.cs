namespace CHDMounter.Core.Tests.Services;

public class LoggingServiceTests
{
    [Fact]
    public void LogAddsEntry()
    {
        var service = new LoggingService();
        service.Log("test message");
        Assert.Single(service.LogEntries);
        Assert.Equal("test message", service.LogEntries[0].Message);
        Assert.False(service.LogEntries[0].IsError);
    }

    [Fact]
    public void LogErrorAddsErrorEntry()
    {
        var service = new LoggingService();
        service.LogError("error message");
        Assert.Single(service.LogEntries);
        Assert.Equal("error message", service.LogEntries[0].Message);
        Assert.True(service.LogEntries[0].IsError);
    }

    [Fact]
    public void LogMultipleMessagesPreservesOrder()
    {
        var service = new LoggingService();
        service.Log("first");
        service.Log("second");
        service.LogError("third");
        Assert.Equal(3, service.LogEntries.Count);
        Assert.Equal("first", service.LogEntries[0].Message);
        Assert.Equal("second", service.LogEntries[1].Message);
        Assert.Equal("third", service.LogEntries[2].Message);
    }
}