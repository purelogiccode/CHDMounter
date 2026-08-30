using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows;
using CHDMounter.Core.Interfaces;
using Fsp;
using VideoGameFileSystemParser.Parsers;

#pragma warning disable CA1707
namespace CHDMounter_WinFsp.Services;

/// <summary>
///     Mounts and unmounts CHD disc images as virtual drives using the WinFsp file system driver.
///     Supports cross-integrity mounts when running as Administrator.
/// </summary>
internal class MountService : IMountService
{
    private readonly ILoggingService _loggingService;
    private readonly Lock _mountLock = new();
    private ChdContainer? _container;
    private ChdFs? _currentFs;
    private FileSystemHost? _host;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MountService" /> class.
    /// </summary>
    /// <param name="loggingService">The logging service for recording mount operations.</param>
    internal MountService(ILoggingService loggingService)
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
                _loggingService.LogError($"WinFsp not available: {winFspReason ?? "unknown reason"}");
                ShowWinFspNotInstalledDialog();
                return;
            }

            _loggingService.Log($"Opening and parsing CHD: {chdPath} as {consoleType} (WinFsp)...");

            try
            {
                _container = new ChdContainer(chdPath);
                if (!_container.MountAndParse(consoleType))
                {
                    _loggingService.LogError(
                        $"Failed to open or parse CHD as {consoleType}: {_container.LastError ?? "unknown reason"}.");
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
                        candidates = GetCrossIntegrityMountCandidates(chdPath);
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
                    if (crossIntegrity && IsDriveLetterMountPoint(mountPoint))
                    {
                        _loggingService.Log(
                            "Cross-integrity mode: Drive letter mounts are not supported. Redirecting to folder mount.");
                        candidates = GetCrossIntegrityMountCandidates(chdPath);
                    }
                    else if (IsDriveLetterMountPoint(mountPoint))
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
                var isDriveLetter = IsDriveLetterMountPoint(candidates[0]);
                var persistentAcls = crossIntegrity && !isDriveLetter;
                _currentFs = new ChdFs(_container, persistentAcls);

                Exception? lastError = null;
                foreach (var candidate in candidates)
                {
                    MountPoint = candidate;
                    _loggingService.Log($"Mounting at {MountPoint} (WinFsp)...");

                    // Folder mounts need the target directory to exist before
                    // WinFsp can mount at it.
                    if (!IsDriveLetterMountPoint(MountPoint))
                        Directory.CreateDirectory(MountPoint);

                    try
                    {
                        _host = new FileSystemHost(_currentFs);

                        var securityDescriptor = persistentAcls ? CreateCrossIntegritySecurityDescriptor() : null;
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
                    catch (Exception ex) when (candidates.Count > 1)
                    {
                        lastError = ex;
                        _host?.Dispose();
                        _host = null;
                        _loggingService.Log($"Mount point {MountPoint} is unavailable ({ex.Message}). Trying next...");
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
                    _currentFs?.Dispose();
                    _currentFs = null;
                    if (_container is not null)
                    {
                        _container.Dispose();
                        _container = null;
                    }

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
                    _loggingService.LogError($"Error: {ex.Message}");
                }

            _host?.Dispose();
            _host = null;
            _currentFs?.Dispose();
            _currentFs = null;
            _container?.Dispose();
            _container = null;
            IsMounted = false;
            MountPoint = "";
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Unmount();
        GC.SuppressFinalize(this);
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

    private static byte[] CreateCrossIntegritySecurityDescriptor()
    {
        const string sddl = "D:P(A;;FA;;;WD)";
        var sd = new RawSecurityDescriptor(sddl);
        var bytes = new byte[sd.BinaryLength];
        sd.GetBinaryForm(bytes, 0);
        return bytes;
    }

    private static List<string> GetCrossIntegrityMountCandidates(string chdPath)
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CHDMounter", "Mounts");
        var folderName = Path.GetFileNameWithoutExtension(chdPath);

        // The base folder may already be an active mount point (e.g. leftover
        // from a crashed session, or the same game mounted twice), which makes
        // WinFsp fail with STATUS_OBJECT_NAME_COLLISION (0xC0000035). Provide
        // suffixed fallback folders so the mount can still succeed.
        var candidates = new List<string>();
        for (var i = 1; i <= 5; i++)
        {
            var name = i == 1 ? folderName : $"{folderName} ({i})";
            candidates.Add(Path.Combine(baseDir, SanitizeFolderName(name)));
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

    private static bool IsDriveLetterMountPoint(string mountPoint)
    {
        return mountPoint is [_, ':', ..] && char.IsLetter(mountPoint[0])
                                          && (mountPoint.Length == 2 || mountPoint is [_, _, '\\']);
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

        if (isTypeInitFailure || innermost is DllNotFoundException or BadImageFormatException)
            return new InvalidOperationException(
                "The WinFsp native library (winfsp-x64.dll / winfsp-x86.dll) could not be loaded. " +
                "Install or repair WinFsp, then restart this application.",
                lastError);

        return lastError;
    }

    private static void ShowWinFspNotInstalledDialog()
    {
        const string message = "The WinFsp file system driver is required to mount CHD files as virtual drives. " +
                               "It does not appear to be installed on this system.\n\n" +
                               "Would you like to open the WinFsp download page?";

        var result = MessageBox.Show(message, "WinFsp Not Found",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/winfsp/winfsp/releases",
                UseShellExecute = true
            });
    }
}