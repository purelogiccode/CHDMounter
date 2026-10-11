using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;

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
        catch
        {
            VersionText.Text = "1.0.0";
        }

        CheckForUpdates();
    }

    private void CheckForUpdates()
    {
        var result = UpdateChecker.Result;
        if (result is { HasUpdate: true })
        {
            UpdateBanner.IsVisible = true;
            UpdateText.Text = $"A new version ({result.LatestVersion}) is available!";
            UpdateLink.Tag = result.DownloadUrl;
        }
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
