using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using Serilog;

namespace CHDMounter.Core.Views;

/// <summary>
///     A dialog window that displays application information, version, and update availability.
/// </summary>
public partial class AboutWindow
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AboutWindow" /> class.
    /// </summary>
    public AboutWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";
            VersionText.Text = version;
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
            UpdateBanner.Visibility = Visibility.Visible;
            UpdateText.Text = $"A new version ({result.LatestVersion}) is available!";
            UpdateLink.Text = "Click here to download";
            UpdateLink.Tag = result.DownloadUrl;
        }
    }

    private void UpdateLink_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string url })
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to open update URL: {Url}", url);
            }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to open hyperlink: {Uri}", e.Uri.AbsoluteUri);
        }

        e.Handled = true;
    }
}