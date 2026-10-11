using CHDMounter.Services;
using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Tests.Services;

public class CommandLineParserTests
{
    [Fact]
    public void EmptyArgsReturnDefaults()
    {
        var options = CommandLineParser.Parse([]);

        Assert.Null(options.ChdPath);
        Assert.Null(options.MountPoint);
        Assert.Equal(ConsoleType.Unknown, options.ConsoleType);
        Assert.False(options.LaunchExplorer);
        Assert.False(options.AutoSelectMountPoint);
    }

    [Fact]
    public void LinuxAbsolutePathsAreTreatedAsPositional()
    {
        // Regression: on Linux/macOS absolute paths start with '/', which used to be
        // mistaken for a flag marker and silently dropped.
        var options = CommandLineParser.Parse(["/s:iso9660", "/home/vm/game.chd", "/home/vm/mnt"]);

        Assert.Equal("/home/vm/game.chd", options.ChdPath);
        Assert.Equal("/home/vm/mnt", options.MountPoint);
        Assert.Equal(ConsoleTypeRegistry.Parse("iso9660"), options.ConsoleType);
    }

    [Fact]
    public void AbsoluteChdPathWithoutMountPointIsPositional()
    {
        var options = CommandLineParser.Parse(["/home/vm/game.chd"]);

        Assert.Equal("/home/vm/game.chd", options.ChdPath);
        Assert.Null(options.MountPoint);
        Assert.Equal(ConsoleType.Unknown, options.ConsoleType);
    }

    [Fact]
    public void LaunchExplorerFlagIsCaseInsensitive()
    {
        var options = CommandLineParser.Parse(["/L", "game.chd"]);

        Assert.True(options.LaunchExplorer);
        Assert.Equal("game.chd", options.ChdPath);
    }

    [Fact]
    public void AutoSelectFlagClearsExplicitMountPoint()
    {
        var options = CommandLineParser.Parse(["/a", "/s:ps2", "game.chd", "M:"]);

        Assert.True(options.AutoSelectMountPoint);
        Assert.Null(options.MountPoint);
        Assert.Equal("game.chd", options.ChdPath);
    }

    [Fact]
    public void ConsoleAliasAsPositionalAfterPathSelectsConsoleType()
    {
        var options = CommandLineParser.Parse(["game.chd", "ps2"]);

        Assert.Equal("game.chd", options.ChdPath);
        Assert.Null(options.MountPoint);
        Assert.Equal(ConsoleTypeRegistry.Parse("ps2"), options.ConsoleType);
    }

    [Fact]
    public void ConsoleAliasAsLeadingPositionalSelectsConsoleType()
    {
        var options = CommandLineParser.Parse(["ps2", "game.chd"]);

        Assert.Equal("game.chd", options.ChdPath);
        Assert.Equal(ConsoleTypeRegistry.Parse("ps2"), options.ConsoleType);
    }

    [Fact]
    public void UnknownAliasFallsBackToMountPoint()
    {
        var options = CommandLineParser.Parse(["/s:ps2", "game.chd", "notanalias"]);

        Assert.Equal("notanalias", options.MountPoint);
        Assert.Equal(ConsoleTypeRegistry.Parse("ps2"), options.ConsoleType);
    }

    [Fact]
    public void ExplicitMountPointIsPreserved()
    {
        var options = CommandLineParser.Parse(["/s:ps2", "game.chd", "M:"]);

        Assert.Equal("M:", options.MountPoint);
    }

    [Fact]
    public void SwitchAliasTakesPrecedenceOverPositionalAlias()
    {
        var options = CommandLineParser.Parse(["/s:ps1", "game.chd", "ps2"]);

        Assert.Equal("game.chd", options.ChdPath);
        Assert.Null(options.MountPoint);
        Assert.Equal(ConsoleTypeRegistry.Parse("ps1"), options.ConsoleType);
    }
}
