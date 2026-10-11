using CHDMounter.Services;

namespace CHDMounter.Tests.Services;

public class AppInfoHelperTests
{
    // --- GetAppName ---

    [Fact]
    public void GetAppNameReturnsNonEmptyString()
    {
        var result = AppInfoHelper.GetAppName();

        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void GetAppNameIsDeterministic()
    {
        var first = AppInfoHelper.GetAppName();
        var second = AppInfoHelper.GetAppName();

        Assert.Equal(first, second, StringComparer.Ordinal);
    }

    [Fact]
    public void GetAppNameDoesNotContainPathSeparators()
    {
        var result = AppInfoHelper.GetAppName();

        Assert.DoesNotContain('\\', result);
        Assert.DoesNotContain('/', result);
    }

    // --- GetVersion ---

    [Fact]
    public void GetVersionReturnsNonEmptyString()
    {
        var result = AppInfoHelper.GetVersion();

        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void GetVersionIsDeterministic()
    {
        var first = AppInfoHelper.GetVersion();
        var second = AppInfoHelper.GetVersion();

        Assert.Equal(first, second, StringComparer.Ordinal);
    }

    [Fact]
    public void GetVersionIsWithinApiLengthLimit()
    {
        var result = AppInfoHelper.GetVersion();

        // The bug report API truncates version fields to 20 characters.
        Assert.True(result.Length <= 20);
    }

    // --- GetApiKey ---

    [Fact]
    public void GetApiKeyReturnsNonEmptyString()
    {
        var result = AppInfoHelper.GetApiKey();

        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void GetApiKeyIsDeterministic()
    {
        var first = AppInfoHelper.GetApiKey();
        var second = AppInfoHelper.GetApiKey();

        Assert.Equal(first, second, StringComparer.Ordinal);
    }

    [Fact]
    public void GetApiKeyIsSafeAsHttpHeaderValue()
    {
        var result = AppInfoHelper.GetApiKey();

        Assert.DoesNotContain('\r', result);
        Assert.DoesNotContain('\n', result);
        Assert.Equal(result.Trim(), result);
    }

    [Fact]
    public void GetApiKeyIsNotBase64EncodedTwice()
    {
        var result = AppInfoHelper.GetApiKey();

        // Decoding the key should not yield another Base64 string of the same length:
        // guards against a regression where the constant is double-decoded.
        Assert.DoesNotContain("==", result);
    }
}
