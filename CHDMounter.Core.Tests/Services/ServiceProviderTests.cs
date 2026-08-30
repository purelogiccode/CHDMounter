namespace CHDMounter.Core.Tests.Services;

public class ServiceProviderTests
{
    [Fact]
    public void RegisterThenGetReturnsSameInstance()
    {
        var service = new TestService();
        ServiceProvider.Register<ITestService>(service);
        var resolved = ServiceProvider.Get<ITestService>();
        Assert.Same(service, resolved);
    }

    [Fact]
    public void GetUnregisteredServiceThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(static () => ServiceProvider.Get<INeverRegisteredService>());
    }

    [Fact]
    public void TryGetUnregisteredServiceReturnsNull()
    {
        var result = ServiceProvider.TryGet<INeverRegisteredService>();
        Assert.Null(result);
    }

    [Fact]
    public void TryGetRegisteredServiceReturnsInstance()
    {
        var service = new TestService();
        ServiceProvider.Register<ITestService>(service);
        var resolved = ServiceProvider.TryGet<ITestService>();
        Assert.Same(service, resolved);
    }

    [Fact]
    public void RegisterTwiceOverwritesWithLast()
    {
        var service1 = new TestService();
        var service2 = new TestService();
        ServiceProvider.Register<ITestService>(service1);
        ServiceProvider.Register<ITestService>(service2);
        Assert.Same(service2, ServiceProvider.Get<ITestService>());
    }

    [Fact]
    public void DisposeAllServicesDisposesDisposableServices()
    {
        var disposable = new DisposableTestService();
        ServiceProvider.Register<IDisposableService>(disposable);
        ServiceProvider.DisposeAllServices();
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void DisposeAllServicesNonDisposableServicesDoNotThrow()
    {
        var service = new TestService();
        ServiceProvider.Register<ITestService>(service);
        var exception = Record.Exception(static () => ServiceProvider.DisposeAllServices());
        Assert.Null(exception);
    }

    [Fact]
    public void DisposeAllServicesDisposeExceptionDoesNotThrow()
    {
        var throwing = new ThrowingDisposableService();
        ServiceProvider.Register<IThrowingDisposable>(throwing);
        var exception = Record.Exception(static () => ServiceProvider.DisposeAllServices());
        Assert.Null(exception);
    }

    [Fact]
    public void DisposeAllServicesClearsAllServices()
    {
        var service = new TestService();
        ServiceProvider.Register<ITestService>(service);
        ServiceProvider.DisposeAllServices();
        Assert.Null(ServiceProvider.TryGet<ITestService>());
    }

    private interface ITestService;

    private interface INeverRegisteredService;

    private interface IDisposableService : IDisposable;

    private interface IThrowingDisposable : IDisposable;

    private class TestService : ITestService;

    private class DisposableTestService : IDisposableService
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }
    }

    private class ThrowingDisposableService : IThrowingDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            throw new InvalidOperationException("Test exception");
        }
    }
}