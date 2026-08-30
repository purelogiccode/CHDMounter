using System.Reflection;
using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Core.Tests.Parsers;

public class SectorReaderHelperTests
{
    private static byte InvokeBcdToByte(byte bcd)
    {
        var method = typeof(SectorReader).GetMethod("BcdToByte", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (byte)method.Invoke(null, [bcd])!;
    }

    [Theory]
    [InlineData(0x00, 0)]
    [InlineData(0x01, 1)]
    [InlineData(0x09, 9)]
    [InlineData(0x10, 10)]
    [InlineData(0x15, 15)]
    [InlineData(0x23, 23)]
    [InlineData(0x59, 59)]
    [InlineData(0x75, 75)]
    [InlineData(0x99, 99)]
    public void BcdToByteConvertsCorrectly(byte bcd, byte expected)
    {
        var result = InvokeBcdToByte(bcd);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BcdToByteHandlesZero()
    {
        Assert.Equal(0, InvokeBcdToByte(0x00));
    }

    [Fact]
    public void BcdToByteHandlesMaxBcd()
    {
        Assert.Equal(99, InvokeBcdToByte(0x99));
    }

    [Fact]
    public void SectorReaderHasSectorSizeConstant()
    {
        var field = typeof(SectorReader).GetField("SectorSize", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        Assert.Equal(2048, field.GetValue(null));
    }

    [Fact]
    public void SectorReaderImplementsIDisposable()
    {
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(SectorReader)));
    }

    [Fact]
    public void SectorReaderDisposeDoesNotThrow()
    {
        var method = typeof(SectorReader).GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);
    }
}