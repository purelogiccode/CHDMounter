namespace CHDMounter.Core.Models;

/// <summary>
///     Represents the application configuration settings.
/// </summary>
public class AppSettings
{
    /// <summary>
    ///     Gets or sets a value indicating whether the mounted drive should automatically open in File Explorer.
    ///     Defaults to <c>true</c>.
    /// </summary>
    public bool AutoOpenMountedDrive { get; set; } = true;
}