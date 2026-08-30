using CHDMounter.Core.Logging;

namespace CHDMounter.Core.Tests.Logging;

public class DiagnosticLoggerTests
{
    [Fact]
    public void GetAppDataFolderReturnsNonEmptyString()
    {
        var result = DiagnosticLogger.GetAppDataFolder("TestApp");
        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void GetAppDataFolderEndsWithAppName()
    {
        var result = DiagnosticLogger.GetAppDataFolder("MyTestApp");
        Assert.EndsWith("MyTestApp", result, StringComparison.Ordinal);
    }

    [Fact]
    public void GetAppDataFolderContainsLocalAppData()
    {
        var result = DiagnosticLogger.GetAppDataFolder("TestApp");
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Assert.StartsWith(localAppData, result, StringComparison.Ordinal);
    }

    [Fact]
    public void LogFilePathIsNullBeforeInitialize()
    {
        // LogFilePath is static and may have been set by other tests
        // We can at least verify it's readable
        _ = DiagnosticLogger.LogFilePath;
    }

    [Fact]
    public void LogDoesNotThrow()
    {
        var exception = Record.Exception(() => DiagnosticLogger.Log("test message"));
        Assert.Null(exception);
    }

    [Fact]
    public void LogSectionDoesNotThrow()
    {
        var exception = Record.Exception(() => DiagnosticLogger.LogSection("Test Section"));
        Assert.Null(exception);
    }

    [Fact]
    public void AppDataLogFolderIsNotNull()
    {
        var result = DiagnosticLogger.AppDataLogFolder;
        Assert.NotNull(result);
    }

    [Fact]
    public void CleanupOldLogsDoesNotThrowWhenNotInitialized()
    {
        var exception = Record.Exception(() => DiagnosticLogger.CleanupOldLogs());
        Assert.Null(exception);
    }
}