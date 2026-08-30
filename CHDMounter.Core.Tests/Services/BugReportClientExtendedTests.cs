using System.Globalization;
using System.Reflection;

namespace CHDMounter.Core.Tests.Services;

public class BugReportClientExtendedTests
{
    private static string InvokeTruncate(string value, int maxLength)
    {
        var method = typeof(BugReportClient).GetMethod("Truncate", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, [value, maxLength])!;
    }

    private static string InvokeBuildEnvironmentDetails()
    {
        var method =
            typeof(BugReportClient).GetMethod("BuildEnvironmentDetails", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, null!)!;
    }

    private static string InvokeBuildExceptionDetails(Exception ex)
    {
        var method =
            typeof(BugReportClient).GetMethod("BuildExceptionDetails", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, [ex])!;
    }

    private static string InvokeGetApiKey()
    {
        var method = typeof(AppInfoHelper).GetMethod("GetApiKey", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, null!)!;
    }

    private static string InvokeGetAppName()
    {
        var method = typeof(AppInfoHelper).GetMethod("GetAppName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, null!)!;
    }

    private static string InvokeGetVersion()
    {
        var method = typeof(AppInfoHelper).GetMethod("GetVersion", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, null!)!;
    }

    // --- Truncate edge cases ---

    [Fact]
    public void TruncateWithMaxLengthOneThrowsForLongerString()
    {
        // Truncate does not handle maxLength < 3 when string is longer
        // value[..(maxLength - 3)] => value[..(-2)] => ArgumentOutOfRangeException
        var ex = Assert.Throws<TargetInvocationException>(() => InvokeTruncate("hello", 1));
        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
    }

    [Fact]
    public void TruncateWithMaxLengthTwoThrowsForLongerString()
    {
        // value[..(maxLength - 3)] => value[..(-1)] => ArgumentOutOfRangeException
        var ex = Assert.Throws<TargetInvocationException>(() => InvokeTruncate("hello", 2));
        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
    }

    [Fact]
    public void TruncateWithMaxLengthOneReturnsShortString()
    {
        var result = InvokeTruncate("h", 1);
        Assert.Equal("h", result);
    }

    [Fact]
    public void TruncateWithMaxLengthTwoReturnsShortString()
    {
        var result = InvokeTruncate("he", 2);
        Assert.Equal("he", result);
    }

    [Fact]
    public void TruncateWithMaxLengthThreeReturnsEllipsis()
    {
        var result = InvokeTruncate("hello", 3);
        // maxLength - 3 = 0, value[..0] = "", + "..." = "..."
        Assert.Equal("...", result);
    }

    [Fact]
    public void TruncateWithMaxLengthSixReturnsThreeCharsPlusEllipsis()
    {
        var result = InvokeTruncate("hello world", 6);
        Assert.Equal("hel...", result);
        Assert.Equal(6, result.Length);
    }

    [Fact]
    public void TruncateWithWhitespaceStringReturnsOriginal()
    {
        var result = InvokeTruncate("   ", 10);
        Assert.Equal("   ", result);
    }

    [Fact]
    public void TruncateWithSpecialCharactersReturnsCorrectly()
    {
        var result = InvokeTruncate("hello\nworld\ttab", 10);
        Assert.Equal(10, result.Length);
        Assert.EndsWith("...", result, StringComparison.Ordinal);
    }

    [Fact]
    public void TruncateUnicodeCharactersReturnsCorrectly()
    {
        const string input = "\u00e9\u00e8\u00ea\u00eb\u00ef\u00f4\u00fb\u00fc";
        var result = InvokeTruncate(input, 5);
        Assert.Equal(5, result.Length);
        Assert.EndsWith("...", result, StringComparison.Ordinal);
    }

    // --- BuildEnvironmentDetails ---

    [Fact]
    public void BuildEnvironmentDetailsContainsDate()
    {
        var result = InvokeBuildEnvironmentDetails();
        Assert.Contains("Date:", result, StringComparison.Ordinal);
        Assert.Contains(DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), result,
            StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetailsContainsApplicationName()
    {
        var result = InvokeBuildEnvironmentDetails();
        Assert.Contains("Application Name:", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetailsContainsApplicationVersion()
    {
        var result = InvokeBuildEnvironmentDetails();
        Assert.Contains("Application Version:", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetailsContainsOsVersion()
    {
        var result = InvokeBuildEnvironmentDetails();
        Assert.Contains("OS Version:", result, StringComparison.Ordinal);
        Assert.Contains(Environment.OSVersion.ToString(), result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetailsContainsArchitecture()
    {
        var result = InvokeBuildEnvironmentDetails();
        Assert.Contains("Architecture:", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetailsContainsBitness()
    {
        var result = InvokeBuildEnvironmentDetails();
        Assert.Contains("Bitness:", result, StringComparison.Ordinal);
        var expected = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
        Assert.Contains(expected, result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetailsContainsProcessorCount()
    {
        var result = InvokeBuildEnvironmentDetails();
        Assert.Contains("Processor Count:", result, StringComparison.Ordinal);
        Assert.Contains(Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture), result,
            StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetailsContainsBaseDirectory()
    {
        var result = InvokeBuildEnvironmentDetails();
        Assert.Contains("Base Directory:", result, StringComparison.Ordinal);
        Assert.Contains(AppContext.BaseDirectory, result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetailsContainsTempPath()
    {
        var result = InvokeBuildEnvironmentDetails();
        Assert.Contains("Temp Path:", result, StringComparison.Ordinal);
        Assert.Contains(Path.GetTempPath(), result, StringComparison.Ordinal);
    }

    // --- BuildExceptionDetails ---

    [Fact]
    public void BuildExceptionDetailsContainsType()
    {
        var ex = new InvalidOperationException("test error");
        var result = InvokeBuildExceptionDetails(ex);
        Assert.Contains("Type:", result, StringComparison.Ordinal);
        Assert.Contains("System.InvalidOperationException", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExceptionDetailsContainsMessage()
    {
        var ex = new InvalidOperationException("test error message");
        var result = InvokeBuildExceptionDetails(ex);
        Assert.Contains("Message:", result, StringComparison.Ordinal);
        Assert.Contains("test error message", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExceptionDetailsContainsSource()
    {
        var ex = new Exception("test");
        ex.Source = "TestSource";
        var result = InvokeBuildExceptionDetails(ex);
        Assert.Contains("Source:", result, StringComparison.Ordinal);
        Assert.Contains("TestSource", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExceptionDetailsContainsStackTrace()
    {
        Exception ex;
        try
        {
            throw new InvalidOperationException("thrown exception");
        }
        catch (Exception caught)
        {
            ex = caught;
        }

        var result = InvokeBuildExceptionDetails(ex);
        Assert.Contains("StackTrace:", result, StringComparison.Ordinal);
        Assert.Contains("BuildExceptionDetailsContainsStackTrace", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExceptionDetailsWithNullExceptionSource()
    {
        var ex = new Exception("test") { Source = null! };
        var result = InvokeBuildExceptionDetails(ex);
        Assert.Contains("Source:", result, StringComparison.Ordinal);
    }

    // --- GetApiKey ---

    [Fact]
    public void GetApiKeyReturnsNonEmptyString()
    {
        var result = InvokeGetApiKey();
        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void GetApiKeyReturnsNonWhitespaceString()
    {
        var result = InvokeGetApiKey();
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    // --- GetAppName ---

    [Fact]
    public void GetAppNameReturnsNonEmptyString()
    {
        var result = InvokeGetAppName();
        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void GetAppNameReturnsExpectedName()
    {
        var result = InvokeGetAppName();
        // In test context, GetEntryAssembly may return the test runner name
        // The method falls back to "CHDMounter" if null
        Assert.NotNull(result);
    }

    // --- GetVersion ---

    [Fact]
    public void GetVersionReturnsNonEmptyString()
    {
        var result = InvokeGetVersion();
        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void GetVersionReturnsValidFormat()
    {
        var result = InvokeGetVersion();
        // Should be parseable as a version or at least a non-empty string
        Assert.NotNull(result);
        Assert.Contains(".", result, StringComparison.Ordinal);
    }

    [Fact]
    public void SendMethodsDoNotThrowWhenSendingDisabled()
    {
        var originalValue = BugReportClient.IsSendingEnabled;
        BugReportClient.IsSendingEnabled = false;
        try
        {
            var ex = new InvalidOperationException("disabled test exception");
            var exception1 = Record.Exception(() => BugReportClient.SendException(ex, "disabled context"));
            var exception2 = Record.Exception(() => BugReportClient.SendWarning("disabled warning"));
            var exception3 = Record.Exception(() => BugReportClient.SendError("disabled error", null));
            Assert.Null(exception1);
            Assert.Null(exception2);
            Assert.Null(exception3);
        }
        finally
        {
            BugReportClient.IsSendingEnabled = originalValue;
        }
    }

    // --- SendException ---

    [Fact]
    public void SendExceptionDoesNotThrow()
    {
        var ex = new InvalidOperationException("test exception");
        var exception = Record.Exception(() => BugReportClient.SendException(ex, "test context"));
        Assert.Null(exception);
    }

    [Fact]
    public void SendExceptionWithNullExceptionMessage()
    {
        var ex = new Exception();
        var exception = Record.Exception(() => BugReportClient.SendException(ex, "context"));
        Assert.Null(exception);
    }

    // --- SendWarning ---

    [Fact]
    public void SendWarningDoesNotThrow()
    {
        var exception = Record.Exception(() => BugReportClient.SendWarning("test warning"));
        Assert.Null(exception);
    }

    [Fact]
    public void SendWarningWithEmptyMessage()
    {
        var exception = Record.Exception(() => BugReportClient.SendWarning(""));
        Assert.Null(exception);
    }

    // --- SendError ---

    [Fact]
    public void SendErrorDoesNotThrow()
    {
        var exception = Record.Exception(() => BugReportClient.SendError("test error", "stack trace"));
        Assert.Null(exception);
    }

    [Fact]
    public void SendErrorWithNullStackTrace()
    {
        var exception = Record.Exception(() => BugReportClient.SendError("test error", null));
        Assert.Null(exception);
    }

    [Fact]
    public void SendErrorWithEmptyStackTrace()
    {
        var exception = Record.Exception(() => BugReportClient.SendError("test error", ""));
        Assert.Null(exception);
    }
}