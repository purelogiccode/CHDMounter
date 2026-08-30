using CHDMounter.Core.Logging;

namespace CHDMounter.Core.Tests.Logging;

public class AppLoggerTests
{
    [Fact]
    public void InitializeDoesNotThrowWithValidPath()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"test_log_{Guid.NewGuid():N}.log");
        try
        {
            var exception = Record.Exception(() => AppLogger.Initialize(tempPath));
            Assert.Null(exception);
        }
        finally
        {
            AppLogger.CloseAndFlush();
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public void CloseAndFlushDoesNotThrow()
    {
        var exception = Record.Exception(() => AppLogger.CloseAndFlush());
        Assert.Null(exception);
    }
}