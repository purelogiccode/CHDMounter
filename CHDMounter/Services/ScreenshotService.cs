using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CHDMounter.Interfaces;
using Serilog;

namespace CHDMounter.Services;

/// <summary>
///     Captures screenshots of the active application window and saves them as PNG files.
/// </summary>
public class ScreenshotService : IScreenshotService
{
    private readonly ILoggingService _loggingService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ScreenshotService" /> class.
    /// </summary>
    /// <param name="loggingService">The logging service for recording screenshot results.</param>
    public ScreenshotService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    /// <summary>
    ///     Takes a screenshot of the active window and saves it to the local application data folder.
    /// </summary>
    public void TakeScreenshot()
    {
        try
        {
            var window = GetActiveWindow();
            if (window is null)
            {
                _loggingService.LogUserError("Screenshot: no active window found.");
                return;
            }

            var scaling = window.RenderScaling <= 0 ? 1.0 : window.RenderScaling;
            var width = (int)Math.Round(window.ClientSize.Width * scaling);
            var height = (int)Math.Round(window.ClientSize.Height * scaling);
            if (width <= 0 || height <= 0)
            {
                _loggingService.LogUserError("Screenshot: invalid window dimensions.");
                return;
            }

            var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96 * scaling, 96 * scaling));
            bitmap.Render(window);

            var fileName = $"screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.png";
            var savedPath = TrySaveImage(bitmap, fileName);
            bitmap.Dispose();

            if (savedPath is not null)
                _loggingService.Log($"Screenshot saved: {savedPath}");
            else
                _loggingService.LogUserError("Screenshot: failed to save image.");
        }
        catch (Exception ex)
        {
            _loggingService.LogUserError($"Screenshot error: {ex.Message}");
        }
    }

    private static Avalonia.Controls.Window? GetActiveWindow()
    {
        if (!Dispatcher.UIThread.CheckAccess())
            return null;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.Windows.FirstOrDefault(static w => w.IsActive) ?? desktop.MainWindow;

        return null;
    }

    private static string? TrySaveImage(RenderTargetBitmap bitmap, string fileName)
    {
        try
        {
            var appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CHDMounter",
                "Screenshot");
            Directory.CreateDirectory(appDataDir);
            var path = Path.Combine(appDataDir, fileName);
            bitmap.Save(path, new PngBitmapEncoderOptions());
            return path;
        }
        catch (Exception ex)
        {
            // Save failures (disk full, permissions) are environmental, not app bugs.
            Log.Information(ex, "ScreenshotService: Failed to save screenshot to AppData");
        }

        try
        {
            var appFolder = AppContext.BaseDirectory;
            var screenshotDir = Path.Combine(appFolder, "Screenshot");
            Directory.CreateDirectory(screenshotDir);
            var path = Path.Combine(screenshotDir, fileName);
            bitmap.Save(path, new PngBitmapEncoderOptions());
            return path;
        }
        catch (Exception ex)
        {
            Log.Information(ex, "ScreenshotService: Failed to save screenshot to app folder");
        }

        return null;
    }
}
