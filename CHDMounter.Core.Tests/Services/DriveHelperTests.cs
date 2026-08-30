namespace CHDMounter.Core.Tests.Services;

public class DriveHelperTests
{
    [Fact]
    public void PickDriveLetterReturnsNonEmptyString()
    {
        var result = DriveHelper.PickDriveLetter();
        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void PickDriveLetterEndsWithColon()
    {
        var result = DriveHelper.PickDriveLetter();
        Assert.EndsWith(":", result, StringComparison.Ordinal);
    }

    [Fact]
    public void PickDriveLetterReturnsSingleLetter()
    {
        var result = DriveHelper.PickDriveLetter();
        Assert.Equal(2, result.Length);
    }

    [Fact]
    public void PickDriveLetterReturnsLetterInRangeMtoQ()
    {
        var result = DriveHelper.PickDriveLetter();
        var letter = result[0];
        Assert.True(letter is >= 'M' and <= 'Q' or >= 'D' and <= 'Z',
            $"Drive letter '{letter}' is not in expected range M-Q or D-Z");
    }

    [Fact]
    public void PickDriveLetterReturnsAvailableDrive()
    {
        var result = DriveHelper.PickDriveLetter();
        var letter = result[0];
        var drives = DriveInfo.GetDrives().Select(d => d.Name[0]).ToHashSet();
        Assert.DoesNotContain(letter, drives);
    }

    [Fact]
    public void PickDriveLetterPrefersMthroughQ()
    {
        var drives = DriveInfo.GetDrives().Select(d => d.Name[0]).ToHashSet();
        var result = DriveHelper.PickDriveLetter();
        var letter = result[0];

        if (!drives.Contains('M'))
            Assert.Equal('M', letter);
        else if (!drives.Contains('N')) Assert.Equal('N', letter);
    }

    [Fact]
    public void GetAvailableDriveLettersReturnsValidCandidates()
    {
        var letters = DriveHelper.GetAvailableDriveLetters().ToList();

        Assert.NotEmpty(letters);
        Assert.All(letters, l => Assert.Matches("^[A-Z]:$", l));
    }

    [Fact]
    public void GetAvailableDriveLettersExcludesCurrentlyUsedLetters()
    {
        var drives = DriveInfo.GetDrives().Select(d => d.Name[0]).ToHashSet();
        var letters = DriveHelper.GetAvailableDriveLetters().Select(l => l[0]).ToList();

        Assert.DoesNotContain(letters, l => drives.Contains(l));
    }

    [Fact]
    public void GetAvailableDriveLettersFirstCandidateMatchesPickDriveLetter()
    {
        var first = DriveHelper.GetAvailableDriveLetters().First();

        Assert.Equal(DriveHelper.PickDriveLetter(), first);
    }
}