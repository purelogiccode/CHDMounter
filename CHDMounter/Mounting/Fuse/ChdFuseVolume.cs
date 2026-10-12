using System.Buffers;
using FuseSharp;
using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Mounting.Fuse;

/// <summary>
///     Adapts a parsed <see cref="ChdContainer" /> to the read-only <see cref="IFuseVolume" />
///     contract used by the FuseSharp mount layer. Paths received from FUSE are POSIX-style;
///     the container normalizes them to its own backslash-separated, case-insensitive scheme.
/// </summary>
internal sealed class ChdFuseVolume : IFuseVolume
{
    private readonly ChdContainer _container;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChdFuseVolume" /> class.
    /// </summary>
    /// <param name="container">The parsed CHD container to expose.</param>
    /// <param name="creationTime">The volume creation time reported to the operating system.</param>
    public ChdFuseVolume(ChdContainer container, DateTime creationTime)
    {
        _container = container;
        VolumeCreationTime = creationTime;
    }

    /// <inheritdoc />
    public string VolumeLabel => _container.VolumeName;

    /// <inheritdoc />
    public DateTime VolumeCreationTime { get; }

    /// <inheritdoc />
    public ulong VolumeSize => _container.VolumeSize;

    /// <inheritdoc />
    public IFuseEntry? GetEntry(string path)
    {
        var entry = _container.FindFile(ToContainerPath(path));
        return entry is null ? null : new FuseEntry(entry);
    }

    /// <inheritdoc />
    public IEnumerable<IFuseEntry> GetFolderList(string path)
    {
        foreach (var entry in _container.ListDirectory(ToContainerPath(path)))
            yield return new FuseEntry(entry);
    }

    /// <inheritdoc />
    public int ReadFile(IFuseEntry entry, Span<byte> buffer, long offset)
    {
        if (entry is not FuseEntry fuseEntry || fuseEntry.Entry.IsDirectory || buffer.Length == 0)
            return 0;

        var readBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            var read = _container.ReadFile(fuseEntry.Entry, (ulong)offset, readBuffer, 0, buffer.Length);
            readBuffer.AsSpan(0, read).CopyTo(buffer);
            return read;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
        }
    }

    private static string ToContainerPath(string path)
    {
        return string.IsNullOrEmpty(path) ? "\\" : path;
    }

    /// <summary>
    ///     Wraps a <see cref="FileEntry" /> for the FUSE layer.
    /// </summary>
    private sealed class FuseEntry : IFuseEntry
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="FuseEntry" /> class.
        /// </summary>
        /// <param name="entry">The container entry to wrap.</param>
        internal FuseEntry(FileEntry entry)
        {
            Entry = entry;
        }

        /// <summary>
        ///     Gets the wrapped container entry.
        /// </summary>
        internal FileEntry Entry { get; }

        /// <inheritdoc />
        public string FileName => Entry.Name;

        /// <inheritdoc />
        public bool IsDirectory => Entry.IsDirectory;

        /// <inheritdoc />
        public long Size => (long)Entry.Size;
    }
}
