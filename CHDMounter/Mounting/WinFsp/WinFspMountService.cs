using System.Runtime.Versioning;
using System.Security.Principal;
using CHDMounter.Interfaces;
using CHDMounter.Services;
using CHDMounter.Views;
using Fsp;
using VideoGameFileSystemParser.Parsers;

#pragma warning disable CA1707
namespace CHDMounter.Mounting.WinFsp;

/// <summary>
///     Mounts and unmounts CHD disc images as virtual drives using the WinFsp file system driver.
///     Supports cross-integrity mounts when running as Administrator.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WinFspMountService : IMountService
{
    private readonly ILoggingService _loggingService;
    private readonly Lock _mountLock = new();
    private ChdContainer? _container;
    private ChdFs? _currentFs;
    private FileSystemHost? _host;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WinFspMountService" /> class.
    /// </summary>
    /// <param name="loggingService">The logging service for recording mount operations.</param>
    internal WinFspMountService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    /// <inheritdoc />
    public bool IsMounted { get; private set; }

    /// <inheritdoc />
    public string MountPoint { get; private set; } = "";

    /// <inheritdoc />
    public bool CanMount()
    {
        return IsWinFspInstalled();
    }

    /// <inheritdoc />
    public void Mount(string chdPath, string? mountPoint, ConsoleType consoleType)
    {
        lock (_mountLock)
        {
            if (IsMounted) throw new InvalidOperationException("Already mounted.");

            if (!IsWinFspInstalled(out var winFspReason))
            {
                // Missing driver is an environment issue, not an app bug: log without filing a bug report.
                _loggingService.LogUserError($"WinFsp not available: {winFspReason ?? "unknown reason"}");
                ShowWinFspNotInstalledDialog();
                return;
            }

            _loggingService.Log($"Opening and parsing CHD: {chdPath} as {consoleType} (WinFsp)...");

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

                var crossIntegrity = IsRunningAsAdministrator();
                if (crossIntegrity)
                    _loggingService.Log(
                        "Running as Administrator: Cross-integrity mount enforced so standard processes can access the drive.");

                // Per-session or elevated-session drives can be invisible to
                // DriveInfo.GetDrives(), so a picked letter may already be in use.
                // Enumerate candidates and retry on mount failure.
                List<string> candidates;
                if (string.IsNullOrEmpty(mountPoint))
                {
                    if (crossIntegrity)
                    {
                        candidates = MountPathHelper.BuildFolderMountCandidates(chdPath);
                    }
                    else
                    {
                        candidates = DriveHelper.GetAvailableDriveLetters().ToList();
                        if (candidates.Count == 0)
                            throw new InvalidOperationException(
                                "No available drive letter found. All drive letters are in use.");
                    }
                }
                else
                {
                    if (crossIntegrity && MountPathHelper.IsDriveLetterMountPoint(mountPoint))
                    {
                        _loggingService.Log(
                            "Cross-integrity mode: Drive letter mounts are not supported. Redirecting to folder mount.");
                        candidates = MountPathHelper.BuildFolderMountCandidates(chdPath);
                    }
                    else if (MountPathHelper.IsDriveLetterMountPoint(mountPoint))
                    {
                        // Try the explicitly requested letter first; if it is
                        // unavailable (e.g. NTSTATUS 0xC000000E when the device
                        // does not exist), fall back to auto-assigned letters.
                        candidates = [mountPoint, .. DriveHelper.GetAvailableDriveLetters()];
                    }
                    else
                    {
                        candidates = [mountPoint];
                    }
                }

                // ChdFs wraps and owns the container, so create it once before the
                // loop: retried mounts must reuse a live container. In the
                // multi-candidate (auto drive letter) case crossIntegrity is false,
                // so persistentAcls is always false there.
                // NOTE: ChdFs derives from Fsp FileSystemBase, so its construction touches
                // Fsp.Interop.Api's static constructor, which throws TypeInitializationException
                // when winfsp-x64/x86.dll is missing or mismatched. Keep it inside the
                // translated region so users get the friendly "install or repair WinFsp"
                // message instead of a raw "type initializer" crash (previously filed as a bug).
                var isDriveLetter = MountPathHelper.IsDriveLetterMountPoint(candidates[0]);
                var persistentAcls = crossIntegrity && !isDriveLetter;
                try
                {
                    _currentFs = new ChdFs(_container, MountPathHelper.GetVolumeCreationTime(chdPath),
                        persistentAcls);
                }
                catch (Exception ex)
                {
                    throw TranslateMountFailure(ex);
                }

                Exception? lastError = null;
                for (var i = 0; i < candidates.Count; i++)
                {
                    MountPoint = candidates[i];
                    _loggingService.Log($"Mounting at {MountPoint} (WinFsp)...");

                    try
                    {
                        // WinFsp creates the folder mount point itself and removes it on
                        // unmount; it fails with STATUS_OBJECT_NAME_COLLISION if the
                        // directory already exists. An empty leftover directory is deleted,
                        // while a busy or non-empty path fails this candidate so the loop
                        // tries the next one.
                        if (!MountPathHelper.IsDriveLetterMountPoint(MountPoint)
                            && !MountPathHelper.TryPrepareWinFspMountPoint(MountPoint))
                            throw new IOException(
                                $"WinFsp cannot mount at {MountPoint}: the directory already exists " +
                                "and is either an active mount point or not empty.");

                        _host = new FileSystemHost(_currentFs);

                        var securityDescriptor = persistentAcls ? WinFspSecurityDescriptor.Create() : null;
                        if (securityDescriptor is not null)
                            _loggingService.Log("Cross-integrity: using permissive DACL (Everyone Full Access).");

                        // Mount returns an NTSTATUS rather than throwing; a busy or
                        // invalid mount point must be treated as a failed attempt so
                        // the retry loop can try the next candidate.
                        var status = _host.Mount(MountPoint, securityDescriptor, true, unchecked((uint)-1));
                        if (status != 0)
                            throw new IOException($"WinFsp mount failed at {MountPoint} (NTSTATUS 0x{status:X8}).");

                        IsMounted = true;
                        _loggingService.Log($"Mounted at {MountPoint} (WinFsp).");
                        return;
                    }
                    catch (Exception ex)
                    {
                        // Single-candidate (explicit folder) mounts have no fallback:
                        // translate immediately so WinFsp load failures surface as the
                        // friendly "install or repair WinFsp" message. Multi-candidate
                        // mounts collect the error and try the next letter.
                        if (candidates.Count == 1)
                            throw TranslateMountFailure(ex);

                        lastError = ex;
                        _host?.Dispose();
                        _host = null;
                        if (i < candidates.Count - 1)
                            _loggingService.Log(
                                $"Mount point {MountPoint} is unavailable ({ex.Message}). Trying next...");
                    }
                }

                throw lastError is not null
                    ? TranslateMountFailure(lastError)
                    : new InvalidOperationException("No available drive letter found. All drive letters are in use.");
            }
            finally
            {
                if (!IsMounted)
                {
                    _host?.Dispose();
                    _host = null;

                    // ChdFs owns and disposes the container: only dispose the container
                    // directly when the wrapper was never created.
                    if (_currentFs is not null)
                    {
                        _currentFs.Dispose();
                        _currentFs = null;
                    }
                    else
                    {
                        _container?.Dispose();
                    }

                    _container = null;
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
            if (!IsMounted) return;

            _loggingService.Log($"Unmounting {MountPoint} (WinFsp)...");
            if (_host is not null)
                try
                {
                    _host.Unmount();
                }
                catch (Exception ex)
                {
                    // Unmount races (drive already gone, handle closed) are environmental,
                    // not app bugs: log without filing a bug report.
                    _loggingService.LogUserError($"Unmount failed: {ex.Message}");
                }

            _host?.Dispose();
            _host = null;

            // ChdFs owns and disposes the container; only dispose the container directly
            // when the wrapper does not exist.
            if (_currentFs is not null)
            {
                _currentFs.Dispose();
                _currentFs = null;
            }
            else
            {
                _container?.Dispose();
            }

            _container = null;
            IsMounted = false;
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

    private static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static bool IsWinFspInstalled()
    {
        return WinFspEnvironment.EnsureWinFspLoadable(out _);
    }

    private static bool IsWinFspInstalled(out string? reason)
    {
        return WinFspEnvironment.EnsureWinFspLoadable(out reason);
    }

    private static Exception TranslateMountFailure(Exception lastError)
    {
        var innermost = lastError;
        while (innermost.InnerException is not null)
            innermost = innermost.InnerException;

        var isTypeInitFailure = lastError is TypeInitializationException ||
                                (lastError is InvalidOperationException &&
                                 lastError.Message.Contains("type initializer", StringComparison.OrdinalIgnoreCase));

        // EntryPointNotFoundException / TypeLoadException happen when a loadable but
        // too-old WinFsp DLL is used: Fsp.Interop.Api's static constructor resolves
        // all entry points and enforces a version match, then throws the wrapping
        // TypeInitializationException users reported.
        if (isTypeInitFailure || innermost is DllNotFoundException or BadImageFormatException
            or EntryPointNotFoundException or TypeLoadException)
            return new InvalidOperationException(
                "The WinFsp native library (winfsp-a64.dll / winfsp-x64.dll / winfsp-x86.dll) could not be loaded. " +
                "Install or repair WinFsp, then restart this application.",
                lastError);

        return lastError;
    }

    private static void ShowWinFspNotInstalledDialog()
    {
        const string message = "The WinFsp file system driver is required to mount CHD files as virtual drives. " +
                               "It does not appear to be installed, or the installed version cannot be used " +
                               "by this application.\n\n" +
                               "Would you like to open the WinFsp download page?";

        // Shown without blocking the mount worker so no mount lock is held while the
        // dialog waits for the UI thread (prevents a UI-thread Unmount/exit deadlock).
        Dialogs.ConfirmLater("WinFsp Not Found or Outdated", message,
            () => PlatformHelper.OpenUrl("https://github.com/winfsp/winfsp/releases"));
    }
}