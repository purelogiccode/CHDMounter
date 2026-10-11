using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CHDMounter.Interfaces;
using CHDMounter.Logging;
using CHDMounter.Services;
using CHDMounter.Views;
using Serilog;

namespace CHDMounter;

/// <summary>
///     Application entry point for the CHD mounter. Handles service registration, logging
///     initialization, and application lifecycle.
/// </summary>
public class App : Application
{
    private static TextWriter? _originalConsoleOut;
    private static TextWriter? _originalConsoleError;
    private static LogTextWriter? _logTextWriter;

    /// <summary>
    ///     Gets the command-line arguments captured at startup.
    /// </summary>
    internal static string[] StartupArgs { get; private set; } = [];

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            InitializeApplication(desktop.Args ?? []);

            desktop.MainWindow = new MainWindow();
            desktop.Exit += OnDesktopExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    ///     Initializes logging, configures global exception handlers, and registers shared services.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    private static void InitializeApplication(string[] args)
    {
        try
        {
            StartupArgs = args;

            DiagnosticLogger.Initialize();
            BugReportClient.IsSendingEnabled = true;
            DiagnosticLogger.CleanupOldLogs();
            DiagnosticLogger.LogSection("APPLICATION STARTUP");
            DiagnosticLogger.Log($"  Version: {Assembly.GetExecutingAssembly().GetName().Version}");
            DiagnosticLogger.Log($"  Arguments: [{string.Join(", ", StartupArgs)}]");
            DiagnosticLogger.Log($"  Base directory: {AppContext.BaseDirectory}");
            DiagnosticLogger.Log($"  OS: {RuntimeInformation.OSDescription}");
            DiagnosticLogger.Log($"  Framework: {RuntimeInformation.FrameworkDescription}");

            RegisterAppServices();

            _originalConsoleOut = Console.Out;
            _originalConsoleError = Console.Error;
            _logTextWriter = new LogTextWriter(_originalConsoleOut);
            Console.SetOut(_logTextWriter);
            Console.SetError(_logTextWriter);

            var loggingService = ServiceProvider.Get<ILoggingService>();
            loggingService.Log("CHDMounter - CHD Virtual Drive Mounter");
            loggingService.Log("Supports mounting CHD (Compressed Hunks of Data) files as virtual drives");
            loggingService.Log("");
            if (DiagnosticLogger.LogFilePath is not null)
            {
                loggingService.Log($"Debug log: {DiagnosticLogger.LogFilePath}");
                loggingService.Log("");
            }

            loggingService.Log("Usage: CHDMounter [/l] [/a] [/s:<alias>] <chd_file> [mount_point]");
            loggingService.Log("Example: CHDMounter /s:ps2 game.chd");
            loggingService.Log("Example: CHDMounter /l /s:segadreamcast game.chd M:");
            loggingService.Log(
                "Console aliases: see the Console Type Reference in the README (e.g. neogeocd, cuebin2352, cueisowav2352).");
            loggingService.Log("Mount cores: Dokan, WinFsp (Windows) or FUSE (Linux/macOS) - selectable in Settings.");
            loggingService.Log("Run without args to open the UI and select filesystem type.");
            loggingService.Log("");

            ErrorLogger.InitializeGlobalExceptionHandlers();

            StatsClient.SendStats();
            UpdateChecker.CheckForUpdates();
        }
        catch (Exception ex)
        {
            try
            {
                Log.Error(ex, "Critical error during application startup");
            }
            catch
            {
                Console.Error.WriteLine($"Critical startup error: {ex.Message}\n\n{ex.StackTrace}");
            }

            throw;
        }
    }

    private static void RegisterAppServices()
    {
        var loggingService = new LoggingService();
        ServiceProvider.Register<ILoggingService>(loggingService);

        var settingsService = new SettingsService("CHDMounter");
        ServiceProvider.Register<ISettingsService>(settingsService);

        var mountService = new Mounting.MountService(loggingService, settingsService);
        ServiceProvider.Register<IMountService>(mountService);

        var screenshotService = new ScreenshotService(loggingService);
        ServiceProvider.Register<IScreenshotService>(screenshotService);
    }

    /// <summary>
    ///     Handles application shutdown: logs the shutdown section, flushes logging output,
    ///     and disposes all registered services.
    /// </summary>
    private static void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        DiagnosticLogger.LogSection("APPLICATION SHUTDOWN");
        try
        {
            try
            {
                ServiceProvider.DisposeAllServices();
            }
            catch (Exception ex)
            {
                ErrorLogger.ReportSilentException(ex, "App.OnExit: Failed to dispose services");
            }

            if (_originalConsoleOut is not null) Console.SetOut(_originalConsoleOut);
            if (_originalConsoleError is not null) Console.SetError(_originalConsoleError);

            try
            {
                _logTextWriter?.Dispose();
            }
            catch (Exception ex)
            {
                ErrorLogger.ReportSilentException(ex, "App.OnExit: Failed to dispose LogTextWriter");
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.ReportSilentException(ex, "App.OnExit: Error during exit cleanup");
        }

        try
        {
            AppLogger.CloseAndFlush();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to flush loggers during shutdown");
        }
    }
}
