using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CHDMounter.Services;
using Serilog;

namespace CHDMounter.Views;

/// <summary>
///     A dialog window that displays application information, version, and update availability.
/// </summary>
public partial class AboutWindow : Window
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AboutWindow" /> class.
    /// </summary>
    public AboutWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        try
        {
            VersionText.Text = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "AboutWindow: Failed to read the application version");
            VersionText.Text = "1.0.0";
        }

        CheckForUpdates();
    }

    private void CheckForUpdates()
    {
        var timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        var elapsed = TimeSpan.Zero;

        timer.Tick += (_, _) =>
        {
            var result = UpdateChecker.Result;

            // The check runs in the background; keep polling until it completes so the
            // banner is not missed when the window opens before the check finishes.
            if (result is null && elapsed < TimeSpan.FromSeconds(30))
            {
                elapsed += timer.Interval;
                return;
            }

            timer.Stop();
            if (result is { HasUpdate: true })
            {
                UpdateBanner.IsVisible = true;
                UpdateText.Text = $"A new version ({result.LatestVersion}) is available!";
                UpdateLink.Tag = result.DownloadUrl;
            }
        };
        timer.Start();
    }

    private void UpdateLink_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: string url })
            PlatformHelper.OpenUrl(url);
    }

    private void OpenUrl_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: string url })
            PlatformHelper.OpenUrl(url);
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
