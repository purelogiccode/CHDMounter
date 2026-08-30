using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Serilog;

namespace CHDMounter.Core.Services;

/// <summary>
///     Captures screenshots of the foreground window and saves them as PNG files.
/// </summary>
public class ScreenshotService : IScreenshotService
{
    private const int DwmwaExtendedFrameBounds = 9;
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
    ///     Takes a screenshot of the current foreground window and saves it to the local application data folder.
    /// </summary>
    public void TakeScreenshot()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                _loggingService.LogError("Screenshot: no foreground window found.");
                return;
            }

            if (!GetWindowRect(hwnd, out var rect))
            {
                _loggingService.LogError("Screenshot: failed to get window rect.");
                return;
            }

            // Use extended frame bounds to exclude invisible window borders/shadows on Windows 10/11.
            DwmGetWindowAttribute(hwnd, DwmwaExtendedFrameBounds, out var extRect, Marshal.SizeOf<Rect>());
            var width = extRect.Right - extRect.Left;
            var height = extRect.Bottom - extRect.Top;
            var left = extRect.Left;
            var top = extRect.Top;

            if (width <= 0 || height <= 0)
            {
                width = rect.Right - rect.Left;
                height = rect.Bottom - rect.Top;
                left = rect.Left;
                top = rect.Top;
            }

            if (width <= 0 || height <= 0)
            {
                _loggingService.LogError("Screenshot: invalid window dimensions.");
                return;
            }

            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(left, top, 0, 0, new Size(width, height));

            var fileName = $"screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}.png";
            var savedPath = TrySaveImage(bitmap, fileName);

            if (savedPath is not null)
                _loggingService.Log($"Screenshot saved: {savedPath}");
            else
                _loggingService.LogError("Screenshot: failed to save image.");
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Screenshot error: {ex.Message}");
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("dwmapi.dll")]
    private static extern int
        DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out Rect pvAttribute, int cbAttribute);

    private static string? TrySaveImage(Image image, string fileName)
    {
        try
        {
            var appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CHDMounter",
                "Screenshot");
            Directory.CreateDirectory(appDataDir);
            var path = Path.Combine(appDataDir, fileName);
            image.Save(path, ImageFormat.Png);
            return path;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "ScreenshotService: Failed to save screenshot to AppData");
        }

        try
        {
            var appFolder = AppContext.BaseDirectory;
            var screenshotDir = Path.Combine(appFolder, "Screenshot");
            Directory.CreateDirectory(screenshotDir);
            var path = Path.Combine(screenshotDir, fileName);
            image.Save(path, ImageFormat.Png);
            return path;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "ScreenshotService: Failed to save screenshot to app folder");
        }

        return null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}