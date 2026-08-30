using CHDMounter.Core.Logging;

namespace CHDMounter.Core.Tests.Logging;

public class ErrorLoggerTests
{
    [Fact]
    public void InitializeGlobalExceptionHandlersDoesNotThrow()
    {
        var exception = Record.Exception(() => ErrorLogger.InitializeGlobalExceptionHandlers());
        Assert.Null(exception);
    }

    [Fact]
    public void ReportSilentExceptionDoesNotThrow()
    {
        var exception = Record.Exception(() =>
            ErrorLogger.ReportSilentException(new Exception("test"), "test context"));
        Assert.Null(exception);
    }
}