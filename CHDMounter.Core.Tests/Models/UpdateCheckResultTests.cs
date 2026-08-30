namespace CHDMounter.Core.Tests.Models;

public class UpdateCheckResultTests
{
    [Fact]
    public void DefaultHasUpdateIsFalse()
    {
        var result = new UpdateCheckResult();
        Assert.False(result.HasUpdate);
    }

    [Fact]
    public void DefaultCurrentVersionIsEmpty()
    {
        var result = new UpdateCheckResult();
        Assert.Equal(string.Empty, result.CurrentVersion);
    }

    [Fact]
    public void DefaultLatestVersionIsEmpty()
    {
        var result = new UpdateCheckResult();
        Assert.Equal(string.Empty, result.LatestVersion);
    }

    [Fact]
    public void DefaultReleaseUrlIsEmpty()
    {
        var result = new UpdateCheckResult();
        Assert.Equal(string.Empty, result.ReleaseUrl);
    }

    [Fact]
    public void DefaultDownloadUrlIsEmpty()
    {
        var result = new UpdateCheckResult();
        Assert.Equal(string.Empty, result.DownloadUrl);
    }

    [Fact]
    public void CanSetAllProperties()
    {
        var result = new UpdateCheckResult
        {
            HasUpdate = true,
            CurrentVersion = "1.0.0",
            LatestVersion = "2.0.0",
            ReleaseUrl = "https://example.com/release",
            DownloadUrl = "https://example.com/download"
        };

        Assert.True(result.HasUpdate);
        Assert.Equal("1.0.0", result.CurrentVersion);
        Assert.Equal("2.0.0", result.LatestVersion);
        Assert.Equal("https://example.com/release", result.ReleaseUrl);
        Assert.Equal("https://example.com/download", result.DownloadUrl);
    }
}