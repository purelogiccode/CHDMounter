namespace Tester.Models;

/// <summary>
///     Represents the result of parsing a single CHD file during testing.
/// </summary>
/// <param name="FileName">The file name of the CHD file.</param>
/// <param name="FilePath">The full path to the CHD file.</param>
/// <param name="Success">Whether the CHD file was parsed successfully.</param>
/// <param name="ErrorMessage">The error message if parsing failed; otherwise, empty.</param>
/// <param name="VolumeName">The volume name extracted from the CHD file.</param>
/// <param name="VolumeSize">The total size of the volume in bytes.</param>
/// <param name="FileCount">The number of files found in the CHD image.</param>
/// <param name="DirectoryCount">The number of directories found in the CHD image.</param>
/// <param name="Duration">The time taken to parse the CHD file.</param>
internal sealed record TestResult(
    string FileName,
    string FilePath,
    bool Success,
    string ErrorMessage,
    string VolumeName,
    ulong VolumeSize,
    int FileCount,
    int DirectoryCount,
    TimeSpan Duration
);