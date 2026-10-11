namespace CHDMounter.Models;

/// <summary>
///     Represents the application configuration settings.
/// </summary>
public class AppSettings
{
    /// <summary>
    ///     Gets or sets a value indicating whether the mounted drive should automatically open in the file manager.
    ///     Defaults to <c>true</c>.
    /// </summary>
    public bool AutoOpenMountedDrive { get; set; } = true;

    /// <summary>
    ///     Gets or sets the file system driver used for mounting. Defaults to <see cref="MountBackend.Dokan" />
    ///     on Windows; Linux and macOS always use <see cref="MountBackend.Fuse" />.
    /// </summary>
    public MountBackend MountBackend { get; set; } = MountBackend.Dokan;
}