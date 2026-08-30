namespace CHDMounter.Core.Tests.Models;

public class ConsoleTypeTests
{
    [Fact]
    public void ConsoleTypeAllMembersAreDefined()
    {
        var values = Enum.GetValues<ConsoleType>();
        Assert.Contains(ConsoleType.Xbox, values);
        Assert.Contains(ConsoleType.Xbox360, values);
        Assert.Contains(ConsoleType.Ps1, values);
        Assert.Contains(ConsoleType.Ps2, values);
        Assert.Contains(ConsoleType.Ps3, values);
        Assert.Contains(ConsoleType.Psp, values);
        Assert.Contains(ConsoleType.Dreamcast, values);
        Assert.Contains(ConsoleType.CDi, values);
        Assert.Contains(ConsoleType.ThreeDo, values);
        Assert.Contains(ConsoleType.GenericIso9660, values);
    }

    [Fact]
    public void ConsoleTypeUnknownIsZero()
    {
        Assert.Equal(0, (int)ConsoleType.Unknown);
    }

    [Fact]
    public void ConsoleTypeCountGreaterThan20()
    {
        var count = Enum.GetValues<ConsoleType>().Length;
        Assert.True(count >= 22);
    }
}

public class FsNodeTypeTests
{
    [Fact]
    public void FsNodeTypeHasExpectedValues()
    {
        var values = Enum.GetValues<FsNodeType>();
        Assert.Contains(FsNodeType.File, values);
        Assert.Contains(FsNodeType.Directory, values);
        Assert.Contains(FsNodeType.Symlink, values);
    }

    [Fact]
    public void FsNodeTypeFileIsZero()
    {
        Assert.Equal(0, (int)FsNodeType.File);
    }

    [Fact]
    public void FsNodeTypeNumericValuesAreAsExpected()
    {
        Assert.Equal(0, (int)FsNodeType.File);
        Assert.Equal(4, (int)FsNodeType.Directory);
        Assert.Equal(12, (int)FsNodeType.Symlink);
    }
}