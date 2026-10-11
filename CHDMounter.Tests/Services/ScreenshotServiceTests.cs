using CHDMounter.Interfaces;
using CHDMounter.Services;

namespace CHDMounter.Tests.Services;

public class ScreenshotServiceTests
{
    [Fact]
    public void ScreenshotServiceImplementsInterface()
    {
        var service = new ScreenshotService(new LoggingService(dispatcher: null));

        Assert.IsAssignableFrom<IScreenshotService>(service);
    }

    [Fact]
    public void TakeScreenshotDoesNotThrowWhenNoWindowIsActive()
    {
        var service = new ScreenshotService(new LoggingService(dispatcher: null));

        // Without an initialized UI there is no active window; the service must
        // report the condition through logging instead of throwing.
        var exception = Record.Exception(service.TakeScreenshot);

        Assert.Null(exception);
    }

    [Fact]
    public void GetPrimaryScreenshotDirectoryIsInsideApplicationFolder()
    {
        var result = ScreenshotService.GetPrimaryScreenshotDirectory();

        Assert.StartsWith(AppContext.BaseDirectory, result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetPrimaryScreenshotDirectoryEndsWithScreenshotFolder()
    {
        var result = ScreenshotService.GetPrimaryScreenshotDirectory();

        Assert.EndsWith("Screenshot", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetPrimaryScreenshotDirectoryIsFullyQualified()
    {
        var result = ScreenshotService.GetPrimaryScreenshotDirectory();

        Assert.True(Path.IsPathFullyQualified(result));
    }

    [Fact]
    public void GetFallbackScreenshotDirectoryIsInsideLocalApplicationData()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var result = ScreenshotService.GetFallbackScreenshotDirectory();

        Assert.StartsWith(localAppData, result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetFallbackScreenshotDirectoryContainsApplicationName()
    {
        var result = ScreenshotService.GetFallbackScreenshotDirectory();

        Assert.Contains(AppInfoHelper.GetAppName(), result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetFallbackScreenshotDirectoryEndsWithScreenshotFolder()
    {
        var result = ScreenshotService.GetFallbackScreenshotDirectory();

        Assert.EndsWith("Screenshot", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetFallbackScreenshotDirectoryIsFullyQualified()
    {
        var result = ScreenshotService.GetFallbackScreenshotDirectory();

        Assert.True(Path.IsPathFullyQualified(result));
    }

    [Fact]
    public void PrimaryAndFallbackDirectoriesAreDifferent()
    {
        var primary = ScreenshotService.GetPrimaryScreenshotDirectory();
        var fallback = ScreenshotService.GetFallbackScreenshotDirectory();

        Assert.NotEqual(primary, fallback, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void PrimaryScreenshotDirectoryIsStableAcrossCalls()
    {
        var first = ScreenshotService.GetPrimaryScreenshotDirectory();
        var second = ScreenshotService.GetPrimaryScreenshotDirectory();

        Assert.Equal(first, second, StringComparer.Ordinal);
    }

    [Fact]
    public void FallbackScreenshotDirectoryIsStableAcrossCalls()
    {
        var first = ScreenshotService.GetFallbackScreenshotDirectory();
        var second = ScreenshotService.GetFallbackScreenshotDirectory();

        Assert.Equal(first, second, StringComparer.Ordinal);
    }
}
