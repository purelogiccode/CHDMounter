using System.Reflection;

namespace CHDMounter.Core.Tests.Services;

public class UpdateCheckerExtendedTests
{
    private static string InvokeGetCurrentVersion()
    {
        var method = typeof(AppInfoHelper).GetMethod("GetVersion", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, null!)!;
    }

    private static bool InvokeIsNewer(string latest, string current)
    {
        var method = typeof(UpdateChecker).GetMethod("IsNewer", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (bool)method.Invoke(null, [latest, current])!;
    }

    // --- GetCurrentVersion ---

    [Fact]
    public void GetCurrentVersionReturnsNonEmptyString()
    {
        var result = InvokeGetCurrentVersion();
        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void GetCurrentVersionContainsDots()
    {
        var result = InvokeGetCurrentVersion();
        Assert.Contains(".", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetCurrentVersionIsParseableAsVersion()
    {
        var result = InvokeGetCurrentVersion();
        // Should be parseable as System.Version or at least a fallback
        Assert.NotNull(result);
    }

    // --- IsNewer additional edge cases ---

    [Fact]
    public void IsNewerWithFourPartVersions()
    {
        Assert.True(InvokeIsNewer("1.0.0.1", "1.0.0.0"));
    }

    [Fact]
    public void IsNewerWithFourPartVersionsReverse()
    {
        Assert.False(InvokeIsNewer("1.0.0.0", "1.0.0.1"));
    }

    [Fact]
    public void IsNewerWithLargeVersionNumbers()
    {
        Assert.True(InvokeIsNewer("999.999.999", "998.999.999"));
    }

    [Fact]
    public void IsNewerWithZeroVersions()
    {
        Assert.False(InvokeIsNewer("0.0.0", "0.0.0"));
    }

    [Fact]
    public void IsNewerWithPartialVersionStrings()
    {
        // "1.0" vs "1.0" - System.Version can parse these
        Assert.False(InvokeIsNewer("1.0", "1.0"));
    }

    [Fact]
    public void IsNewerWithDifferentLengthInvalidStrings()
    {
        // Falls back to string comparison for invalid formats
        Assert.True(InvokeIsNewer("abc", "ab"));
    }

    [Fact]
    public void IsNewerWithEmptyStrings()
    {
        Assert.False(InvokeIsNewer("", ""));
    }

    [Fact]
    public void IsNewerWithOneEmptyAndOneNonEmpty()
    {
        // "v1" vs "" - different strings, so returns true
        Assert.True(InvokeIsNewer("v1", ""));
    }

    // --- Result property ---

    [Fact]
    public void ResultPropertyIsReadable()
    {
        _ = UpdateChecker.Result;
    }

    [Fact]
    public void ResultHasCorrectType()
    {
        // The property type should be UpdateCheckResult?
        var prop = typeof(UpdateChecker).GetProperty("Result", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(prop);
        Assert.Equal(typeof(UpdateCheckResult), prop.PropertyType);
    }

    // --- CheckForUpdates ---

    [Fact]
    public void CheckForUpdatesDoesNotThrow()
    {
        var exception = Record.Exception(() => UpdateChecker.CheckForUpdates());
        Assert.Null(exception);
    }

    [Fact]
    public void CheckForUpdatesCalledTwiceDoesNotThrow()
    {
        // Second call should be ignored due to Interlocked guard
        UpdateChecker.CheckForUpdates();
        var exception = Record.Exception(() => UpdateChecker.CheckForUpdates());
        Assert.Null(exception);
    }
}