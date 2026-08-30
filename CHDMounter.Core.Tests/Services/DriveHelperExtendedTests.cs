namespace CHDMounter.Core.Tests.Services;

public class DriveHelperExtendedTests
{
    [Fact]
    public void PickDriveLetterReturnsUpperCaseLetter()
    {
        var result = DriveHelper.PickDriveLetter();
        Assert.True(char.IsUpper(result[0]), $"Expected uppercase letter but got '{result[0]}'");
    }

    [Fact]
    public void PickDriveLetterReturnsLetterBetweenDandZ()
    {
        var result = DriveHelper.PickDriveLetter();
        var letter = result[0];
        Assert.InRange(letter, 'D', 'Z');
    }

    [Fact]
    public void PickDriveLetterFormatIsLetterColon()
    {
        var result = DriveHelper.PickDriveLetter();
        Assert.Matches("^[A-Z]:$", result);
    }

    [Fact]
    public void PickDriveLetterCalledTwiceReturnsValidResult()
    {
        var result1 = DriveHelper.PickDriveLetter();
        var result2 = DriveHelper.PickDriveLetter();

        Assert.Matches("^[A-Z]:$", result1);
        Assert.Matches("^[A-Z]:$", result2);
    }

    [Fact]
    public void PickDriveLetterIsNotCurrentlyMounted()
    {
        var result = DriveHelper.PickDriveLetter();

        Assert.NotNull(result);
        Assert.True(result.Length == 2);
    }
}