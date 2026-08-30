using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Core.Tests.Parsers;

public class ParserFactoryTests
{
    [Fact]
    public void GetAllSupportedConsolesReturnsAll21Plus()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles().ToList();
        Assert.True(consoles.Count >= 27);
    }

    [Fact]
    public void GetAllSupportedConsolesContainsExpectedConsoles()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles().ToList();
        Assert.Contains(consoles, static c => c.Type == ConsoleType.Xbox);
        Assert.Contains(consoles, static c => c.Type == ConsoleType.Ps1);
        Assert.Contains(consoles, static c => c.Type == ConsoleType.Dreamcast);
        Assert.Contains(consoles, static c => c.Type == ConsoleType.CDi);
        Assert.Contains(consoles, static c => c.Type == ConsoleType.ThreeDo);
        Assert.Contains(consoles, static c => c.Type == ConsoleType.X68000);
        Assert.Contains(consoles, static c => c.Type == ConsoleType.GenericIso9660);
        Assert.Contains(consoles, static c => c.Type == ConsoleType.GenericCueBin2352);
    }

    [Fact]
    public void GetAllSupportedConsolesAllHaveNonEmptyName()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles();
        foreach (var console in consoles)
            Assert.False(string.IsNullOrEmpty(console.Name), $"Console {console.Type} has empty name");
    }

    [Fact]
    public void GetAllSupportedConsolesNamesAreUnique()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles();
        var names = consoles.Select(static c => c.Name).ToList();
        Assert.Equal(names.Distinct(StringComparer.Ordinal).Count(), names.Count);
    }

    [Fact]
    public void GetAllSupportedConsolesEveryEntryResolvesViaItsAliases()
    {
        // Multiple display entries may intentionally share a ConsoleType
        // (e.g. the isoraw2352 entries), so types are not unique; instead
        // every entry must be resolvable from the console registry.
        var consoles = ParserFactory.GetAllSupportedConsoles().ToList();
        Assert.NotEmpty(consoles);
        foreach (var console in consoles)
        {
            var type = ConsoleTypeRegistry.Parse(ConsoleTypeRegistry.GetAliases(console.Type).FirstOrDefault() ?? "");
            Assert.Equal(console.Type, type);
        }
    }
}