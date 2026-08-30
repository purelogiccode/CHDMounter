using Serilog;

namespace CHDMounter.Core.Logging;

/// <summary>
///     A <see cref="TextWriter" /> that wraps the original console output and forwards
///     <see cref="WriteLine(string?)" /> calls to the application's <see cref="ILoggingService" />.
/// </summary>
internal class LogTextWriter : TextWriter
{
    private readonly TextWriter _originalWriter;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LogTextWriter" /> class.
    /// </summary>
    /// <param name="originalWriter">The original console text writer to delegate output to.</param>
    public LogTextWriter(TextWriter originalWriter)
    {
        _originalWriter = originalWriter;
    }

    /// <inheritdoc />
    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        _originalWriter.Write(value);
    }

    public override void WriteLine(string? value)
    {
        _originalWriter.WriteLine(value);
        try
        {
            var loggingService = ServiceProvider.TryGet<ILoggingService>();
            if (value is not null && loggingService is not null)
                loggingService.Log(value);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "LogTextWriter: Failed to forward message to logging service");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _originalWriter.Flush();
            _originalWriter.Dispose();
        }

        base.Dispose(disposing);
    }
}