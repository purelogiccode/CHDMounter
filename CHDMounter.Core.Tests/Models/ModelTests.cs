namespace CHDMounter.Core.Tests.Models;

public class FsNodeTests
{
    [Fact]
    public void NewFsNodeHasDefaultValues()
    {
        var node = new FsNode();
        Assert.Equal("", node.Name);
        Assert.Equal(0u, node.Lba);
        Assert.Equal(0ul, node.Size);
        Assert.Equal(0, node.FileNumber);
        Assert.False(node.IsInterleaved);
        Assert.False(node.IsDirectory);
        Assert.False(node.IsMultiExtent);
        Assert.False(node.IsRawPassthrough);
        Assert.NotNull(node.Extents);
        Assert.Empty(node.Extents);
        Assert.NotNull(node.Children);
        Assert.Empty(node.Children);
    }

    [Fact]
    public void FsNodeCanSetProperties()
    {
        var node = new FsNode
        {
            Name = "test.bin",
            Lba = 100,
            Size = 2048,
            FileNumber = 5,
            IsInterleaved = true,
            IsDirectory = true,
            IsMultiExtent = true,
            IsRawPassthrough = true
        };

        Assert.Equal("test.bin", node.Name);
        Assert.Equal(100u, node.Lba);
        Assert.Equal(2048ul, node.Size);
        Assert.Equal(5, node.FileNumber);
        Assert.True(node.IsInterleaved);
        Assert.True(node.IsDirectory);
        Assert.True(node.IsMultiExtent);
        Assert.True(node.IsRawPassthrough);
    }

    [Fact]
    public void FsNodeChildrenCanBeAdded()
    {
        var parent = new FsNode { Name = "parent" };
        var child = new FsNode { Name = "child" };
        parent.Children.Add(child);
        Assert.Single(parent.Children);
        Assert.Equal("child", parent.Children[0].Name);
    }

    [Fact]
    public void FsNodeExtentsCanBeAdded()
    {
        var node = new FsNode();
        var extent = new FsExtent { Lba = 50, Size = 100 };
        node.Extents.Add(extent);
        Assert.Single(node.Extents);
        Assert.Equal(50u, node.Extents[0].Lba);
        Assert.Equal(100ul, node.Extents[0].Size);
    }
}

public class FileEntryTests
{
    [Fact]
    public void NewFileEntryHasDefaultValues()
    {
        var entry = new FileEntry();
        Assert.Equal("", entry.Name);
        Assert.Equal("", entry.FullPath);
        Assert.Equal(0u, entry.Lba);
        Assert.Equal(0ul, entry.Size);
        Assert.Equal(0ul, entry.Offset);
        Assert.False(entry.IsDirectory);
        Assert.Equal(0, entry.FileNumber);
        Assert.False(entry.IsInterleaved);
        Assert.False(entry.IsRawPassthrough);
        Assert.NotNull(entry.Extents);
        Assert.Empty(entry.Extents);
    }

    [Fact]
    public void FileEntryCanSetProperties()
    {
        var entry = new FileEntry
        {
            Name = "game.iso",
            FullPath = "/game.iso",
            Lba = 200,
            Size = 1024000,
            Offset = 100,
            IsDirectory = false,
            FileNumber = 2,
            IsInterleaved = true,
            IsRawPassthrough = false
        };

        Assert.Equal("game.iso", entry.Name);
        Assert.Equal("/game.iso", entry.FullPath);
        Assert.Equal(200u, entry.Lba);
        Assert.Equal(1024000ul, entry.Size);
        Assert.Equal(100ul, entry.Offset);
        Assert.False(entry.IsDirectory);
        Assert.Equal(2, entry.FileNumber);
        Assert.True(entry.IsInterleaved);
        Assert.False(entry.IsRawPassthrough);
    }

    [Fact]
    public void FileEntryModifiedTimeHasValue()
    {
        var entry = new FileEntry();
        Assert.Equal(DateTime.MinValue, entry.ModifiedTime);
    }

    [Fact]
    public void FileEntryExtentsCanBeAdded()
    {
        var entry = new FileEntry();
        var extent = new FileExtent { Lba = 42, Size = 1024 };
        entry.Extents.Add(extent);
        Assert.Single(entry.Extents);
        Assert.Equal(42u, entry.Extents[0].Lba);
        Assert.Equal(1024ul, entry.Extents[0].Size);
    }
}

public class TrackInfoTests
{
    [Fact]
    public void NewTrackInfoHasDefaultValues()
    {
        var track = new TrackInfo();
        Assert.Equal(0, track.Index);
        Assert.Equal(0u, track.StartLba);
        Assert.Equal(0u, track.ChdOffset);
        Assert.Equal(0u, track.Frames);
        Assert.Equal("", track.TrackType);
        Assert.False(track.IsDataTrack);
        Assert.Equal(0u, track.Pregap);
        Assert.Equal(0u, track.Postgap);
        Assert.Equal("", track.Metadata);
    }

    [Fact]
    public void TrackInfoCanSetProperties()
    {
        var track = new TrackInfo
        {
            Index = 1,
            StartLba = 100,
            ChdOffset = 200,
            Frames = 1000,
            TrackType = "MODE2_RAW",
            IsDataTrack = true,
            Pregap = 150,
            Postgap = 225,
            Metadata = "test metadata"
        };

        Assert.Equal(1, track.Index);
        Assert.Equal(100u, track.StartLba);
        Assert.Equal(200u, track.ChdOffset);
        Assert.Equal(1000u, track.Frames);
        Assert.Equal("MODE2_RAW", track.TrackType);
        Assert.True(track.IsDataTrack);
        Assert.Equal(150u, track.Pregap);
        Assert.Equal(225u, track.Postgap);
        Assert.Equal("test metadata", track.Metadata);
    }

    [Fact]
    public void TrackInfoIsNotDataTrackForAudioTracks()
    {
        var track = new TrackInfo
        {
            TrackType = "AUDIO",
            IsDataTrack = false
        };
        Assert.False(track.IsDataTrack);
        Assert.Equal("AUDIO", track.TrackType);
    }
}

public class LogEntryTests
{
    [Fact]
    public void NewLogEntryHasDefaultValues()
    {
        var entry = new LogEntry();
        Assert.Equal("", entry.Message);
        Assert.False(entry.IsError);
    }

    [Fact]
    public void LogEntryTimestampIsSetOnCreation()
    {
        var entry = new LogEntry();
        Assert.True(entry.Timestamp <= DateTime.Now);
        Assert.True(entry.Timestamp > DateTime.Now.AddSeconds(-5));
    }

    [Fact]
    public void LogEntryCanSetProperties()
    {
        var timestamp = DateTime.Now;
        var entry = new LogEntry
        {
            Timestamp = timestamp,
            Message = "Test message",
            IsError = true
        };
        Assert.Equal(timestamp, entry.Timestamp);
        Assert.Equal("Test message", entry.Message);
        Assert.True(entry.IsError);
    }
}

public class ConsoleInfoTests
{
    [Fact]
    public void ConsoleInfoRecordStoresValues()
    {
        var info = new ConsoleInfo(ConsoleType.Xbox, "Xbox");
        Assert.Equal(ConsoleType.Xbox, info.Type);
        Assert.Equal("Xbox", info.Name);
    }

    [Fact]
    public void ConsoleInfoValueEqualityWorks()
    {
        var a = new ConsoleInfo(ConsoleType.Ps1, "PS1");
        var b = new ConsoleInfo(ConsoleType.Ps1, "PS1");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ConsoleInfoDifferentValuesAreNotEqual()
    {
        var a = new ConsoleInfo(ConsoleType.Ps1, "PS1");
        var b = new ConsoleInfo(ConsoleType.Ps2, "PS2");
        Assert.NotEqual(a, b);
    }
}