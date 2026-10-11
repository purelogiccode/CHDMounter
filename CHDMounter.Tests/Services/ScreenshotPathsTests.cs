using CHDMounter.Services;

namespace CHDMounter.Tests.Services;

public class ScreenshotPathsTests
{
    [Fact]
    public void GetPrimaryDirectoryIsInsideApplicationFolder()
    {
        var result = ScreenshotPaths.GetPrimaryDirectory();

        Assert.StartsWith(AppContext.BaseDirectory, result, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("Screenshot", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetFallbackDirectoryUsesProvidedApplicationName()
    {
        var result = ScreenshotPaths.GetFallbackDirectory("SomeApp");

        Assert.Contains("SomeApp", result, StringComparison.Ordinal);
        Assert.EndsWith("Screenshot", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetFallbackDirectoryIsInsideLocalApplicationData()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var result = ScreenshotPaths.GetFallbackDirectory("SomeApp");

        Assert.StartsWith(localAppData, result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetFallbackDirectoryVariesWithApplicationName()
    {
        var first = ScreenshotPaths.GetFallbackDirectory("AppOne");
        var second = ScreenshotPaths.GetFallbackDirectory("AppTwo");

        Assert.NotEqual(first, second, StringComparer.OrdinalIgnoreCase);
    }
}
