using System.Security.AccessControl;
using System.Security.Principal;
using CHDMounter.Core.Interfaces;
using DokanNet;
using Serilog;
using VideoGameFileSystemParser.Parsers;
using DokanFileAccess = DokanNet.FileAccess;

namespace CHDMounter;

/// <summary>
///     Implements the Dokan file system interface to expose a CHD container as a read-only virtual drive.
/// </summary>
internal sealed class ChdFs : IDokanOperations, IDisposable, IAsyncDisposable
{
    private readonly ChdContainer _container;
    private readonly ILoggingService _loggingService;
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChdFs" /> class.
    /// </summary>
    /// <param name="container">The parsed CHD container to serve files from.</param>
    /// <param name="loggingService">The logging service for recording mount/unmount events.</param>
    public ChdFs(ChdContainer container, ILoggingService loggingService)
    {
        _container = container;
        _loggingService = loggingService;
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

    public NtStatus CreateFile(string fileName, DokanFileAccess access, FileShare share, FileMode mode,
        FileOptions options, FileAttributes attributes, IDokanFileInfo info)
    {
        var entry = _container.FindFile(fileName);
        if (entry is null)
            return DokanResult.FileNotFound;

        if (entry.IsDirectory)
        {
            info.IsDirectory = true;
            return mode switch
            {
                FileMode.Open or FileMode.OpenOrCreate => DokanResult.Success,
                FileMode.CreateNew => DokanResult.FileExists,
                _ => DokanResult.AccessDenied
            };
        }

        info.IsDirectory = false;

        if ((access & (DokanFileAccess.WriteData | DokanFileAccess.AppendData)) != DokanFileAccess.None)
            return DokanResult.AccessDenied;

        info.Context = entry;

        return mode switch
        {
            FileMode.Open or FileMode.OpenOrCreate => DokanResult.Success,
            FileMode.CreateNew => DokanResult.FileExists,
            _ => DokanResult.AccessDenied
        };
    }

    public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
    {
        bytesRead = 0;

        if (info.IsDirectory)
            return DokanResult.AccessDenied;

        FileEntry entry;
        if (info.Context is FileEntry ctxEntry)
        {
            entry = ctxEntry;
        }
        else
        {
            var found = _container.FindFile(fileName);
            if (found is null)
                return DokanResult.FileNotFound;

            entry = found;
        }

        bytesRead = _container.ReadFile(entry, (ulong)offset, buffer, 0, buffer.Length);
        return DokanResult.Success;
    }

    public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
    {
        fileInfo = new FileInformation();

        var entry = _container.FindFile(fileName);
        if (entry is null)
            return DokanResult.FileNotFound;

        fileInfo.Attributes = entry.IsDirectory
            ? FileAttributes.Directory
            : FileAttributes.Archive | FileAttributes.ReadOnly;
        fileInfo.FileName = entry.Name;
        fileInfo.Length = (long)entry.Size;
        fileInfo.LastWriteTime = entry.ModifiedTime;
        fileInfo.CreationTime = entry.ModifiedTime;
        fileInfo.LastAccessTime = entry.ModifiedTime;

        info.IsDirectory = entry.IsDirectory;
        return DokanResult.Success;
    }

    public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
    {
        var entries = _container.ListDirectory(fileName).ToList();

        if (entries.Count == 0)
            if (_container.FindFile(fileName) is null)
            {
                files = Array.Empty<FileInformation>();
                return DokanResult.FileNotFound;
            }

        var result = new List<FileInformation>
        {
            new() { FileName = ".", Attributes = FileAttributes.Directory },
            new() { FileName = "..", Attributes = FileAttributes.Directory }
        };

        foreach (var entry in entries)
            result.Add(new FileInformation
            {
                FileName = entry.Name,
                Attributes = entry.IsDirectory
                    ? FileAttributes.Directory
                    : FileAttributes.Archive | FileAttributes.ReadOnly,
                Length = (long)entry.Size,
                LastWriteTime = entry.ModifiedTime,
                CreationTime = entry.ModifiedTime,
                LastAccessTime = entry.ModifiedTime
            });

        files = result;
        return DokanResult.Success;
    }

    public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files,
        IDokanFileInfo info)
    {
        var result = FindFiles(fileName, out var allFiles, info);
        if (result != DokanResult.Success)
        {
            files = Array.Empty<FileInformation>();
            return result;
        }

        if (searchPattern is "*" or "*.*")
        {
            files = allFiles;
            return DokanResult.Success;
        }

        // Windows passes NT 8.3 DOS wildcard patterns (e.g. "<.cue" for
        // "*.cue"), which the shared matcher understands.
        files = allFiles.Where(f => FileNameMatcher.IsMatch(f.FileName, searchPattern)).ToList();
        return DokanResult.Success;
    }

    public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features,
        out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
    {
        volumeLabel = _container.VolumeName;
        features = FileSystemFeatures.ReadOnlyVolume | FileSystemFeatures.CasePreservedNames |
                   FileSystemFeatures.UnicodeOnDisk;
        fileSystemName = "CHDFS";
        maximumComponentLength = 255;
        return DokanResult.Success;
    }

    public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes,
        out long totalNumberOfFreeBytes, IDokanFileInfo info)
    {
        totalNumberOfBytes = (long)_container.VolumeSize;
        freeBytesAvailable = 0;
        totalNumberOfFreeBytes = 0;
        return DokanResult.Success;
    }

    public NtStatus Mounted(string mountPoint, IDokanFileInfo info)
    {
        _loggingService.Log($"Dokan mounted at {mountPoint}");
        return DokanResult.Success;
    }

    public NtStatus Unmounted(IDokanFileInfo info)
    {
        _loggingService.Log("Dokan unmounted");
        return DokanResult.Success;
    }

    public void Cleanup(string fileName, IDokanFileInfo info)
    {
    }

    public void CloseFile(string fileName, IDokanFileInfo info)
    {
        info.Context = null;
    }

    public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
    {
        bytesWritten = 0;
        return DokanResult.AccessDenied;
    }

    public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime,
        DateTime? lastWriteTime, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }

    public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
    {
        return DokanResult.Success;
    }

    public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
    {
        return DokanResult.Success;
    }

    public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
    {
        streams = Array.Empty<FileInformation>();
        return DokanResult.NotImplemented;
    }

    public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections,
        IDokanFileInfo info)
    {
        try
        {
            var entry = _container.FindFile(fileName);
            if (entry is null)
            {
                security = null!;
                return DokanResult.FileNotFound;
            }

            var isDir = entry.IsDirectory;

            var everyoneSid = new SecurityIdentifier("S-1-1-0");

            if (isDir)
            {
                var ds = new DirectorySecurity();
                ds.AddAccessRule(new FileSystemAccessRule(everyoneSid, FileSystemRights.Read, AccessControlType.Allow));
                ds.SetOwner(everyoneSid);
                ds.SetGroup(everyoneSid);
                security = ds;
            }
            else
            {
                var fs = new FileSecurity();
                fs.AddAccessRule(new FileSystemAccessRule(everyoneSid, FileSystemRights.ReadAndExecute,
                    AccessControlType.Allow));
                fs.SetOwner(everyoneSid);
                fs.SetGroup(everyoneSid);
                security = fs;
            }

            return DokanResult.Success;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetFileSecurity error for {FileName}", fileName);
            security = null!;
            return DokanResult.Error;
        }
    }

    public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections,
        IDokanFileInfo info)
    {
        return DokanResult.AccessDenied;
    }
}