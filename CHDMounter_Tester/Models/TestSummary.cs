namespace Tester.Models;

/// <summary>
///     Aggregates the results of a batch CHD parsing test run, providing summary statistics.
/// </summary>
internal sealed class TestSummary
{
    /// <summary>Gets or sets the name of the console type used for the test run.</summary>
    internal string ConsoleName { get; set; } = "";

    /// <summary>Gets or sets the folder path containing the CHD files tested.</summary>
    internal string ChdFolder { get; set; } = "";

    /// <summary>Gets or sets the date and time the test run started.</summary>
    internal DateTime StartTime { get; set; }

    /// <summary>Gets or sets the date and time the test run ended.</summary>
    internal DateTime EndTime { get; set; }

    /// <summary>Gets or sets the list of individual test results.</summary>
    internal List<TestResult> Results { get; set; } = [];

    /// <summary>Gets the total number of CHD files tested.</summary>
    internal int TotalFiles => Results.Count;

    /// <summary>Gets the number of CHD files that parsed successfully.</summary>
    internal int SuccessCount => Results.Count(r => r.Success);

    /// <summary>Gets the number of CHD files that failed to parse.</summary>
    internal int FailCount => Results.Count(r => !r.Success);

    /// <summary>Gets the total duration of the test run.</summary>
    internal TimeSpan TotalDuration => EndTime - StartTime;

    /// <summary>Gets the total volume size in bytes across all successfully parsed CHD files.</summary>
    internal long TotalBytes => Results.Where(r => r.Success).Sum(r => (long)r.VolumeSize);

    /// <summary>Gets the total number of file and directory entries across all successfully parsed CHD files.</summary>
    internal int TotalEntries => Results.Where(r => r.Success).Sum(r => r.FileCount + r.DirectoryCount);

    /// <summary>Gets the average parsing duration across all tested CHD files.</summary>
    internal TimeSpan AverageDuration => Results.Count > 0
        ? TimeSpan.FromMilliseconds(Results.Average(r => r.Duration.TotalMilliseconds))
        : TimeSpan.Zero;

    /// <summary>Gets the fastest test result, or <c>null</c> if no results exist.</summary>
    internal TestResult? Fastest => Results.Count > 0 ? Results.MinBy(r => r.Duration) : null;

    /// <summary>Gets the slowest test result, or <c>null</c> if no results exist.</summary>
    internal TestResult? Slowest => Results.Count > 0 ? Results.MaxBy(r => r.Duration) : null;

    /// <summary>Gets or sets the complete log output from the test run.</summary>
    internal List<string> LogLines { get; set; } = [];
}