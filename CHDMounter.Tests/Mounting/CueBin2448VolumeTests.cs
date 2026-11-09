using CHDMounter.Mounting;
using CHDMounter.Services;
using CHDSharp.Encoder;
using VideoGameFileSystemParser.Models;

namespace CHDMounter.Tests.Mounting;

public sealed class CueBin2448VolumeTests : IDisposable
{
    private const int FrameBytes = 2448;
    private const int SectorBytes = 2352;
    private const int Frames = 10;

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(), "CHDMounter.Tests", Guid.NewGuid().ToString("N"));

    public CueBin2448VolumeTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch (IOException)
        {
            // A failed cleanup must not fail the test run.
        }
    }

    [Fact]
    public void TryCreateBuildsCueAnd2448ByteBinEntries()
    {
        var chdPath = CreateCdPlusGChd();

        Assert.True(CueBin2448Volume.TryCreate(chdPath, out var volume, out var error), error);
        using (volume)
        {
            Assert.NotNull(volume);
            Assert.Equal("disc", volume!.VolumeName);
            Assert.Equal((ulong)(Frames * FrameBytes), volume.VolumeSize);

            var bin = volume.FindFile("\\disc.bin");
            var cue = volume.FindFile("\\disc.cue");
            Assert.NotNull(bin);
            Assert.NotNull(cue);
            Assert.Equal((ulong)(Frames * FrameBytes), bin!.Size);

            var cueBytes = new byte[cue!.Size];
            var read = volume.ReadFile(cue, 0, cueBytes, 0, cueBytes.Length);
            Assert.Equal(cueBytes.Length, read);

            var cueText = System.Text.Encoding.ASCII.GetString(cueBytes);
            Assert.Contains("FILE \"disc.bin\" BINARY", cueText, StringComparison.Ordinal);
            Assert.Contains("TRACK 01 CDG", cueText, StringComparison.Ordinal);
            Assert.Contains("INDEX 01 00:00:00", cueText, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ReadFilePreservesAudioSamplesAndSubchannelBytes()
    {
        var chdPath = CreateCdPlusGChd();
        var expected = BuildSourceFrames();

        Assert.True(CueBin2448Volume.TryCreate(chdPath, out var volume, out var error), error);
        using (volume)
        {
            Assert.NotNull(volume);
            var bin = volume!.FindFile("\\disc.bin");
            Assert.NotNull(bin);

            var actual = new byte[FrameBytes];
            Assert.Equal(FrameBytes, volume.ReadFile(bin!, 0, actual, 0, actual.Length));

            // Audio sector data is little-endian in a BIN file; the subchannel tail is
            // stored verbatim and must not be byte-swapped with the audio samples.
            Assert.Equal(expected[..SectorBytes], actual[..SectorBytes]);
            Assert.Equal(expected[SectorBytes..FrameBytes], actual[SectorBytes..FrameBytes]);
        }
    }

    [Fact]
    public void ReadFileHandlesPartialReadsAcrossFrameBoundaries()
    {
        var chdPath = CreateCdPlusGChd();
        var expected = BuildSourceFrames();

        Assert.True(CueBin2448Volume.TryCreate(chdPath, out var volume, out var error), error);
        using (volume)
        {
            Assert.NotNull(volume);
            var bin = volume!.FindFile("\\disc.bin");
            Assert.NotNull(bin);

            const int offset = FrameBytes - 5;
            var buffer = new byte[10];
            var read = volume.ReadFile(bin!, offset, buffer, 0, buffer.Length);

            Assert.Equal(buffer.Length, read);
            Assert.Equal(expected[offset..(offset + buffer.Length)], buffer);
        }
    }

    [Fact]
    public void MountFormatRegistryResolvesCueBin2448Alias()
    {
        Assert.True(MountFormatRegistry.TryParse("cuebin2448", out var format));
        Assert.Equal(ConsoleType.GenericCueBin2352, format.ConsoleType);
        Assert.True(format.IncludeSubchannel);
        Assert.Equal(MountFormatRegistry.CueBin2448DisplayName, format.DisplayName);
    }

    [Fact]
    public void MountFormatRegistryKeepsStandardAliasesUnchanged()
    {
        Assert.True(MountFormatRegistry.TryParse("cuebin2352", out var format));
        Assert.Equal(ConsoleType.GenericCueBin2352, format.ConsoleType);
        Assert.False(format.IncludeSubchannel);
    }

    private string CreateCdPlusGChd()
    {
        var binPath = Path.Combine(_tempDirectory, "disc.bin");
        var cuePath = Path.Combine(_tempDirectory, "disc.cue");
        var chdPath = Path.Combine(_tempDirectory, "disc.chd");

        File.WriteAllBytes(binPath, BuildSourceFrames());
        File.WriteAllText(cuePath,
            $"FILE \"{Path.GetFileName(binPath)}\" BINARY\n  TRACK 01 AUDIO RW\n    INDEX 01 00:00:00\n");
        ChdEncoder.EncodeCd(cuePath, chdPath, codecTags: [CodecTags.None]);
        return chdPath;
    }

    private static byte[] BuildSourceFrames()
    {
        var data = new byte[Frames * FrameBytes];
        for (var frame = 0; frame < Frames; frame++)
        {
            data[frame * FrameBytes] = (byte)(frame + 1);
            data[frame * FrameBytes + 1] = (byte)(0xA0 + frame);
            data[frame * FrameBytes + 2] = (byte)(0x50 + frame);

            for (var i = 0; i < 96; i++)
                data[frame * FrameBytes + SectorBytes + i] = (byte)(0x10 + frame + i);
        }

        return data;
    }
}
