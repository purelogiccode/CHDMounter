using VideoGameFileSystemParser.Models;
using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Mounting;

/// <summary>
///     Adapts a parsed <see cref="ChdContainer" /> to the <see cref="IDiscVolume" /> contract.
/// </summary>
internal sealed class ChdContainerVolume : IDiscVolume
{
    private readonly ChdContainer _container;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChdContainerVolume" /> class.
    /// </summary>
    /// <param name="container">The parsed container to expose. The volume owns the container.</param>
    internal ChdContainerVolume(ChdContainer container)
    {
        _container = container;
    }

    /// <inheritdoc />
    public string VolumeName => _container.VolumeName;

    /// <inheritdoc />
    public ulong VolumeSize => _container.VolumeSize;

    /// <inheritdoc />
    public IReadOnlyList<FileEntry> Entries => _container.Entries;

    /// <inheritdoc />
    public FileEntry? FindFile(string path)
    {
        return _container.FindFile(path);
    }

    /// <inheritdoc />
    public IEnumerable<FileEntry> ListDirectory(string path)
    {
        return _container.ListDirectory(path);
    }

    /// <inheritdoc />
    public int ReadFile(FileEntry entry, ulong offset, byte[] buffer, int bufOffset, int count)
    {
        return _container.ReadFile(entry, offset, buffer, bufOffset, count);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _container.Dispose();
    }
}
