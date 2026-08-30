using CHDMounter.Core.Logging;

namespace CHDMounter.Core.Tests.Logging;

public class LogTextWriterTests
{
    // Note: LogTextWriter is internal, so we test it indirectly through its public behavior
    // The class wraps a TextWriter and delegates Write/WriteLine calls

    [Fact]
    public void LogTextWriterIsInternal()
    {
        var type = typeof(DiagnosticLogger).Assembly
            .GetType("CHDMounter.Core.Logging.LogTextWriter");
        // The class should exist but be internal
        Assert.NotNull(type);
        Assert.False(type.IsPublic);
    }
}