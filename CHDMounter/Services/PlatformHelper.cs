using System.Diagnostics;
using Serilog;

namespace CHDMounter.Services;

/// <summary>
///     Cross-platform helpers for interacting with the operating system shell.
/// </summary>
internal static class PlatformHelper
{
    /// <summary>
    ///     Opens a URL in the user's default browser.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    internal static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to open URL: {Url}", url);
        }
    }

    /// <summary>
    ///     Opens a file or folder in the platform's file manager
    ///     (Explorer on Windows, Finder on macOS, xdg-open on Linux).
    /// </summary>
    /// <param name="path">The file or folder path to reveal.</param>
    internal static void OpenInFileManager(string path)
    {
        var executable = OperatingSystem.IsWindows()
            ? "explorer.exe"
            : OperatingSystem.IsMacOS()
                ? "open"
                : "xdg-open";

        try
        {
            var startInfo = new ProcessStartInfo(executable) { UseShellExecute = false };
            startInfo.ArgumentList.Add(path);
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to open path in file manager: {Path}", path);
        }
    }
}
