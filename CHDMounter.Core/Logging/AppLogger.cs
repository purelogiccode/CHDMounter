using System.Globalization;
using Serilog;

namespace CHDMounter.Core.Logging;

/// <summary>
///     Configures and manages the Serilog logging pipeline with file, debug, and bug report sinks.
/// </summary>
public static class AppLogger
{
    /// <summary>
    ///     Initializes the Serilog logger with file output, debug output, and bug report sink.
    /// </summary>
    /// <param name="logFilePath">The file path for the rolling log file.</param>
    public static void Initialize(string logFilePath)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.Sink(new BugReportSink())
            .CreateLogger();
    }

    /// <summary>
    ///     Closes and flushes the Serilog logger, ensuring all pending log events are written.
    /// </summary>
    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }
}