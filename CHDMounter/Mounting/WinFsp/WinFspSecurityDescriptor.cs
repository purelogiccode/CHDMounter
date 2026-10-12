using System.Runtime.Versioning;
using System.Security.AccessControl;

namespace CHDMounter.Mounting.WinFsp;

/// <summary>
///     Builds the security descriptor used by cross-integrity folder mounts so that
///     standard (non-elevated) processes can read the mounted drive.
/// </summary>
/// <remarks>
///     The descriptor must include an owner: Windows' <c>AccessCheck</c> rejects an
///     ownerless descriptor with <c>ERROR_INVALID_SECURITY_DESCR</c>, which WinFsp
///     surfaces as "The security descriptor structure is invalid" when the mounted
///     drive is opened. The DACL grants Full Access to SYSTEM, Administrators and
///     Everyone, matching WinFsp's own default root descriptor.
/// </remarks>
[SupportedOSPlatform("windows")]
internal static class WinFspSecurityDescriptor
{
    /// <summary>
    ///     The SDDL used for cross-integrity mounts: owner/group BUILTIN\Administrators
    ///     and Full Access for SYSTEM, Administrators and Everyone.
    /// </summary>
    internal const string Sddl = "O:BAG:BAD:P(A;;FA;;;SY)(A;;FA;;;BA)(A;;FA;;;WD)";

    private static byte[]? _cached;

    /// <summary>
    ///     Creates the self-relative binary security descriptor.
    /// </summary>
    /// <returns>The security descriptor bytes.</returns>
    internal static byte[] Create()
    {
        if (_cached is not null)
            return _cached;

        var descriptor = new RawSecurityDescriptor(Sddl);
        var bytes = new byte[descriptor.BinaryLength];
        descriptor.GetBinaryForm(bytes, 0);
        _cached = bytes;
        return bytes;
    }
}
