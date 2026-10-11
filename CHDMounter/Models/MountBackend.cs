namespace CHDMounter.Core.Models;

/// <summary>
///     Identifies the file system driver used to expose a CHD image as a virtual drive.
/// </summary>
public enum MountBackend
{
    /// <summary>
    ///     Mounts through the Dokan user-mode file system driver (Windows only).
    /// </summary>
    Dokan = 0,

    /// <summary>
    ///     Mounts through the WinFsp user-mode file system driver (Windows only).
    /// </summary>
    WinFsp = 1,

    /// <summary>
    ///     Mounts through FUSE 3 - libfuse3 on Linux or macFUSE on macOS.
    /// </summary>
    Fuse = 2
}
