namespace CHDMounter.Core.Models;

/// <summary>
///     Represents the result of an application update check against the GitHub releases API.
/// </summary>
public class UpdateCheckResult
{
    /// <summary>
    ///     Gets a value indicating whether a newer version is available.
    /// </summary>
    public bool HasUpdate { get; init; }

    /// <summary>
    ///     Gets the current application version string.
    /// </summary>
    public string CurrentVersion { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the latest available version string.
    /// </summary>
    public string LatestVersion { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the URL to the GitHub release page.
    /// </summary>
    public string ReleaseUrl { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the direct download URL for the latest release asset.
    /// </summary>
    public string DownloadUrl { get; init; } = string.Empty;
}