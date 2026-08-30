using System.Globalization;
using System.Windows;
using CHDMounter.Core.Logging;
using Serilog;

namespace Tester;

/// <summary>
///     Application entry point for the CHD parsing test tool. Configures Serilog logging
///     and handles application lifecycle.
/// </summary>
public partial class App
{
    internal static ILogger? Logger { get; private set; }
    internal static string LogFilePath { get; private set; } = "";

    /// <summary>
    ///     Handles application startup: initializes Serilog logging and configures global exception handlers.
    /// </summary>
    /// <param name="e">The startup event arguments.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CHDMounter_Tester", "logs");
        Directory.CreateDirectory(logDir);
        LogFilePath = Path.Combine(logDir, $"tester_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        BugReportClient.IsSendingEnabled = true;

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(LogFilePath,
                formatProvider: CultureInfo.InvariantCulture,
                rollingInterval: RollingInterval.Infinite,
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Sink(new BugReportSink());

        Logger = loggerConfig.CreateLogger();
        Log.Logger = Logger;

        Logger.Information("Tester application starting");
        Logger.Information("Log file: {LogFilePath}", LogFilePath);

        StatsClient.SendStats();
        UpdateChecker.CheckForUpdates();

        DispatcherUnhandledException += (_, args) =>
        {
            Logger.Fatal(args.Exception, "Unhandled UI exception");
            MessageBox.Show($"Unhandled error: {args.Exception.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                Logger.Fatal(ex, "Unhandled domain exception");
        };
    }

    /// <summary>
    ///     Handles application shutdown: logs the shutdown message and closes and flushes the Serilog logger.
    /// </summary>
    /// <param name="e">The exit event arguments.</param>
    protected override void OnExit(ExitEventArgs e)
    {
        Logger?.Information("Tester application shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}