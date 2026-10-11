using System.Reflection;

namespace CHDMounter.Core.Services;

/// <summary>
///     Provides shared helper methods for application metadata and API key access.
/// </summary>
internal static class AppInfoHelper
{
    private const string ApiKeyEncoded =
        "YUdwb04zbDFOblExTm5SNWNqVTBNRzg1ZFRnM05qYzJOelp5TlRZM05EVXpORFExTXpJek5USTJOR00zTldJMmREZG5aMmRvWjJjM05uUnlaalUyTkdVPQ==";

    /// <summary>
    ///     Gets the application name from the entry assembly, or "CHDMounter" as a fallback.
    /// </summary>
    internal static string GetAppName()
    {
        try
        {
            return Assembly.GetEntryAssembly()?.GetName().Name ?? "CHDMounter";
        }
        catch
        {
            return "CHDMounter";
        }
    }

    /// <summary>
    ///     Gets the application version string from the entry assembly, or "1.0.0" as a fallback.
    /// </summary>
    internal static string GetVersion()
    {
        try
        {
            return Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }

    /// <summary>
    ///     Decodes and returns the API key from the embedded encoded constant.
    /// </summary>
    internal static string GetApiKey()
    {
        var once = Encoding.UTF8.GetString(Convert.FromBase64String(ApiKeyEncoded));
        return Encoding.UTF8.GetString(Convert.FromBase64String(once));
    }
}