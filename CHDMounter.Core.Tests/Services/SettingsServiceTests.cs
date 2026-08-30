namespace CHDMounter.Core.Tests.Services;

public class SettingsServiceTests
{
    [Fact]
    public void ConstructorCreatesDefaultSettingsWhenFileDoesNotExist()
    {
        var appName = $"TestApp_{Guid.NewGuid():N}";
        var service = new SettingsService(appName);
        Assert.NotNull(service.Settings);
        Assert.True(service.Settings.AutoOpenMountedDrive);
    }

    [Fact]
    public void SettingsPropertyReturnsDefaultAppSettings()
    {
        var appName = $"TestApp_{Guid.NewGuid():N}";
        var service = new SettingsService(appName);
        Assert.IsType<AppSettings>(service.Settings);
    }

    [Fact]
    public void SaveDoesNotThrowWhenDirectoryDoesNotExist()
    {
        var appName = $"TestApp_{Guid.NewGuid():N}";
        var service = new SettingsService(appName);
        var exception = Record.Exception(() => service.Save());
        Assert.Null(exception);
    }

    [Fact]
    public void SaveAndLoadPersistsSettings()
    {
        var appName = $"TestApp_{Guid.NewGuid():N}";
        var service1 = new SettingsService(appName);
        service1.Settings.AutoOpenMountedDrive = false;
        service1.Save();

        var service2 = new SettingsService(appName);
        Assert.False(service2.Settings.AutoOpenMountedDrive);
    }

    [Fact]
    public void MultipleSaveCallsDoNotThrow()
    {
        var appName = $"TestApp_{Guid.NewGuid():N}";
        var service = new SettingsService(appName);
        service.Settings.AutoOpenMountedDrive = false;
        service.Save();
        service.Save();
        service.Save();

        var loaded = new SettingsService(appName);
        Assert.False(loaded.Settings.AutoOpenMountedDrive);
    }

    [Fact]
    public void SettingsCanBeModifiedAfterLoad()
    {
        var appName = $"TestApp_{Guid.NewGuid():N}";
        var service = new SettingsService(appName);
        service.Settings.AutoOpenMountedDrive = false;
        Assert.False(service.Settings.AutoOpenMountedDrive);
        service.Settings.AutoOpenMountedDrive = true;
        Assert.True(service.Settings.AutoOpenMountedDrive);
    }
}