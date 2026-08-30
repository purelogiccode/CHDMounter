using System.Reflection;

namespace CHDMounter.Core.Tests.Services;

public class BugReportClientTests
{
    private static string InvokeTruncate(string value, int maxLength)
    {
        var method = typeof(BugReportClient).GetMethod("Truncate", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, [value, maxLength])!;
    }

    [Fact]
    public void TruncateReturnsOriginalWhenShorterThanMax()
    {
        var result = InvokeTruncate("hello", 10);
        Assert.Equal("hello", result);
    }

    [Fact]
    public void TruncateReturnsOriginalWhenEqualToMax()
    {
        var result = InvokeTruncate("hello", 5);
        Assert.Equal("hello", result);
    }

    [Fact]
    public void TruncateReturnsTruncatedWithEllipsisWhenLongerThanMax()
    {
        var result = InvokeTruncate("hello world", 8);
        Assert.Equal("hello...", result);
        Assert.Equal(8, result.Length);
    }

    [Fact]
    public void TruncateHandlesEmptyString()
    {
        var result = InvokeTruncate("", 10);
        Assert.Equal("", result);
    }

    [Fact]
    public void TruncateHandlesNullString()
    {
        var result = InvokeTruncate(null!, 10);
        Assert.Null(result);
    }

    [Fact]
    public void TruncateWithMaxLengthFourReturnsOriginalMinusOnePlusEllipsis()
    {
        var result = InvokeTruncate("hello", 4);
        Assert.Equal("h...", result);
    }

    [Fact]
    public void TruncateLongStringCorrectly()
    {
        var longString = new string('a', 5000);
        var result = InvokeTruncate(longString, 4000);
        Assert.Equal(4000, result.Length);
        Assert.EndsWith("...", result, StringComparison.Ordinal);
    }
}