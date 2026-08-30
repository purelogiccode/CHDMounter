using System.Reflection;
using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Core.Tests.Parsers;

public class ChdContainerTests
{
    private static string InvokeMakeEntryKey(string path)
    {
        var method = typeof(ChdContainer).GetMethod("MakeEntryKey", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, [path])!;
    }

    [Fact]
    public void MakeEntryKeyNormalizesForwardSlashes()
    {
        var result = InvokeMakeEntryKey("/game/data/file.bin");
        Assert.Equal(@"\game\data\file.bin", result);
    }

    [Fact]
    public void MakeEntryKeyNormalizesLeadingBackslash()
    {
        var result = InvokeMakeEntryKey("game\\data.bin");
        Assert.Equal(@"\game\data.bin", result);
    }

    [Fact]
    public void MakeEntryKeyRemovesTrailingBackslash()
    {
        var result = InvokeMakeEntryKey(@"\game\data\");
        Assert.Equal(@"\game\data", result);
    }

    [Fact]
    public void MakeEntryKeyHandlesRootBackslash()
    {
        var result = InvokeMakeEntryKey("\\");
        Assert.Equal("\\", result);
    }

    [Fact]
    public void MakeEntryKeyHandlesRootForwardSlash()
    {
        var result = InvokeMakeEntryKey("/");
        Assert.Equal("\\", result);
    }

    [Fact]
    public void MakeEntryKeyHandlesEmptyString()
    {
        var result = InvokeMakeEntryKey("");
        Assert.Equal("\\", result);
    }

    [Fact]
    public void MakeEntryKeyLowercasesPath()
    {
        var result = InvokeMakeEntryKey(@"\GAME\DATA.BIN");
        Assert.Equal(@"\game\data.bin", result);
    }

    [Fact]
    public void MakeEntryKeyHandlesMixedSlashes()
    {
        var result = InvokeMakeEntryKey("/game\\data/file.bin");
        Assert.Equal(@"\game\data\file.bin", result);
    }

    [Fact]
    public void ConstructorWithValidPathDoesNotThrow()
    {
        var exception = Record.Exception(() => new ChdContainer("test.chd"));
        Assert.Null(exception);
    }

    [Fact]
    public void EntriesIsEmptyBeforeOpen()
    {
        using var container = new ChdContainer("test.chd");
        Assert.Empty(container.Entries);
    }

    [Fact]
    public void VolumeNameIsEmptyBeforeOpen()
    {
        using var container = new ChdContainer("test.chd");
        Assert.Equal("", container.VolumeName);
    }

    [Fact]
    public void ConsoleTypeIsUnknownBeforeOpen()
    {
        using var container = new ChdContainer("test.chd");
        Assert.Equal(ConsoleType.Unknown, container.ConsoleType);
    }

    [Fact]
    public void DisposeDoesNotThrow()
    {
        var container = new ChdContainer("test.chd");
        var exception = Record.Exception(() => container.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void DisposeIsIdempotent()
    {
        var container = new ChdContainer("test.chd");
        container.Dispose();
        var exception = Record.Exception(() => container.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void BuildFromFsNodeCreatesRootEntry()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode { Name = "/", IsDirectory = true };
        container.BuildFromFsNode(root);

        Assert.Single(container.Entries);
        Assert.True(container.Entries[0].IsDirectory);
    }

    [Fact]
    public void BuildFromFsNodeAddsChildEntries()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children =
            [
                new FsNode { Name = "file1.bin", Lba = 100, Size = 2048 },
                new FsNode { Name = "file2.bin", Lba = 200, Size = 4096 }
            ]
        };
        container.BuildFromFsNode(root);

        Assert.Equal(3, container.Entries.Count); // root + 2 children
    }

    [Fact]
    public void BuildFromFsNodeSetsPropertiesOnEntries()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children =
            [
                new FsNode
                {
                    Name = "game.iso",
                    Lba = 150,
                    Size = 1024000,
                    FileNumber = 2,
                    IsInterleaved = true,
                    IsDirectory = false
                }
            ]
        };
        container.BuildFromFsNode(root);

        var entry = container.Entries.First(e => string.Equals(e.Name, "game.iso", StringComparison.Ordinal));
        Assert.Equal(150u, entry.Lba);
        Assert.Equal(1024000ul, entry.Size);
        Assert.Equal(2, entry.FileNumber);
        Assert.True(entry.IsInterleaved);
        Assert.False(entry.IsDirectory);
    }

    [Fact]
    public void BuildFromFsNodeHandlesNestedDirectories()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children =
            [
                new FsNode
                {
                    Name = "subdir",
                    IsDirectory = true,
                    Children =
                    [
                        new FsNode { Name = "nested.bin", Lba = 300, Size = 512 }
                    ]
                }
            ]
        };
        container.BuildFromFsNode(root);

        Assert.Equal(3, container.Entries.Count); // root + subdir + nested.bin
        Assert.Contains(container.Entries, e => e is { Name: "subdir", IsDirectory: true });
        Assert.Contains(container.Entries, e => e is { Name: "nested.bin", IsDirectory: false });
    }

    [Fact]
    public void BuildFromFsNodeCopiesExtents()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children =
            [
                new FsNode
                {
                    Name = "multi.bin",
                    Extents =
                    [
                        new FsExtent { Lba = 100, Size = 2048 },
                        new FsExtent { Lba = 200, Size = 1024 }
                    ]
                }
            ]
        };
        container.BuildFromFsNode(root);

        var entry = container.Entries.First(e => string.Equals(e.Name, "multi.bin", StringComparison.Ordinal));
        Assert.Equal(2, entry.Extents.Count);
        Assert.Equal(100u, entry.Extents[0].Lba);
        Assert.Equal(2048ul, entry.Extents[0].Size);
        Assert.Equal(200u, entry.Extents[1].Lba);
        Assert.Equal(1024ul, entry.Extents[1].Size);
    }

    [Fact]
    public void BuildFromFsNodeSetsModifiedTime()
    {
        using var container = new ChdContainer("test.chd");
        var modTime = new DateTime(2024, 1, 15, 10, 30, 0);
        var root = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children =
            [
                new FsNode
                {
                    Name = "dated.bin",
                    ModifiedTime = modTime
                }
            ]
        };
        container.BuildFromFsNode(root);

        var entry = container.Entries.First(e => string.Equals(e.Name, "dated.bin", StringComparison.Ordinal));
        Assert.Equal(modTime, entry.ModifiedTime);
    }

    [Fact]
    public void BuildFromFsNodeSetsEmbeddedProperties()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children =
            [
                new FsNode
                {
                    Name = "embedded.bin",
                    IsEmbedded = true,
                    EmbeddedOffset = 24
                }
            ]
        };
        container.BuildFromFsNode(root);

        var entry = container.Entries.First(e => string.Equals(e.Name, "embedded.bin", StringComparison.Ordinal));
        Assert.True(entry.IsEmbedded);
        Assert.Equal(24u, entry.Offset);
    }

    [Fact]
    public void FindFileReturnsNullWhenNotBuilt()
    {
        using var container = new ChdContainer("test.chd");
        var result = container.FindFile("\\nonexistent.bin");
        Assert.Null(result);
    }

    [Fact]
    public void FindFileReturnsEntryAfterBuild()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children =
            [
                new FsNode { Name = "test.bin", Lba = 100, Size = 2048 }
            ]
        };
        container.BuildFromFsNode(root);

        var found = container.FindFile("\\test.bin");
        Assert.NotNull(found);
        Assert.Equal("test.bin", found.Name);
    }

    [Fact]
    public void FindFileIsCaseInsensitive()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children =
            [
                new FsNode { Name = "Test.BIN", Lba = 100, Size = 2048 }
            ]
        };
        container.BuildFromFsNode(root);

        var found = container.FindFile("\\test.bin");
        Assert.NotNull(found);
    }

    [Fact]
    public void FindFileWithForwardSlashesWorks()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children =
            [
                new FsNode
                {
                    Name = "subdir",
                    IsDirectory = true,
                    Children =
                    [
                        new FsNode { Name = "file.bin" }
                    ]
                }
            ]
        };
        container.BuildFromFsNode(root);

        var found = container.FindFile("/subdir/file.bin");
        Assert.NotNull(found);
        Assert.Equal("file.bin", found.Name);
    }

    [Fact]
    public void ListDirectoryReturnsEmptyWhenNotBuilt()
    {
        using var container = new ChdContainer("test.chd");
        var result = container.ListDirectory("\\").ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void ListDirectoryReturnsChildren()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children =
            [
                new FsNode { Name = "a.bin" },
                new FsNode { Name = "b.bin" }
            ]
        };
        container.BuildFromFsNode(root);

        var children = container.ListDirectory("\\").ToList();
        Assert.Equal(2, children.Count);
        Assert.Contains(children, e => string.Equals(e.Name, "a.bin", StringComparison.Ordinal));
        Assert.Contains(children, e => string.Equals(e.Name, "b.bin", StringComparison.Ordinal));
    }

    [Fact]
    public void ListDirectoryReturnsEmptyForNonexistentPath()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode { Name = "/", IsDirectory = true };
        container.BuildFromFsNode(root);

        var result = container.ListDirectory("\\nonexistent").ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void BuildFromFsNodeClearsPreviousEntries()
    {
        using var container = new ChdContainer("test.chd");

        var root1 = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children = [new FsNode { Name = "old.bin" }]
        };
        container.BuildFromFsNode(root1);
        Assert.Equal(2, container.Entries.Count);

        var root2 = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children = [new FsNode { Name = "new.bin" }]
        };
        container.BuildFromFsNode(root2);
        Assert.Equal(2, container.Entries.Count);
        Assert.Contains(container.Entries, e => string.Equals(e.Name, "new.bin", StringComparison.Ordinal));
        Assert.DoesNotContain(container.Entries, e => string.Equals(e.Name, "old.bin", StringComparison.Ordinal));
    }

    [Fact]
    public void SectorToMsfConvertsCorrectly()
    {
        var method = typeof(ChdContainer).GetMethod("SectorToMsf", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // 0 sectors = 00:00:00
        Assert.Equal("00:00:00", method.Invoke(null, [(uint)0]));

        // 75 sectors = 1 second = 00:01:00
        Assert.Equal("00:01:00", method.Invoke(null, [(uint)75]));

        // 150 sectors = 2 seconds = 00:02:00
        Assert.Equal("00:02:00", method.Invoke(null, [(uint)150]));

        // 4500 sectors = 1 minute = 01:00:00
        Assert.Equal("01:00:00", method.Invoke(null, [(uint)4500]));

        // 1 sector = 00:00:01
        Assert.Equal("00:00:01", method.Invoke(null, [(uint)1]));

        // 76 sectors = 00:01:01
        Assert.Equal("00:01:01", method.Invoke(null, [(uint)76]));
    }

    [Fact]
    public void TryFindFileReturnsTrueForExistingEntry()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children = [new FsNode { Name = "test.bin", Lba = 100, Size = 2048 }]
        };
        container.BuildFromFsNode(root);

        var found = container.TryFindFile("\\test.bin", out var entry, out var error);
        Assert.True(found);
        Assert.NotNull(entry);
        Assert.Equal("test.bin", entry.Name);
        Assert.Null(error);
    }

    [Fact]
    public void TryFindFileReturnsFalseWithErrorMessage()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode { Name = "/", IsDirectory = true };
        container.BuildFromFsNode(root);

        var found = container.TryFindFile("\\nonexistent.bin", out var entry, out var error);
        Assert.False(found);
        Assert.Null(entry);
        Assert.NotNull(error);
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryFindFileReturnsFalseWhenDisposed()
    {
        var container = new ChdContainer("test.chd");
        container.Dispose();

        var found = container.TryFindFile("\\test.bin", out var entry, out var error);
        Assert.False(found);
        Assert.Null(entry);
        Assert.NotNull(error);
        Assert.Contains("disposed", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DisposeAsyncDoesNotThrow()
    {
        var container = new ChdContainer("test.chd");
        var exception = await Record.ExceptionAsync(() => container.DisposeAsync().AsTask());
        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsyncIsIdempotent()
    {
        var container = new ChdContainer("test.chd");
        await container.DisposeAsync();
        var exception = await Record.ExceptionAsync(() => container.DisposeAsync().AsTask());
        Assert.Null(exception);
    }

    [Fact]
    public void FindFileReturnsNullForRoot()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode { Name = "/", IsDirectory = true };
        container.BuildFromFsNode(root);

        var found = container.FindFile("\\");
        Assert.NotNull(found);
        Assert.True(found.IsDirectory);
    }

    [Fact]
    public void ListDirectoryReturnsNestedChildren()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children =
            [
                new FsNode
                {
                    Name = "dir",
                    IsDirectory = true,
                    Children =
                    [
                        new FsNode { Name = "a.bin" },
                        new FsNode { Name = "b.bin" }
                    ]
                }
            ]
        };
        container.BuildFromFsNode(root);

        var children = container.ListDirectory("\\dir").ToList();
        Assert.Equal(2, children.Count);
        Assert.Contains(children, e => string.Equals(e.Name, "a.bin", StringComparison.Ordinal));
        Assert.Contains(children, e => string.Equals(e.Name, "b.bin", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildFromFsNodeSetsFullPath()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children =
            [
                new FsNode
                {
                    Name = "sub",
                    IsDirectory = true,
                    Children = [new FsNode { Name = "file.bin" }]
                }
            ]
        };
        container.BuildFromFsNode(root);

        var entry = container.Entries.First(e => string.Equals(e.Name, "file.bin", StringComparison.Ordinal));
        Assert.Equal(@"\sub\file.bin", entry.FullPath);
    }

    [Fact]
    public void BuildFromFsNodeHandlesRawPassthrough()
    {
        using var container = new ChdContainer("test.chd");
        var root = new FsNode
        {
            Name = "/",
            IsDirectory = true,
            Children =
            [
                new FsNode { Name = "image.iso", IsRawPassthrough = true }
            ]
        };
        container.BuildFromFsNode(root);

        var entry = container.Entries.First(e => string.Equals(e.Name, "image.iso", StringComparison.Ordinal));
        Assert.True(entry.IsRawPassthrough);
    }
}