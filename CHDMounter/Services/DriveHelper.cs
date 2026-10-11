namespace CHDMounter.Core.Services;

/// <summary>
///     Provides helper methods for selecting available drive letters.
/// </summary>
public static class DriveHelper
{
    /// <summary>
    ///     Picks an available drive letter from the range M-Q, falling back to Z-D.
    /// </summary>
    /// <returns>A drive letter string in the format "X:" (e.g., "M:").</returns>
    public static string PickDriveLetter()
    {
        var drives = DriveInfo.GetDrives().Select(static d => d.Name[0]).ToHashSet();
        foreach (var c in CandidateLetters())
            if (!drives.Contains(c))
                return $"{c}:";

        throw new InvalidOperationException("No available drive letter found. All drive letters are in use.");
    }

    /// <summary>
    ///     Enumerates candidate drive letters (M-Q first, then Z-D) that appear unused
    ///     according to <see cref="DriveInfo.GetDrives" />. Note that per-session or
    ///     elevated-session drives may not be visible here, so a returned letter can still
    ///     fail to mount; callers should retry with the next candidate on mount errors.
    /// </summary>
    /// <returns>The candidate drive letters in priority order, e.g. "M:", "N:", ...</returns>
    public static IEnumerable<string> GetAvailableDriveLetters()
    {
        var drives = DriveInfo.GetDrives().Select(static d => d.Name[0]).ToHashSet();
        foreach (var c in CandidateLetters())
            if (!drives.Contains(c))
                yield return $"{c}:";
    }

    private static IEnumerable<char> CandidateLetters()
    {
        for (var c = 'M'; c <= 'Q'; c++)
            yield return c;

        for (var c = 'Z'; c >= 'D'; c--)
            yield return c;
    }
}