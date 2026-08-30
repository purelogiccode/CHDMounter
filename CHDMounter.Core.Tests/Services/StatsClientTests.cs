using System.Reflection;

namespace CHDMounter.Core.Tests.Services;

public class StatsClientTests
{
    private static string InvokeGetApiKey()
    {
        var method = typeof(AppInfoHelper).GetMethod("GetApiKey", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, null!)!;
    }

    private static string InvokeGetAppId()
    {
        return AppInfoHelper.GetAppName().ToLowerInvariant();
    }

    private static string InvokeGetVersion()
    {
        var method = typeof(AppInfoHelper).GetMethod("GetVersion", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, null!)!;
    }

    // --- GetApiKey ---

    [Fact]
    public void GetApiKeyReturnsNonEmptyString()
    {
        var result = InvokeGetApiKey();
        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void GetApiKeyReturnsNonWhitespaceString()
    {
        var result = InvokeGetApiKey();
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    // --- GetAppId ---

    [Fact]
    public void GetAppIdReturnsNonEmptyString()
    {
        var result = InvokeGetAppId();
        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void GetAppIdReturnsLowercaseString()
    {
        var result = InvokeGetAppId();
        Assert.Equal(result.ToLowerInvariant(), result);
    }

    [Fact]
    public void GetAppIdReturnsExpectedFallback()
    {
        // In test context, GetEntryAssembly may return test runner
        // The method falls back to "CHDMounter" if null
        var result = InvokeGetAppId();
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    // --- GetVersion ---

    [Fact]
    public void GetVersionReturnsNonEmptyString()
    {
        var result = InvokeGetVersion();
        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void GetVersionReturnsVersionWithDots()
    {
        var result = InvokeGetVersion();
        Assert.Contains(".", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVersionReturnsExpectedFallback()
    {
        var result = InvokeGetVersion();
        Assert.NotNull(result);
        // Should be parseable as a version or at least contain dots
        Assert.True(result.Split('.').Length >= 2);
    }

    // --- SendStats ---

    [Fact]
    public void SendStatsDoesNotThrow()
    {
        var exception = Record.Exception(() => StatsClient.SendStats());
        Assert.Null(exception);
    }

    [Fact]
    public void SendStatsCalledTwiceDoesNotThrow()
    {
        // The second call should be ignored due to Interlocked guard
        StatsClient.SendStats();
        var exception = Record.Exception(() => StatsClient.SendStats());
        Assert.Null(exception);
    }
}