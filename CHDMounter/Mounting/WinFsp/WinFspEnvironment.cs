using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Fsp;
using Microsoft.Win32;
using Serilog;

namespace CHDMounter.Mounting.WinFsp;

/// <summary>
///     Locates the WinFsp installation and ensures the WinFsp .NET bindings
///     (Fsp.Interop.Api) can initialize in the current process: the native DLL
///     (winfsp-a64.dll / winfsp-x64.dll / winfsp-x86.dll) must be loadable,
///     every entry point the bindings require must exist, and the native/managed
///     versions must match.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class WinFspEnvironment
{
    /// <summary>
    ///     Prepends the WinFsp bin directory to the process PATH when the native
    ///     DLL is not already resolvable, verifies the DLL can be loaded, and then
    ///     forces the WinFsp .NET bindings to initialize exactly like a real mount
    ///     would. The PATH is only treated as sufficient when one of its entries
    ///     actually contains the native DLL; a substring match (e.g. a folder whose
    ///     name merely contains "WinFsp") is not enough.
    /// </summary>
    /// <param name="reason">When WinFsp cannot be made usable, describes why.</param>
    /// <returns><c>true</c> if the WinFsp .NET bindings can initialize; otherwise <c>false</c>.</returns>
    internal static bool EnsureWinFspLoadable(out string? reason)
    {
        reason = null;

        try
        {
            var dllName = GetNativeDllName();
            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";

            if (TryProbeDll(dllName))
            {
                if (!IsDllInPath(currentPath, dllName))
                {
                    // Not on PATH but loadable — the loader may have found it via the
                    // application directory or system folders. Prepend the real bin dir
                    // anyway so loadability does not depend on app-layout accidents.
                    var binDir = FindWinFspBinDir();
                    if (binDir is not null)
                    {
                        var verified = Path.Combine(binDir, dllName);
                        if (File.Exists(verified)) PrependToPath(binDir, currentPath);
                    }
                }

                return EnsureBindingsLoadable(out reason);
            }

            var candidateDir = FindWinFspBinDir();
            if (candidateDir is null)
            {
                reason =
                    "WinFsp is not registered on this system (registry key SOFTWARE\\WinFsp not found). Install WinFsp to mount CHD files.";
                return false;
            }

            if (!Directory.Exists(candidateDir))
            {
                reason = $"WinFsp bin directory does not exist: {candidateDir}";
                return false;
            }

            var dllPath = Path.Combine(candidateDir, dllName);
            if (!File.Exists(dllPath))
            {
                reason = $"WinFsp native DLL not found: {dllPath}. Reinstall WinFsp to mount CHD files.";
                return false;
            }

            PrependToPath(candidateDir, currentPath);
            if (TryProbeDll(dllName))
                return EnsureBindingsLoadable(out reason);

            reason = $"WinFsp native DLL could not be loaded: {dllPath}. Reinstall WinFsp to mount CHD files.";
            return false;
        }
        catch (Exception ex)
        {
            reason = $"Failed to initialize WinFsp: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    ///     Forces the WinFsp .NET bindings to initialize the same way a real mount
    ///     would: Fsp.Interop.Api loads the native DLL, resolves every entry point
    ///     the bindings need and verifies the native/managed version match.
    ///     A native DLL can be loadable and still fail here (for example an older
    ///     WinFsp release that lacks newer exports), which would otherwise surface
    ///     during the mount attempt as "The type initializer for 'Fsp.Interop.Api'
    ///     threw an exception" (bugs 67653-67656, 67777/67778 and earlier reports).
    /// </summary>
    /// <param name="reason">When the bindings cannot be initialized, describes why.</param>
    /// <returns><c>true</c> if the WinFsp .NET bindings initialize; otherwise <c>false</c>.</returns>
    private static bool EnsureBindingsLoadable(out string? reason)
    {
        try
        {
            // FileSystemHost.Version() is a public entry point that touches
            // Fsp.Interop.Api, triggering the same static initialization the mount
            // path relies on, without creating any kernel-mode file system objects.
            _ = FileSystemHost.Version();
            reason = null;
            return true;
        }
        catch (Exception ex)
        {
            var innermost = ex;
            while (innermost.InnerException is not null)
                innermost = innermost.InnerException;

            reason =
                $"WinFsp is installed but cannot be used by this application ({innermost.GetType().Name}: {innermost.Message}). " +
                "Install or repair WinFsp to mount CHD files.";
            return false;
        }
    }

    /// <summary>
    ///     Gets the WinFsp native DLL name matching the processor architecture the
    ///     WinFsp .NET bindings expect (arm64 uses winfsp-a64.dll).
    /// </summary>
    private static string GetNativeDllName()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => "winfsp-a64.dll",
            Architecture.X64 => "winfsp-x64.dll",
            _ => "winfsp-x86.dll"
        };
    }

    /// <summary>
    ///     Finds the WinFsp bin directory from the registry (SxsDir or InstallDir value).
    /// </summary>
    /// <returns>The bin directory path, or <c>null</c> if WinFsp is not registered.</returns>
    internal static string? FindWinFspBinDir()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\WinFsp")
                        ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WinFsp");
        if (key is null)
            return null;

        var sxsDir = key.GetValue("SxsDir") as string;
        if (!string.IsNullOrEmpty(sxsDir))
        {
            var sxsBin = Path.Combine(sxsDir, "bin");
            if (Directory.Exists(sxsBin))
                return sxsBin;
        }

        var installDir = key.GetValue("InstallDir") as string;
        if (!string.IsNullOrEmpty(installDir))
        {
            var installBin = Path.Combine(installDir, "bin");
            if (Directory.Exists(installBin))
                return installBin;
        }

        return null;
    }

    private static bool TryProbeDll(string dllName)
    {
        try
        {
            var handle = LoadLibraryEx(dllName, IntPtr.Zero, 0);
            if (handle == IntPtr.Zero)
                return false;
            FreeLibrary(handle);
            return true;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "WinFspEnvironment: Failed to probe native library '{DllName}'", dllName);
            return false;
        }
    }

    private static bool IsDllInPath(string path, string dllName)
    {
        foreach (var entry in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            if (File.Exists(Path.Combine(entry.Trim('"'), dllName)))
                return true;

        return false;
    }

    private static void PrependToPath(string binDir, string currentPath)
    {
        var separator = Path.DirectorySeparatorChar;
        if (currentPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(entry => string.Equals(entry.Trim('"').TrimEnd(separator),
                binDir.TrimEnd(separator), StringComparison.OrdinalIgnoreCase)))
            return;

        Environment.SetEnvironmentVariable("PATH", binDir + Path.PathSeparator + currentPath,
            EnvironmentVariableTarget.Process);
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibraryEx(string lpszFile, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32.dll")]
    private static extern bool FreeLibrary(IntPtr hLibModule);
}