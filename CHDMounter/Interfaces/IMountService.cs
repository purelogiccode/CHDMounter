namespace CHDMounter.Core.Interfaces;

/// <summary>
///     Defines a service for mounting and unmounting CHD disc images as virtual drives.
/// </summary>
public interface IMountService : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     Gets a value indicating whether a virtual drive is currently mounted.
    /// </summary>
    bool IsMounted { get; }

    /// <summary>
    ///     Gets the drive letter or path of the currently mounted virtual drive.
    /// </summary>
    string MountPoint { get; }

    /// <summary>
    ///     Determines whether a CHD image can currently be mounted.
    /// </summary>
    /// <returns><c>true</c> if mounting is possible; otherwise, <c>false</c>.</returns>
    bool CanMount();

    /// <summary>
    ///     Mounts a CHD disc image as a virtual drive.
    /// </summary>
    /// <param name="chdPath">The file path to the CHD image.</param>
    /// <param name="mountPoint">The drive letter or mount point to use, or <c>null</c> to auto-assign.</param>
    /// <param name="consoleType">The console type that determines how the image is parsed.</param>
    void Mount(string chdPath, string? mountPoint, ConsoleType consoleType);

    /// <summary>
    ///     Unmounts the currently mounted virtual drive.
    /// </summary>
    void Unmount();
}