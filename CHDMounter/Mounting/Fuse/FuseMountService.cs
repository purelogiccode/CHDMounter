using CHDMounter.FuseSharp;
using CHDMounter.Interfaces;
using CHDMounter.Services;
using CHDMounter.Views;
using Serilog;
using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Mounting.Fuse;

/// <summary>
///     Mounts and unmounts CHD disc images as virtual drives using FUSE 3
///     (libfuse3 on Linux, macFUSE on macOS).
/// </summary>
internal sealed class FuseMountService : IMountService
{
    private readonly ILoggingService _loggingService;
    private readonly Lock _mountLock = new();
    private ChdContainer? _container;
    private FuseFileSystem? _fileSystem;
    private Thread? _mountThread;
    private TaskCompletionSource<bool>? _mountedSource;
    private volatile bool _isMounted;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FuseMountService" /> class.
    /// </summary>
    /// <param name="loggingService">The logging service for recording mount operations.</param>
    public FuseMountService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    /// <inheritdoc />
    public bool IsMounted => _isMounted;

    /// <inheritdoc />
    public string MountPoint { get; private set; } = "";

    /// <inheritdoc />
    public bool CanMount()
    {
        return FuseAvailability.Check(out _);
    }

    /// <inheritdoc />
    public void Mount(string chdPath, string? mountPoint, ConsoleType consoleType)
    {
        lock (_mountLock)
        {
            if (_isMounted)
                throw new InvalidOperationException("Already mounted.");

            if (!FuseAvailability.Check(out _))
            {
                // Missing FUSE library is an environment issue, not an app bug: no bug report.
                _loggingService.LogUserError(
                    "FUSE 3 library not found. Install libfuse3 (Linux) or macFUSE (macOS) to mount CHD files.");
                ShowFuseNotInstalledDialog();
                return;
            }

            _loggingService.Log($"Opening and parsing CHD: {chdPath} as {consoleType} (FUSE)...");

            try
            {
                _container = new ChdContainer(chdPath);
                if (!_container.MountAndParse(consoleType))
                {
                    // A parse failure usually means the wrong console type was selected for this
                    // disc. This is expected user error, not an app bug: log without filing a bug report.
                    _loggingService.LogUserError(
                        $"Failed to open or parse CHD as {consoleType}: {_container.LastError ?? "unknown reason"}. " +
                        "Try a different console type.");
                    return;
                }

                _loggingService.Log($"Parsing complete. Volume: {_container.VolumeName}");

                var creationTime = GetCreationTime(chdPath);
                var candidates = ResolveMountCandidates(chdPath, mountPoint);
                Exception? lastError = null;

                foreach (var candidate in candidates)
                {
                    MountPoint = candidate;
                    _loggingService.Log($"Mounting at {MountPoint} (FUSE)...");

                    try
                    {
                        // FUSE requires the mount point directory to exist before mounting.
                        Directory.CreateDirectory(MountPoint);
                        StartMount(MountPoint, creationTime);
                        _loggingService.Log($"Mounted at {MountPoint} (FUSE).");
                        return;
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        StopMount(disposeContainer: false);
                        _loggingService.Log($"Mount point {MountPoint} is unavailable ({ex.Message}). Trying next...");
                    }
                }

                throw lastError ?? new InvalidOperationException("No available mount point found.");
            }
            finally
            {
                if (!_isMounted)
                {
                    StopMount(disposeContainer: true);
                    MountPoint = "";
                }
            }
        }
    }

    /// <inheritdoc />
    public void Unmount()
    {
        lock (_mountLock)
        {
            if (!_isMounted) return;

            _loggingService.Log($"Unmounting {MountPoint} (FUSE)...");
            try
            {
                StopMount(disposeContainer: true);
            }
            catch (Exception ex)
            {
                // Unmount races (already unmounted externally) are environmental, not app bugs.
                _loggingService.LogUserError($"Unmount failed: {ex.Message}");
            }

            MountPoint = "";
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Unmount();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    ///     Starts the blocking FUSE session on a dedicated background thread and waits until
    ///     the FUSE init callback reports the mount as live (or the thread exits with failure).
    /// </summary>
    /// <param name="mountPoint">The existing directory to mount on.</param>
    /// <param name="creationTime">The volume creation time.</param>
    private void StartMount(string mountPoint, DateTime creationTime)
    {
        var volume = new ChdFuseVolume(_container!, creationTime);
        var fileSystem = new FuseFileSystem(volume);
        var mountedSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _fileSystem = fileSystem;
        _mountedSource = mountedSource;
        FileSystemExitCode = -1;

        var thread = new Thread(() =>
        {
            try
            {
                FileSystemExitCode = fileSystem.Run(mountPoint, debug: false, () =>
                {
                    _isMounted = true;
                    mountedSource.TrySetResult(true);
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError("FUSE mount thread failed", ex);
            }
            finally
            {
                _isMounted = false;
                mountedSource.TrySetResult(false);
            }
        })
        {
            IsBackground = true,
            Name = "CHDMounter FUSE"
        };

        _mountThread = thread;
        thread.Start();

        // The session blocks until unmounted, so a signal/timeout is the only completion
        // the caller can observe while mounting.
        if (!mountedSource.Task.Wait(TimeSpan.FromSeconds(30)))
        {
            fileSystem.Exit();
            throw new TimeoutException($"Timed out mounting the FUSE volume at {mountPoint}.");
        }

        if (!_isMounted)
            throw new IOException($"FUSE mount failed at {mountPoint} (exit code {FileSystemExitCode}).");
    }

    private int FileSystemExitCode { get; set; } = -1;

    /// <summary>
    ///     Requests the FUSE session to exit and releases the mount resources.
    /// </summary>
    /// <param name="disposeContainer">
    ///     When <c>true</c>, the parsed CHD container is disposed as well. Retried mount
    ///     candidates keep the container alive.
    /// </param>
    private void StopMount(bool disposeContainer)
    {
        try
        {
            _fileSystem?.Exit();
        }
        catch (Exception ex)
        {
            _loggingService.LogUserError($"FUSE session exit failed: {ex.Message}");
        }

        _mountThread?.Join(TimeSpan.FromSeconds(10));
        _mountThread = null;
        _fileSystem = null;
        _mountedSource = null;
        _isMounted = false;

        if (disposeContainer)
        {
            _container?.Dispose();
            _container = null;
        }
    }

    /// <summary>
    ///     Picks the directory to mount on: the explicit mount point when given, otherwise a
    ///     folder derived from the CHD file name (with suffixed fallbacks for stale mounts).
    /// </summary>
    /// <param name="chdPath">The CHD file path.</param>
    /// <param name="mountPoint">The user-requested mount point, or <c>null</c> to auto-assign.</param>
    /// <returns>The candidate mount point directories, in priority order.</returns>
    private static List<string> ResolveMountCandidates(string chdPath, string? mountPoint)
    {
        if (!string.IsNullOrEmpty(mountPoint))
            return [mountPoint];

        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CHDMounter", "Mounts");
        var folderName = Path.GetFileNameWithoutExtension(chdPath);
        if (string.IsNullOrEmpty(folderName)) folderName = "CHD";
        folderName = SanitizeFolderName(folderName);

        var candidates = new List<string>();
        for (var i = 1; i <= 5; i++)
        {
            var name = i == 1 ? folderName : $"{folderName} ({i})";
            candidates.Add(Path.Combine(baseDir, name));
        }

        return candidates;
    }

    private static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            if (invalid.Contains(chars[i]))
                chars[i] = '_';

        return new string(chars);
    }

    private static DateTime GetCreationTime(string chdPath)
    {
        try
        {
            return File.GetLastWriteTimeUtc(chdPath);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "FuseMountService: Failed to read creation time for '{ChdPath}'", chdPath);
            return DateTime.UtcNow;
        }
    }

    private static void ShowFuseNotInstalledDialog()
    {
        var isMac = OperatingSystem.IsMacOS();
        var message = isMac
            ? "macFUSE (libfuse3) is required to mount CHD files as virtual drives. " +
              "It does not appear to be installed on this system.\n\n" +
              "Would you like to open the macFUSE download page?"
            : "The FUSE 3 library (libfuse3) is required to mount CHD files as virtual drives. " +
              "It does not appear to be installed on this system.\n\n" +
              "Would you like to open the FUSE project page for installation instructions?";

        if (Dialogs.Confirm("FUSE Library Not Found", message))
            PlatformHelper.OpenUrl(isMac ? "https://macfuse.io" : "https://github.com/libfuse/libfuse");
    }
}
