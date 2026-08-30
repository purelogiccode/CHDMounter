using System.Text;
using CHDSharp;
using CHDSharp.Models;
using VideoGameFileSystemParser.Parsers;
using Xunit.Abstractions;

namespace CHDMounter.Core.Tests.Parsers;

public class CDiDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public CDiDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DiagnosticCdiReadyDisc()
    {
        const string path = @"G:\MAME\MAME Software List CHDs\cdi\aliengat\alien gate (us, set 1)(cdi-ready).chd";
        if (!File.Exists(path))
            return;

        var err = ChdFile.Open(path, out var chd);
        Assert.Equal(ChdError.Chderrnone, err);
        Assert.NotNull(chd);

        try
        {
            _output.WriteLine($"UnitBytes={chd.UnitBytes} HunkBytes={chd.HunkBytes}");

            var reader = new SectorReader(chd, chd.UnitBytes);
            Assert.NotEmpty(reader.Tracks);

            foreach (var t in reader.Tracks)
                _output.WriteLine(
                    $"  Track {t.Index}: {t.TrackType} data={t.IsDataTrack} frames={t.Frames} pregap={t.Pregap} startLba={t.StartLba} chdOffset={t.ChdOffset}");

            reader.Reset();

            var rawReadCount = 0;
            for (uint lba = 0; lba < 26; lba++)
                if (reader.ReadRawSector(lba, out var raw))
                {
                    rawReadCount++;
                    Assert.NotNull(raw);
                    Assert.True(raw.Length > 0, $"Raw sector at LBA {lba} should not be empty");
                }

            _output.WriteLine($"Raw sectors read: {rawReadCount}/26");
            Assert.True(rawReadCount > 0, "Should be able to read at least some raw sectors");

            var buf = new byte[2048];
            var cookedReadCount = 0;
            for (uint lba = 0; lba < 26; lba++)
                if (reader.ReadSector(lba, buf))
                {
                    cookedReadCount++;
                    var sig1 = Encoding.ASCII.GetString(buf, 1, 5);
                    _output.WriteLine($"  LBA={lba,3}: type={buf[0]:X2} sig1='{sig1}'");
                }

            _output.WriteLine($"Cooked sectors read: {cookedReadCount}/26");

            reader.Reset();
            var dataTrack = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack);
            if (dataTrack != null)
            {
                reader.SetTrack(dataTrack, true);
                Assert.True(reader.ReadSector(0, buf), "Locked read at LBA 0 should succeed");
                Assert.True(reader.ReadSector(150, buf), "Locked read at LBA 150 should succeed");
            }
            else
            {
                _output.WriteLine("No data track found (audio-only CDi-ready disc)");
            }
        }
        finally
        {
            chd.Dispose();
        }
    }

    [Fact]
    public void DiagnosticCdiWithIsoFallbackDiscs()
    {
        var paths = new[]
        {
            @"G:\MAME\MAME Software List CHDs\cdi\asspres2\from the associated press - the best of photo journalism (1993)[dvc].chd",
            @"G:\MAME\MAME Software List CHDs\cdi\photodem\photo cd demo disc v3.0 (1993)(philips)(eu)[1993-03].chd",
            @"G:\MAME\MAME Software List CHDs\cdi\pcd1904\pcd1904.chd"
        };

        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;

            _output.WriteLine($"=== {Path.GetFileName(path)} ===");
            var err = ChdFile.Open(path, out var chd);
            if (err != ChdError.Chderrnone || chd is null) continue;

            try
            {
                _output.WriteLine($"UnitBytes={chd.UnitBytes} HunkBytes={chd.HunkBytes}");

                var reader = new SectorReader(chd, chd.UnitBytes);
                _output.WriteLine($"Tracks: {reader.Tracks.Count}");

                foreach (var t in reader.Tracks)
                    _output.WriteLine(
                        $"  Track {t.Index}: {t.TrackType} data={t.IsDataTrack} frames={t.Frames} pregap={t.Pregap} startLba={t.StartLba} chdOffset={t.ChdOffset}");

                reader.Reset();
                var dataTrack = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack);
                if (dataTrack == null)
                {
                    _output.WriteLine("  No data track found!");
                    continue;
                }

                reader.SetTrack(dataTrack, true);

                for (uint offset = 0; offset < Math.Min(200u, dataTrack.Frames); offset++)
                {
                    var lba = dataTrack.StartLba + offset;
                    var buf = new byte[2048];
                    if (!reader.ReadSector(lba, buf)) continue;

                    var sig1 = Encoding.ASCII.GetString(buf, 1, 5);
                    var sig2 = Encoding.ASCII.GetString(buf, 0, 5);
                    if (offset < 30 || sig1 is "CD-I " or "CD001" || sig2 is "CD001")
                        _output.WriteLine(
                            $"  LBA={lba} (offset {offset}): type={buf[0]:X2} sig1='{sig1}' sig2='{sig2}'");
                }
            }
            finally
            {
                chd.Dispose();
            }
        }
    }

    [Fact]
    public void DiagnosticMusicCdiDiscs()
    {
        var paths = new[]
        {
            @"I:\Philips CD-i\Pavarotti - O Sole Mio (USA).chd",
            @"I:\Philips CD-i\James Brown - Non Stop Hit Machine (USA).chd"
        };

        var anyTested = false;

        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;

            anyTested = true;

            _output.WriteLine($"=== {Path.GetFileName(path)} ===");
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                _output.WriteLine($"UnitBytes={chd.UnitBytes} HunkBytes={chd.HunkBytes}");

                var reader = new SectorReader(chd, chd.UnitBytes);
                Assert.NotEmpty(reader.Tracks);

                foreach (var t in reader.Tracks)
                    _output.WriteLine(
                        $"  Track {t.Index}: {t.TrackType} data={t.IsDataTrack} frames={t.Frames} pregap={t.Pregap} startLba={t.StartLba} chdOffset={t.ChdOffset}");

                reader.Reset();
                var dataTrack = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack);
                if (dataTrack is null)
                {
                    _output.WriteLine("No data track found (audio-only disc)");
                    continue;
                }

                _output.WriteLine(
                    $"Data track: {dataTrack.Index} type={dataTrack.TrackType} startLba={dataTrack.StartLba} frames={dataTrack.Frames} pregap={dataTrack.Pregap}");

                var rawReadCount = 0;
                for (uint lba = 0; lba < 30; lba++)
                    if (reader.ReadRawSector(lba, out var raw))
                    {
                        rawReadCount++;
                        Assert.NotNull(raw);
                    }

                _output.WriteLine($"Raw sectors read: {rawReadCount}/30");
                Assert.True(rawReadCount > 0, "Should be able to read at least some raw sectors");

                reader.Reset();
                reader.SetTrack(dataTrack, true);
                var buf = new byte[2048];
                var cookedReadCount = 0;

                for (uint offset = 0; offset < 30; offset++)
                {
                    var lba = dataTrack.StartLba + offset;
                    if (reader.ReadSector(lba, buf))
                    {
                        cookedReadCount++;
                        var sig1 = Encoding.ASCII.GetString(buf, 1, 5);
                        _output.WriteLine($"  LBA={lba} off={offset}: type={buf[0]:X2} sig1='{sig1}'");
                    }
                }

                _output.WriteLine($"Cooked sectors read: {cookedReadCount}/30");

                reader.Reset();
                var root = new FsNode();
                var parser = new CDiFsParser(reader);
                var ok = parser.Parse(root, dataTrack);
                _output.WriteLine($"CDiFsParser result: {ok}, children: {root.Children.Count}");
                Assert.True(ok, "CDiFsParser should parse the data track successfully");
                Assert.NotEmpty(root.Children);
            }
            finally
            {
                chd.Dispose();
            }
        }

        if (!anyTested)
            _output.WriteLine("No music CDi CHD files found at expected paths — test skipped");
    }
}