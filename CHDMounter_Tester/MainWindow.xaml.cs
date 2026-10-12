using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CHDMounter.Logging;
using CHDMounter.Services;
using Microsoft.Win32;
using Serilog;
using Tester.Models;
using Tester.Services;
using VideoGameFileSystemParser.Models;
using VideoGameFileSystemParser.Parsers;

namespace Tester;

/// <summary>
///     The main window for the CHD parsing test tool. Provides UI for selecting CHD folders,
///     running parsing tests, and exporting results to PDF.
/// </summary>
public partial class MainWindow
{
    private static readonly SolidColorBrush GreenBrush = new(Colors.Green);
    private static readonly SolidColorBrush RedBrush = new(Colors.Red);
    private static readonly SolidColorBrush CyanBrush = new(Colors.Cyan);
    private static readonly SolidColorBrush YellowBrush = new(Colors.Yellow);
    private static readonly SolidColorBrush GrayBrush = new(Colors.Gray);
    private static readonly SolidColorBrush LightGrayBrush = new(Colors.LightGray);
    private readonly DispatcherTimer _elapsedTimer;
    private readonly ILogger _logger;
    private readonly WpfScreenshotService _screenshotService;
    private CancellationTokenSource? _cts;
    private TestSummary? _lastSummary;
    private Stopwatch? _stopwatch;
    private TestRunnerService? _testRunner;

    static MainWindow()
    {
        GreenBrush.Freeze();
        RedBrush.Freeze();
        CyanBrush.Freeze();
        YellowBrush.Freeze();
        GrayBrush.Freeze();
        LightGrayBrush.Freeze();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MainWindow" /> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        _logger = App.Logger ?? new LoggerConfiguration().WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();
        _screenshotService = new WpfScreenshotService();

        _elapsedTimer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _elapsedTimer.Tick += ElapsedTimer_Tick;

        PopulateConsoleTypes();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        AppendLog("[Tester] CHD Parsing Test Tool", CyanBrush);
        AppendLog("[Tester] Select a folder containing .chd files, choose a console type, and click Run Tests.",
            GrayBrush);
        AppendLog("", GrayBrush);
        _logger.Information("MainWindow loaded");

        CheckForUpdates();
    }

    private void CheckForUpdates()
    {
        var timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
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
                UpdateBanner.Visibility = Visibility.Visible;
                UpdateBannerText.Text = $"A new version ({result.LatestVersion}) is available!";
                UpdateBannerButton.Tag = result.DownloadUrl;
            }
        };
        timer.Start();
    }

    private void UpdateBannerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string url })
            PlatformHelper.OpenUrl(url);
    }

    private void UpdateDismiss_Click(object sender, RoutedEventArgs e)
    {
        UpdateBanner.Visibility = Visibility.Collapsed;
    }

    private void PopulateConsoleTypes()
    {
        var consoles = ParserFactory.GetAllSupportedConsoles()
            .OrderBy(static c => c.Name, StringComparer.Ordinal)
            .ToList();
        ConsoleComboBox.ItemsSource = consoles;
        ConsoleComboBox.DisplayMemberPath = "Name";
        ConsoleComboBox.SelectedValuePath = "Type";
        ConsoleComboBox.SelectedIndex = 0;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select folder containing .chd files"
        };

        if (dialog.ShowDialog() == true) ChdFolderTextBox.Text = dialog.FolderName;
    }

    private async void RunButton_ClickAsync(object sender, RoutedEventArgs e)
    {
        try
        {
            var folderPath = ChdFolderTextBox.Text.Trim();
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                MessageBox.Show("Please select a valid folder containing .chd files.", "Invalid Folder",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ConsoleComboBox.SelectedItem is not ConsoleInfo consoleInfo)
            {
                MessageBox.Show("Please select a console type.", "Invalid Console",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RunButton.IsEnabled = false;
            RunButton.Visibility = Visibility.Collapsed;
            CancelButton.IsEnabled = true;
            CancelButton.Visibility = Visibility.Visible;
            ExportPdfButton.Visibility = Visibility.Collapsed;
            SummaryPanel.Visibility = Visibility.Collapsed;

            ClearLog();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _lastSummary = null;

            _testRunner = new TestRunnerService(_logger);
            _testRunner.LogMessage += OnLogMessage;
            _testRunner.AllCompleted += OnAllCompleted;

            _stopwatch = Stopwatch.StartNew();
            _elapsedTimer.Start();

            StatusText.Text = "Running tests...";

            try
            {
                await _testRunner.RunTestsAsync(folderPath, consoleInfo, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                AppendLog("[Cancelled] Test run was cancelled.", YellowBrush);
                _logger.Warning("Test run cancelled");
            }
            catch (Exception ex)
            {
                AppendLog($"[Error] {ex.Message}", RedBrush);
                _logger.Error(ex, "Error during test run");
            }
            finally
            {
                // Always restore the idle UI once the run has ended (success, cancel or error).
                _elapsedTimer.Stop();
                _stopwatch?.Stop();
                _stopwatch = null;
                RunButton.Visibility = Visibility.Visible;
                RunButton.IsEnabled = true;
                CancelButton.Visibility = Visibility.Collapsed;
                CancelButton.IsEnabled = true;
                StatusText.Text = "Ready";

                _cts?.Dispose();
                _cts = null;
            }
        }
        catch (Exception ex)
        {
            AppendLog($"[Error] {ex.Message}", RedBrush);
            _logger.Error(ex, "Error during test run");
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        CancelButton.IsEnabled = false;
    }

    private void OnLogMessage(object? sender, EventArgs<string> e)
    {
        var message = e.Value;
        _ = Dispatcher.InvokeAsync(() =>
        {
            if (message.StartsWith("  OK", StringComparison.Ordinal))
                AppendLog(message, GreenBrush);
            else if (message.StartsWith("  FAIL", StringComparison.Ordinal))
                AppendLog(message, RedBrush);
            else if (message.StartsWith(new string('=', 60), StringComparison.Ordinal))
                AppendLog(message, CyanBrush);
            else
                AppendLog(message, LightGrayBrush);
        });
    }

    private void OnAllCompleted(object? sender, EventArgs<TestSummary> e)
    {
        var summary = e.Value;
        _ = Dispatcher.InvokeAsync(() =>
        {
            _lastSummary = summary;
            ShowSummary(summary);
        });
    }

    private void ShowSummary(TestSummary summary)
    {
        SummaryPanel.Visibility = Visibility.Visible;
        SummaryText.Text = $"Results: {summary.SuccessCount}/{summary.TotalFiles} succeeded";

        SuccessCountText.Text = $"{summary.SuccessCount} OK";
        SuccessBadge.Visibility = summary.SuccessCount > 0 ? Visibility.Visible : Visibility.Collapsed;

        FailCountText.Text = $"{summary.FailCount} FAIL";
        FailBadge.Visibility = summary.FailCount > 0 ? Visibility.Visible : Visibility.Collapsed;

        ExportPdfButton.Visibility = Visibility.Visible;
    }

    private async void ExportPdfButton_ClickAsync(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_lastSummary is null)
            {
                MessageBox.Show("No test results to export.", "Export PDF",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Export Test Summary to PDF",
                Filter = "PDF files (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                FileName = $"CHD_Test_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };

            if (dialog.ShowDialog() == true)
                try
                {
                    ExportPdfButton.IsEnabled = false;
                    StatusText.Text = "Exporting PDF...";

                    var summary = _lastSummary;
                    var path = dialog.FileName;
                    await Task.Run(() =>
                    {
                        PdfExportService.ExportToPdf(summary, path);
                    });

                    AppendLog($"[Export] Summary exported to: {path}", GreenBrush);
                    _logger.Information("Summary exported to PDF: {Path}", path);

                    MessageBox.Show($"Report exported successfully to:\n{path}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    AppendLog($"[Export Error] {ex.Message}", RedBrush);
                    _logger.Error(ex, "Failed to export PDF");
                    MessageBox.Show($"Failed to export PDF: {ex.Message}",
                        "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    ExportPdfButton.IsEnabled = true;
                    StatusText.Text = "Ready";
                }
        }
        catch (Exception ex)
        {
            AppendLog($"[Export Error] {ex.Message}", RedBrush);
            _logger.Error(ex, "Failed to export PDF");
            MessageBox.Show($"Failed to export PDF: {ex.Message}",
                "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ElapsedTimer_Tick(object? sender, EventArgs e)
    {
        if (_stopwatch is not null) ElapsedText.Text = $"Elapsed: {_stopwatch.Elapsed.TotalSeconds:F1}s";
    }

    private void AppendLog(string message, SolidColorBrush brush)
    {
        var paragraph = new Paragraph();
        var run = new Run(message + Environment.NewLine)
        {
            Foreground = brush
        };
        paragraph.Inlines.Add(run);

        LogTextBox.Document.Blocks.Add(paragraph);
        LogTextBox.ScrollToEnd();
    }

    private void ClearLog()
    {
        LogTextBox.Document.Blocks.Clear();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("CHD Parsing Test Tool\n\nPureLogicCode.com", "About Tester",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenAppDataFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = DiagnosticLogger.GetAppDataFolder("CHDMounter_Tester");
        if (Directory.Exists(folder))
            PlatformHelper.OpenInFileManager(folder);
        else
            AppendLog("[Error] AppData folder not found.", RedBrush);
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F8)
        {
            _screenshotService.TakeScreenshot();
            e.Handled = true;
        }
    }

    private void MainWindow_Closing(object sender, CancelEventArgs e)
    {
        // Cancel only: the run's finally disposes the source once it stops observing
        // the token (disposing here could race with Task.Run(..., token)).
        _cts?.Cancel();
        _elapsedTimer.Stop();
        _elapsedTimer.Tick -= ElapsedTimer_Tick;

        if (_testRunner is not null)
        {
            _testRunner.LogMessage -= OnLogMessage;
            _testRunner.AllCompleted -= OnAllCompleted;
        }
    }
}