using System.Runtime.CompilerServices;
using Serilog;

namespace CHDMounter.Core.Tests;

internal static class TestInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Never send real bug reports from the test suite - unit tests would
        // otherwise pollute the production bug-report database with test noise.
        BugReportClient.IsSendingEnabled = false;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .WriteTo.Debug()
            .CreateLogger();
    }
}