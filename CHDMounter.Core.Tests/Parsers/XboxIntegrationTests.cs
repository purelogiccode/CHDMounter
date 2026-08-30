using System.Globalization;
using CHDSharp;
using CHDSharp.Models;
using VideoGameFileSystemParser.Parsers;
using Xunit.Abstractions;

namespace CHDMounter.Core.Tests.Parsers;

public class XboxIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public XboxIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static List<string> GetXboxPaths()
    {
        return SequentialTestRunner.CollectPaths(
            ChdPathCatalog.Xbox.Paths);
    }

    private static List<string> GetXbox360Paths()
    {
        return SequentialTestRunner.CollectPaths(
            ChdPathCatalog.Xbox360.Paths);
    }

    [Fact]
    public void XdvdfsParserParsesXboxDisc()
    {
        var paths = GetXboxPaths();
        SequentialTestRunner.Run(_output, nameof(XdvdfsParserParsesXboxDisc), paths, static (path, output) =>
        {
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                var unitBytes = chd.UnitBytes;
                var reader = new SectorReader(chd, unitBytes);
                var track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack)
                            ?? reader.Tracks.FirstOrDefault()
                            ?? new TrackInfo();

                output.WriteLine($"UnitBytes={unitBytes} Tracks={reader.Tracks.Count} TrackType={track.TrackType}");

                var root = new FsNode();
                var parser = new XdvdfsParser(reader);
                parser.SetTrack(track);
                var ok = parser.Parse(root);
                output.WriteLine($"XdvdfsParser: {(ok ? "OK" : "FAILED")}");

                if (!ok)
                {
                    output.WriteLine("Trying Iso9660Parser fallback...");
                    var isoParser = new Iso9660Parser(reader);
                    ok = isoParser.Parse(root, track);
                    output.WriteLine($"Iso9660Parser: {(ok ? "OK" : "FAILED")}");
                }

                Assert.True(ok, "Neither XdvdfsParser nor Iso9660Parser could parse the disc");

                int files = 0, dirs = 0;
                ulong maxSize = 0;
                Walk(root, ref files, ref dirs, ref maxSize);
                output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes");

                var topTwenty = root.Children.OrderByDescending(static n => n.Size).Take(20);
                foreach (var c in topTwenty)
                    output.WriteLine(
                        $"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}  mtime={c.ModifiedTime:yyyy-MM-dd HH:mm:ss}");

                Assert.True(files > 10, $"Suspiciously few files parsed: {files}");
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

    [Fact]
    public void XdvdfsAndUdfParserParsesXbox360Disc()
    {
        var paths = GetXbox360Paths();
        SequentialTestRunner.Run(_output, nameof(XdvdfsAndUdfParserParsesXbox360Disc), paths, static (path, output) =>
        {
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                var unitBytes = chd.UnitBytes;
                var reader = new SectorReader(chd, unitBytes);
                var track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack)
                            ?? reader.Tracks.FirstOrDefault()
                            ?? new TrackInfo();

                output.WriteLine($"UnitBytes={unitBytes} Tracks={reader.Tracks.Count} TrackType={track.TrackType}");

                var root = new FsNode();
                var parser = new XdvdfsParser(reader);
                parser.SetTrack(track);
                var ok = parser.Parse(root);
                output.WriteLine($"XdvdfsParser: {(ok ? "OK" : "FAILED")}");

                if (!ok)
                {
                    output.WriteLine("Trying UdfParser fallback...");
                    var udfParser = new UdfParser(reader);
                    ok = udfParser.Parse(root, track);
                    output.WriteLine($"UdfParser: {(ok ? "OK" : "FAILED")}");
                }

                if (!ok)
                {
                    output.WriteLine("Trying Iso9660Parser fallback...");
                    var isoParser = new Iso9660Parser(reader);
                    ok = isoParser.Parse(root, track);
                    output.WriteLine($"Iso9660Parser: {(ok ? "OK" : "FAILED")}");
                }

                Assert.True(ok, "No parser could parse the disc");

                int files = 0, dirs = 0;
                ulong maxSize = 0;
                Walk(root, ref files, ref dirs, ref maxSize);

                var totalSize = (ulong)0;
                CountTotal(root, ref totalSize);
                output.WriteLine(
                    $"FsNode tree: {files} files, {dirs} dirs, total={totalSize:N0} bytes, largest file {maxSize:N0} bytes");

                var topTwenty = root.Children.OrderByDescending(static n => n.Size).Take(20);
                foreach (var c in topTwenty)
                    output.WriteLine(
                        $"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}  mtime={c.ModifiedTime:yyyy-MM-dd HH:mm:ss}");

                Assert.True(files > 10, $"Suspiciously few files parsed: {files}");

                var xexFiles = FindNodes(root).Count(static n =>
                    !n.IsDirectory && n.Name.EndsWith(".xex", StringComparison.OrdinalIgnoreCase));
                output.WriteLine($"XEX files found: {xexFiles}");
            }
            finally
            {
                chd.Dispose();
            }

            return true;
        });
    }

    private static List<FsNode> FindNodes(FsNode node)
    {
        var list = new List<FsNode> { node };
        foreach (var c in node.Children)
            list.AddRange(FindNodes(c));
        return list;
    }

    private static void CountTotal(FsNode node, ref ulong total)
    {
        foreach (var c in node.Children)
        {
            if (!c.IsDirectory) total += c.Size;

            CountTotal(c, ref total);
        }
    }
}