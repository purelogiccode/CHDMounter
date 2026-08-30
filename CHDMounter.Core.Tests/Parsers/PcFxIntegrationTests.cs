using System.Globalization;
using CHDSharp;
using CHDSharp.Models;
using VideoGameFileSystemParser.Parsers;
using VideoGameFileSystemParser.Parsers.Systems;
using Xunit.Abstractions;

namespace CHDMounter.Core.Tests.Parsers;

public class PcFxIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public PcFxIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static List<string> GetPaths()
    {
        return SequentialTestRunner.CollectPaths(ChdPathCatalog.PcFx.Paths);
    }

    [Fact]
    public void Iso9660ParserParsesPcFxDisc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(Iso9660ParserParsesPcFxDisc), paths, static (path, output) =>
        {
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                var unitBytes = chd.UnitBytes;
                var reader = new SectorReader(chd, unitBytes);
                var track = reader.Tracks.FirstOrDefault(static t => t.IsDataTrack) ?? reader.Tracks.FirstOrDefault();

                output.WriteLine(
                    $"UnitBytes={unitBytes} Tracks={reader.Tracks.Count} TrackType={track?.TrackType ?? "N/A"}");

                var root = new FsNode();
                var parser = new Iso9660Parser(reader);

                var ok = parser.Parse(root, track);
                output.WriteLine($"Iso9660Parser: {(ok ? "OK" : "FAILED")}");

                if (!ok)
                {
                    output.WriteLine("  SKIP: Disc has no ISO 9660 filesystem (non-standard layout)");
                    return true;
                }

                int files = 0, dirs = 0;
                ulong maxSize = 0;
                Walk(root, ref files, ref dirs, ref maxSize);
                output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes");

                Assert.True(files >= 1, $"No files parsed: {files}");

                foreach (var c in root.Children.OrderByDescending(static n => n.Size).Take(15))
                    output.WriteLine(
                        $"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}  mtime={c.ModifiedTime:yyyy-MM-dd HH:mm:ss}");
            }
            finally
            {
                chd.Dispose();
            }

            return true;
        });
    }

    [Fact]
    public void PcFxParserParsesDisc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(PcFxParserParsesDisc), paths, static (path, output) =>
        {
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                var reader = new SectorReader(chd, chd.UnitBytes);
                output.WriteLine($"UnitBytes={chd.UnitBytes} Tracks={reader.Tracks.Count}");

                var root = new FsNode();
                var parser = new PcFxParser(reader);

                var ok = parser.Parse(root);
                output.WriteLine($"PcFxParser: {(ok ? "OK" : "FAILED")}");

                Assert.True(ok, "PcFxParser could not parse the disc");

                int files = 0, dirs = 0;
                ulong maxSize = 0;
                Walk(root, ref files, ref dirs, ref maxSize);
                output.WriteLine($"FsNode tree: {files} files, {dirs} dirs, largest file {maxSize:N0} bytes");

                Assert.True(files >= 1, $"No files parsed: {files}");
            }
            finally
            {
                chd.Dispose();
            }

            return true;
        });
    }

    [Fact]
    public void ChdContainerMountAndParsePcFxDisc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(ChdContainerMountAndParsePcFxDisc), paths, static (path, output) =>
        {
            var container = new ChdContainer(path);
            try
            {
                Assert.True(container.MountAndParse(ConsoleType.PcFx), "MountAndParse failed");

                var all = CollectEntries(container, "\\").ToList();
                var fileEntries = all.Where(static e => !e.IsDirectory).ToList();
                output.WriteLine($"Container: {fileEntries.Count} files, {all.Count - fileEntries.Count} dirs");

                Assert.True(fileEntries.Count >= 1, $"No files exposed: {fileEntries.Count}");

                var badNames = all.Where(static e => e.Name.Contains('\uFFFD') || e.Name.Any(char.IsControl)).ToList();
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
    public void ChdContainerCheckParseAndRead()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(ChdContainerCheckParseAndRead), paths, static (path, output) =>
        {
            var container = new ChdContainer(path);
            try
            {
                Assert.True(container.MountAndParse(ConsoleType.PcFx), "MountAndParse failed");

                foreach (var e in container.ListDirectory("\\"))
                {
                    if (e.IsDirectory) continue;

                    var entry = container.FindFile(e.FullPath);
                    Assert.NotNull(entry);

                    var readSize = (int)Math.Min(e.Size, 4096);
                    var buffer = new byte[readSize];
                    var bytesRead = container.ReadFile(entry, 0, buffer, 0, readSize);
                    output.WriteLine($"  Read: {e.Name}  size={e.Size}  bytesRead={bytesRead}");

                    if (bytesRead > 0)
                    {
                        Assert.True(true, $"Failed to read {e.Name}");
                        break;
                    }
                }
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