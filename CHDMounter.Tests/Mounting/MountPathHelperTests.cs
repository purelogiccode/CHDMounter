using CHDMounter.Mounting;

namespace CHDMounter.Tests.Mounting;

public class MountPathHelperTests
{
    // --- IsDriveLetterMountPoint ---

    [Theory]
    [InlineData("M:", true)]
    [InlineData("m:", true)]
    [InlineData("M:\\", true)]
    [InlineData("M:\\folder", false)]
    [InlineData("folder", false)]
    [InlineData("folder\\M:", false)]
    [InlineData("", false)]
    [InlineData("C", false)]
    [InlineData("1:", false)]
    public void IsDriveLetterMountPointClassifies(string mountPoint, bool expected)
    {
        Assert.Equal(expected, MountPathHelper.IsDriveLetterMountPoint(mountPoint));
    }

    // --- BuildFolderMountCandidates ---

    [Fact]
    public void BuildFolderMountCandidatesReturnsFiveCandidates()
    {
        var candidates = MountPathHelper.BuildFolderMountCandidates(@"C:\games\My Game.chd");

        Assert.Equal(5, candidates.Count);
    }

    [Fact]
    public void BuildFolderMountCandidatesAddsSuffixedFallbacks()
    {
        var candidates = MountPathHelper.BuildFolderMountCandidates(@"C:\games\Game.chd");

        Assert.EndsWith("Game", candidates[0], StringComparison.Ordinal);
        Assert.EndsWith("Game (2)", candidates[1], StringComparison.Ordinal);
        Assert.EndsWith("Game (5)", candidates[4], StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFolderMountCandidatesAreInsideMountBaseDirectory()
    {
        var candidates = MountPathHelper.BuildFolderMountCandidates(@"C:\games\Game.chd");

        Assert.All(candidates,
            candidate => Assert.StartsWith(MountPathHelper.MountBaseDirectory, candidate, StringComparison.Ordinal));
    }

    [Fact]
    public void BuildFolderMountCandidatesSanitizesInvalidCharacters()
    {
        var candidates = MountPathHelper.BuildFolderMountCandidates(@"C:\games\a:b*c?.chd");
        var invalid = Path.GetInvalidFileNameChars();

        Assert.All(candidates, candidate =>
        {
            var name = Path.GetFileName(candidate);
            Assert.DoesNotContain(name, c => invalid.Contains(c));
        });
    }

    [Fact]
    public void BuildFolderMountCandidatesFallsBackForEmptyName()
    {
        var candidates = MountPathHelper.BuildFolderMountCandidates(@"C:\games\.chd");

        Assert.Equal("CHD", Path.GetFileName(candidates[0]));
        Assert.Equal("CHD (2)", Path.GetFileName(candidates[1]));
    }

    // --- GetVolumeCreationTime ---

    [Fact]
    public void GetVolumeCreationTimeReturnsFileWriteTime()
    {
        var path = Path.GetTempFileName();
        try
        {
            var expected = File.GetLastWriteTimeUtc(path);

            var result = MountPathHelper.GetVolumeCreationTime(path);

            Assert.Equal(expected, result);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetVolumeCreationTimeFallsBackForInvalidPath()
    {
        var before = DateTime.UtcNow;

        // A NUL character always makes the path invalid, so the fallback is exercised
        // deterministically regardless of which drives exist on the test machine.
        var result = MountPathHelper.GetVolumeCreationTime("\0missing.chd");

        Assert.InRange(result, before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    }
}
