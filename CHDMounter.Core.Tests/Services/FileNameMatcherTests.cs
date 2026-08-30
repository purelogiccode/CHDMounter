namespace CHDMounter.Core.Tests.Services;

public class FileNameMatcherTests
{
    [Theory]
    [InlineData("Xeno Crisis (World) (Unl).cue", "<.cue", true)] // NT 8.3 DOS wildcard form of "*.cue"
    [InlineData("Xeno Crisis (World) (Unl).bin", "<.bin", true)]
    [InlineData("Xeno Crisis (World) (Unl).cue", "<.bin", false)]
    [InlineData("Xeno Crisis (World) (Unl).cue", "*.cue", true)] // plain form must keep working too
    [InlineData("Xeno Crisis (World) (Unl).bin", "*.cue", false)]
    [InlineData("Xeno Crisis (World) (Unl).cue", "<",
        false)] // bare DOS_STAR matches nothing (Dokan reference semantics)
    [InlineData("Xeno Crisis (World) (Unl).cue", "*", true)]
    [InlineData("Xeno Crisis (World) (Unl).cue", "*.*", true)]
    [InlineData("Xeno Crisis (World) (Unl).cue", "<.*", true)]
    [InlineData("xeno.iso", "<.iso", true)]
    [InlineData("xeno", "<", false)]
    [InlineData("xeno", "<.iso", false)]
    [InlineData("multi.part.name.bin", "<.bin", true)] // DOS_STAR stops at the LAST dot
    [InlineData("multi.part.name.bin", "<.cue", false)]
    [InlineData("abc", "???", true)]
    [InlineData("abcd", "???", false)]
    [InlineData("abc", "a>c", true)]
    [InlineData("a.c", "a>c", false)] // DOS_QM does not consume a trailing dot
    [InlineData("file", "file>", false)] // trailing DOS_QM cannot match a shorter name
    [InlineData("file1", "file>", true)]
    [InlineData("", ">", false)] // DOS_QM with exhausted name matches nothing
    [InlineData("a", "a*>", false)] // '*' loop must not let a trailing DOS_QM match
    [InlineData("ab", "a*", true)]
    [InlineData("name.cue", "\"cue", false)] // DOS_DOT requires the dot
    [InlineData("name.cue", "<\"cue", true)] // "<" + DOS_DOT
    [InlineData("XENO.CUE", "<.cue", true)] // case-insensitive
    [InlineData("game", "game", true)]
    [InlineData("game", "games", false)]
    [InlineData("", "*", true)]
    [InlineData("", "<", true)]
    [InlineData("", "?", false)]
    public void IsMatch_ReturnsExpected(string name, string pattern, bool expected)
    {
        Assert.Equal(expected, FileNameMatcher.IsMatch(name, pattern));
    }
}