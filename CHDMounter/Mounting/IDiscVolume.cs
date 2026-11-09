namespace CHDMounter.Mounting;

/// <summary>
///     A read-only file tree exposed by a mounted disc image. Implementations hide whether
///     the data comes from a parsed <see cref="VideoGameFileSystemParser.Parsers.ChdContainer" />
///     or from a CHDMounter-specific virtual export.
/// </summary>
internal interface IDiscVolume : IDisposable
{
    /// <summary>Gets the volume label reported to the operating system.</summary>
    string VolumeName { get; }

    /// <summary>Gets the total size of the disc image in bytes.</summary>
    ulong VolumeSize { get; }

    /// <summary>Gets all entries in the volume.</summary>
    IReadOnlyList<FileEntry> Entries { get; }

    /// <summary>Finds an entry by its container path.</summary>
    /// <param name="path">The path to search for.</param>
    /// <returns>The matching entry, or <c>null</c> when it does not exist.</returns>
    FileEntry? FindFile(string path);

    /// <summary>Enumerates the children of a directory.</summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The child entries.</returns>
    IEnumerable<FileEntry> ListDirectory(string path);

    /// <summary>Reads data from an entry.</summary>
    /// <param name="entry">The entry to read.</param>
    /// <param name="offset">The byte offset within the entry.</param>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="bufOffset">The offset within the destination buffer.</param>
    /// <param name="count">The maximum number of bytes to read.</param>
    /// <returns>The number of bytes read.</returns>
    int ReadFile(FileEntry entry, ulong offset, byte[] buffer, int bufOffset, int count);
}
