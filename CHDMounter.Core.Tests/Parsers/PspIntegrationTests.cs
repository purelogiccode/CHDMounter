using System.Globalization;
using System.Text;
using CHDSharp;
using CHDSharp.Models;
using VideoGameFileSystemParser.Parsers;
using VideoGameFileSystemParser.Parsers.Systems;
using Xunit.Abstractions;

namespace CHDMounter.Core.Tests.Parsers;

public class PspIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public PspIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static List<string> GetPaths()
    {
        return SequentialTestRunner.CollectPaths(
            ChdPathCatalog.PlayStationPortable.Paths);
    }

    [Fact]
    public void Iso9660ParserParsesPspDisc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(Iso9660ParserParsesPspDisc), paths, static (path, output) =>
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

                var track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack)
                            ?? reader.Tracks.FirstOrDefault()
                            ?? new TrackInfo();

                output.WriteLine($"UnitBytes={unitBytes} Tracks={reader.Tracks.Count} TrackType={track.TrackType}");

                var root = new FsNode();
                var parser = new Iso9660Parser(reader);

                var ok = parser.Parse(root, track);
                output.WriteLine($"Iso9660Parser: {(ok ? "OK" : "FAILED")}");

                Assert.True(ok, "Iso9660Parser could not parse the disc");

                int files = 0, dirs = 0;
                ulong maxSize = 0;
                Walk(root, ref files, ref dirs, ref maxSize);
                output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes");

                var topTwenty = root.Children.OrderByDescending(static n => n.Size).Take(20);
                foreach (var c in topTwenty)
                    output.WriteLine(
                        $"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}  mtime={c.ModifiedTime:yyyy-MM-dd HH:mm:ss}");

                Assert.True(files > 5, $"Suspiciously few files parsed: {files}");
            }
            finally
            {
                chd.Dispose();
            }

            return true;
        });
    }

    [Fact]
    public void PspParserParsesPspDisc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(PspParserParsesPspDisc), paths, static (path, output) =>
        {
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                var reader = new SectorReader(chd, chd.UnitBytes);
                var hasDataTrack = reader.Tracks.Any(static t => t.IsDataTrack);

                if (!hasDataTrack)
                {
                    output.WriteLine("Audio-only disc — no data track to parse");
                    return true;
                }

                var root = new FsNode();
                var parser = new PspParser(reader);

                var ok = parser.Parse(root);
                output.WriteLine($"PspParser: {(ok ? "OK" : "FAILED")}");

                Assert.True(ok, "PspParser could not parse the disc");

                int files = 0, dirs = 0;
                ulong maxSize = 0;
                Walk(root, ref files, ref dirs, ref maxSize);
                output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes");

                Assert.True(files > 5, $"Suspiciously few files parsed: {files}");

                var hasPspGame = root.Children.Any(static n => n is { Name: "PSP_GAME", IsDirectory: true });
                var hasUmdDataBin = root.Children.Any(static n =>
                    string.Equals(n.Name, "UMD_DATA.BIN", StringComparison.Ordinal));
                output.WriteLine(
                    $"PSP_GAME: {(hasPspGame ? "YES" : "NO")}  UMD_DATA.BIN: {(hasUmdDataBin ? "YES" : "NO")}");
            }
            finally
            {
                chd.Dispose();
            }

            return true;
        });
    }

    [Fact]
    public void ChdContainerMountAndParsePspDisc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(ChdContainerMountAndParsePspDisc), paths, static (path, output) =>
        {
            var container = new ChdContainer(path);
            try
            {
                var parsed = container.MountAndParse(ConsoleType.Psp);

                if (!parsed && !container.HasDataTracks)
                {
                    output.WriteLine("Audio-only disc — no data track to parse");
                    return true;
                }

                Assert.True(parsed, "MountAndParse failed");

                foreach (var e in container.ListDirectory("\\"))
                    output.WriteLine(
                        $"  {(e.IsDirectory ? "<DIR>" : e.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {e.Name}");

                var all = CollectEntries(container, "\\").ToList();
                var fileEntries = all.Where(static e => !e.IsDirectory).ToList();
                output.WriteLine($"Container: {fileEntries.Count} files, {all.Count - fileEntries.Count} dirs");

                Assert.True(fileEntries.Count > 5, $"Suspiciously few files: {fileEntries.Count}");

                var badNames = all.Where(static e => e.Name.Contains('\uFFFD') || e.Name.Any(char.IsControl)).ToList();
                foreach (var bad in badNames)
                    output.WriteLine($"BAD NAME: {bad.FullPath}");
                Assert.Empty(badNames);

                var umdData = container.FindFile(@"\UMD_DATA.BIN");
                {
                    var buf = new byte[2048];
                    if (umdData != null)
                    {
                        var bytesRead = container.ReadFile(umdData, 0, buf, 0, buf.Length);
                        var title = Encoding.ASCII.GetString(buf, 0, Math.Min(bytesRead, 128)).TrimEnd('\0');
                        output.WriteLine($"UMD_DATA.BIN ({bytesRead} bytes): '{title}'");
                    }
                }

                var paramSfo = container.FindFile(@"\PSP_GAME\PARAM.SFO");
                if (paramSfo != null) output.WriteLine($"PSP_GAME\\PARAM.SFO: FOUND ({paramSfo.Size:N0} bytes)");
            }
            finally
            {
                container.Dispose();
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