using System.Diagnostics;
using Serilog;
using Tester.Models;
using VideoGameFileSystemParser.Models;
using VideoGameFileSystemParser.Parsers;

namespace Tester.Services;

/// <summary>
///     Orchestrates CHD parsing tests across a folder of CHD files, collecting results and emitting progress events.
/// </summary>
internal sealed class TestRunnerService
{
    private readonly List<string> _logLines = [];
    private readonly ILogger _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TestRunnerService" /> class.
    /// </summary>
    /// <param name="logger">The Serilog logger for recording test output.</param>
    public TestRunnerService(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///     Raised when a log message should be displayed to the user.
    /// </summary>
    public event EventHandler<EventArgs<string>>? LogMessage;

    /// <summary>
    ///     Raised when all tests have completed with the final summary.
    /// </summary>
    public event EventHandler<EventArgs<TestSummary>>? AllCompleted;

    /// <summary>
    ///     Runs parsing tests on all CHD files in the specified folder.
    /// </summary>
    /// <param name="folderPath">The folder containing CHD files to test.</param>
    /// <param name="consoleInfo">The console type information to use for parsing.</param>
    /// <param name="ct">A cancellation token to abort the test run.</param>
    /// <returns>A <see cref="TestSummary" /> containing the aggregated results.</returns>
    public async Task<TestSummary> RunTestsAsync(string folderPath, ConsoleInfo consoleInfo,
        CancellationToken ct = default)
    {
        var summary = new TestSummary
        {
            ConsoleName = consoleInfo.Name,
            ChdFolder = folderPath,
            StartTime = DateTime.Now
        };

        _logLines.Clear();

        var chdFiles = Directory.GetFiles(folderPath, "*.chd", SearchOption.AllDirectories);
        if (chdFiles.Length == 0)
        {
            EmitLog("No .chd files found in the selected folder.");
            summary.EndTime = DateTime.Now;
            summary.LogLines = _logLines.ToList();
            AllCompleted?.Invoke(this, new EventArgs<TestSummary>(summary));
            return summary;
        }

        EmitLog($"Found {chdFiles.Length} CHD file(s) in: {folderPath}");
        EmitLog($"Console type: {consoleInfo.Name}");
        EmitLog(new string('-', 60));

        var swTotal = Stopwatch.StartNew();

        for (var i = 0; i < chdFiles.Length; i++)
        {
            if (ct.IsCancellationRequested) break;

            var chdPath = chdFiles[i];
            var fileName = Path.GetFileName(chdPath);
            EmitLog("");
            EmitLog($"[{i + 1}/{chdFiles.Length}] Testing: {fileName}");

            var sw = Stopwatch.StartNew();
            TestResult result;

            try
            {
                await using var container = new ChdContainer(chdPath);

                var success = await Task.Run(() => container.MountAndParse(consoleInfo.Type), ct);
                sw.Stop();

                if (success)
                {
                    var fileCount = 0;
                    var dirCount = 0;
                    foreach (var entry in container.Entries)
                        if (entry.IsDirectory)
                            dirCount++;
                        else
                            fileCount++;

                    if (dirCount > 0) dirCount--;

                    result = new TestResult(
                        fileName,
                        chdPath,
                        true,
                        "",
                        container.VolumeName,
                        container.VolumeSize,
                        fileCount,
                        dirCount,
                        sw.Elapsed
                    );

                    EmitLog($"  OK  Volume: {container.VolumeName}, Size: {FormatBytes(container.VolumeSize)}, " +
                            $"Files: {fileCount}, Dirs: {dirCount}, Time: {sw.Elapsed.TotalSeconds:F2}s");
                }
                else if (container is { HasDataTracks: false, VolumeSize: > 0 })
                {
                    result = new TestResult(
                        fileName,
                        chdPath,
                        true,
                        "Audio-only disc, no data track to parse",
                        container.VolumeName,
                        container.VolumeSize,
                        0,
                        0,
                        sw.Elapsed
                    );

                    EmitLog(
                        $"  OK   Audio-only disc — no data track to parse. Size: {FormatBytes(container.VolumeSize)}, Time: {sw.Elapsed.TotalSeconds:F2}s");
                }
                else
                {
                    result = new TestResult(
                        fileName,
                        chdPath,
                        false,
                        "Failed to parse CHD file",
                        "",
                        0,
                        0,
                        0,
                        sw.Elapsed
                    );

                    EmitLog($"  FAIL  Could not parse CHD file. Time: {sw.Elapsed.TotalSeconds:F2}s");
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                result = new TestResult(
                    fileName,
                    chdPath,
                    false,
                    ex.Message,
                    "",
                    0,
                    0,
                    0,
                    sw.Elapsed
                );

                EmitLog($"  FAIL  {ex.Message}");
                _logger.Error(ex, "Error testing {ChdPath}", chdPath);
            }

            summary.Results.Add(result);
        }

        swTotal.Stop();
        summary.EndTime = DateTime.Now;

        EmitLog("");
        EmitLog(new string('=', 60));
        EmitLog("TEST SUMMARY");
        EmitLog(new string('=', 60));
        EmitLog($"  Console:      {summary.ConsoleName}");
        EmitLog($"  Total files:  {summary.TotalFiles}");
        EmitLog($"  Succeeded:    {summary.SuccessCount}");
        EmitLog($"  Failed:       {summary.FailCount}");
        EmitLog($"  Total time:   {summary.TotalDuration.TotalSeconds:F2}s");
        EmitLog($"  Avg time:     {summary.AverageDuration.TotalSeconds:F2}s");
        EmitLog($"  Total bytes:  {FormatBytes((ulong)summary.TotalBytes)}");
        EmitLog($"  Total entries:{summary.TotalEntries:N0}");
        if (summary.Fastest is not null)
            EmitLog($"  Fastest:      {summary.Fastest.FileName} ({summary.Fastest.Duration.TotalSeconds:F2}s)");
        if (summary.Slowest is not null)
            EmitLog($"  Slowest:      {summary.Slowest.FileName} ({summary.Slowest.Duration.TotalSeconds:F2}s)");
        EmitLog(new string('=', 60));

        summary.LogLines = _logLines.ToList();
        AllCompleted?.Invoke(this, new EventArgs<TestSummary>(summary));
        return summary;
    }

    private void EmitLog(string message)
    {
        _logLines.Add(message);
        LogMessage?.Invoke(this, new EventArgs<string>(message));
        _logger.Information("{Message}", message);
    }

    private static string FormatBytes(ulong bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        var order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}