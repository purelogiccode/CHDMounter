using System.Collections.Concurrent;

namespace CHDMounter.Core.Tests.Services;

public class LoggingServiceThreadSafetyTests
{
    [Fact]
    public void LogFromMultipleThreadsDoesNotThrow()
    {
        var service = new LoggingService();
        var exceptions = new ConcurrentBag<Exception>();

        Parallel.For(0, 100, i =>
        {
            try
            {
                service.Log($"Message {i}");
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
        Assert.True(service.LogEntries.Count > 0);
    }

    [Fact]
    public void LogErrorFromMultipleThreadsDoesNotThrow()
    {
        var service = new LoggingService();
        var exceptions = new ConcurrentBag<Exception>();

        Parallel.For(0, 100, i =>
        {
            try
            {
                service.LogError($"Error {i}");
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
        Assert.True(service.LogEntries.Count > 0);
    }

    [Fact]
    public void MixedLogAndLogErrorFromMultipleThreadsDoesNotThrow()
    {
        var service = new LoggingService();
        var exceptions = new ConcurrentBag<Exception>();

        Parallel.For(0, 100, i =>
        {
            try
            {
                if (i % 2 == 0)
                    service.Log($"Info {i}");
                else
                    service.LogError($"Error {i}");
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
        Assert.True(service.LogEntries.Count > 0);
    }

    [Fact]
    public void DuplicateMessagesWithin100MsAreSuppressed()
    {
        var service = new LoggingService();
        service.Log("duplicate");
        service.Log("duplicate");
        service.Log("duplicate");

        Assert.Single(service.LogEntries);
    }

    [Fact]
    public void DuplicateMessagesAfterDelayAreNotSuppressed()
    {
        var service = new LoggingService();
        service.Log("message");
        Thread.Sleep(150);
        service.Log("message");

        Assert.Equal(2, service.LogEntries.Count);
    }

    [Fact]
    public void DifferentMessagesAreNotSuppressed()
    {
        var service = new LoggingService();
        service.Log("first");
        service.Log("second");
        service.Log("third");

        Assert.Equal(3, service.LogEntries.Count);
    }

    [Fact]
    public void MaxEntriesIsRespected()
    {
        var service = new LoggingService();
        for (var i = 0; i < 5100; i++) service.Log($"Message {i}");

        Assert.True(service.LogEntries.Count <= 5000);
    }
}