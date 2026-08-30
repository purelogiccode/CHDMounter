using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Fsp;
using Fsp.Interop;
using VideoGameFileSystemParser.Parsers;
using FileInfo = Fsp.Interop.FileInfo;

namespace CHDMounter_WinFsp;

/// <summary>
///     Implements the WinFsp file system interface to expose a CHD container as a read-only virtual drive.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal sealed class ChdFs : FileSystemBase, IDisposable, IAsyncDisposable
{
    private static long _nextIndexNumber;
    private readonly ChdContainer _container;
    private readonly bool _persistentAcls;

    private byte[]? _cachedSecurityDescriptor;

    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChdFs" /> class.
    /// </summary>
    /// <param name="container">The parsed CHD container to serve files from.</param>
    /// <param name="persistentAcls">If <c>true</c>, enables persistent ACL support for cross-integrity mounts.</param>
    public ChdFs(ChdContainer container, bool persistentAcls = false)
    {
        _container = container;
        _persistentAcls = persistentAcls;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _container.Dispose();
    }

    public override int Init(object host)
    {
        if (host is FileSystemHost fsHost)
        {
            fsHost.CasePreservedNames = true;
            fsHost.UnicodeOnDisk = true;
            fsHost.PersistentAcls = _persistentAcls;
            fsHost.PostCleanupWhenModifiedOnly = true;
            fsHost.FlushAndPurgeOnCleanup = true;
            fsHost.PassQueryDirectoryPattern = true;
            fsHost.MaxComponentLength = 255;
            fsHost.SectorSize = 2048;
            fsHost.FileSystemName = _container.VolumeName;
            fsHost.VolumeCreationTime = DateTimeToFileTimeUtc(DateTime.UtcNow);
            fsHost.VolumeSerialNumber = (uint)Environment.TickCount;
        }

        return STATUS_SUCCESS;
    }

    public override int Open(string FileName, uint CreateOptions, uint GrantedAccess,
        out object FileNode, out object FileDesc, out FileInfo FileInfo, out string NormalizedName)
    {
        return OpenOrCreate(FileName, out FileNode, out FileDesc, out FileInfo, out NormalizedName);
    }

    public override void Close(object FileNode, object FileDesc)
    {
    }

    private int OpenOrCreate(string FileName, out object FileNode, out object FileDesc,
        out FileInfo FileInfo, out string NormalizedName)
    {
        FileNode = null!;
        FileDesc = null!;
        FileInfo = default;
        NormalizedName = FileName;

        var entry = _container.FindFile(FileName);
        if (entry is null)
            return STATUS_OBJECT_NAME_NOT_FOUND;

        NormalizedName = entry.FullPath;
        FileNode = entry;
        FileDesc = entry;
        FileInfo = EntryToFileInfo(entry);
        return STATUS_SUCCESS;
    }

    public override int Read(object FileNode, object FileDesc, IntPtr Buffer, ulong Offset,
        uint Length, out uint BytesTransferred)
    {
        BytesTransferred = 0;

        if (FileNode is FileEntry { IsDirectory: true })
            return STATUS_ACCESS_DENIED;

        if (FileNode is not FileEntry entry)
            return STATUS_INVALID_HANDLE;

        var readBuffer = ArrayPool<byte>.Shared.Rent((int)Length);
        try
        {
            var read = _container.ReadFile(entry, Offset, readBuffer, 0, (int)Length);
            if (read > 0)
                Marshal.Copy(readBuffer, 0, Buffer, read);
            BytesTransferred = (uint)read;
            return STATUS_SUCCESS;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
        }
    }

    public override int GetFileInfo(object FileNode, object FileDesc, out FileInfo FileInfo)
    {
        if (FileNode is FileEntry entry)
        {
            FileInfo = EntryToFileInfo(entry);
            return STATUS_SUCCESS;
        }

        FileInfo = default;
        return STATUS_UNSUCCESSFUL;
    }

    public override int GetDirInfoByName(object FileNode, object FileDesc, string FileName,
        out string NormalizedName, out FileInfo FileInfo)
    {
        NormalizedName = FileName;
        FileInfo = default;

        if (FileNode is not FileEntry { IsDirectory: true })
            return STATUS_OBJECT_NAME_NOT_FOUND;

        foreach (var child in _container.ListDirectory(ResolvePath(FileNode)))
            if (string.Equals(child.Name, FileName, StringComparison.OrdinalIgnoreCase))
            {
                NormalizedName = child.Name;
                FileInfo = EntryToFileInfo(child);
                return STATUS_SUCCESS;
            }

        return STATUS_OBJECT_NAME_NOT_FOUND;
    }

    public override bool ReadDirectoryEntry(object FileNode, object FileDesc, string Pattern,
        string Marker, ref object Context, out string FileName, out FileInfo FileInfo)
    {
        FileName = null!;
        FileInfo = default;

        if (FileNode is not FileEntry { IsDirectory: true })
            return false;

        List<FileEntry> entries;
        int index;

        if (Context is (List<FileEntry> cached, int cachedIndex))
        {
            entries = cached;
            index = cachedIndex;
        }
        else
        {
            entries = _container.ListDirectory(ResolvePath(FileNode)).ToList();
            index = 0;

            if (!string.IsNullOrEmpty(Marker))
            {
                if (string.Equals(Marker, ".", StringComparison.OrdinalIgnoreCase))
                {
                    // Continue after the "." entry: next is "..".
                    index = 1;
                }
                else if (string.Equals(Marker, "..", StringComparison.OrdinalIgnoreCase))
                {
                    // Continue after the ".." entry: next is the first child.
                    index = 2;
                }
                else
                {
                    for (var i = 0; i < entries.Count; i++)
                        if (string.Equals(entries[i].Name, Marker, StringComparison.OrdinalIgnoreCase))
                        {
                            index = i + 3;
                            break;
                        }

                    if (index == 0)
                        return false;
                }
            }
        }

        switch (index)
        {
            case 0:
                FileName = ".";
                FileInfo = new FileInfo { FileAttributes = (uint)FileAttributes.Directory };
                Context = (entries, 1);
                return true;
            case 1:
                FileName = "..";
                FileInfo = new FileInfo { FileAttributes = (uint)FileAttributes.Directory };
                Context = (entries, 2);
                return true;
        }

        while (true)
        {
            var entryIndex = index - 2;
            if (entryIndex >= entries.Count)
                return false;

            var entry = entries[entryIndex];
            index++;
            Context = (entries, index);

            if (!string.IsNullOrEmpty(Pattern) && !string.Equals(Pattern, "*", StringComparison.Ordinal) &&
                !string.Equals(Pattern, "*.*", StringComparison.Ordinal))
                if (!MatchesPattern(entry.Name, Pattern))
                    continue;

            FileName = entry.Name;
            FileInfo = EntryToFileInfo(entry);
            return true;
        }
    }

    private static bool MatchesPattern(string name, string pattern)
    {
        return FileNameMatcher.IsMatch(name, pattern);
    }

    public override int GetVolumeInfo(out VolumeInfo VolumeInfo)
    {
        VolumeInfo = default;
        VolumeInfo.TotalSize = _container.VolumeSize;
        VolumeInfo.FreeSize = 0;
        return STATUS_SUCCESS;
    }

    public override int GetSecurityByName(string FileName, out uint FileAttributes,
        ref byte[] SecurityDescriptor)
    {
        var entry = _container.FindFile(FileName);
        if (entry is null)
        {
            FileAttributes = 0;
            return STATUS_OBJECT_NAME_NOT_FOUND;
        }

        FileAttributes = (uint)(entry.IsDirectory
            ? System.IO.FileAttributes.Directory
            : System.IO.FileAttributes.Archive | System.IO.FileAttributes.ReadOnly);

        if (_persistentAcls)
        {
            var sd = CreateDefaultSecurityDescriptor();
            if (SecurityDescriptor is null || SecurityDescriptor.Length < sd.Length)
                SecurityDescriptor = new byte[sd.Length];

            Array.Copy(sd, SecurityDescriptor, sd.Length);
        }

        return STATUS_SUCCESS;
    }

    private byte[] CreateDefaultSecurityDescriptor()
    {
        if (_cachedSecurityDescriptor is not null)
            return _cachedSecurityDescriptor;

        const string sddl = "D:P(A;;FA;;;WD)";
        var sd = new RawSecurityDescriptor(sddl);
        var bytes = new byte[sd.BinaryLength];
        sd.GetBinaryForm(bytes, 0);
        _cachedSecurityDescriptor = bytes;
        return bytes;
    }

    private static FileInfo EntryToFileInfo(FileEntry entry)
    {
        return new FileInfo
        {
            FileAttributes = (uint)(entry.IsDirectory
                ? FileAttributes.Directory
                : FileAttributes.Archive | FileAttributes.ReadOnly),
            FileSize = entry.Size,
            AllocationSize = entry.Size,
            CreationTime = DateTimeToFileTimeUtc(entry.ModifiedTime),
            LastAccessTime = DateTimeToFileTimeUtc(entry.ModifiedTime),
            LastWriteTime = DateTimeToFileTimeUtc(entry.ModifiedTime),
            ChangeTime = DateTimeToFileTimeUtc(entry.ModifiedTime),
            IndexNumber = (ulong)Interlocked.Increment(ref _nextIndexNumber)
        };
    }

    private static string ResolvePath(object fileNode)
    {
        return fileNode is FileEntry e ? e.FullPath : "\\";
    }

    private static ulong DateTimeToFileTimeUtc(DateTime dateTime)
    {
        try
        {
            return (ulong)dateTime.ToFileTimeUtc();
        }
        catch (ArgumentOutOfRangeException)
        {
            return 0;
        }
    }
}