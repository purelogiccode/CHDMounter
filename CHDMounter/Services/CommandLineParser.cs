using CHDMounter.Models;

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
        var includeSubchannel = false;
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
                if (MountFormatRegistry.TryParse(normalized[3..], out var format))
                {
                    consoleType = format.ConsoleType;
                    includeSubchannel = format.IncludeSubchannel;
                }
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
        // MountFormatRegistry - numeric indexes are not supported.
        if (positional.Count >= 2
            && MountFormatRegistry.TryParse(positional[0], out var firstFormat))
        {
            if (consoleType is null)
            {
                consoleType = firstFormat.ConsoleType;
                includeSubchannel = firstFormat.IncludeSubchannel;
            }

            pos++;
        }

        if (positional.Count > pos)
        {
            chdPath = positional[pos];
            pos++;
        }

        if (positional.Count > pos)
        {
            if (MountFormatRegistry.TryParse(positional[pos], out var format))
            {
                if (consoleType is null)
                {
                    consoleType = format.ConsoleType;
                    includeSubchannel = format.IncludeSubchannel;
                }
            }
            else
            {
                mountPoint = positional[pos];
            }

            pos++;
        }

        if (positional.Count > pos) mountPoint = positional[pos];

        // /a forces automatic drive-letter selection even when a mount point is given.
        if (autoSelectMountPoint)
            mountPoint = null;

        return new CommandLineOptions
        {
            ConsoleType = consoleType ?? ConsoleType.Unknown,
            IncludeSubchannel = includeSubchannel,
            ChdPath = chdPath,
            MountPoint = mountPoint,
            LaunchExplorer = launchExplorer,
            AutoSelectMountPoint = autoSelectMountPoint
        };
    }
}
