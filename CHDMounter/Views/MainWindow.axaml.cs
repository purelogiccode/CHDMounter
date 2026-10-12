using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CHDMounter.Interfaces;
using CHDMounter.Logging;
using CHDMounter.Models;
using CHDMounter.Services;
using Serilog;
using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Views;

/// <summary>
///     The main application window. Handles command-line arguments, console selection,
///     the mount/unmount flow, update checking and logging output.
/// </summary>
public partial class MainWindow : Window
{
    private readonly IScreenshotService _screenshotService;
    private readonly List<ConsoleInfo> _consoleTypes = [];

    private string? _chdPath;
    private string? _cliMountPoint;

    private bool _isClosing;
    private bool _launchExplorer;
    private int _renderedLogEntries;
    private ConsoleType _selectedConsoleType = ConsoleType.Unknown;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MainWindow" /> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        LoggingService = ServiceProvider.Get<ILoggingService>();
        MountService = ServiceProvider.Get<IMountService>();
        _screenshotService = ServiceProvider.Get<IScreenshotService>();

        PopulateConsoleTypes();
        WireUpLogging();
        UpdateTitle();

        Opened += MainWindow_Opened;
        Closing += MainWindow_ClosingAsync;
        KeyDown += MainWindow_KeyDown;
    }

    private ILoggingService LoggingService { get; }

    private IMountService MountService { get; }

    private void MainWindow_Opened(object? sender, EventArgs e)
    {
        var args = App.StartupArgs;
        if (args.Length > 0)
            HandleCommandLineArgs(args);

        CheckForUpdates();
    }

    private void UpdateTitle()
    {
        var backend = ServiceProvider.TryGet<ISettingsService>()?.Settings.MountBackend ?? MountBackend.Dokan;
        if (!OperatingSystem.IsWindows()) backend = MountBackend.Fuse;
        Title = $"CHDMounter ({backend})";
    }

    private void WireUpLogging()
    {
        LoggingService.LogEntries.CollectionChanged += (_, e) =>
        {
            if (e.Action != NotifyCollectionChangedAction.Add) return;

            Dispatcher.UIThread.Post(() =>
            {
                if (e.NewItems is null) return;

                foreach (LogEntry entry in e.NewItems)
                    LogTextBlock.Text += FormatLogEntry(entry);

                _renderedLogEntries += e.NewItems.Count;

                // ILoggingService trims its collection at MaxEntries; rebuild the
                // rendered text at the same bound so it cannot grow without limit.
                if (_renderedLogEntries > CHDMounter.Services.LoggingService.MaxEntries)
                    RebuildLogText();

                LogScrollViewer.Offset = LogScrollViewer.Offset.WithY(LogScrollViewer.Extent.Height);
            }, DispatcherPriority.Background);
        };
    }

    private void RebuildLogText()
    {
        var builder = new StringBuilder();
        foreach (var entry in LoggingService.LogEntries)
            builder.Append(FormatLogEntry(entry));

        LogTextBlock.Text = builder.ToString();
        _renderedLogEntries = LoggingService.LogEntries.Count;
    }

    private static string FormatLogEntry(LogEntry entry)
    {
        return $"[{entry.Timestamp:HH:mm:ss}] {entry.Message}{Environment.NewLine}";
    }

    private void PopulateConsoleTypes()
    {
        _consoleTypes.Clear();
        _consoleTypes.AddRange(ParserFactory.GetAllSupportedConsoles()
            .OrderBy(static c => c.Name, StringComparer.Ordinal));
        _consoleTypes.Insert(0, new ConsoleInfo(ConsoleType.Unknown, "Unknown"));
        ConsoleTypeComboBox.ItemsSource = _consoleTypes;
        ConsoleTypeComboBox.SelectedIndex = 0;
    }

    private static void CheckForUpdates()
    {
        var timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        var elapsed = TimeSpan.Zero;

        timer.Tick += async (_, _) =>
        {
            var result = UpdateChecker.Result;

            // The check runs in the background; keep polling until it completes so the
            // notification is never missed, with a generous timeout for slow networks.
            if (result is null && elapsed < TimeSpan.FromSeconds(30))
            {
                elapsed += timer.Interval;
                return;
            }

            timer.Stop();
            if (result is { HasUpdate: true })
            {
                var message = $"A new version ({result.LatestVersion}) is available!\n\nWould you like to download it?";
                const string caption = "Update Available";
                if (await Dialogs.ConfirmAsync(caption, message))
                    PlatformHelper.OpenUrl(result.DownloadUrl);
            }
        };
        timer.Start();
    }

    private void HandleCommandLineArgs(string[] args)
    {
        var options = CommandLineParser.Parse(args);

        _launchExplorer = options.LaunchExplorer;
        _cliMountPoint = options.MountPoint;

        if (options.ChdPath is not null)
        {
            ChdFilePathTextBox.Text = options.ChdPath;
            _chdPath = options.ChdPath;
        }

        if (options.ConsoleType != ConsoleType.Unknown)
        {
            _selectedConsoleType = options.ConsoleType;
            SelectConsoleTypeInCombo(options.ConsoleType);
        }

        ValidateAndEnableMount();

        if (options.ChdPath is null || !File.Exists(options.ChdPath))
            return;

        if (options.ConsoleType != ConsoleType.Unknown)
            MountDisk();
        else
            ShowDragDropConsoleModal(options.ChdPath);
    }

    private void ShowDragDropConsoleModal(string chdPath)
    {
        Dispatcher.UIThread.Post(() => _ = ShowDragDropConsoleModalAsync(chdPath), DispatcherPriority.Background);
    }

    private async Task ShowDragDropConsoleModalAsync(string chdPath)
    {
        try
        {
            var dialog = new ConsoleSelectionWindow(chdPath);

            // Await rather than block: blocking the UI thread on the dialog task freezes
            // input handling, leaving the selection window visible but unresponsive.
            if (await dialog.ShowDialog<bool>(this))
            {
                _selectedConsoleType = dialog.SelectedConsoleType;
                SelectConsoleTypeInCombo(dialog.SelectedConsoleType);
                MountDisk();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open modal selection window");
        }
    }

    private void SelectConsoleTypeInCombo(ConsoleType type)
    {
        foreach (var item in _consoleTypes)
            if (item.Type == type)
            {
                ConsoleTypeComboBox.SelectedItem = item;
                return;
            }
    }

    private void ValidateAndEnableMount()
    {
        var type = ConsoleTypeComboBox.SelectedItem is ConsoleInfo ci ? ci.Type : _selectedConsoleType;
        MountButton.IsEnabled = !string.IsNullOrEmpty(_chdPath)
                                && type != ConsoleType.Unknown
                                && File.Exists(_chdPath)
                                && !MountService.IsMounted;
    }

    /// <summary>
    ///     Handles selection changes in the console type combo box.
    /// </summary>
    private void ConsoleType_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ConsoleTypeComboBox.SelectedItem is ConsoleInfo ci) _selectedConsoleType = ci.Type;

        ValidateAndEnableMount();
    }

    /// <summary>
    ///     Handles text changes in the CHD file path text box.
    /// </summary>
    private void ChdFilePath_TextChanged(object? sender, TextChangedEventArgs e)
    {
        _chdPath = ChdFilePathTextBox.Text?.Trim().Trim('"');
        ValidateAndEnableMount();
    }

    /// <summary>
    ///     Opens a file dialog to browse for a CHD file.
    /// </summary>
    private async void BrowseChd_ClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select CHD File",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("CHD files") { Patterns = ["*.chd"] },
                    FilePickerFileTypes.All
                ]
            });

            var path = files.FirstOrDefault()?.TryGetLocalPath();
            if (path is null) return;

            ChdFilePathTextBox.Text = path;
            _chdPath = path;
            ValidateAndEnableMount();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to open the CHD file picker");
        }
    }

    /// <summary>
    ///     Opens a CHD file via the file menu.
    /// </summary>
    private void OpenChd_Click(object? sender, RoutedEventArgs e)
    {
        BrowseChd_ClickAsync(sender, e);
    }

    /// <summary>
    ///     Handles the mount button click by initiating an asynchronous mount operation.
    /// </summary>
    private async void Mount_ClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            await MountDiskAsync();
        }
        catch (Exception ex)
        {
            LogMountFailure(ex);
        }
    }

    private void MountDisk()
    {
        _ = MountDiskAsync().ContinueWith(t =>
        {
            if (t.IsFaulted)
                LogMountFailure(t.Exception?.InnerException ?? t.Exception!);
        }, TaskScheduler.Default);
    }

    private async Task MountDiskAsync()
    {
        if (string.IsNullOrEmpty(_chdPath)) return;

        var type = _selectedConsoleType;
        if (ConsoleTypeComboBox.SelectedItem is ConsoleInfo sci) type = sci.Type;

        if (!MountService.CanMount())
        {
            StatusText.Text = "Driver not found";

            // Route through Mount() so the service logs the specific reason
            // (missing driver or outdated Dokan/WinFsp/FUSE) and shows the guidance
            // dialog with the download link.
            await Task.Run(() => MountService.Mount(_chdPath, _cliMountPoint, type));
            return;
        }

        MountButton.IsEnabled = false;
        UnmountButton.IsEnabled = false;
        StatusText.Text = "Mounting...";

        try
        {
            await Task.Run(() => MountService.Mount(_chdPath, _cliMountPoint, type));

            if (MountService.IsMounted)
            {
                StatusText.Text = "Mounted";
                DriveLetterText.Text = MountService.MountPoint;
                UnmountButton.IsEnabled = true;

                try
                {
                    var settings = ServiceProvider.TryGet<ISettingsService>();
                    if (_launchExplorer || settings is { Settings.AutoOpenMountedDrive: true })
                        PlatformHelper.OpenInFileManager(MountService.MountPoint);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to auto-open mounted drive");
                }
            }
            else
            {
                StatusText.Text = "Mount failed";
                MountButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            LogMountFailure(ex);
            StatusText.Text = "Mount failed";
            MountButton.IsEnabled = true;
        }
    }

    private void LogMountFailure(Exception ex)
    {
        LogFailure("Mount failed", ex);
    }

    private void LogUnmountFailure(Exception ex)
    {
        LogFailure("Unmount failed", ex);
    }

    private void LogFailure(string action, Exception ex)
    {
        var sb = new StringBuilder($"{action}: {ex.Message}");
        var current = ex.InnerException;
        while (current is not null)
        {
            sb.AppendLine();
            sb.Append($"  caused by: {current.GetType().Name}: {current.Message}");
            current = current.InnerException;
        }

        var fullMessage = sb.ToString();
        if (IsExpectedFailure(ex, fullMessage))
        {
            // Missing Dokan/WinFsp/FUSE driver, wrong console type, busy mount point, etc.
            // are user/environment issues, not app bugs: show in UI without filing a bug report.
            LoggingService.LogUserError(fullMessage);
        }
        else
        {
            // Unexpected failure: file a bug report WITH the exception stack preserved.
            LoggingService.LogError(fullMessage, ex);
        }
    }

    private static bool IsExpectedFailure(Exception ex, string fullMessage)
    {
        // Walk the whole exception chain: a TypeInitializationException wrapping a
        // DllNotFoundException (the WinFsp "type initializer for 'Fsp.Interop.Api'" crash)
        // is environmental, not a bug.
        for (var current = ex; current is not null; current = current.InnerException)
            if (current is TypeInitializationException or DllNotFoundException or BadImageFormatException
                or EntryPointNotFoundException)
                return true;

        return fullMessage.Contains("winfsp", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("dokan", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("fuse", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("type initializer", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("native library", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("could not be loaded", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("failed to open or parse chd", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("recognizable file system", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("no file system parser", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("try a different console type", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("no available drive letter", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("already in use", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("mount point", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("is unavailable", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("ntstatus", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("already mounted", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("driver not found", StringComparison.OrdinalIgnoreCase)
               || fullMessage.Contains("not available", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Handles the unmount button click by initiating an asynchronous unmount operation.
    /// </summary>
    private async void Unmount_ClickAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            UnmountButton.IsEnabled = false;
            StatusText.Text = "Unmounting...";

            try
            {
                await Task.Run(() => MountService.Unmount());
                StatusText.Text = "Unmounted";
                DriveLetterText.Text = "";
                MountButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                // Unmount races (drive already gone, handle closed) are environmental;
                // genuinely unexpected exceptions are still reported.
                LogUnmountFailure(ex);
                StatusText.Text = "Unmount failed";
                UnmountButton.IsEnabled = MountService.IsMounted;
            }
        }
        catch (Exception ex)
        {
            LogUnmountFailure(ex);
            StatusText.Text = "Unmount failed";
            UnmountButton.IsEnabled = MountService.IsMounted;
        }
    }

    /// <summary>
    ///     Closes the application window.
    /// </summary>
    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    ///     Opens the application data folder in the file manager.
    /// </summary>
    private void OpenAppDataFolder_Click(object? sender, RoutedEventArgs e)
    {
        var folder = DiagnosticLogger.GetAppDataFolderForCurrentApp();
        if (Directory.Exists(folder))
            PlatformHelper.OpenInFileManager(folder);
        else
            LoggingService.LogUserError($"AppData folder not found: {folder}");
    }

    /// <summary>
    ///     Opens the settings dialog.
    /// </summary>
    private async void Settings_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var settingsService = ServiceProvider.TryGet<ISettingsService>();
            var dialog = new SettingsWindow(settingsService);

            // Await the dialog instead of blocking the UI thread: blocking on the task
            // returned by ShowDialog freezes the dispatcher, so the dialog would render
            // but never process input (no control or link could be clicked).
            await dialog.ShowDialog(this);
            UpdateTitle();
        }
        catch (Exception ex)
        {
            LoggingService.LogError("Error in method Settings_Click", ex);
        }
    }

    /// <summary>
    ///     Opens the donation page in the default browser.
    /// </summary>
    private void Donate_Click(object? sender, RoutedEventArgs e)
    {
        PlatformHelper.OpenUrl("https://www.purelogiccode.com/donate");
    }

    /// <summary>
    ///     Opens the about dialog.
    /// </summary>
    private async void About_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new AboutWindow();
            await dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            LoggingService.LogError("Error in method About_Click", ex);
        }
    }

    private async void MainWindow_ClosingAsync(object? sender, WindowClosingEventArgs e)
    {
        try
        {
            if (_isClosing) return;

            if (!MountService.IsMounted) return;

            e.Cancel = true;
            _isClosing = true;
            StatusText.Text = "Unmounting before exit...";

            try
            {
                await Task.Run(() => MountService.Unmount());
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to unmount during window close");
            }

            _isClosing = false;
            Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during window closing");
        }
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F8)
        {
            _screenshotService.TakeScreenshot();
            e.Handled = true;
        }
    }
}
