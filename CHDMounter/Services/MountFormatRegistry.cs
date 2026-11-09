using VideoGameFileSystemParser.Models;

namespace CHDMounter.Services;

/// <summary>
///     Describes one selectable mount format: the parser-facing console type plus any
///     CHDMounter-specific export option.
/// </summary>
/// <param name="DisplayName">The human-readable name shown in the UI.</param>
/// <param name="ConsoleType">The console type passed to the CHD parser.</param>
/// <param name="IncludeSubchannel">
///     <c>true</c> when the mounted CUE/BIN must use full 2448-byte CD frames
///     (2352 bytes of sector data plus 96 bytes of subchannel data).
/// </param>
internal sealed record MountFormatInfo(string DisplayName, ConsoleType ConsoleType, bool IncludeSubchannel);

/// <summary>
///     Builds the mount-format list shown in the UI and resolves command-line aliases.
///     The standard formats come from the parser package; <c>cuebin2448</c> is a
///     CHDMounter extension that exposes the subchannel data stored in CD+G CHDs.
/// </summary>
internal static class MountFormatRegistry
{
    /// <summary>The command-line alias for the 2448-byte CUE/BIN mode.</summary>
    internal const string CueBin2448Alias = "cuebin2448";

    /// <summary>The display name for the 2448-byte CUE/BIN mode.</summary>
    internal const string CueBin2448DisplayName = "CUE/BIN RAW 2448 (CD+G)";

    private const string UnknownDisplayName = "Unknown";

    /// <summary>All selectable formats, in the display order used by the parser registry.</summary>
    internal static IReadOnlyList<MountFormatInfo> All { get; } = BuildAll();

    /// <summary>The placeholder entry used when no format has been selected.</summary>
    internal static MountFormatInfo Unknown { get; } = new(UnknownDisplayName, ConsoleType.Unknown, false);

    /// <summary>
    ///     Resolves a command-line alias, including the CHDMounter-only <c>cuebin2448</c> alias.
    /// </summary>
    /// <param name="alias">The alias to resolve.</param>
    /// <param name="format">The matching format, or <see cref="Unknown" />.</param>
    /// <returns><c>true</c> if the alias was recognized; otherwise <c>false</c>.</returns>
    internal static bool TryParse(string? alias, out MountFormatInfo format)
    {
        if (string.Equals(alias?.Trim(), CueBin2448Alias, StringComparison.OrdinalIgnoreCase))
        {
            format = All.First(static f => f.IncludeSubchannel);
            return true;
        }

        var consoleType = ConsoleTypeRegistry.Parse(alias);
        if (consoleType == ConsoleType.Unknown)
        {
            format = Unknown;
            return false;
        }

        format = All.First(f => f.ConsoleType == consoleType && !f.IncludeSubchannel);
        return true;
    }

    /// <summary>
    ///     Finds the format entry for a console type and subchannel option.
    /// </summary>
    /// <param name="consoleType">The parser-facing console type.</param>
    /// <param name="includeSubchannel">Whether the 2448-byte CUE/BIN mode is requested.</param>
    /// <returns>The matching format, or <see cref="Unknown" />.</returns>
    internal static MountFormatInfo Find(ConsoleType consoleType, bool includeSubchannel)
    {
        return All.FirstOrDefault(f => f.ConsoleType == consoleType && f.IncludeSubchannel == includeSubchannel)
               ?? Unknown;
    }

    private static IReadOnlyList<MountFormatInfo> BuildAll()
    {
        var formats = ConsoleTypeRegistry.All
            .Select(static info => new MountFormatInfo(info.DisplayName, info.Type, false))
            .ToList();

        formats.Add(new MountFormatInfo(CueBin2448DisplayName, ConsoleType.GenericCueBin2352, true));
        return formats;
    }
}
