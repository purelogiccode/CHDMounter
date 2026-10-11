using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CHDMounter.Services;
using Serilog;

namespace Tester.Services;

/// <summary>
///     Captures screenshots of the active WPF window and saves them as PNG files in the
///     shared screenshot directories.
/// </summary>
internal sealed class WpfScreenshotService
{
    /// <summary>
    ///     Takes a screenshot of the active window and saves it to the application
    ///     "Screenshot" folder, falling back to %LOCALAPPDATA%\%APPLICATIONNAME% when
    ///     that fails.
    /// </summary>
    public void TakeScreenshot()
    {
        try
        {
            var window = GetActiveWindow();
            if (window is null)
            {
                Log.Information("Screenshot: no active window found.");
                return;
            }

            var dpi = VisualTreeHelper.GetDpi(window);
            var width = (int)Math.Round(window.ActualWidth * dpi.DpiScaleX);
            var height = (int)Math.Round(window.ActualHeight * dpi.DpiScaleY);
            if (width <= 0 || height <= 0)
            {
                Log.Information("Screenshot: invalid window dimensions.");
                return;
            }

            var bitmap = new RenderTargetBitmap(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY,
                PixelFormats.Pbgra32);
            bitmap.Render(window);

            var fileName = $"screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.png";
            var savedPath = TrySaveImage(bitmap, fileName);

            if (savedPath is not null)
                Log.Information("Screenshot saved: {Path}", savedPath);
            else
                Log.Warning("Screenshot: failed to save image.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Screenshot failed");
        }
    }

    private static Window? GetActiveWindow()
    {
        var application = Application.Current;
        if (application is null)
            return null;

        return application.Windows.Cast<Window>().FirstOrDefault(static window => window.IsActive)
               ?? application.MainWindow;
    }

    private static string? TrySaveImage(RenderTargetBitmap bitmap, string fileName)
    {
        try
        {
            var directory = ScreenshotPaths.GetPrimaryDirectory();
            Directory.CreateDirectory(directory);
            return SaveImage(bitmap, Path.Combine(directory, fileName));
        }
        catch (Exception ex)
        {
            Log.Information(ex, "WpfScreenshotService: Failed to save screenshot to app folder");
        }

        try
        {
            var directory = ScreenshotPaths.GetFallbackDirectory(AppInfoHelper.GetAppName());
            Directory.CreateDirectory(directory);
            return SaveImage(bitmap, Path.Combine(directory, fileName));
        }
        catch (Exception ex)
        {
            Log.Information(ex, "WpfScreenshotService: Failed to save screenshot to AppData");
        }

        return null;
    }

    private static string SaveImage(RenderTargetBitmap bitmap, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
        return path;
    }
}
