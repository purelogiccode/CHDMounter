using CHDSharp;
using CHDSharp.Models;
using VideoGameFileSystemParser.Parsers;
using VideoGameFileSystemParser.Parsers.Systems;
using Xunit.Abstractions;

namespace CHDMounter.Core.Tests.Parsers;

public class PcFxDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public PcFxDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static TheoryData<string, string> PcFxCompareChds => new()
    {
        { @"G:\MAME\MAME Software List CHDs\pcfx\dknight4\dragon knight 4 (japan).chd", "FAILING - MAME DK4" },
        { @"G:\NEC PC-FX\Dragon Knight 4 (Japan).chd", "FAILING - NEC DK4" },
        { @"G:\MAME\MAME Software List CHDs\pcfx\batlheat\battle heat (japan).chd", "FAILING - MAME BattleHeat" },
        { @"G:\NEC PC-FX\Battle Heat (Japan).chd", "FAILING - NEC BattleHeat" },
        { @"G:\MAME\MAME Software List CHDs\pcfx\farland\farland story fx (japan).chd", "FAILING - MAME Farland" },
        { @"G:\NEC PC-FX\AnimeFreak FX Vol. 1 (Japan).chd", "FAILING - AnimeFreak" },
        { @"G:\NEC PC-FX\Pia Carrot e Youkoso! We've Been Waiting for You (Japan).chd", "PASSING - Pia Carrot" },
        { @"G:\NEC PC-FX\Sotsugyou II FX - Neo Generation (Japan) (SABS, SACS).chd", "PASSING - Sotsugyou" },
        { @"G:\NEC PC-FX\Super PC Engine Fan Deluxe - Special CD-ROM Vol. 1 (Japan).chd", "PASSING - Super PCEFan" },
        { @"G:\NEC PC-FX\Team Innocent - The Point of No Return - G.C.P.O.SS (Japan).chd", "PASSING - Team Innocent" }
    };

    [Theory]
    [MemberData(nameof(PcFxCompareChds))]
    public void DiagnoseChdMetadata(string chdPath, string label)
    {
        if (!File.Exists(chdPath))
        {
            _output.WriteLine($"SKIP: {chdPath} not found");
            return;
        }

        var err = ChdFile.Open(chdPath, out var chd);
        Assert.Equal(ChdError.Chderrnone, err);
        Assert.NotNull(chd);

        try
        {
            _output.WriteLine($"=== {label} ===");
            _output.WriteLine($"  CHD: hunkSize={chd.HunkBytes}, unitBytes={chd.UnitBytes}");

            foreach (var meta in chd.Metadata)
                _output.WriteLine($"  Meta: tag='{meta.Tag}' text='{meta.GetText()}'");

            // ---- Test 1: Generic Iso9660Parser ----
            {
                var reader = new SectorReader(chd, chd.UnitBytes);
                foreach (var t in reader.Tracks)
                    _output.WriteLine(
                        $"  Track[{t.Index}]: Type='{t.TrackType}' IsData={t.IsDataTrack} Frames={t.Frames} StartLba={t.StartLba} ChdOffset={t.ChdOffset} Pregap={t.Pregap}");

                var track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack) ?? reader.Tracks.FirstOrDefault();
                if (track == null)
                {
                    _output.WriteLine("  NO TRACK!");
                    goto testPcfx;
                }

                _output.WriteLine(
                    $"  Using track: Index={track.Index} StartLba={track.StartLba} Frames={track.Frames}");

                var root = new FsNode();
                var parser = new Iso9660Parser(reader);
                var ok = parser.Parse(root, track);
                _output.WriteLine(
                    $"  Iso9660Parser: {(ok ? "OK" : "FAILED")} SectorHeaderOffset={reader.SectorHeaderOffset} SyncOffset={reader.SyncOffset}");

                if (ok)
                {
                    int files = 0, dirs = 0;
                    ulong maxSize = 0;
                    Walk(root, ref files, ref dirs, ref maxSize);
                    _output.WriteLine($"  Iso9660Parser tree: {files} files, {dirs} dirs");
                }
                else
                {
                    DumpSectorScan(chd, track);
                }
            }

            // ---- Test 2: Dedicated PcFxIsoParser ----
            testPcfx:
            {
                var reader2 = new SectorReader(chd, chd.UnitBytes);
                foreach (var t in reader2.Tracks)
                    _output.WriteLine(
                        $"  Track[{t.Index}]: Type='{t.TrackType}' IsData={t.IsDataTrack} Frames={t.Frames} StartLba={t.StartLba} ChdOffset={t.ChdOffset} Pregap={t.Pregap}");

                var track2 = reader2.Tracks.FirstOrDefault(static t => t.IsDataTrack) ??
                             reader2.Tracks.FirstOrDefault();
                if (track2 == null)
                {
                    _output.WriteLine("  PcFxIso: NO TRACK!");
                    return;
                }

                _output.WriteLine(
                    $"  PcFxIso using track: Index={track2.Index} StartLba={track2.StartLba} Frames={track2.Frames}");

                var root2 = new FsNode();
                var pcfxParser = new PcFxIsoParser(reader2);
                var ok2 = pcfxParser.Parse(root2, track2);
                _output.WriteLine(
                    $"  PcFxIsoParser: {(ok2 ? "OK" : "FAILED")} SectorHeaderOffset={reader2.SectorHeaderOffset} SyncOffset={reader2.SyncOffset}");

                if (ok2)
                {
                    int files = 0, dirs = 0;
                    ulong maxSize = 0;
                    Walk(root2, ref files, ref dirs, ref maxSize);
                    _output.WriteLine($"  PcFxIsoParser tree: {files} files, {dirs} dirs");
                }
                else
                {
                    DumpSectorOffsets(chd, track2);
                }
            }
        }
        finally
        {
            chd.Dispose();
        }
    }

    private void DumpSectorScan(ChdFile chd, TrackInfo track)
    {
        var cd001 = "CD001"u8.ToArray();
        var sectorsPerHunk = chd.HunkBytes / chd.UnitBytes;
        uint found = 0;
        var hunkBuf = new byte[chd.HunkBytes];
        var lastHunk = 0xFFFFFFFF;
        var endFrame = track.ChdOffset + Math.Min(track.Frames, 500u);

        _output.WriteLine($"  Scanning sectors {track.ChdOffset}..{endFrame - 1} for CD001/CDROM...");
        for (var frame = track.ChdOffset; frame < endFrame && found < 5; frame++)
        {
            var h = frame / sectorsPerHunk;
            var s = frame % sectorsPerHunk;
            if (h != lastHunk)
            {
                if (chd.ReadHunk(h, hunkBuf) != ChdError.Chderrnone) continue;

                lastHunk = h;
            }

            var secOff = (int)(s * chd.UnitBytes);
            if (secOff + 16 > hunkBuf.Length) continue;

            if (chd.UnitBytes >= 2352)
            {
                // raw sector - check at offset 16 (mode 1 data start) and offset 24 (mode 2 data start)
                for (var off = 0; off < 120; off++)
                {
                    if (secOff + off + 16 > hunkBuf.Length) break;

                    var rawOk = true;
                    for (var j = 0; j < 5; j++)
                        if (hunkBuf[secOff + off + 1 + j] != cd001[j])
                        {
                            rawOk = false;
                            break;
                        }

                    if (rawOk)
                    {
                        var msf =
                            $"{hunkBuf[secOff + off + 12]:X2}:{hunkBuf[secOff + off + 13]:X2}:{hunkBuf[secOff + off + 14]:X2}";
                        _output.WriteLine(
                            $"    RAW CD001 at frame={frame} offsetInSector={off} MSF={msf} typeByte={hunkBuf[secOff + off]:X2}");
                        found++;
                        if (found >= 3) break;
                    }
                }
            }
            else
            {
                // cooked sector - check at offset 1
                var rawOk = true;
                for (var j = 0; j < 5; j++)
                    if (hunkBuf[secOff + 1 + j] != cd001[j])
                    {
                        rawOk = false;
                        break;
                    }

                if (rawOk)
                {
                    _output.WriteLine($"    COOKED CD001 at frame={frame} typeByte={hunkBuf[secOff]:X2}");
                    found++;
                }
            }
        }

        if (found == 0) _output.WriteLine("    CD001 NOT FOUND in first 500 frames");
    }

    private void DumpSectorOffsets(ChdFile chd, TrackInfo track)
    {
        var cd001 = "CD001"u8.ToArray();
        var cdrom = "CDROM"u8.ToArray();
        var sectorsPerHunk = chd.HunkBytes / chd.UnitBytes;
        uint found = 0;
        var hunkBuf = new byte[chd.HunkBytes];
        var endFrame = track.ChdOffset + Math.Min(track.Frames, 500u);

        _output.WriteLine($"  PcFxIso scan sectors {track.ChdOffset}..{endFrame - 1} for PVD...");

        // Simulate what PcFxIsoParser does: check sectors at trackStart + [16, 17, 166, 167]
        // but with correct sector header offset applied
        var trackStartLba = track.StartLba;
        var dataOffsets = track.IsDataTrack ? chd.UnitBytes >= 2352 ? new uint[] { 16, 24 } : new uint[] { 0 } : [0];

        foreach (var vdOffset in new uint[] { 16, 17, 166, 167 })
        {
            var lba = trackStartLba + vdOffset;
            var rel = (long)lba - trackStartLba;
            if (rel < 0 || rel >= track.Frames) continue;

            var frame = track.ChdOffset + (uint)rel;
            var h = frame / sectorsPerHunk;
            var s = frame % sectorsPerHunk;
            if (chd.ReadHunk(h, hunkBuf) != ChdError.Chderrnone) continue;

            foreach (var dataOff in dataOffsets)
            {
                var secOff = (int)(s * chd.UnitBytes + dataOff);
                if (secOff + 2048 > hunkBuf.Length) continue;

                var rawMatch = true;
                for (var j = 0; j < 5; j++)
                    if (hunkBuf[secOff + 1 + j] != cd001[j])
                    {
                        rawMatch = false;
                        break;
                    }

                var hsMatch = true;
                for (var j = 0; j < 5; j++)
                    if (hunkBuf[secOff + 9 + j] != cdrom[j])
                    {
                        hsMatch = false;
                        break;
                    }

                if (rawMatch || hsMatch)
                {
                    var typeByte = hunkBuf[secOff];
                    var volChars = new char[32];
                    for (var j = 0; j < 32 && secOff + 40 + j < hunkBuf.Length; j++)
                    {
                        var b = hunkBuf[secOff + 40 + j];
                        volChars[j] = b is >= 0x20 and < 0x7F ? (char)b : '.';
                    }

                    var volId = new string(volChars).Trim();

                    _output.WriteLine(
                        $"    FOUND {(rawMatch ? "CD001" : "CDROM")} at LBA={lba} dataOff={dataOff} typeByte={typeByte:X2} volId='{volId}'");
                    found++;
                }

                // Also try byte-offset scanning within cooked data
                for (var i = 0; i < 2048 - 16; i++)
                {
                    if (hunkBuf[secOff + i + 1] == cd001[0] &&
                        hunkBuf[secOff + i + 2] == cd001[1] &&
                        hunkBuf[secOff + i + 3] == cd001[2] &&
                        hunkBuf[secOff + i + 4] == cd001[3] &&
                        hunkBuf[secOff + i + 5] == cd001[4])
                    {
                        _output.WriteLine(
                            $"    BYTE-OFFSET CD001 at LBA={lba} dataOff={dataOff} byteOfs={i} typeByte={hunkBuf[secOff + i]:X2}");
                        found++;
                    }

                    if (hunkBuf.Length > secOff + i + 13 &&
                        hunkBuf[secOff + i + 9] == cdrom[0] &&
                        hunkBuf[secOff + i + 10] == cdrom[1] &&
                        hunkBuf[secOff + i + 11] == cdrom[2] &&
                        hunkBuf[secOff + i + 12] == cdrom[3] &&
                        hunkBuf[secOff + i + 13] == cdrom[4])
                        _output.WriteLine($"    BYTE-OFFSET CDROM at LBA={lba} dataOff={dataOff} byteOfs={i}");
                }
            }
        }

        if (found == 0) _output.WriteLine("    NO PVD found at any expected location");

        // Raw hex dump of key sectors: pregap start, PVD location
        _output.WriteLine("  Raw hex dump at pregap boundaries:");
        var dumpFrames = new List<uint>();
        foreach (var rel in new uint[] { 0, 16, track.Pregap, track.Pregap + 16, track.Pregap + 17 })
            if (track.ChdOffset + rel < endFrame)
                dumpFrames.Add(track.ChdOffset + rel);

        foreach (var frame in dumpFrames)
        {
            var h = frame / sectorsPerHunk;
            var s = frame % sectorsPerHunk;
            if (chd.ReadHunk(h, hunkBuf) != ChdError.Chderrnone) continue;

            var baseOff = (int)(s * chd.UnitBytes);
            var maxLen = Math.Min(256, hunkBuf.Length - baseOff);
            if (maxLen <= 0 || baseOff + maxLen > hunkBuf.Length) continue;

            var hex = Convert.ToHexString(hunkBuf, baseOff, maxLen);
            var modeByte = baseOff + 15 < hunkBuf.Length ? hunkBuf[baseOff + 15] : (byte)0;
            _output.WriteLine($"    Frame={frame} rel={frame - track.ChdOffset} hunk={h} mode={modeByte:X2}:");
            for (var row = 0; row < Math.Min(8, (maxLen + 15) / 16); row++)
            {
                var rowOff = row * 16;
                var rowHex = hex.Substring(rowOff * 2, Math.Min(32, (maxLen - rowOff) * 2));
                _output.WriteLine($"      {rowOff:X3}: {rowHex}");
            }
        }

        // Try reading several sectors directly with ReadSector to test offset detection
        _output.WriteLine("  Testing ReadSector at LBAs 0, 16, 166, trackStart+0, trackStart+16:");
        var reader = new SectorReader(chd, chd.UnitBytes);
        reader.SetTrack(track, true);
        foreach (var testLba in new uint[] { 0, 16, 166, track.StartLba, track.StartLba + 16 })
        {
            var buf = new byte[2048];
            if (reader.ReadSector(testLba, buf))
            {
                var iso = buf[1] == 'C' && buf[2] == 'D' && buf[3] == '0' && buf[4] == '0' && buf[5] == '1';
                _output.WriteLine(
                    $"    ReadSector(lba={testLba}) OK isoCD001={iso} firstBytes={buf[0]:X2} {buf[1]:X2} {buf[2]:X2} {buf[3]:X2} {buf[4]:X2} {buf[5]:X2}");
            }
            else
            {
                _output.WriteLine($"    ReadSector(lba={testLba}) FAILED");
            }
        }
    }

    private static void Walk(FsNode node, ref int files, ref int dirs, ref ulong maxSize)
    {
        foreach (var c in node.Children)
            if (c.IsDirectory)
            {
                dirs++;
                Walk(c, ref files, ref dirs, ref maxSize);
            }
            else
            {
                files++;
                if (c.Size > maxSize) maxSize = c.Size;
            }
    }
}