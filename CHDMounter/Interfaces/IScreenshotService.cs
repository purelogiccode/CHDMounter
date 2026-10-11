namespace CHDMounter.Core.Interfaces;

/// <summary>
///     Defines a service for capturing screenshots of the application window.
/// </summary>
public interface IScreenshotService
{
    /// <summary>
    ///     Takes a screenshot of the foreground window and saves it to disk.
    /// </summary>
    void TakeScreenshot();
}