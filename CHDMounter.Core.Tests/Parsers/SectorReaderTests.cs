using System.Reflection;
using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Core.Tests.Parsers;

public class SectorReaderTests
{
    [Fact]
    public void GetSectorDataOffsetReturns16ForNullTrack()
    {
        Assert.Equal(16u, SectorReader.GetSectorDataOffset(null));
    }

    [Fact]
    public void GetSectorDataOffsetReturns0ForAudioTrack()
    {
        var track = new TrackInfo { IsDataTrack = false, TrackType = "AUDIO" };
        Assert.Equal(0u, SectorReader.GetSectorDataOffset(track));
    }

    [Fact]
    public void GetSectorDataOffsetReturns16ForMode1()
    {
        var track = new TrackInfo { IsDataTrack = true, TrackType = "MODE1/2352" };
        Assert.Equal(16u, SectorReader.GetSectorDataOffset(track));
    }

    [Fact]
    public void GetSectorDataOffsetReturns24ForMode2()
    {
        var track = new TrackInfo { IsDataTrack = true, TrackType = "MODE2/2352" };
        Assert.Equal(24u, SectorReader.GetSectorDataOffset(track));
    }

    [Fact]
    public void GetSectorDataOffsetReturns24ForMode2Raw()
    {
        var track = new TrackInfo { IsDataTrack = true, TrackType = "MODE2_RAW" };
        Assert.Equal(24u, SectorReader.GetSectorDataOffset(track));
    }

    [Fact]
    public void GetSectorDataOffsetReturns24ForCdi()
    {
        var track = new TrackInfo { IsDataTrack = true, TrackType = "CDI/2352" };
        Assert.Equal(24u, SectorReader.GetSectorDataOffset(track));
    }

    [Fact]
    public void GetSectorDataOffsetReturns16ForMode1Cooked()
    {
        var track = new TrackInfo { IsDataTrack = true, TrackType = "MODE1/2048" };
        Assert.Equal(16u, SectorReader.GetSectorDataOffset(track));
    }

    [Fact]
    public void GetSectorScrambleReturnsNonEmptySpan()
    {
        var scramble = SectorReader.GetSectorScramble();
        Assert.True(scramble.Length > 0);
    }

    [Fact]
    public void GetSectorScrambleReturnsExpectedLength()
    {
        var scramble = SectorReader.GetSectorScramble();
        Assert.Equal(2352, scramble.Length);
    }

    [Fact]
    public void GetSectorScrambleStartsWithZeros()
    {
        var scramble = SectorReader.GetSectorScramble();
        Assert.Equal(0, scramble[0]);
        Assert.Equal(0, scramble[1]);
        Assert.True(scramble[..12].ToArray().All(b => b == 0));
    }

    [Fact]
    public void BcdToByteConvertsCorrectly()
    {
        var method = typeof(SectorReader).GetMethod("BcdToByte", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        Assert.Equal((byte)0, method.Invoke(null, [(byte)0x00]));
        Assert.Equal((byte)1, method.Invoke(null, [(byte)0x01]));
        Assert.Equal((byte)9, method.Invoke(null, [(byte)0x09]));
        Assert.Equal((byte)10, method.Invoke(null, [(byte)0x10]));
        Assert.Equal((byte)12, method.Invoke(null, [(byte)0x12]));
        Assert.Equal((byte)45, method.Invoke(null, [(byte)0x45]));
        Assert.Equal((byte)59, method.Invoke(null, [(byte)0x59]));
        Assert.Equal((byte)99, method.Invoke(null, [(byte)0x99]));
    }
}