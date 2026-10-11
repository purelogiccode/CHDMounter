namespace CHDMounter.Services;

/// <summary>
///     Resolves the screenshot output directories shared by the Avalonia and WPF
///     screenshot services.
/// </summary>
internal static class ScreenshotPaths
{
    /// <summary>
    ///     Gets the primary screenshot directory: the "Screenshot" folder inside the
    ///     application folder.
    /// </summary>
    /// <returns>The full path to the primary screenshot directory.</returns>
    internal static string GetPrimaryDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "Screenshot");
    }

    /// <summary>
    ///     Gets the fallback screenshot directory: the "Screenshot" folder inside
    ///     %LOCALAPPDATA%\{applicationName}.
    /// </summary>
    /// <param name="applicationName">The application name used as the folder name.</param>
    /// <returns>The full path to the fallback screenshot directory.</returns>
    internal static string GetFallbackDirectory(string applicationName)
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            applicationName,
            "Screenshot");
    }
}
