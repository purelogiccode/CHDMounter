namespace CHDMounter.Core.Tests.Models;

public class AppSettingsExtendedTests
{
    [Fact]
    public void AppSettingsDefaultConstructorSetsDefaults()
    {
        var settings = new AppSettings();
        Assert.True(settings.AutoOpenMountedDrive);
    }

    [Fact]
    public void AppSettingsCanTogglePropertyMultipleTimes()
    {
        var settings = new AppSettings();
        Assert.True(settings.AutoOpenMountedDrive);

        settings.AutoOpenMountedDrive = false;
        Assert.False(settings.AutoOpenMountedDrive);

        settings.AutoOpenMountedDrive = true;
        Assert.True(settings.AutoOpenMountedDrive);
    }

    [Fact]
    public void AppSettingsPropertyIsReadWrite()
    {
        var settings = new AppSettings();
        settings.AutoOpenMountedDrive = false;
        Assert.False(settings.AutoOpenMountedDrive);
    }

    [Fact]
    public void AppSettingsNewInstanceHasDefaultValues()
    {
        var settings1 = new AppSettings();
        var settings2 = new AppSettings();

        Assert.Equal(settings1.AutoOpenMountedDrive, settings2.AutoOpenMountedDrive);
    }

    [Fact]
    public void AppSettingsModificationDoesNotAffectOtherInstances()
    {
        var settings1 = new AppSettings();
        var settings2 = new AppSettings();

        settings1.AutoOpenMountedDrive = false;

        Assert.True(settings2.AutoOpenMountedDrive);
    }
}