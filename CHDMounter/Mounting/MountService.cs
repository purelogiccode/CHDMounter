using CHDMounter.Interfaces;
using CHDMounter.Models;
using CHDMounter.Mounting.Dokan;
using CHDMounter.Mounting.Fuse;
using CHDMounter.Mounting.WinFsp;

namespace CHDMounter.Mounting;

/// <summary>
///     Routes mount operations to the backend selected in the application settings
///     (Dokan or WinFsp on Windows, FUSE on Linux and macOS). Backend instances are
///     created lazily and kept so a backend can be switched between mounts without
///     restarting the application.
/// </summary>
internal sealed class MountService : IMountService
{
    private readonly ILoggingService _loggingService;
    private readonly ISettingsService _settingsService;
    private readonly Lock _mountLock = new();
    private readonly List<IMountService> _backends = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="MountService" /> class.
    /// </summary>
    /// <param name="loggingService">The logging service for recording mount operations.</param>
    /// <param name="settingsService">The settings service providing the selected mount backend.</param>
    public MountService(ILoggingService loggingService, ISettingsService settingsService)
    {
        _loggingService = loggingService;
        _settingsService = settingsService;
    }

    /// <inheritdoc />
    public bool IsMounted
    {
        get
        {
            lock (_mountLock)
            {
                return _backends.Any(static backend => backend.IsMounted);
            }
        }
    }

    /// <inheritdoc />
    public string MountPoint
    {
        get
        {
            lock (_mountLock)
            {
                return _backends.FirstOrDefault(static backend => backend.IsMounted)?.MountPoint ?? "";
            }
        }
    }

    /// <inheritdoc />
    public bool CanMount()
    {
        lock (_mountLock)
        {
            return GetBackend(SelectedBackend).CanMount();
        }
    }

    /// <inheritdoc />
    public void Mount(string chdPath, string? mountPoint, ConsoleType consoleType)
    {
        lock (_mountLock)
        {
            if (_backends.Any(static backend => backend.IsMounted))
                throw new InvalidOperationException("Already mounted.");

            var backend = GetBackend(SelectedBackend);
            _loggingService.Log($"Mount core: {SelectedBackend}.");
            backend.Mount(chdPath, mountPoint, consoleType);
        }
    }

    /// <inheritdoc />
    public void Unmount()
    {
        lock (_mountLock)
        {
            foreach (var backend in _backends)
                if (backend.IsMounted)
                    backend.Unmount();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_mountLock)
        {
            foreach (var backend in _backends)
                try
                {
                    backend.Dispose();
                }
                catch (Exception ex)
                {
                    _loggingService.LogUserError($"Failed to release the mount backend: {ex.Message}");
                }

            _backends.Clear();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    ///     Gets the backend selected in the settings, restricted to the backends the
    ///     current operating system supports.
    /// </summary>
    private MountBackend SelectedBackend
    {
        get
        {
            var configured = _settingsService.Settings.MountBackend;
            if (OperatingSystem.IsWindows())
                return configured == MountBackend.Fuse ? MountBackend.Dokan : configured;

            return MountBackend.Fuse;
        }
    }

    private IMountService GetBackend(MountBackend backend)
    {
        if (OperatingSystem.IsWindows())
        {
            if (backend == MountBackend.WinFsp)
            {
                var winFsp = _backends.OfType<WinFspMountService>().FirstOrDefault();
                if (winFsp is null)
                {
                    winFsp = new WinFspMountService(_loggingService);
                    _backends.Add(winFsp);
                }

                return winFsp;
            }

            if (backend == MountBackend.Dokan)
            {
                var dokan = _backends.OfType<DokanMountService>().FirstOrDefault();
                if (dokan is null)
                {
                    dokan = new DokanMountService(_loggingService);
                    _backends.Add(dokan);
                }

                return dokan;
            }
        }

        var fuse = _backends.OfType<FuseMountService>().FirstOrDefault();
        if (fuse is null)
        {
            fuse = new FuseMountService(_loggingService);
            _backends.Add(fuse);
        }

        return fuse;
    }
}
