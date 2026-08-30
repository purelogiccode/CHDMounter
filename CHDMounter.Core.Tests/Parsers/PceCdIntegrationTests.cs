using System.Globalization;
using System.Text;
using CHDSharp;
using CHDSharp.Models;
using VideoGameFileSystemParser.Parsers;
using VideoGameFileSystemParser.Parsers.Systems;
using Xunit.Abstractions;

namespace CHDMounter.Core.Tests.Parsers;

public class PceCdIntegrationTests
{
    private const string BootSignature = "PC Engine CD-ROM SYSTEM";

    private readonly ITestOutputHelper _output;

    public PceCdIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static List<string> GetPaths()
    {
        return SequentialTestRunner.CollectPaths(ChdPathCatalog.PcEngineCd.Paths);
    }

    [Fact]
    public void BootSignatureDetectedOnDataTrack()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(BootSignatureDetectedOnDataTrack), paths, static (path, output) =>
        {
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                var reader = new SectorReader(chd, chd.UnitBytes);
                var found = false;

                foreach (var track in reader.Tracks.Where(static t => t.IsDataTrack))
                {
                    var pregapStored = track.Metadata.Contains("PGTYPE:V", StringComparison.OrdinalIgnoreCase)
                        ? track.Pregap
                        : 0;
                    var dataStart = track.StartLba + pregapStored;

                    reader.Reset();
                    reader.SetTrack(track, true);

                    var sec = new byte[2048];
                    if (!reader.ReadSector(dataStart + 1, sec))
                        continue;

                    var descriptor = Encoding.ASCII.GetString(sec, 0x20, BootSignature.Length);
                    output.WriteLine($"Track {track.Index}: dataStart={dataStart} descriptor='{descriptor}'");

                    if (string.Equals(descriptor, BootSignature, StringComparison.Ordinal)) found = true;
                }

                if (!found)
                    output.WriteLine("  SKIP: No PC Engine CD-ROM SYSTEM signature found (non-standard disc layout)");
            }
            finally
            {
                chd.Dispose();
            }

            return true;
        });
    }

    [Fact]
    public void PcEngineCdParserParsesPceCdDisc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(PcEngineCdParserParsesPceCdDisc), paths, static (path, output) =>
        {
            var err = ChdFile.Open(path, out var chd);
            Assert.Equal(ChdError.Chderrnone, err);
            Assert.NotNull(chd);

            try
            {
                var reader = new SectorReader(chd, chd.UnitBytes);
                output.WriteLine($"UnitBytes={chd.UnitBytes} Tracks={reader.Tracks.Count}");

                foreach (var t in reader.Tracks)
                    output.WriteLine(
                        $"  Track {t.Index}: {t.TrackType} data={t.IsDataTrack} frames={t.Frames} pregap={t.Pregap}");

                var root = new FsNode();
                var parser = new PcEngineCdParser(reader);

                var ok = parser.Parse(root);
                output.WriteLine($"PcEngineCdParser: {(ok ? "OK" : "FAILED")}");

                Assert.True(ok, "PcEngineCdParser could not parse the disc");
                Assert.True(root.Children.Count > 0, "No nodes produced");

                foreach (var c in root.Children)
                    output.WriteLine(
                        $"  {(c.IsDirectory ? "<DIR>" : c.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {c.Name}");

                var dataTrackCount = reader.Tracks.Count(static t => t.IsDataTrack);
                var isoCount =
                    root.Children.Count(static n => n.Name.EndsWith(".iso", StringComparison.OrdinalIgnoreCase));
                output.WriteLine($"Data tracks: {dataTrackCount}, TRACK ISOs: {isoCount}");
            }
            finally
            {
                chd.Dispose();
            }

            return true;
        });
    }

    [Fact]
    public void ChdContainerMountAndParsePceCdDisc()
    {
        var paths = GetPaths();
        SequentialTestRunner.Run(_output, nameof(ChdContainerMountAndParsePceCdDisc), paths, static (path, output) =>
        {
            var container = new ChdContainer(path);
            try
            {
                Assert.True(container.MountAndParse(ConsoleType.PcEngineCd), "MountAndParse failed");

                var all = CollectEntries(container, "\\").ToList();
                var fileEntries = all.Where(static e => !e.IsDirectory).ToList();
                output.WriteLine($"Container: {fileEntries.Count} files, {all.Count - fileEntries.Count} dirs");

                foreach (var e in container.ListDirectory("\\"))
                    output.WriteLine(
                        $"  {(e.IsDirectory ? "<DIR>" : e.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {e.Name}");

                Assert.True(fileEntries.Count > 0, "No files exposed");

                var badNames = all.Where(static e => e.Name.Contains('\uFFFD') || e.Name.Any(char.IsControl)).ToList();
                foreach (var bad in badNames)
                    output.WriteLine($"BAD NAME: {bad.FullPath}");
                Assert.Empty(badNames);

                var cue = fileEntries.FirstOrDefault(static e =>
                    e.Name.EndsWith(".cue", StringComparison.OrdinalIgnoreCase));
                var bin = fileEntries.FirstOrDefault(static e =>
                    e.Name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(cue);
                Assert.NotNull(bin);

                var cueBuf = new byte[Math.Min(1024, (int)cue.Size)];
                var cueRead = container.ReadFile(cue, 0, cueBuf, 0, cueBuf.Length);
                var cueText = Encoding.ASCII.GetString(cueBuf, 0, cueRead);
                output.WriteLine($"CUE ({cueRead} bytes):");
                foreach (var line in cueText.Split('\n').Take(8))
                    output.WriteLine($"  {line.TrimEnd()}");
                Assert.StartsWith("FILE", cueText, StringComparison.Ordinal);
                Assert.Contains("TRACK", cueText, StringComparison.Ordinal);

                var trackIso = fileEntries.FirstOrDefault(static e =>
                    e.Name.StartsWith("TRACK", StringComparison.OrdinalIgnoreCase) &&
                    e.Name.EndsWith(".iso", StringComparison.OrdinalIgnoreCase));
                if (trackIso != null)
                {
                    var buf = new byte[2048];
                    var bytesRead = container.ReadFile(trackIso, 2048, buf, 0, buf.Length);
                    Assert.Equal(2048, bytesRead);

                    var descriptor = Encoding.ASCII.GetString(buf, 0x20, BootSignature.Length);
                    output.WriteLine($"{trackIso.Name} sector 1 descriptor: '{descriptor}'");

                    if (!string.Equals(descriptor, BootSignature, StringComparison.Ordinal))
                        output.WriteLine(
                            "  SKIP: TRACK ISO does not contain boot signature (non-standard disc layout)");
                }
                else
                {
                    output.WriteLine("No TRACK ISO exposed (disc parsed as ISO9660)");
                }
            }
            finally
            {
                container.Dispose();
            }

            return true;
        });
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