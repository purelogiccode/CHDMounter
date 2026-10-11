namespace CHDMounter.Models;

/// <summary>
///     The command-line options parsed for the main window.
/// </summary>
internal sealed class CommandLineOptions
{
    /// <summary>
    ///     Gets the console/format type selected on the command line, or
    ///     <see cref="ConsoleType.Unknown" /> when no valid alias was supplied.
    /// </summary>
    internal ConsoleType ConsoleType { get; init; } = ConsoleType.Unknown;

    /// <summary>
    ///     Gets the path to the CHD file, or <c>null</c> when none was supplied.
    /// </summary>
    internal string? ChdPath { get; init; }

    /// <summary>
    ///     Gets the mount point requested on the command line, or <c>null</c> to
    ///     auto-select one.
    /// </summary>
    internal string? MountPoint { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the mounted drive should be opened in the
    ///     file manager (<c>/l</c>).
    /// </summary>
    internal bool LaunchExplorer { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the mount point must be auto-selected even
    ///     when one was supplied (<c>/a</c>).
    /// </summary>
    internal bool AutoSelectMountPoint { get; init; }
}
