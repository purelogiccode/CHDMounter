using System.Reflection;
using CHDMounter.Views;

namespace CHDMounter.Tests.Views;

public class MainWindowFailureClassificationTests
{
    private static bool InvokeIsExpectedFailure(Exception ex, string? fullMessage = null)
    {
        var method = typeof(MainWindow).GetMethod("IsExpectedFailure",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (bool)method.Invoke(null, [ex, fullMessage ?? ex.Message])!;
    }

    [Fact]
    public void DllNotFoundIsExpected()
    {
        Assert.True(InvokeIsExpectedFailure(new DllNotFoundException("missing native library")));
    }

    [Fact]
    public void BadImageFormatIsExpected()
    {
        Assert.True(InvokeIsExpectedFailure(new BadImageFormatException("wrong architecture")));
    }

    [Fact]
    public void EntryPointNotFoundIsExpected()
    {
        Assert.True(InvokeIsExpectedFailure(new EntryPointNotFoundException("missing export")));
    }

    [Fact]
    public void TypeInitializationWrappingDllNotFoundIsExpected()
    {
        var ex = new TypeInitializationException("Fsp.Interop.Api", new DllNotFoundException("winfsp"));
        Assert.True(InvokeIsExpectedFailure(ex));
    }

    [Fact]
    public void WinFspMessageIsExpected()
    {
        Assert.True(InvokeIsExpectedFailure(new InvalidOperationException("WinFsp driver not found")));
    }

    [Fact]
    public void DokanMessageIsExpected()
    {
        Assert.True(InvokeIsExpectedFailure(new InvalidOperationException("Dokan library is unavailable")));
    }

    [Fact]
    public void FuseMessageIsExpected()
    {
        Assert.True(InvokeIsExpectedFailure(new InvalidOperationException("FUSE mount thread failed")));
    }

    [Fact]
    public void AlreadyMountedMessageIsExpected()
    {
        Assert.True(InvokeIsExpectedFailure(new InvalidOperationException("Drive is already mounted")));
    }

    [Fact]
    public void MountPointMessageIsExpected()
    {
        Assert.True(InvokeIsExpectedFailure(new InvalidOperationException("Mount point M: is busy")));
    }

    [Fact]
    public void GenericFailureIsNotExpected()
    {
        Assert.False(InvokeIsExpectedFailure(new InvalidOperationException("something truly unexpected")));
    }

    [Fact]
    public void NullReferenceIsNotExpected()
    {
        Assert.False(InvokeIsExpectedFailure(new NullReferenceException("Object reference not set")));
    }

    [Fact]
    public void MessageMatchIsCaseInsensitive()
    {
        Assert.True(InvokeIsExpectedFailure(new InvalidOperationException("WINFSP NOT AVAILABLE")));
    }

    [Fact]
    public void DeeplyNestedExpectedExceptionIsClassified()
    {
        var ex = new InvalidOperationException("mount failed",
            new TargetInvocationException("wrapper", new DllNotFoundException("native")));
        Assert.True(InvokeIsExpectedFailure(ex));
    }

    [Fact]
    public void FullMessageParameterIsUsedForClassification()
    {
        var ex = new InvalidOperationException("opaque failure");
        Assert.True(InvokeIsExpectedFailure(ex, "Mount failed: winfsp driver not found"));
    }
}
