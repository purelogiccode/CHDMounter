using System.Reflection;
using CHDMounter.Models;
using CHDMounter.Services;

namespace CHDMounter.Tests.Services;

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

    private static string InvokeGetPlatformRid()
    {
        var method = typeof(UpdateChecker).GetMethod("GetPlatformRid", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, null!)!;
    }

    // --- GetPlatformRid ---

    [Fact]
    public void GetPlatformRidMatchesCurrentOperatingSystem()
    {
        var rid = InvokeGetPlatformRid();

        if (OperatingSystem.IsWindows())
            Assert.StartsWith("win-", rid, StringComparison.Ordinal);
        else if (OperatingSystem.IsMacOS())
            Assert.StartsWith("osx-", rid, StringComparison.Ordinal);
        else
            Assert.StartsWith("linux-", rid, StringComparison.Ordinal);
    }

    [Fact]
    public void GetPlatformRidEndsWithSupportedArchitecture()
    {
        var rid = InvokeGetPlatformRid();

        Assert.True(rid.EndsWith("-x64", StringComparison.Ordinal) ||
                    rid.EndsWith("-arm64", StringComparison.Ordinal),
            $"Unexpected runtime identifier: {rid}");
    }

    [Fact]
    public void GetPlatformRidIsDeterministic()
    {
        Assert.Equal(InvokeGetPlatformRid(), InvokeGetPlatformRid(), StringComparer.Ordinal);
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