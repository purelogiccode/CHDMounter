using System.Runtime.InteropServices;
using System.Text.Json;
using Serilog;

namespace CHDMounter.Core.Services;

/// <summary>
///     Sends bug reports and warnings to a remote API endpoint with rate-limited queuing.
/// </summary>
public static class BugReportClient
{
    private const string BaseUrl = "https://www.purelogiccode.com/bugreport/api/send-bug-report";
    private static readonly HttpClient Client = new();
    private static readonly ConcurrentQueue<Func<Task>> PendingReports = new();
    private static int _isProcessing;

    /// <summary>
    ///     Gets or sets a value indicating whether bug reports are actually sent to the API.
    ///     Defaults to <c>false</c> (fail-closed) so tests and any code that does not
    ///     explicitly opt in never pollute the production bug-report database.
    ///     Production entry points must set this to <c>true</c> once during startup.
    /// </summary>
    internal static bool IsSendingEnabled { get; set; }

    internal static void SendException(Exception ex, string context)
    {
        var envDetails = BuildEnvironmentDetails();
        var errorDetails = $"{context}: {ex.Message}";
        var exceptionDetails = BuildExceptionDetails(ex);

        var message = $"""
                       === Environment Details ===
                       {envDetails}

                       === Error Details ===
                       {errorDetails}

                       === Exception Details ===
                       {exceptionDetails}
                       """;

        Enqueue(message, ex.StackTrace ?? "");
    }

    internal static void SendWarning(string message)
    {
        var envDetails = BuildEnvironmentDetails();

        var formatted = $"""
                         === Environment Details ===
                         {envDetails}

                         === Error Details ===
                         {message}

                         === Exception Details ===
                         No exception - this is a warning
                         """;

        Enqueue(formatted, "");
    }

    internal static void SendError(string message, string? stackTrace)
    {
        var envDetails = BuildEnvironmentDetails();
        var exceptionDetails = string.IsNullOrEmpty(stackTrace)
            ? "No exception information available"
            : $"Type: Unknown\nMessage: {message}\nSource: Unknown\nStackTrace: {stackTrace}";

        var formatted = $"""
                         === Environment Details ===
                         {envDetails}

                         === Error Details ===
                         {message}

                         === Exception Details ===
                         {exceptionDetails}
                         """;

        Enqueue(formatted, stackTrace ?? "");
    }

    private static void Enqueue(string message, string stackTrace)
    {
        if (!IsSendingEnabled)
            return;

        // Defense-in-depth against test pollution (bugs 66257/66258/66259 were filed as
        // "ReSharperTestRunner" v2.16.1.112 from CHDMounter.Core.Tests): even if a test
        // accidentally enables sending, never file reports from a test harness process.
        if (IsTestHarnessApp())
            return;

        PendingReports.Enqueue(() => SendAsync(message, stackTrace));
        if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0) _ = Task.Run(ProcessQueueAsync);
    }

    private static bool IsTestHarnessApp()
    {
        string appName;
        try
        {
            appName = AppInfoHelper.GetAppName();
        }
        catch
        {
            return false;
        }

        return appName.Contains("Test", StringComparison.OrdinalIgnoreCase)
               || appName.Contains("ReSharper", StringComparison.OrdinalIgnoreCase)
               || appName.Contains("xunit", StringComparison.OrdinalIgnoreCase)
               || appName.Contains("nunit", StringComparison.OrdinalIgnoreCase)
               || appName.Contains("mstest", StringComparison.OrdinalIgnoreCase)
               || appName.Contains("vstest", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task ProcessQueueAsync()
    {
        try
        {
            while (PendingReports.TryDequeue(out var sendAction))
            {
                try
                {
                    await sendAction();
                }
                catch (Exception ex)
                {
                    // Log at Information (not Warning): a failure to reach the bug-report API
                    // is a network/environment issue, and Warning would itself file another
                    // bug report via the sink (recursive pollution).
                    Log.Information(ex, "BugReportClient: Failed to send bug report");
                }

                await Task.Delay(6000);
            }
        }
        finally
        {
            while (true)
            {
                if (PendingReports.IsEmpty)
                    if (Interlocked.CompareExchange(ref _isProcessing, 0, 1) == 1)
                        break;

                if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
                {
                    _ = Task.Run(ProcessQueueAsync);
                    break;
                }
            }
        }
    }

    private static async Task SendAsync(string message, string stackTrace)
    {
        try
        {
            var payload = new
            {
                message = Truncate(message, 4000),
                applicationName = AppInfoHelper.GetAppName(),
                version = AppInfoHelper.GetVersion(),
                environment = "Production",
                stackTrace = Truncate(stackTrace, 8000)
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
            request.Content = content;
            request.Headers.Add("X-API-KEY", AppInfoHelper.GetApiKey());

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await Client.SendAsync(request, cts.Token);
        }
        catch (Exception ex)
        {
            Log.Information(ex, "BugReportClient: Failed to send bug report to API");
        }
    }

    private static string BuildEnvironmentDetails()
    {
        return $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
               $"Application Name: {AppInfoHelper.GetAppName()}\n" +
               $"Application Version: {AppInfoHelper.GetVersion()}\n" +
               $"OS Version: {Environment.OSVersion}\n" +
               $"Architecture: {RuntimeInformation.OSArchitecture}\n" +
               $"Bitness: {(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")}\n" +
               $"Windows Version: {RuntimeInformation.OSDescription}\n" +
               $"Processor Count: {Environment.ProcessorCount}\n" +
               $"Base Directory: {AppContext.BaseDirectory}\n" +
               $"Temp Path: {Path.GetTempPath()}";
    }

    private static string BuildExceptionDetails(Exception ex)
    {
        return $"Type: {ex.GetType().FullName}\n" +
               $"Message: {ex.Message}\n" +
               $"Source: {ex.Source}\n" +
               $"StackTrace: {ex.StackTrace}";
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        // Guard against tiny limits (previously value[..(maxLength - 3)] threw
        // ArgumentOutOfRangeException for maxLength 1-2). "..." exactly fills 3.
        if (maxLength <= 0)
            return "";
        if (maxLength <= 2)
            return value[..maxLength];
        if (maxLength == 3)
            return "...";

        return value[..(maxLength - 3)] + "...";
    }
}