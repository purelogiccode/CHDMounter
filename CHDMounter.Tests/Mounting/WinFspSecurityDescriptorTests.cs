using System.Security.AccessControl;
using CHDMounter.Mounting.WinFsp;

namespace CHDMounter.Tests.Mounting;

public class WinFspSecurityDescriptorTests
{
    [Fact]
    public void CreateIncludesOwnerAndGroup()
    {
        var bytes = WinFspSecurityDescriptor.Create();
        var descriptor = new RawSecurityDescriptor(bytes, 0);

        // Windows' AccessCheck rejects an ownerless descriptor with
        // ERROR_INVALID_SECURITY_DESCR ("The security descriptor structure is invalid"),
        // which made cross-integrity folder mounts unreadable.
        Assert.NotNull(descriptor.Owner);
        Assert.NotNull(descriptor.Group);
    }

    [Fact]
    public void CreateGrantsEveryoneFullAccess()
    {
        var bytes = WinFspSecurityDescriptor.Create();
        var descriptor = new RawSecurityDescriptor(bytes, 0);

        Assert.NotNull(descriptor.DiscretionaryAcl);
        Assert.True(descriptor.DiscretionaryAcl!.Count > 0);

        var everyoneFullAccess = descriptor.DiscretionaryAcl
            .Cast<CommonAce>()
            .Any(ace => ace.AceQualifier == AceQualifier.AccessAllowed
                        && string.Equals(ace.SecurityIdentifier.Value, "S-1-1-0", StringComparison.Ordinal)
                        && (ace.AccessMask & 0x1F01FF) == 0x1F01FF);

        Assert.True(everyoneFullAccess);
    }

    [Fact]
    public void CreateProducesSelfRelativeDescriptor()
    {
        var bytes = WinFspSecurityDescriptor.Create();

        const ushort selfRelative = 0x8000;
        Assert.NotEqual(0, BitConverter.ToUInt16(bytes, 2) & selfRelative);
    }
}
