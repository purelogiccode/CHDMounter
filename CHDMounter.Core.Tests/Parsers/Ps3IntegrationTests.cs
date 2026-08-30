using System.Globalization;
using System.Text;
using CHDSharp;
using CHDSharp.Models;
using VideoGameFileSystemParser.Parsers;
using VideoGameFileSystemParser.Parsers.Systems;
using Xunit.Abstractions;

namespace CHDMounter.Core.Tests.Parsers;

public class Ps3IntegrationTests
{
    private readonly ITestOutputHelper _output;

    public Ps3IntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static List<string> GetPaths()
    {
        return SequentialTestRunner.CollectPaths(
            ChdPathCatalog.PlayStation3.Paths);
    }

    [Fact]
    public void UdfParserParsesPs3Disc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(UdfParserParsesPs3Disc), paths, static (path, output) =>
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

                var root = new FsNode();
                var udfOk = new UdfParser(reader).Parse(root, track);
                output.WriteLine(
                    $"UnitBytes={unitBytes} Tracks={reader.Tracks.Count} UdfParser={(udfOk ? "OK" : "FAILED")}");
                Assert.True(udfOk, "UdfParser.Parse failed (PS3 would fall back to ISO9660)");

                int files = 0, dirs = 0, multiExtent = 0;
                ulong maxSize = 0;
                Walk(root, ref files, ref dirs, ref multiExtent, ref maxSize);
                output.WriteLine(
                    $"FsNode tree: {files} files, {dirs} dirs, {multiExtent} multi-extent files, largest file {maxSize:N0} bytes");

                Assert.True(files > 10, "Suspiciously few files parsed");
                Assert.Contains(root.Children, static n => n is { Name: "PS3_GAME", IsDirectory: true });
                return true;
            }
            finally
            {
                chd.Dispose();
            }
        });
    }

    [Fact]
    public void Iso9660BridgeParsesPs3Disc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(Iso9660BridgeParsesPs3Disc), paths, static (path, output) =>
        {
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                var reader = new SectorReader(chd, chd.UnitBytes);
                var root = new FsNode();
                var ok = new Iso9660Parser(reader).Parse(root);
                output.WriteLine(
                    $"ISO9660 bridge parse: {(ok ? "OK" : "FAILED")}, top-level entries: {root.Children.Count}");
                Assert.True(ok, "Iso9660Parser failed on the PS3 UDF-bridge ISO part");

                foreach (var c in root.Children)
                    output.WriteLine(
                        $"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}  mtime={c.ModifiedTime:yyyy-MM-dd HH:mm:ss}");

                var sfb = root.Children.FirstOrDefault(static n =>
                    string.Equals(n.Name, "PS3_DISC.SFB", StringComparison.Ordinal));
                Assert.NotNull(sfb);
                Assert.NotNull(sfb.ModifiedTime);

                var sec = new byte[2048];
                Assert.True(reader.ReadSector(sfb.Lba, sec));
                Assert.Equal(".SFB"u8.ToArray(), sec[..4]);
                return true;
            }
            finally
            {
                chd.Dispose();
            }
        });
    }

    [Fact]
    public void PlayStation3ParserParsesPs3Disc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(PlayStation3ParserParsesPs3Disc), paths, static (path, output) =>
        {
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                var reader = new SectorReader(chd, chd.UnitBytes);
                var root = new FsNode();
                var parser = new PlayStation3Parser(reader);

                var ok = parser.Parse(root);
                output.WriteLine($"PlayStation3Parser: {(ok ? "OK" : "FAILED")}");

                Assert.True(ok, "PlayStation3Parser could not parse the disc");

                int files = 0, dirs = 0, multiExtent = 0;
                ulong maxSize = 0;
                Walk(root, ref files, ref dirs, ref multiExtent, ref maxSize);
                output.WriteLine(
                    $"FsNode tree: {files} files, {dirs} dirs, {multiExtent} multi-extent files, largest file {maxSize:N0} bytes");

                Assert.True(files > 10, $"Suspiciously few files parsed: {files}");
                Assert.Contains(root.Children, static n => n is { Name: "PS3_GAME", IsDirectory: true });
                return true;
            }
            finally
            {
                chd.Dispose();
            }
        });
    }

    [Fact]
    public void ChdContainerMountAndParsePs3Disc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(ChdContainerMountAndParsePs3Disc), paths, static (path, output) =>
        {
            var container = new ChdContainer(path);
            try
            {
                Assert.True(container.MountAndParse(ConsoleType.Ps3), "MountAndParse failed");

                foreach (var e in container.ListDirectory("\\"))
                    output.WriteLine(
                        $"  {(e.IsDirectory ? "<DIR>" : e.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {e.Name}");

                var all = CollectEntries(container, "\\").ToList();
                var fileEntries = all.Where(static e => !e.IsDirectory).ToList();
                output.WriteLine($"Container: {fileEntries.Count} files, {all.Count - fileEntries.Count} dirs");

                Assert.True(fileEntries.Count > 10, $"Suspiciously few files: {fileEntries.Count}");

                var badNames = all.Where(static e => e.Name.Contains('\uFFFD') || e.Name.Any(char.IsControl)).ToList();
                foreach (var bad in badNames)
                    output.WriteLine($"BAD NAME: {bad.FullPath}");
                Assert.Empty(badNames);

                var magic = new byte[4];

                var sfb = container.FindFile(@"\PS3_DISC.SFB");
                if (sfb != null) Assert.Equal(4, container.ReadFile(sfb, 0, magic, 0, 4));
                Assert.Equal(".SFB"u8.ToArray(), magic);
                output.WriteLine("PS3_DISC.SFB: OK");

                var sfo = container.FindFile(@"\PS3_GAME\PARAM.SFO");
                {
                    if (sfo != null)
                    {
                        Assert.Equal(4, container.ReadFile(sfo, 0, magic, 0, 4));
                        Assert.Equal("\0PSF"u8.ToArray(), magic);
                        output.WriteLine("PARAM.SFO: OK");

                        var sfoBuf = new byte[(int)Math.Min(sfo.Size, 2048)];
                        var sfoLen = container.ReadFile(sfo, 0, sfoBuf, 0, sfoBuf.Length);
                        var title = ReadSfoString(sfoBuf, sfoLen);
                        if (title != null)
                            output.WriteLine($"  TITLE_ID: {title}");
                    }
                }
                return true;
            }
            finally
            {
                container.Dispose();
            }
        });
    }

    [Fact]
    public void ChdContentsMatchOriginalIso()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(ChdContentsMatchOriginalIso), paths, (path, output) =>
        {
            var container = new ChdContainer(path);
            try
            {
                Assert.True(container.MountAndParse(ConsoleType.Ps3), "MountAndParse failed");

                foreach (var e in container.ListDirectory("\\"))
                    output.WriteLine(
                        $"  {(e.IsDirectory ? "<DIR>" : e.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {e.Name}");

                var all = CollectEntries(container, "\\").ToList();
                var fileEntries = all.Where(static e => !e.IsDirectory).ToList();
                output.WriteLine($"Container: {fileEntries.Count} files, {all.Count - fileEntries.Count} dirs");

                var badNames = all.Where(static e => e.Name.Contains('\uFFFD') || e.Name.Any(char.IsControl)).ToList();
                foreach (var bad in badNames)
                    output.WriteLine($"BAD NAME: {bad.FullPath}");
                Assert.Empty(badNames);

                var magic = new byte[4];
                var sfb = container.FindFile("\\PS3_DISC.SFB");
                Assert.NotNull(sfb);
                Assert.Equal(4, container.ReadFile(sfb, 0, magic, 0, 4));
                Assert.Equal(".SFB"u8.ToArray(), magic);

                var sfo = container.FindFile(@"\PS3_GAME\PARAM.SFO");
                Assert.NotNull(sfo);
                Assert.Equal(4, container.ReadFile(sfo, 0, magic, 0, 4));
                Assert.Equal("\0PSF"u8.ToArray(), magic);

                var isoPath = Path.ChangeExtension(path, ".iso");
                if (!File.Exists(isoPath))
                {
                    output.WriteLine("SKIP: no companion .iso for cross-validation");
                    return true;
                }

                using var iso = File.OpenRead(isoPath);

                var samples = fileEntries
                    .Where(static e => e.Size > 0)
                    .OrderByDescending(static e => e.Size)
                    .Take(8)
                    .Concat([sfo, sfb])
                    .ToList();

                foreach (var entry in samples)
                    VerifyHead(container, iso, entry);

                var multi = fileEntries.Where(static e => e.Extents.Count > 1).ToList();
                output.WriteLine($"Multi-extent files: {multi.Count}");
                foreach (var entry in multi.Take(4))
                {
                    ulong sum = 0;
                    foreach (var x in entry.Extents) sum += x.Size;

                    Assert.Equal(entry.Size, sum);
                    VerifyExtentBoundary(container, iso, entry);
                }

                return true;
            }
            finally
            {
                container.Dispose();
            }
        });
    }

    private void VerifyHead(ChdContainer container, Stream iso, FileEntry entry)
    {
        var ext = entry.Extents.Count > 0 ? entry.Extents[0] : new FileExtent { Lba = entry.Lba, Size = entry.Size };
        var n = (int)Math.Min(65536, Math.Min(ext.Size, entry.Size));
        var chdBuf = new byte[n];
        Assert.Equal(n, container.ReadFile(entry, 0, chdBuf, 0, n));

        var isoBuf = new byte[n];
        iso.Position = (long)ext.Lba * 2048;
        iso.ReadExactly(isoBuf, 0, n);

        Assert.True(chdBuf.AsSpan().SequenceEqual(isoBuf), $"Data mismatch in head of {entry.FullPath}");
        _output.WriteLine(
            $"OK head {n,6} bytes  {entry.FullPath}  (LBA {ext.Lba}, size {entry.Size:N0}, extents {entry.Extents.Count})");
    }

    private void VerifyExtentBoundary(ChdContainer container, Stream iso, FileEntry entry)
    {
        var ext0 = entry.Extents[0];
        var ext1 = entry.Extents[1];
        var offset = ext0.Size - 2048;

        var chdBuf = new byte[4096];
        Assert.Equal(4096, container.ReadFile(entry, offset, chdBuf, 0, 4096));

        var isoBuf = new byte[4096];
        iso.Position = (long)ext0.Lba * 2048 + (long)offset;
        iso.ReadExactly(isoBuf, 0, 2048);
        iso.Position = (long)ext1.Lba * 2048;
        iso.ReadExactly(isoBuf, 2048, 2048);

        Assert.True(chdBuf.AsSpan().SequenceEqual(isoBuf), $"Data mismatch at extent boundary of {entry.FullPath}");
        _output.WriteLine(
            $"OK extent boundary  {entry.FullPath}  (ext0 {ext0.Size:N0} @ {ext0.Lba} -> ext1 @ {ext1.Lba})");
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

    private static string? ReadSfoString(byte[] buf, int length)
    {
        try
        {
            if (length < 20) return null;

            var keyTableStart = BitConverter.ToUInt32(buf, 8);
            var dataTableStart = BitConverter.ToUInt32(buf, 16);
            var numEntries = BitConverter.ToUInt32(buf, 20);

            for (uint i = 0; i < numEntries; i++)
            {
                var pos = (int)(20 + i * 16);
                if (pos + 16 > length) break;

                var keyOff = BitConverter.ToUInt16(buf, pos);
                var dataOff = BitConverter.ToUInt32(buf, pos + 8);
                var dataLen = BitConverter.ToUInt32(buf, pos + 12);

                var keyEnd = Array.IndexOf<byte>(buf, 0, (int)(keyTableStart + keyOff));
                if (keyEnd < 0) keyEnd = length;

                var key = Encoding.ASCII.GetString(buf, (int)(keyTableStart + keyOff),
                    keyEnd - (int)(keyTableStart + keyOff));

                if (string.Equals(key, "TITLE_ID", StringComparison.Ordinal))
                {
                    var dataPos = (int)(dataTableStart + dataOff);
                    var dLen = (int)Math.Min(dataLen, (uint)(length - dataPos));
                    return Encoding.ASCII.GetString(buf, dataPos, dLen).TrimEnd('\0');
                }
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private static void Walk(FsNode node, ref int files, ref int dirs, ref int multi, ref ulong maxSize)
    {
        foreach (var c in node.Children)
            if (c.IsDirectory)
            {
                dirs++;
                Walk(c, ref files, ref dirs, ref multi, ref maxSize);
            }
            else
            {
                files++;
                if (c.Extents.Count > 1) multi++;

                if (c.Size > maxSize) maxSize = c.Size;
            }
    }
}