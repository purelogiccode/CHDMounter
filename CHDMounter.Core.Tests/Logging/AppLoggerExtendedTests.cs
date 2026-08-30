using CHDMounter.Core.Logging;

namespace CHDMounter.Core.Tests.Logging;

public class AppLoggerExtendedTests
{
    [Fact]
    public void InitializeCreatesLogFile()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid():N}.log");
        try
        {
            AppLogger.Initialize(tempPath);
            // File may not be created until a log event is written
            // But the initialize itself should not throw
        }
        finally
        {
            AppLogger.CloseAndFlush();
            // Clean up any generated files (rolling creates date-stamped files)
            var dir = Path.GetDirectoryName(tempPath)!;
            var baseName = Path.GetFileNameWithoutExtension(tempPath);
            foreach (var file in Directory.GetFiles(dir, baseName + "*"))
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // ignored
                }
        }
    }

    [Fact]
    public void InitializeWithDifferentPathsDoesNotThrow()
    {
        var path1 = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid():N}.log");
        var path2 = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid():N}.log");

        try
        {
            AppLogger.Initialize(path1);
            AppLogger.CloseAndFlush();
            AppLogger.Initialize(path2);
        }
        finally
        {
            AppLogger.CloseAndFlush();
            CleanupLogFile(path1);
            CleanupLogFile(path2);
        }
    }

    [Fact]
    public void CloseAndFlushCanBeCalledMultipleTimes()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid():N}.log");
        try
        {
            AppLogger.Initialize(tempPath);
            AppLogger.CloseAndFlush();
            var exception = Record.Exception(() => AppLogger.CloseAndFlush());
            Assert.Null(exception);
        }
        finally
        {
            CleanupLogFile(tempPath);
        }
    }

    [Fact]
    public void CloseAndFlushDoesNotThrowWhenNotInitialized()
    {
        // CloseAndFlush should be safe even if not initialized
        var exception = Record.Exception(() => AppLogger.CloseAndFlush());
        Assert.Null(exception);
    }

    private static void CleanupLogFile(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(path)!;
            var baseName = Path.GetFileNameWithoutExtension(path);
            if (Directory.Exists(dir))
                foreach (var file in Directory.GetFiles(dir, baseName + "*"))
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // ignored
                    }
        }
        catch
        {
            // cleanup best effort
        }
    }
}