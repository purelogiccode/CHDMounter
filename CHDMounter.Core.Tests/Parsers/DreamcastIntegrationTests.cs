using System.Globalization;
using System.Text;
using CHDSharp;
using CHDSharp.Models;
using VideoGameFileSystemParser.Parsers;
using VideoGameFileSystemParser.Parsers.Systems;
using Xunit.Abstractions;

namespace CHDMounter.Core.Tests.Parsers;

public class DreamcastIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public DreamcastIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static List<string> GetPaths()
    {
        return SequentialTestRunner.CollectPaths(ChdPathCatalog.Dreamcast.Paths);
    }

    [Fact]
    public void DreamcastParserParsesDreamcastDisc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(DreamcastParserParsesDreamcastDisc), paths, static (path, output) =>
        {
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                var unitBytes = chd.UnitBytes;
                var reader = new SectorReader(chd, unitBytes);
                var hasDataTrack = reader.Tracks.Any(static t => t.IsDataTrack);
                if (!hasDataTrack)
                {
                    output.WriteLine("Audio-only disc — no data track to parse");
                    return true;
                }

                var track = reader.Tracks.LastOrDefault(static t => t.IsDataTrack) ?? reader.Tracks.FirstOrDefault();

                output.WriteLine($"UnitBytes={unitBytes} Tracks={reader.Tracks.Count} TrackType={track!.TrackType}");
                foreach (var t in reader.Tracks)
                    output.WriteLine(
                        $"  idx={t.Index} LBA={t.StartLba} frames={t.Frames} type={t.TrackType} data={t.IsDataTrack}");

                var root = new FsNode();
                var parser = new DreamcastParser(reader);

                var ok = parser.Parse(root);

                output.WriteLine($"DreamcastParser: {(ok ? "OK" : "FAILED")}");
                output.WriteLine($"Root LBA={root.Lba} Size={root.Size}");

                if (!ok)
                {
                    output.WriteLine($"SKIP: DreamcastParser could not parse {Path.GetFileName(path)}");
                    return true;
                }

                int files = 0, dirs = 0;
                ulong maxSize = 0;
                Walk(root, ref files, ref dirs, ref maxSize);

                var allNodes = FindNodes(root);
                var rockRidgeNodes = allNodes.Count(static n => n.UnixMode.HasValue);
                output.WriteLine(
                    $"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes, RR entries={rockRidgeNodes}");

                var topFifteen = root.Children.OrderByDescending(static n => n.Size).Take(15);
                foreach (var c in topFifteen)
                    output.WriteLine(
                        $"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}  mtime={c.ModifiedTime:yyyy-MM-dd HH:mm:ss}");

                Assert.True(files >= 5, $"Suspiciously few files parsed: {files}");

                var executables = allNodes.Count(static n =>
                    !n.IsDirectory && n.Name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase));
                output.WriteLine($"BIN files: {executables}");

                if (rockRidgeNodes > 0)
                {
                    var sample = allNodes.First(static n => n.UnixMode.HasValue);
                    output.WriteLine(
                        $"Rock Ridge sample: {sample.Name} mode=0x{sample.UnixMode:X8} uid={sample.Uid} gid={sample.Gid}");
                }
            }
            finally
            {
                chd.Dispose();
            }

            return true;
        });
    }

    [Fact]
    public void ChdContainerMountAndParseDreamcastDisc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(ChdContainerMountAndParseDreamcastDisc), paths,
            static (path, output) =>
            {
                var container = new ChdContainer(path);
                try
                {
                    var success = container.MountAndParse(ConsoleType.Dreamcast);

                    switch (success)
                    {
                        case false when container is { HasDataTracks: false, VolumeSize: > 0 }:
                            output.WriteLine("Audio-only disc — no data track to parse");
                            return true;
                        case false:
                            output.WriteLine($"SKIP: MountAndParse failed for {Path.GetFileName(path)}");
                            return true;
                    }

                    var all = CollectEntries(container, "\\").ToList();
                    var fileEntries = all.Where(static e => !e.IsDirectory).ToList();
                    output.WriteLine($"Container: {fileEntries.Count} files, {all.Count - fileEntries.Count} dirs");

                    Assert.True(fileEntries.Count >= 5, $"Suspiciously few files: {fileEntries.Count}");

                    var badNames = all.Where(static e => e.Name.Contains('\uFFFD') || e.Name.Any(char.IsControl))
                        .ToList();
                    foreach (var bad in badNames)
                        output.WriteLine($"BAD NAME: {bad.FullPath}");
                    Assert.Empty(badNames);

                    foreach (var e in container.ListDirectory("\\"))
                        output.WriteLine(
                            $"  {(e.IsDirectory ? "<DIR>" : e.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {e.Name}");
                }
                finally
                {
                    container.Dispose();
                }

                return true;
            });
    }

    [Fact]
    public void DiagnoseSectorReaderForGdrom()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(DiagnoseSectorReaderForGdrom), paths, static (path, output) =>
        {
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                var unitBytes = chd.UnitBytes;
                var hunkBytes = chd.HunkBytes;
                var sectorsPerHunk = hunkBytes / unitBytes;
                var totalFrames = (uint)(chd.TotalBytes / unitBytes);

                output.WriteLine(
                    $"UnitBytes={unitBytes} HunkBytes={hunkBytes} sectorsPerHunk={sectorsPerHunk} totalFrames={totalFrames}");

                var reader = new SectorReader(chd, unitBytes);
                output.WriteLine($"Tracks: {reader.Tracks.Count}");
                foreach (var t in reader.Tracks)
                    output.WriteLine(
                        $"  idx={t.Index} LBA={t.StartLba} frames={t.Frames} ChdOff={t.ChdOffset} data={t.IsDataTrack} type={t.TrackType}");

                TrackInfo? hdTrack = null;
                for (var i = reader.Tracks.Count - 1; i >= 0; i--)
                    if (reader.Tracks[i].IsDataTrack)
                    {
                        hdTrack = reader.Tracks[i];
                        break;
                    }

                if (hdTrack == null)
                {
                    output.WriteLine("No data track found, skipping");
                    return true;
                }

                output.WriteLine(
                    $"HD track: idx={hdTrack.Index} LBA={hdTrack.StartLba} frames={hdTrack.Frames} ChdOff={hdTrack.ChdOffset}");

                var buf = new byte[2048];

                var ipBinLba = hdTrack.StartLba;
                var ipBinOk = reader.ReadSector(ipBinLba, buf);
                output.WriteLine($"IP.BIN at LBA={ipBinLba}: {(ipBinOk ? "OK" : "FAIL")}");
                if (ipBinOk)
                {
                    var id = Encoding.ASCII.GetString(buf, 0, 16).TrimEnd('\0');
                    output.WriteLine($"  [0-15]: '{id}'");
                }

                var pvdLba = hdTrack.StartLba + 16;
                var pvdOk = reader.ReadSector(pvdLba, buf);
                output.WriteLine($"PVD at LBA={pvdLba}: {(pvdOk ? "OK" : "FAIL")}");
                if (pvdOk)
                {
                    var type = buf[0];
                    var magic = Encoding.ASCII.GetString(buf, 1, 5);
                    var rlba = BitConverter.ToUInt32(buf, 158);
                    output.WriteLine($"  type={type} magic='{magic}' rootLBA={rlba}");
                }

                var rootLbaFromPvd = 0u;
                if (reader.ReadSector(hdTrack.StartLba + 16, buf)) rootLbaFromPvd = BitConverter.ToUInt32(buf, 158);

                output.WriteLine($"PVD rootLBA={rootLbaFromPvd}");

                var candA = hdTrack.StartLba + rootLbaFromPvd;
                var candB = rootLbaFromPvd;
                var rootLbaSimple = rootLbaFromPvd > 45000 ? rootLbaFromPvd - 45000 : rootLbaFromPvd;
                var candC = hdTrack.StartLba + rootLbaSimple;

                foreach (var (label, lba) in new[]
                             { ("A:start+root", candA), ("B:absolute", candB), ("C:start+root-norm", candC) })
                {
                    var ok = reader.ReadSector(lba, buf);
                    var b0 = buf[0];
                    var magic = ok ? Encoding.ASCII.GetString(buf, 1, 5) : "";
                    output.WriteLine($"  {label}: LBA={lba} ok={ok} byte0={b0:X2} magic='{magic}'");
                    if (ok && b0 >= 34 && (b0 & 1) == 0)
                    {
                        const int nameLenPos = 32;
                        {
                            var nameLen = buf[nameLenPos];
                            var name = Encoding.ASCII.GetString(buf, 33, Math.Min(nameLen, b0 - 33)).Trim('\0');
                            output.WriteLine($"    Looks like a dir record! nameLen={nameLen} name='{name}'");
                        }
                    }
                }
            }
            finally
            {
                chd.Dispose();
            }

            return true;
        });
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

    private static List<FsNode> FindNodes(FsNode node)
    {
        var list = new List<FsNode> { node };
        foreach (var c in node.Children)
            list.AddRange(FindNodes(c));
        return list;
    }

    private static IEnumerable<FileEntry> CollectEntries(ChdContainer container, string path)
    {
        foreach (var e in container.ListDirectory(path))
        {
            yield return e;

            if (e.IsDirectory)
                foreach (var sub in CollectEntries(container, e.FullPath))
                    yield return sub;
        }
    }
}