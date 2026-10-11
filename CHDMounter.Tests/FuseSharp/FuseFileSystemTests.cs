using System.Reflection;
using CHDMounter.FuseSharp;

namespace CHDMounter.Tests.FuseSharp;

public class FuseFileSystemTests
{
    private static string InvokeSanitizeVolumeLabel(string label)
    {
        var method = typeof(FuseFileSystem).GetMethod("SanitizeVolumeLabel",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, [label])!;
    }

    [Fact]
    public void BuildMountArgumentsStartsWithApplicationName()
    {
        var args = FuseFileSystem.BuildMountArguments(false, false, "MyVolume");

        Assert.Equal("CHDMounter", args[0]);
    }

    [Fact]
    public void BuildMountArgumentsIncludesReadOnlyOption()
    {
        var args = FuseFileSystem.BuildMountArguments(false, false, "MyVolume");

        var optionIndex = Array.IndexOf(args, "-o");
        Assert.True(optionIndex >= 0);
        Assert.Equal("ro", args[optionIndex + 1]);
    }

    [Fact]
    public void BuildMountArgumentsDebugAddsDebugFlag()
    {
        var args = FuseFileSystem.BuildMountArguments(false, true, "MyVolume");

        Assert.Contains(args, static a => string.Equals(a, "-d", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildMountArgumentsWithoutDebugHasNoDebugFlag()
    {
        var args = FuseFileSystem.BuildMountArguments(false, false, "MyVolume");

        Assert.DoesNotContain(args, static a => string.Equals(a, "-d", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildMountArgumentsUsesFsnameOnLinux()
    {
        var args = FuseFileSystem.BuildMountArguments(false, false, "MyVolume");

        Assert.Contains(args, static a => string.Equals(a, "fsname=MyVolume", StringComparison.Ordinal));
        Assert.DoesNotContain(args, static a => a.StartsWith("volname=", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildMountArgumentsUsesVolnameOnMac()
    {
        var args = FuseFileSystem.BuildMountArguments(true, false, "MyVolume");

        Assert.Contains(args, static a => string.Equals(a, "volname=MyVolume", StringComparison.Ordinal));
        Assert.DoesNotContain(args, static a => a.StartsWith("fsname=", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildMountArgumentsSanitizesSeparators()
    {
        var args = FuseFileSystem.BuildMountArguments(false, false, "bad,label/with,chars");

        Assert.Contains(args, static a => string.Equals(a, "fsname=badlabelwithchars", StringComparison.Ordinal));
    }

    [Fact]
    public void SanitizeVolumeLabelFallsBackToApplicationNameForWhitespace()
    {
        var result = InvokeSanitizeVolumeLabel("   ");

        Assert.Equal("CHDMounter", result);
    }

    [Fact]
    public void SanitizeVolumeLabelFallsBackToApplicationNameForEmpty()
    {
        var result = InvokeSanitizeVolumeLabel("");

        Assert.Equal("CHDMounter", result);
    }

    [Fact]
    public void SanitizeVolumeLabelTruncatesToMaximumLength()
    {
        var result = InvokeSanitizeVolumeLabel(new string('a', 64));

        Assert.Equal(32, result.Length);
    }

    [Fact]
    public void SanitizeVolumeLabelKeepsShortLabels()
    {
        var result = InvokeSanitizeVolumeLabel("MYDISC");

        Assert.Equal("MYDISC", result);
    }

    [Fact]
    public void SanitizeVolumeLabelTrimsWhitespace()
    {
        var result = InvokeSanitizeVolumeLabel("  MYDISC  ");

        Assert.Equal("MYDISC", result);
    }
}
