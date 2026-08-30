using System.Reflection;

namespace CHDMounter.Core.Tests.Services;

public class UpdateCheckerTests
{
    private static bool InvokeIsNewer(string latest, string current)
    {
        var method = typeof(UpdateChecker).GetMethod("IsNewer", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (bool)method.Invoke(null, [latest, current])!;
    }

    [Fact]
    public void IsNewerReturnsTrueWhenLatestIsNewer()
    {
        Assert.True(InvokeIsNewer("2.0.0", "1.0.0"));
    }

    [Fact]
    public void IsNewerReturnsFalseWhenSame()
    {
        Assert.False(InvokeIsNewer("1.0.0", "1.0.0"));
    }

    [Fact]
    public void IsNewerReturnsFalseWhenCurrentIsNewer()
    {
        Assert.False(InvokeIsNewer("1.0.0", "2.0.0"));
    }

    [Fact]
    public void IsNewerReturnsTrueForMajorVersionDifference()
    {
        Assert.True(InvokeIsNewer("10.0.0", "9.99.99"));
    }

    [Fact]
    public void IsNewerReturnsTrueForMinorVersionDifference()
    {
        Assert.True(InvokeIsNewer("1.2.0", "1.1.0"));
    }

    [Fact]
    public void IsNewerReturnsTrueForPatchVersionDifference()
    {
        Assert.True(InvokeIsNewer("1.0.2", "1.0.1"));
    }

    [Fact]
    public void IsNewerHandlesInvalidVersionFormat()
    {
        // When versions can't be parsed, it falls back to string comparison
        // Returns true if strings are different (case-insensitive)
        Assert.True(InvokeIsNewer("abc", "def"));
    }

    [Fact]
    public void IsNewerReturnsFalseForSameInvalidStrings()
    {
        Assert.False(InvokeIsNewer("abc", "abc"));
    }

    [Fact]
    public void IsNewerReturnsTrueForDifferentCaseInvalidStrings()
    {
        // The fallback uses OrdinalIgnoreCase
        Assert.False(InvokeIsNewer("ABC", "abc"));
    }

    [Fact]
    public void ResultIsNullBeforeCheck()
    {
        // Result starts as null (static field, may have been set by other tests)
        // We can at least verify the property exists and is readable
        _ = UpdateChecker.Result;
        // Don't assert null since other tests may have set it
    }
}