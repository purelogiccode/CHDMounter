namespace CHDMounter.Services;

/// <summary>
///     Resolves the screenshot output directories shared by the Avalonia and WPF
///     screenshot services.
/// </summary>
internal static class ScreenshotPaths
{
    /// <summary>
    ///     Gets the primary screenshot directory: the "Screenshot" folder inside the
    ///     application folder (the folder that contains the executable). For single-file
    ///     builds this intentionally uses the executable path instead of
    ///     <see cref="AppContext.BaseDirectory" />, which points at the runtime's
    ///     extraction directory.
    /// </summary>
    /// <returns>The full path to the primary screenshot directory.</returns>
    internal static string GetPrimaryDirectory()
    {
        var executableDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        if (string.IsNullOrEmpty(executableDirectory))
            executableDirectory = AppContext.BaseDirectory;

        return Path.Combine(executableDirectory, "Screenshot");
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
