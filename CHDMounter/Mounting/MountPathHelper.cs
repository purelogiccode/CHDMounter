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
