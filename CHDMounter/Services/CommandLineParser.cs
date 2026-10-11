using CHDMounter.Models;
using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Services;

/// <summary>
///     Parses the CHDMounter command line:
///     <c>CHDMounter [/l] [/a] [/s:&lt;alias&gt;] &lt;chd_file&gt; [mount_point]</c>.
/// </summary>
internal static class CommandLineParser
{
    /// <summary>
    ///     Parses the command-line arguments into structured options. Only the
    ///     documented flags are treated as flags: on Linux and macOS absolute paths
    ///     also start with '/', so a leading slash alone must not mark an argument as
    ///     a flag. A console alias may also be passed as a positional argument.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The parsed options.</returns>
    internal static CommandLineOptions Parse(string[] args)
    {
        ConsoleType? consoleType = null;
        var launchExplorer = false;
        var autoSelectMountPoint = false;
        var positional = new List<string>();

        foreach (var arg in args)
        {
            var normalized = arg.ToLowerInvariant();
            if (string.Equals(normalized, "/l", StringComparison.Ordinal))
            {
                launchExplorer = true;
            }
            else if (string.Equals(normalized, "/a", StringComparison.Ordinal))
            {
                autoSelectMountPoint = true;
            }
            else if (normalized.StartsWith("/s:", StringComparison.Ordinal))
            {
                consoleType = ConsoleTypeRegistry.Parse(normalized[3..]);
                if (consoleType == ConsoleType.Unknown) consoleType = null;
            }
            else
            {
                positional.Add(arg);
            }
        }

        string? chdPath = null;
        string? mountPoint = null;
        var pos = 0;

        // A leading positional that is a console alias (followed by a path)
        // selects the console type; otherwise the first positional is the
        // CHD path. Console types are resolved exclusively through
        // ConsoleTypeRegistry - numeric indexes are not supported.
        if (positional.Count >= 2
            && ConsoleTypeRegistry.Parse(positional[0]) is var firstType
            && firstType != ConsoleType.Unknown)
        {
            consoleType ??= firstType;
            pos++;
        }

        if (positional.Count > pos)
        {
            chdPath = positional[pos];
            pos++;
        }

        if (positional.Count > pos)
        {
            var ct = ConsoleTypeRegistry.Parse(positional[pos]);
            if (ct != ConsoleType.Unknown)
                consoleType ??= ct;
            else
                mountPoint = positional[pos];

            pos++;
        }

        if (positional.Count > pos) mountPoint = positional[pos];

        // /a forces automatic drive-letter selection even when a mount point is given.
        if (autoSelectMountPoint)
            mountPoint = null;

        return new CommandLineOptions
        {
            ConsoleType = consoleType ?? ConsoleType.Unknown,
            ChdPath = chdPath,
            MountPoint = mountPoint,
            LaunchExplorer = launchExplorer,
            AutoSelectMountPoint = autoSelectMountPoint
        };
    }
}
