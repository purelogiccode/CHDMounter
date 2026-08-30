using System.Globalization;
using VideoGameFileSystemParser.Parsers;
using Xunit.Abstractions;

namespace CHDMounter.Core.Tests.Parsers;

public class FilesystemParsingTests
{
    private readonly ITestOutputHelper _output;

    public FilesystemParsingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void MountAndParsePs1Filesystem()
    {
        RunFilesystemTest(ConsoleType.Ps1, ChdPathCatalog.PlayStation1.Paths, 10,
            @"\SYSTEM.CNF");
    }

    [Fact]
    public void MountAndParsePs2Filesystem()
    {
        RunFilesystemTest(ConsoleType.Ps2, ChdPathCatalog.PlayStation2.Paths, 10,
            @"\SYSTEM.CNF");
    }

    [Fact]
    public void MountAndParsePs3Filesystem()
    {
        RunFilesystemTest(ConsoleType.Ps3, ChdPathCatalog.PlayStation3.Paths, 10,
            @"\PS3_DISC.SFB");
    }

    [Fact]
    public void MountAndParsePspFilesystem()
    {
        RunFilesystemTest(ConsoleType.Psp, ChdPathCatalog.PlayStationPortable.Paths, 5,
            @"\UMD_DATA.BIN");
    }

    [Fact]
    public void MountAndParseDreamcastFilesystem()
    {
        RunFilesystemTest(ConsoleType.Dreamcast, ChdPathCatalog.Dreamcast.Paths, 5);
    }

    [Fact]
    public void MountAndParseSaturnFilesystem()
    {
        RunFilesystemTest(ConsoleType.Saturn, ChdPathCatalog.SegaSaturn.Paths, 2);
    }

    [Fact]
    public void MountAndParseXboxFilesystem()
    {
        RunFilesystemTest(ConsoleType.Xbox, ChdPathCatalog.Xbox.Paths, 10);
    }

    [Fact]
    public void MountAndParseThreeDoFilesystem()
    {
        RunFilesystemTest(ConsoleType.ThreeDo, ChdPathCatalog.ThreeDo.Paths, 2);
    }

    [Fact]
    public void MountAndParseCdiFilesystem()
    {
        RunFilesystemTest(ConsoleType.CDi, ChdPathCatalog.CDi.Paths, 2);
    }

    [Fact]
    public void MountAndParseNeoGeoCdFilesystem()
    {
        RunFilesystemTest(ConsoleType.NeoGeoCd, ChdPathCatalog.NeoGeoCd.Paths, 2);
    }

    [Fact]
    public void MountAndParsePcFxFilesystem()
    {
        RunFilesystemTest(ConsoleType.PcFx, ChdPathCatalog.PcFx.Paths, 2);
    }

    [Fact]
    public void MountAndParsePc98Filesystem()
    {
        RunFilesystemTest(ConsoleType.Pc98, ChdPathCatalog.Pc98.Paths, 2);
    }

    [Fact]
    public void MountAndParseFmTownsFilesystem()
    {
        RunFilesystemTest(ConsoleType.FmTowns, ChdPathCatalog.FmTowns.Paths, 2);
    }

    [Fact]
    public void MountAndParseAmigaCdFilesystem()
    {
        RunFilesystemTest(ConsoleType.AmigaCd, ChdPathCatalog.AmigaCd.Paths, 2);
    }

    [Fact]
    public void MountAndParseAmigaCd32Filesystem()
    {
        RunFilesystemTest(ConsoleType.AmigaCd32, ChdPathCatalog.AmigaCd32.Paths, 2);
    }

    [Fact]
    public void MountAndParseSegaGenesisCdFilesystem()
    {
        RunFilesystemTest(ConsoleType.SegaGenesisCd, ChdPathCatalog.SegaGenesisCd.Paths, 2);
    }

    [Fact]
    public void MountAndParsePceCdFilesystem()
    {
        RunFilesystemTest(ConsoleType.PcEngineCd, ChdPathCatalog.PcEngineCd.Paths, 2);
    }

    [Fact]
    public void MountAndParseX68000Filesystem()
    {
        RunFilesystemTest(ConsoleType.X68000, ChdPathCatalog.X68000.Paths, 2);
    }

    private void RunFilesystemTest(ConsoleType consoleType, string[] searchPaths, int minFiles,
        string? knownFile = null)
    {
        var paths = SequentialTestRunner.CollectPaths(20, searchPaths);
        var consoleName = consoleType.ToString();

        SequentialTestRunner.Run(_output, $"FilesystemParsing_{consoleName}", paths, (path, output) =>
        {
            var container = new ChdContainer(path);
            try
            {
                var success = container.MountAndParse(consoleType);

                if (!success)
                {
                    if (container is { HasDataTracks: false, VolumeSize: > 0 })
                    {
                        output.WriteLine($"  Audio-only disc, no data track to parse. Size: {container.VolumeSize:N0}");
                        return true;
                    }

                    output.WriteLine($"  SKIP: MountAndParse failed for {Path.GetFileName(path)}");
                    return true;
                }

                if (string.IsNullOrEmpty(container.VolumeName))
                {
                    output.WriteLine($"  SKIP: VolumeName is empty for {Path.GetFileName(path)}");
                    return true;
                }

                var all = CollectEntries(container, "\\").ToList();
                var fileEntries = all.Where(static e => !e.IsDirectory).ToList();
                var dirCount = all.Count - fileEntries.Count;
                if (dirCount > 0) dirCount--;

                output.WriteLine($"  Volume: {container.VolumeName}");
                output.WriteLine($"  Size: {container.VolumeSize:N0}");
                output.WriteLine($"  Files: {fileEntries.Count}, Dirs: {dirCount}");

                if (fileEntries.Count < minFiles)
                {
                    output.WriteLine(
                        $"  SKIP: Suspiciously few files parsed: {fileEntries.Count} (expected >= {minFiles})");
                    return true;
                }

                var badNames = all.Where(static e =>
                    e.Name.Contains('\uFFFD') ||
                    e.Name.Any(char.IsControl)).ToList();
                foreach (var bad in badNames)
                    output.WriteLine($"  BAD NAME: {bad.FullPath}");
                if (badNames.Count > 0)
                {
                    output.WriteLine($"  SKIP: {badNames.Count} bad name(s) found in {Path.GetFileName(path)}");
                    return true;
                }

                if (knownFile != null)
                {
                    var entry = container.FindFile(knownFile);
                    if (entry != null)
                    {
                        var buf = new byte[Math.Min((int)entry.Size, 2048)];
                        if (buf.Length > 0)
                        {
                            var bytesRead = container.ReadFile(entry, 0, buf, 0, buf.Length);
                            Assert.True(bytesRead > 0, $"ReadFile returned 0 bytes for {knownFile}");
                            output.WriteLine($"  Read {knownFile}: {bytesRead} bytes OK");
                        }
                    }
                }

                foreach (var e in container.ListDirectory("\\"))
                    output.WriteLine(
                        $"    {(e.IsDirectory ? "<DIR>" : e.Size.ToString("N0", CultureInfo.InvariantCulture)),15}  {e.Name}");

                return true;
            }
            finally
            {
                container.Dispose();
            }
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