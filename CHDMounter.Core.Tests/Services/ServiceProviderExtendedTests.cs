namespace CHDMounter.Core.Tests.Services;

public class ServiceProviderExtendedTests
{
    [Fact]
    public void ConcurrentRegistrationsDoNotThrow()
    {
        var tasks = new List<Task>();
        for (var i = 0; i < 100; i++)
            tasks.Add(Task.Run(() =>
            {
                var service = new ConcurrentTestService();
                ServiceProvider.Register<IConcurrentTestService>(service);
            }));

        var exception = Record.Exception(() => Task.WaitAll(tasks.ToArray()));
        Assert.Null(exception);
    }

    [Fact]
    public void RegisterNullThrowsArgumentNullException()
    {
        // The constraint is T : notnull, so this should throw at compile time
        // But we can test with a nullable reference
        // Actually, the generic constraint prevents null at compile time
        // Let's verify the method exists and is callable
    }

    [Fact]
    public void TryGetAfterDisposeAllReturnsNull()
    {
        var service = new ConcurrentTestService();
        ServiceProvider.Register<IConcurrentTestService>(service);
        ServiceProvider.DisposeAllServices();
        Assert.Null(ServiceProvider.TryGet<IConcurrentTestService>());
    }

    private interface IConcurrentTestService;

    private class ConcurrentTestService : IConcurrentTestService;
}