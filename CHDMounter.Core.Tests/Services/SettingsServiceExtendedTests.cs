namespace CHDMounter.Core.Tests.Services;

public class SettingsServiceExtendedTests : IDisposable
{
    private readonly string _tempDir;

    public SettingsServiceExtendedTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CHDMounter_Test_" + Guid.NewGuid().ToString("N")[..8]);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // cleanup best effort
        }
    }

    [Fact]
    public void SettingsPropertyIsNotNullAfterConstruction()
    {
        // SettingsService uses a fixed path based on app name, but we can test default behavior
        var service = new SettingsService("TestApp_" + Guid.NewGuid().ToString("N")[..8]);
        Assert.NotNull(service.Settings);
    }

    [Fact]
    public void SettingsDefaultAutoOpenMountedDriveIsTrue()
    {
        var service = new SettingsService("TestApp_" + Guid.NewGuid().ToString("N")[..8]);
        Assert.True(service.Settings.AutoOpenMountedDrive);
    }

    [Fact]
    public void SaveDoesNotThrowWithDefaultSettings()
    {
        var appName = "TestApp_" + Guid.NewGuid().ToString("N")[..8];
        var service = new SettingsService(appName);

        var exception = Record.Exception(() => service.Save());
        Assert.Null(exception);
    }

    [Fact]
    public void SaveMultipleTimesRapidlyDoesNotThrow()
    {
        var appName = "TestApp_" + Guid.NewGuid().ToString("N")[..8];
        var service = new SettingsService(appName);

        for (var i = 0; i < 10; i++) service.Save();
    }

    [Fact]
    public void SettingsCanBeModifiedAndSaved()
    {
        var appName = "TestApp_" + Guid.NewGuid().ToString("N")[..8];
        var service = new SettingsService(appName);

        service.Settings.AutoOpenMountedDrive = false;
        service.Save();

        Assert.False(service.Settings.AutoOpenMountedDrive);
    }

    [Fact]
    public void SaveAndLoadRoundTrip()
    {
        var appName = "TestApp_" + Guid.NewGuid().ToString("N")[..8];
        var service1 = new SettingsService(appName);
        service1.Settings.AutoOpenMountedDrive = false;
        service1.Save();

        var service2 = new SettingsService(appName);
        Assert.False(service2.Settings.AutoOpenMountedDrive);
    }

    [Fact]
    public void LoadReturnsDefaultsWhenFileIsCorrupted()
    {
        var appName = "TestApp_" + Guid.NewGuid().ToString("N")[..8];
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName);
        var settingsFile = Path.Combine(settingsDir, "settings.dat");

        try
        {
            Directory.CreateDirectory(settingsDir);
            File.WriteAllText(settingsFile, "this is not valid encrypted data");

            var service = new SettingsService(appName);
            Assert.NotNull(service.Settings);
            Assert.True(service.Settings.AutoOpenMountedDrive);
        }
        finally
        {
            if (Directory.Exists(settingsDir))
                Directory.Delete(settingsDir, true);
        }
    }

    [Fact]
    public void CorruptedSettingsFileIsDeletedSoFailureDoesNotRecur()
    {
        var appName = "TestApp_" + Guid.NewGuid().ToString("N")[..8];
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName);
        var settingsFile = Path.Combine(settingsDir, "settings.dat");

        try
        {
            Directory.CreateDirectory(settingsDir);
            File.WriteAllText(settingsFile, "this is not valid encrypted data");

            var service = new SettingsService(appName);
            Assert.NotNull(service.Settings);
            Assert.False(File.Exists(settingsFile),
                "The corrupt settings file should be deleted after resetting to defaults.");
        }
        finally
        {
            if (Directory.Exists(settingsDir))
                Directory.Delete(settingsDir, true);
        }
    }

    [Fact]
    public void LoadReturnsDefaultsWhenFileIsEmpty()
    {
        var appName = "TestApp_" + Guid.NewGuid().ToString("N")[..8];
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName);
        var settingsFile = Path.Combine(settingsDir, "settings.dat");

        try
        {
            Directory.CreateDirectory(settingsDir);
            File.WriteAllText(settingsFile, "");

            var service = new SettingsService(appName);
            Assert.NotNull(service.Settings);
        }
        finally
        {
            if (Directory.Exists(settingsDir))
                Directory.Delete(settingsDir, true);
        }
    }

    [Fact]
    public void LoadReturnsDefaultsWhenFileDoesNotExist()
    {
        var appName = "TestApp_NonExistent_" + Guid.NewGuid().ToString("N")[..8];

        var service = new SettingsService(appName);
        Assert.NotNull(service.Settings);
        Assert.True(service.Settings.AutoOpenMountedDrive);
    }

    [Fact]
    public void MultipleServiceInstancesWithSameAppShareSettings()
    {
        var appName = "TestApp_" + Guid.NewGuid().ToString("N")[..8];

        var service1 = new SettingsService(appName);
        service1.Settings.AutoOpenMountedDrive = false;
        service1.Save();

        var service2 = new SettingsService(appName);
        Assert.False(service2.Settings.AutoOpenMountedDrive);
    }

    [Fact]
    public void DifferentAppNamesHaveIsolatedSettings()
    {
        var appName1 = "TestApp_A_" + Guid.NewGuid().ToString("N")[..8];
        var appName2 = "TestApp_B_" + Guid.NewGuid().ToString("N")[..8];

        var service1 = new SettingsService(appName1);
        service1.Settings.AutoOpenMountedDrive = false;
        service1.Save();

        var service2 = new SettingsService(appName2);
        // Default for a new app should be true
        Assert.True(service2.Settings.AutoOpenMountedDrive);
    }
}