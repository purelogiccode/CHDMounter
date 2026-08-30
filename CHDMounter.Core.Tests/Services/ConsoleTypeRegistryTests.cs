namespace CHDMounter.Core.Tests.Services;

/// <summary>
///     Tests for <see cref="ConsoleTypeRegistry" /> — the single source of truth
///     for console aliases. Numeric indexes are not supported; the registry is
///     the only way to resolve a console type in CHDMounter.
/// </summary>
public class ConsoleTypeRegistryTests
{
    [Theory]
    [InlineData("3do", ConsoleType.ThreeDo)]
    [InlineData("amigacd", ConsoleType.AmigaCd)]
    [InlineData("amigacd32", ConsoleType.AmigaCd32)]
    [InlineData("amigacdtv", ConsoleType.AmigaCdtv)]
    [InlineData("cdi", ConsoleType.CDi)]
    [InlineData("fmtowns", ConsoleType.FmTowns)]
    [InlineData("neogeocd", ConsoleType.NeoGeoCd)]
    [InlineData("nuon", ConsoleType.Nuon)]
    [InlineData("pcengine", ConsoleType.PcEngineCd)]
    [InlineData("pc98", ConsoleType.Pc98)]
    [InlineData("pcfx", ConsoleType.PcFx)]
    [InlineData("pico", ConsoleType.Pico)]
    [InlineData("pippin", ConsoleType.Pippin)]
    [InlineData("psauto", ConsoleType.PlayStation)]
    [InlineData("ps1", ConsoleType.Ps1)]
    [InlineData("ps2", ConsoleType.Ps2)]
    [InlineData("ps3", ConsoleType.Ps3)]
    [InlineData("isoraw2352", ConsoleType.GenericIsoRaw2352)]
    [InlineData("psp", ConsoleType.Psp)]
    [InlineData("segadreamcast", ConsoleType.Dreamcast)]
    [InlineData("segagenesis", ConsoleType.SegaGenesisCd)]
    [InlineData("segasaturn", ConsoleType.Saturn)]
    [InlineData("x68000", ConsoleType.X68000)]
    [InlineData("xbox", ConsoleType.Xbox)]
    [InlineData("xbox360", ConsoleType.Xbox360)]
    [InlineData("iso9660", ConsoleType.GenericIso9660)]
    [InlineData("isoraw2048", ConsoleType.GenericIsoRaw2048)]
    [InlineData("cueiso2352", ConsoleType.GenericCueIso2352)]
    [InlineData("cueiso2048", ConsoleType.GenericCueIso2048)]
    [InlineData("cueisowav2352", ConsoleType.GenericCueIsoWav2352)]
    [InlineData("cueisowav2048", ConsoleType.GenericCueIsoWav2048)]
    [InlineData("cuebin2352", ConsoleType.GenericCueBin2352)]
    [InlineData("cuebin2048", ConsoleType.GenericCueBin2048)]
    [InlineData("cuebinwav2352", ConsoleType.GenericCueBinWav2352)]
    [InlineData("cuebinwav2048", ConsoleType.GenericCueBinWav2048)]
    public void ParseReturnsCorrectType(string alias, ConsoleType expected)
    {
        Assert.Equal(expected, ConsoleTypeRegistry.Parse(alias));
    }

    [Theory]
    [InlineData("PS2", ConsoleType.Ps2)]
    [InlineData("Ps1", ConsoleType.Ps1)]
    [InlineData("pS1", ConsoleType.Ps1)]
    [InlineData("CueIso2048", ConsoleType.GenericCueIso2048)]
    [InlineData("SEGADREAMCAST", ConsoleType.Dreamcast)]
    [InlineData("  ps3  ", ConsoleType.Ps3)]
    public void ParseIsCaseInsensitiveAndTrims(string alias, ConsoleType expected)
    {
        Assert.Equal(expected, ConsoleTypeRegistry.Parse(alias));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("unknown")]
    [InlineData("nintendo")]
    [InlineData("playstation")]
    [InlineData("ngcd")]
    [InlineData("cuebin")]
    [InlineData("iso")]
    [InlineData("17")]
    [InlineData("8")]
    public void ParseReturnsUnknownForInvalidOrNumeric(string? alias)
    {
        // Numeric indexes are intentionally not supported anymore.
        Assert.Equal(ConsoleType.Unknown, ConsoleTypeRegistry.Parse(alias));
    }

    [Fact]
    public void EveryRegisteredEntryHasAliasesAndDisplayName()
    {
        Assert.NotEmpty(ConsoleTypeRegistry.All);
        foreach (var entry in ConsoleTypeRegistry.All)
        {
            Assert.False(string.IsNullOrEmpty(entry.DisplayName), $"{entry.Type} has no display name");
            Assert.NotEmpty(entry.Aliases);
        }
    }

    [Fact]
    public void DuplicateAliasesResolveToTheSameType()
    {
        // "PS3 ISO RAW 2352", "Xbox ISO RAW 2352" and "ISO RAW 2352"
        // all share the isoraw2352 alias and must resolve to the same type.
        Assert.Equal(ConsoleType.GenericIsoRaw2352, ConsoleTypeRegistry.Parse("isoraw2352"));
        var alias = ConsoleTypeRegistry.GetAliases(ConsoleType.GenericIsoRaw2352).FirstOrDefault()!;
        Assert.Equal(ConsoleType.GenericIsoRaw2352, ConsoleTypeRegistry.Parse(alias));
    }

    [Fact]
    public void GetDisplayNameReturnsRegisteredName()
    {
        Assert.Equal("CUE/BIN RAW 2352", ConsoleTypeRegistry.GetDisplayName(ConsoleType.GenericCueBin2352));
        Assert.Equal("ISO RAW 2048", ConsoleTypeRegistry.GetDisplayName(ConsoleType.GenericIsoRaw2048));
        Assert.Equal("PS3", ConsoleTypeRegistry.GetDisplayName(ConsoleType.Ps3));
        Assert.Equal("ISO 9660", ConsoleTypeRegistry.GetDisplayName(ConsoleType.GenericIso9660));
    }

    [Fact]
    public void RegistryMatchesSupportedConsoleTable()
    {
        // Exact order and aliases from the supported-console table.
        var expected = new (string DisplayName, string[] Aliases)[]
        {
            ("3DO", ["3do"]),
            ("Amiga CD", ["amigacd"]),
            ("Amiga CD32", ["amigacd32"]),
            ("Amiga CDTV", ["amigacdtv"]),
            ("CD-i", ["cdi"]),
            ("FM Towns", ["fmtowns"]),
            ("Neo Geo CD", ["neogeocd"]),
            ("Nuon", ["nuon"]),
            ("PC Engine CD", ["pcengine"]),
            ("PC-98", ["pc98"]),
            ("PC-FX", ["pcfx"]),
            ("Pico", ["pico"]),
            ("Pippin", ["pippin"]),
            ("PlayStation (Auto)", ["psauto"]),
            ("PS1", ["ps1"]),
            ("PS2", ["ps2"]),
            ("PS3", ["ps3"]),
            ("PS3 ISO RAW 2352", ["isoraw2352"]),
            ("PSP", ["psp"]),
            ("Sega Dreamcast", ["segadreamcast"]),
            ("Sega Genesis", ["segagenesis"]),
            ("Sega Saturn", ["segasaturn"]),
            ("X68000", ["x68000"]),
            ("Xbox", ["xbox"]),
            ("Xbox 360", ["xbox360"]),
            ("Xbox ISO RAW 2352", ["isoraw2352"]),
            ("ISO 9660", ["iso9660"]),
            ("ISO RAW 2048", ["isoraw2048"]),
            ("CUE/ISO RAW 2048", ["cueiso2048"]),
            ("CUE/ISO/WAV RAW 2048", ["cueisowav2048"]),
            ("ISO RAW 2352", ["isoraw2352"]),
            ("CUE/ISO RAW 2352", ["cueiso2352"]),
            ("CUE/ISO/WAV RAW 2352", ["cueisowav2352"]),
            ("CUE/BIN RAW 2048", ["cuebin2048"]),
            ("CUE/BIN/WAV RAW 2048", ["cuebinwav2048"]),
            ("CUE/BIN RAW 2352", ["cuebin2352"]),
            ("CUE/BIN/WAV RAW 2352", ["cuebinwav2352"])
        };

        var actual = ConsoleTypeRegistry.All.ToList();
        Assert.Equal(expected.Length, actual.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].DisplayName, actual[i].DisplayName);
            Assert.Equal(expected[i].Aliases, actual[i].Aliases);
        }
    }
}