using System.Globalization;
using System.Reflection;
using CHDMounter.Core.Logging;

namespace CHDMounter.Core.Tests.Logging;

public class DiagnosticLoggerExtendedTests : IDisposable
{
    private readonly string _testAppDataFolder;
    private readonly string _testLogFolder;

    public DiagnosticLoggerExtendedTests()
    {
        _testAppDataFolder = DiagnosticLogger.GetAppDataFolder("CHDMounterTest_" + Guid.NewGuid().ToString("N")[..8]);
        _testLogFolder = Path.Combine(_testAppDataFolder, "logs");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testAppDataFolder))
                Directory.Delete(_testAppDataFolder, true);
        }
        catch
        {
            // cleanup best effort
        }
    }

    // --- Initialize ---

    [Fact]
    public void InitializeCreatesAppDataFolder()
    {
        var appName = "CHDMounterTest_" + Guid.NewGuid().ToString("N")[..8];
        var expectedFolder = DiagnosticLogger.GetAppDataFolder(appName);

        try
        {
            DiagnosticLogger.Initialize(appName);
            Assert.True(Directory.Exists(expectedFolder));
        }
        finally
        {
            if (Directory.Exists(expectedFolder))
                Directory.Delete(expectedFolder, true);
        }
    }

    [Fact]
    public void InitializeCreatesLogsSubfolder()
    {
        var appName = "CHDMounterTest_" + Guid.NewGuid().ToString("N")[..8];
        var expectedLogFolder = Path.Combine(DiagnosticLogger.GetAppDataFolder(appName), "logs");

        try
        {
            DiagnosticLogger.Initialize(appName);
            Assert.True(Directory.Exists(expectedLogFolder));
        }
        finally
        {
            var appFolder = DiagnosticLogger.GetAppDataFolder(appName);
            if (Directory.Exists(appFolder))
                Directory.Delete(appFolder, true);
        }
    }

    [Fact]
    public void InitializeSetsLogFilePath()
    {
        var appName = "CHDMounterTest_" + Guid.NewGuid().ToString("N")[..8];

        try
        {
            DiagnosticLogger.Initialize(appName);
            Assert.NotNull(DiagnosticLogger.LogFilePath);
            Assert.Contains("debug_", DiagnosticLogger.LogFilePath, StringComparison.Ordinal);
            Assert.EndsWith(".log", DiagnosticLogger.LogFilePath, StringComparison.Ordinal);
        }
        finally
        {
            var appFolder = DiagnosticLogger.GetAppDataFolder(appName);
            if (Directory.Exists(appFolder))
                Directory.Delete(appFolder, true);
        }
    }

    [Fact]
    public void InitializeSetsAppDataFolder()
    {
        var appName = "CHDMounterTest_" + Guid.NewGuid().ToString("N")[..8];

        try
        {
            DiagnosticLogger.Initialize(appName);
            Assert.Contains(appName, DiagnosticLogger.AppDataFolder, StringComparison.Ordinal);
            Assert.EndsWith(appName, DiagnosticLogger.AppDataFolder, StringComparison.Ordinal);
        }
        finally
        {
            var appFolder = DiagnosticLogger.GetAppDataFolder(appName);
            if (Directory.Exists(appFolder))
                Directory.Delete(appFolder, true);
        }
    }

    [Fact]
    public void InitializeSetsAppDataLogFolder()
    {
        var appName = "CHDMounterTest_" + Guid.NewGuid().ToString("N")[..8];

        try
        {
            DiagnosticLogger.Initialize(appName);
            Assert.NotNull(DiagnosticLogger.AppDataLogFolder);
            Assert.EndsWith("logs", DiagnosticLogger.AppDataLogFolder, StringComparison.Ordinal);
        }
        finally
        {
            var appFolder = DiagnosticLogger.GetAppDataFolder(appName);
            if (Directory.Exists(appFolder))
                Directory.Delete(appFolder, true);
        }
    }

    [Fact]
    public void InitializeLogFilePathContainsTimestamp()
    {
        var appName = "CHDMounterTest_" + Guid.NewGuid().ToString("N")[..8];
        var dateStamp = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        try
        {
            DiagnosticLogger.Initialize(appName);
            Assert.NotNull(DiagnosticLogger.LogFilePath);
            Assert.Contains(dateStamp, DiagnosticLogger.LogFilePath, StringComparison.Ordinal);
        }
        finally
        {
            var appFolder = DiagnosticLogger.GetAppDataFolder(appName);
            if (Directory.Exists(appFolder))
                Directory.Delete(appFolder, true);
        }
    }

    // --- CleanupOldLogs ---

    [Fact]
    public void CleanupOldLogsDeletesOldFiles()
    {
        Directory.CreateDirectory(_testLogFolder);

        // Create an old log file with a past creation time
        var oldLogFile = Path.Combine(_testLogFolder, "debug_20200101_000000.log");
        File.WriteAllText(oldLogFile, "old log content");

        // Set creation time to 10 days ago
        File.SetCreationTime(oldLogFile, DateTime.Now.AddDays(-10));

        // Set the AppDataLogFolder via reflection since it's private set
        typeof(DiagnosticLogger)
            .GetProperty("AppDataLogFolder", BindingFlags.Public | BindingFlags.Static)!
            .SetValue(null, _testLogFolder);

        DiagnosticLogger.CleanupOldLogs();

        Assert.False(File.Exists(oldLogFile));
    }

    [Fact]
    public void CleanupOldLogsKeepsRecentFiles()
    {
        Directory.CreateDirectory(_testLogFolder);

        var recentLogFile = Path.Combine(_testLogFolder, "debug_recent.log");
        File.WriteAllText(recentLogFile, "recent log content");

        typeof(DiagnosticLogger)
            .GetProperty("AppDataLogFolder", BindingFlags.Public | BindingFlags.Static)!
            .SetValue(null, _testLogFolder);

        DiagnosticLogger.CleanupOldLogs();

        Assert.True(File.Exists(recentLogFile));
    }

    [Fact]
    public void CleanupOldLogsHandlesNonExistentDirectory()
    {
        typeof(DiagnosticLogger)
            .GetProperty("AppDataLogFolder", BindingFlags.Public | BindingFlags.Static)!
            .SetValue(null, Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid()));

        var exception = Record.Exception(() => DiagnosticLogger.CleanupOldLogs());
        Assert.Null(exception);
    }

    [Fact]
    public void CleanupOldLogsOnlyDeletesDebugPatternFiles()
    {
        Directory.CreateDirectory(_testLogFolder);

        // Create an old non-matching file
        var otherFile = Path.Combine(_testLogFolder, "other_log.log");
        File.WriteAllText(otherFile, "other content");
        File.SetCreationTime(otherFile, DateTime.Now.AddDays(-10));

        // Create an old matching file
        var debugFile = Path.Combine(_testLogFolder, "debug_old.log");
        File.WriteAllText(debugFile, "debug content");
        File.SetCreationTime(debugFile, DateTime.Now.AddDays(-10));

        typeof(DiagnosticLogger)
            .GetProperty("AppDataLogFolder", BindingFlags.Public | BindingFlags.Static)!
            .SetValue(null, _testLogFolder);

        DiagnosticLogger.CleanupOldLogs();

        Assert.True(File.Exists(otherFile));
        Assert.False(File.Exists(debugFile));
    }

    // --- GetAppDataFolder ---

    [Fact]
    public void GetAppDataFolderWithDifferentNamesReturnsDifferentPaths()
    {
        var path1 = DiagnosticLogger.GetAppDataFolder("AppOne");
        var path2 = DiagnosticLogger.GetAppDataFolder("AppTwo");
        Assert.NotEqual(path1, path2, StringComparer.Ordinal);
    }

    [Fact]
    public void GetAppDataFolderWithSameNameReturnsSamePath()
    {
        var path1 = DiagnosticLogger.GetAppDataFolder("SameApp");
        var path2 = DiagnosticLogger.GetAppDataFolder("SameApp");
        Assert.Equal(path1, path2);
    }

    // --- GetAppDataFolderForCurrentApp ---

    [Fact]
    public void GetAppDataFolderForCurrentAppReturnsAppDataFolder()
    {
        var result = DiagnosticLogger.GetAppDataFolderForCurrentApp();
        Assert.Equal(DiagnosticLogger.AppDataFolder, result);
    }

    // --- LogSection ---

    [Fact]
    public void LogSectionFormatsCorrectly()
    {
        // LogSection doesn't return anything, just verify it doesn't throw
        var exception = Record.Exception(() => DiagnosticLogger.LogSection("My Section"));
        Assert.Null(exception);
    }
}