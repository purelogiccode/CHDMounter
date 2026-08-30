namespace CHDMounter.Core.Tests.Models;

public class FsNodeExtendedTests
{
    [Fact]
    public void FsNodeIsEmbeddedDefaultIsFalse()
    {
        var node = new FsNode();
        Assert.False(node.IsEmbedded);
    }

    [Fact]
    public void FsNodeEmbeddedOffsetDefaultIsZero()
    {
        var node = new FsNode();
        Assert.Equal(0u, node.EmbeddedOffset);
    }

    [Fact]
    public void FsNodeModifiedTimeDefaultIsNull()
    {
        var node = new FsNode();
        Assert.Null(node.ModifiedTime);
    }

    [Fact]
    public void FsNodeCreatedTimeDefaultIsNull()
    {
        var node = new FsNode();
        Assert.Null(node.CreatedTime);
    }

    [Fact]
    public void FsNodeAccessedTimeDefaultIsNull()
    {
        var node = new FsNode();
        Assert.Null(node.AccessedTime);
    }

    [Fact]
    public void FsNodeNodeTypeDefaultIsFile()
    {
        var node = new FsNode();
        Assert.Equal(FsNodeType.File, node.NodeType);
    }

    [Fact]
    public void FsNodeSymlinkTargetDefaultIsNull()
    {
        var node = new FsNode();
        Assert.Null(node.SymlinkTarget);
    }

    [Fact]
    public void FsNodeUnixModeDefaultIsNull()
    {
        var node = new FsNode();
        Assert.Null(node.UnixMode);
    }

    [Fact]
    public void FsNodeUidDefaultIsNull()
    {
        var node = new FsNode();
        Assert.Null(node.Uid);
    }

    [Fact]
    public void FsNodeGidDefaultIsNull()
    {
        var node = new FsNode();
        Assert.Null(node.Gid);
    }

    [Fact]
    public void FsNodeInodeDefaultIsNull()
    {
        var node = new FsNode();
        Assert.Null(node.Inode);
    }

    [Fact]
    public void FsNodeLinkCountDefaultIsNull()
    {
        var node = new FsNode();
        Assert.Null(node.LinkCount);
    }

    [Fact]
    public void FsNodeCanSetRockRidgeProperties()
    {
        var node = new FsNode
        {
            UnixMode = 33188, // 0o100644 in octal
            Uid = 1000,
            Gid = 1000,
            Inode = 12345,
            LinkCount = 1
        };

        Assert.Equal(33188u, node.UnixMode);
        Assert.Equal(1000u, node.Uid);
        Assert.Equal(1000u, node.Gid);
        Assert.Equal(12345u, node.Inode);
        Assert.Equal(1u, node.LinkCount);
    }

    [Fact]
    public void FsNodeCanSetEmbeddedProperties()
    {
        var node = new FsNode
        {
            IsEmbedded = true,
            EmbeddedOffset = 24
        };

        Assert.True(node.IsEmbedded);
        Assert.Equal(24u, node.EmbeddedOffset);
    }

    [Fact]
    public void FsNodeCanSetTimestamps()
    {
        var modTime = new DateTime(2024, 1, 1);
        var createTime = new DateTime(2023, 12, 1);
        var accessTime = new DateTime(2024, 1, 15);

        var node = new FsNode
        {
            ModifiedTime = modTime,
            CreatedTime = createTime,
            AccessedTime = accessTime
        };

        Assert.Equal(modTime, node.ModifiedTime);
        Assert.Equal(createTime, node.CreatedTime);
        Assert.Equal(accessTime, node.AccessedTime);
    }

    [Fact]
    public void FsNodeCanSetSymlinkProperties()
    {
        var node = new FsNode
        {
            NodeType = FsNodeType.Symlink,
            SymlinkTarget = "/path/to/target"
        };

        Assert.Equal(FsNodeType.Symlink, node.NodeType);
        Assert.Equal("/path/to/target", node.SymlinkTarget);
    }

    [Fact]
    public void FsNodeCanSetDirectoryNodeType()
    {
        var node = new FsNode { NodeType = FsNodeType.Directory };
        Assert.Equal(FsNodeType.Directory, node.NodeType);
    }
}

public class FileEntryExtendedTests
{
    [Fact]
    public void FileEntryIsEmbeddedDefaultIsFalse()
    {
        var entry = new FileEntry();
        Assert.False(entry.IsEmbedded);
    }

    [Fact]
    public void FileEntryCanSetIsEmbedded()
    {
        var entry = new FileEntry { IsEmbedded = true };
        Assert.True(entry.IsEmbedded);
    }

    [Fact]
    public void FileExtentStructLbaAndSize()
    {
        var extent = new FileExtent { Lba = 42, Size = 2048 };
        Assert.Equal(42u, extent.Lba);
        Assert.Equal(2048ul, extent.Size);
    }

    [Fact]
    public void FileExtentDefaultValues()
    {
        var extent = new FileExtent();
        Assert.Equal(0u, extent.Lba);
        Assert.Equal(0ul, extent.Size);
    }

    [Fact]
    public void FileEntryCanSetRawPassthrough()
    {
        var entry = new FileEntry { IsRawPassthrough = true };
        Assert.True(entry.IsRawPassthrough);
    }

    [Fact]
    public void FileEntryMultipleExtents()
    {
        var entry = new FileEntry();
        entry.Extents.Add(new FileExtent { Lba = 100, Size = 2048 });
        entry.Extents.Add(new FileExtent { Lba = 200, Size = 1024 });
        entry.Extents.Add(new FileExtent { Lba = 300, Size = 512 });

        Assert.Equal(3, entry.Extents.Count);
        Assert.Equal(100u, entry.Extents[0].Lba);
        Assert.Equal(200u, entry.Extents[1].Lba);
        Assert.Equal(300u, entry.Extents[2].Lba);
    }
}

public class FsExtentTests
{
    [Fact]
    public void FsExtentDefaultValues()
    {
        var extent = new FsExtent();
        Assert.Equal(0u, extent.Lba);
        Assert.Equal(0ul, extent.Size);
    }

    [Fact]
    public void FsExtentCanSetValues()
    {
        var extent = new FsExtent { Lba = 500, Size = 4096 };
        Assert.Equal(500u, extent.Lba);
        Assert.Equal(4096ul, extent.Size);
    }
}