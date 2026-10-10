# Dokan vs WinFsp

CHDMounter ships two builds that share the same core functionality and UI but use different
kernel/user-mode driver stacks.

| Aspect | Dokan | WinFsp |
|--------|-------|--------|
| Driver | Kernel-mode (`Dokan.sys`) | User-mode (WinFsp) |
| Bindings | `DokanNet` 2.3.0.3 | `winfsp.net` 2.1.25156 |
| File system base | `IDokanOperations` | `FileSystemBase` |
| Elevation | Standard mounts | Cross-integrity folder mounts with permissive DACL when elevated |
| Vendor | <https://github.com/dokan-dev/dokany/releases> | <https://github.com/winfsp/winfsp/releases> |

## Which one should I use?

- Both variants mount CHD images as read-only virtual drives and support all 37 console/format
  modes.
- Pick **Dokan** if you already use Dokany for other tools.
- Pick **WinFsp** if you prefer a user-mode driver, need folder mounts while running elevated, or
  already use WinFsp.

You only need the driver that matches the bundle you downloaded.

## Important

Install the **latest** driver version. CHDMounter verifies that the installed driver exposes the
functions its bindings require; an outdated version will be reported as incompatible and the
mount will be blocked with a dialog linking to the driver download page.
