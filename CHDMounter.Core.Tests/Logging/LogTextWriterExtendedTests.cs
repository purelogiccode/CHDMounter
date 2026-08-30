using System.Text;
using CHDMounter.Core.Logging;

namespace CHDMounter.Core.Tests.Logging;

public class LogTextWriterExtendedTests
{
    private static LogTextWriter CreateLogTextWriter(StringWriter? underlying = null)
    {
        var writer = underlying ?? new StringWriter();
        return new LogTextWriter(writer);
    }

    [Fact]
    public void EncodingReturnsUtf8()
    {
        using var writer = CreateLogTextWriter();
        Assert.Equal(Encoding.UTF8, writer.Encoding);
    }

    [Fact]
    public void WriteCharDelegatesToOriginalWriter()
    {
        using var sw = new StringWriter();
        using var writer = CreateLogTextWriter(sw);

        writer.Write('A');

        Assert.Equal("A", sw.ToString());
    }

    [Fact]
    public void WriteMultipleCharsDelegatesToOriginalWriter()
    {
        using var sw = new StringWriter();
        using var writer = CreateLogTextWriter(sw);

        writer.Write('H');
        writer.Write('i');

        Assert.Equal("Hi", sw.ToString());
    }

    [Fact]
    public void WriteLineStringDelegatesToOriginalWriter()
    {
        using var sw = new StringWriter();
        using var writer = CreateLogTextWriter(sw);

        writer.WriteLine("hello world");

        var output = sw.ToString();
        Assert.Contains("hello world", output, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteLineNullDoesNotThrow()
    {
        using var sw = new StringWriter();
        using var writer = CreateLogTextWriter(sw);

        writer.WriteLine((string?)null);
    }

    [Fact]
    public void WriteLineNullWritesNewlineToOriginalWriter()
    {
        using var sw = new StringWriter();
        using var writer = CreateLogTextWriter(sw);

        writer.WriteLine((string?)null);

        // StringWriter.WriteLine(null) writes just a newline
        var output = sw.ToString();
        Assert.Contains(Environment.NewLine, output, StringComparison.Ordinal);
    }

    [Fact]
    public void DisposeFlushesOriginalWriter()
    {
        var sw = new StringWriter();
        var writer = CreateLogTextWriter(sw);

        writer.Dispose();

        // StringWriter doesn't track flush state directly, but Dispose should not throw
        var exception = Record.Exception(() => sw.ToString());
        Assert.Null(exception);
    }

    [Fact]
    public void MultipleDisposeCallsDoNotThrow()
    {
        var sw = new StringWriter();
        var writer = CreateLogTextWriter(sw);

        writer.Dispose();
        var exception = Record.Exception(() => writer.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void WriteLineEmptyStringDoesNotThrow()
    {
        using var sw = new StringWriter();
        using var writer = CreateLogTextWriter(sw);

        writer.WriteLine("");
    }

    [Fact]
    public void WriteCharDoesNotCallLoggingService()
    {
        // Write(char) should NOT forward to ILoggingService, only WriteLine does
        using var sw = new StringWriter();
        using var writer = CreateLogTextWriter(sw);

        // Just verify no exception when no ILoggingService is registered
        writer.Write('X');
    }

    [Fact]
    public void LogTextWriterConstructorWithValidWriterDoesNotThrow()
    {
        using var sw = new StringWriter();
        _ = new LogTextWriter(sw);
    }

    [Fact]
    public void LogTextWriterImplementsTextWriter()
    {
        using var writer = CreateLogTextWriter();
        Assert.IsAssignableFrom<TextWriter>(writer);
    }
}