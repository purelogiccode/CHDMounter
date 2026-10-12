using Serilog;

namespace CHDMounter.Mounting;

/// <summary>
///     Shared path helpers for the folder-based mount backends (FUSE and WinFsp).
/// </summary>
internal static class MountPathHelper
{
    /// <summary>
    ///     Gets the base directory used for automatic folder mount points
    ///     (%LOCALAPPDATA%\CHDMounter\Mounts).
    /// </summary>
    internal static string MountBaseDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CHDMounter",
        "Mounts");

    /// <summary>
    ///     Determines whether a mount point string is a Windows drive letter
    ///     (for example <c>M:</c> or <c>M:\</c>).
    /// </summary>
    /// <param name="mountPoint">The mount point to inspect.</param>
    /// <returns><c>true</c> when the mount point is a drive letter; otherwise, <c>false</c>.</returns>
    internal static bool IsDriveLetterMountPoint(string mountPoint)
    {
        return mountPoint is [_, ':', ..] && char.IsLetter(mountPoint[0])
                                          && (mountPoint.Length == 2 || mountPoint is [_, _, '\\']);
    }

    /// <summary>
    ///     Builds the folder mount-point candidates for a CHD file: the sanitized file
    ///     name plus suffixed fallbacks for stale or already-active mounts.
    /// </summary>
    /// <param name="chdPath">The CHD file path.</param>
    /// <returns>The candidate mount point directories, in priority order.</returns>
    internal static List<string> BuildFolderMountCandidates(string chdPath)
    {
        var folderName = Path.GetFileNameWithoutExtension(chdPath);
        if (string.IsNullOrEmpty(folderName)) folderName = "CHD";
        folderName = SanitizeFolderName(folderName);

        var candidates = new List<string>();
        for (var i = 1; i <= 5; i++)
        {
            var name = i == 1 ? folderName : $"{folderName} ({i})";
            candidates.Add(Path.Combine(MountBaseDirectory, name));
        }

        return candidates;
    }

    /// <summary>
    ///     Prepares a folder mount point for WinFsp, which requires the directory to
    ///     <em>not</em> exist (WinFsp creates it and removes it on unmount). An empty
    ///     leftover directory is deleted; an active mount point (reparse point) or a
    ///     non-empty directory cannot be used and returns <c>false</c>.
    /// </summary>
    /// <param name="mountPoint">The folder mount point to prepare.</param>
    /// <returns><c>true</c> when WinFsp can mount at the path; otherwise, <c>false</c>.</returns>
    internal static bool TryPrepareWinFspMountPoint(string mountPoint)
    {
        try
        {
            if (!Directory.Exists(mountPoint))
                return true;

            var info = new DirectoryInfo(mountPoint);

            // An active WinFsp/FUSE mount shows up as a reparse point; never delete it.
            if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                return false;

            // WinFsp cannot mount over a non-empty directory (NTFS forbids the reparse point).
            if (info.EnumerateFileSystemInfos().Any())
                return false;

            info.Delete();
            return true;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "MountPathHelper: Could not prepare WinFsp mount point '{MountPoint}'", mountPoint);
            return false;
        }
    }

    /// <summary>
    ///     Gets the volume creation time reported to the operating system for a CHD file:
    ///     the file's last-write time in UTC, or the current time when it cannot be read.
    /// </summary>
    /// <param name="chdPath">The CHD file path.</param>
    /// <returns>The creation time to report for the mounted volume.</returns>
    internal static DateTime GetVolumeCreationTime(string chdPath)
    {
        try
        {
            return File.GetLastWriteTimeUtc(chdPath);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "MountPathHelper: Failed to read creation time for '{ChdPath}'", chdPath);
            return DateTime.UtcNow;
        }
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
}
