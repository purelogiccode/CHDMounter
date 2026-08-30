using Serilog;

namespace CHDMounter.Core.Services;

/// <summary>
///     A simple static service locator for registering and resolving application services.
/// </summary>
public static class ServiceProvider
{
    private static readonly ConcurrentDictionary<Type, object> Services = new();

    /// <summary>
    ///     Registers a service implementation for the specified type.
    /// </summary>
    /// <typeparam name="T">The service type to register.</typeparam>
    /// <param name="implementation">The implementation instance.</param>
    public static void Register<T>(T implementation) where T : notnull
    {
        Services[typeof(T)] = implementation;
    }

    /// <summary>
    ///     Gets the registered service implementation for the specified type.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <returns>The registered service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the service is not registered.</exception>
    public static T Get<T>() where T : notnull
    {
        if (Services.TryGetValue(typeof(T), out var service))
            return (T)service;

        throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    /// <summary>
    ///     Attempts to get the registered service implementation, returning <c>null</c> if not found.
    /// </summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    /// <returns>The registered service instance, or <c>null</c> if not registered.</returns>
    public static T TryGet<T>() where T : class
    {
        if (Services.TryGetValue(typeof(T), out var service))
            return (T)service;

        return null!;
    }

    /// <summary>
    ///     Disposes all registered services that implement <see cref="IDisposable" /> and clears the registry.
    /// </summary>
    public static void DisposeAllServices()
    {
        foreach (var kvp in Services)
            if (kvp.Value is IDisposable disposable)
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "ServiceProvider: Failed to dispose {ServiceType}", kvp.Key.Name);
                }

        Services.Clear();
    }
}