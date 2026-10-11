using System.Text.Json;

namespace CHDMounter.Core.Services;

/// <summary>
///     Sends anonymous application usage statistics to a remote analytics endpoint.
/// </summary>
public static class StatsClient
{
    private const string BaseUrl = "https://www.purelogiccode.com/ApplicationStats/stats";
    private static readonly HttpClient Client = new();
    private static int _sent;

    /// <summary>
    ///     Sends application statistics once per process lifetime. Subsequent calls are ignored.
    /// </summary>
    public static void SendStats()
    {
        if (Interlocked.CompareExchange(ref _sent, 1, 0) != 0)
            return;

        _ = Task.Run(SendAsync);
    }

    private static async Task SendAsync()
    {
        try
        {
            var payload = new
            {
                applicationId = AppInfoHelper.GetAppName().ToLowerInvariant(),
                version = AppInfoHelper.GetVersion()
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
            request.Content = content;
            request.Headers.Add("Authorization", $"Bearer {AppInfoHelper.GetApiKey()}");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await Client.SendAsync(request, cts.Token).ConfigureAwait(false);
        }
        catch
        {
            // silently fail
        }
    }
}