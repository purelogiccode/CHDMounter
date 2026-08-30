using System.Globalization;
using CHDSharp;
using CHDSharp.Models;
using VideoGameFileSystemParser.Parsers;
using VideoGameFileSystemParser.Parsers.Systems;
using Xunit.Abstractions;

namespace CHDMounter.Core.Tests.Parsers;

public class ThreeDoIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public ThreeDoIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static List<string> GetPaths()
    {
        return SequentialTestRunner.CollectPaths(ChdPathCatalog.ThreeDo.Paths);
    }

    [Fact]
    public void ThreeDoParserParsesThreeDoDisc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(ThreeDoParserParsesThreeDoDisc), paths, static (path, output) =>
        {
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                var unitBytes = chd.UnitBytes;
                var reader = new SectorReader(chd, unitBytes);
                output.WriteLine($"UnitBytes={unitBytes} Tracks={reader.Tracks.Count}");

                var root = new FsNode();
                var parser = new ThreeDoParser(reader);

                var track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack)
                            ?? reader.Tracks.FirstOrDefault()
                            ?? new TrackInfo();
                var ok = parser.Parse(root, track);
                output.WriteLine($"ThreeDoParser: {(ok ? "OK" : "FAILED")}");

                Assert.True(ok, "ThreeDoParser could not parse the disc");

                int files = 0, dirs = 0;
                ulong maxSize = 0;
                Walk(root, ref files, ref dirs, ref maxSize);
                output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes");

                Assert.True(files > 2, $"Suspiciously few files parsed: {files}");

                foreach (var c in root.Children.OrderByDescending(static n => n.Size).Take(15))
                    output.WriteLine(
                        $"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}");
            }
            finally
            {
                chd.Dispose();
            }

            return true;
        });
    }

    [Fact]
    public void ThreeDoConsoleParserParsesThreeDoDisc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(ThreeDoConsoleParserParsesThreeDoDisc), paths, static (path, output) =>
        {
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                var reader = new SectorReader(chd, chd.UnitBytes);
                output.WriteLine($"UnitBytes={chd.UnitBytes} Tracks={reader.Tracks.Count}");

                var root = new FsNode();
                var parser = new ThreeDoConsoleParser(reader);

                var ok = parser.Parse(root);
                output.WriteLine($"ThreeDoConsoleParser: {(ok ? "OK" : "FAILED")}");

                Assert.True(ok, "ThreeDoConsoleParser could not parse the disc");

                int files = 0, dirs = 0;
                ulong maxSize = 0;
                Walk(root, ref files, ref dirs, ref maxSize);
                output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes");

                Assert.True(files > 2, $"Suspiciously few files parsed: {files}");
            }
            finally
            {
                chd.Dispose();
            }

            return true;
        });
    }

    [Fact]
    public void ChdContainerMountAndParseThreeDoDisc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(ChdContainerMountAndParseThreeDoDisc), paths, static (path, output) =>
        {
            var container = new ChdContainer(path);
            try
            {
                Assert.True(container.MountAndParse(ConsoleType.ThreeDo), "MountAndParse failed");

                var all = CollectEntries(container, "\\").ToList();
                var fileEntries = all.Where(static e => !e.IsDirectory).ToList();
                output.WriteLine($"Container: {fileEntries.Count} files, {all.Count - fileEntries.Count} dirs");

                Assert.True(fileEntries.Count > 2, $"Suspiciously few files: {fileEntries.Count}");

                var badNames = all.Where(static e => e.Name.Contains('\uFFFD') || e.Name.Contains('\0')).ToList();
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
    public void BulkParseAllThreeDoDiscs()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(BulkParseAllThreeDoDiscs), paths, static (path, output) =>
        {
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                var reader = new SectorReader(chd, chd.UnitBytes);

                var root = new FsNode();
                var parser = new ThreeDoParser(reader);
                var track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack)
                            ?? reader.Tracks.FirstOrDefault()
                            ?? new TrackInfo();

                var ok = parser.Parse(root, track);
                var fileName = Path.GetFileName(path);

                int files = 0, dirs = 0;
                ulong maxSize = 0;
                Walk(root, ref files, ref dirs, ref maxSize);

                output.WriteLine(
                    $"[{(ok ? "OK" : "FAIL")}] {fileName}  UnitBytes={chd.UnitBytes}  Tracks={reader.Tracks.Count}  Files={files}  Dirs={dirs}  MaxFile={maxSize:N0}");

                if (ok) Assert.True(files > 2, $"Suspiciously few files parsed: {files}");
            }
            finally
            {
                chd.Dispose();
            }

            return true;
        });
    }

    [Fact]
    public void M2DiscPaserDiagnostic()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(M2DiscPaserDiagnostic), paths, static (path, output) =>
        {
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                var reader = new SectorReader(chd, chd.UnitBytes);
                var track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack) ?? reader.Tracks.FirstOrDefault();
                var fileName = Path.GetFileName(path);

                output.WriteLine($"--- {fileName} (UnitBytes={chd.UnitBytes}, Tracks={reader.Tracks.Count}) ---");

                bool ok3Do;
                int f3, d3;
                ulong m3;
                if (track is not null)
                {
                    ok3Do = TryThreeDo(reader, track, out f3, out d3, out m3);
                }
                else
                {
                    ok3Do = false;
                    f3 = 0;
                    d3 = 0;
                    m3 = 0;
                }

                output.WriteLine($"  ThreeDoParser: {(ok3Do ? $"OK ({f3} files, {d3} dirs, max={m3:N0})" : "FAIL")}");

                reader = new SectorReader(chd, chd.UnitBytes);
                var track2 = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack) ?? reader.Tracks.FirstOrDefault();
                bool okIso;
                int fi, di;
                if (track2 is not null)
                {
                    okIso = TryIso9660(reader, track2, out fi, out di);
                }
                else
                {
                    okIso = false;
                    fi = 0;
                    di = 0;
                }

                output.WriteLine($"  Iso9660Parser: {(okIso ? $"OK ({fi} files, {di} dirs)" : "FAIL")}");

                var okThreeDoCt = TryContainerMount(path, ConsoleType.ThreeDo, out var c3F, out var c3D);
                output.WriteLine($"  Container ThreeDo: {(okThreeDoCt ? $"OK ({c3F} files, {c3D} dirs)" : "FAIL")}");

                var okIsoCt = TryContainerMount(path, ConsoleType.GenericIso9660, out var cif, out var cid);
                output.WriteLine($"  Container ISO9660: {(okIsoCt ? $"OK ({cif} files, {cid} dirs)" : "FAIL")}");
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

    private static bool TryThreeDo(SectorReader reader, TrackInfo track, out int files, out int dirs, out ulong maxSize)
    {
        files = 0;
        dirs = 0;
        maxSize = 0;
        var root = new FsNode();
        var parser = new ThreeDoParser(reader);
        var ok = parser.Parse(root, track);
        if (ok) Walk(root, ref files, ref dirs, ref maxSize);
        return ok;
    }

    private static bool TryIso9660(SectorReader reader, TrackInfo track, out int files, out int dirs)
    {
        files = 0;
        dirs = 0;
        ulong maxSize = 0;
        var root = new FsNode();
        var parser = new Iso9660Parser(reader);
        var ok = parser.Parse(root, track);
        if (ok) Walk(root, ref files, ref dirs, ref maxSize);
        return ok;
    }

    private static bool TryContainerMount(string chdPath, ConsoleType consoleType, out int files, out int dirs)
    {
        files = 0;
        dirs = 0;
        try
        {
            var container = new ChdContainer(chdPath);
            try
            {
                if (!container.MountAndParse(consoleType))
                    return false;

                var all = CollectEntries(container, "\\").ToList();
                var fileEntries = all.Where(static e => !e.IsDirectory).ToList();
                files = fileEntries.Count;
                dirs = all.Count - fileEntries.Count;
                return fileEntries.Count > 2;
            }
            finally
            {
                container.Dispose();
            }
        }
        catch
        {
            return false;
        }
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