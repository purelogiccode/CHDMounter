namespace CHDMounter.Core.Tests.Models;

public class AppSettingsTests
{
    [Fact]
    public void DefaultAutoOpenMountedDriveIsTrue()
    {
        var settings = new AppSettings();
        Assert.True(settings.AutoOpenMountedDrive);
    }

    [Fact]
    public void AutoOpenMountedDriveCanBeSetToFalse()
    {
        var settings = new AppSettings { AutoOpenMountedDrive = false };
        Assert.False(settings.AutoOpenMountedDrive);
    }

    [Fact]
    public void AutoOpenMountedDriveCanBeToggled()
    {
        var settings = new AppSettings();
        Assert.True(settings.AutoOpenMountedDrive);
        settings.AutoOpenMountedDrive = false;
        Assert.False(settings.AutoOpenMountedDrive);
        settings.AutoOpenMountedDrive = true;
        Assert.True(settings.AutoOpenMountedDrive);
    }
}