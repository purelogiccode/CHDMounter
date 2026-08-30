using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace CHDMounter_WinFsp.Services;

/// <summary>
///     Locates the WinFsp installation and ensures the native WinFsp DLL
///     (winfsp-x64.dll / winfsp-x86.dll) is loadable by the current process.
/// </summary>
internal static class WinFspEnvironment
{
    /// <summary>
    ///     Prepends the WinFsp bin directory to the process PATH when the native
    ///     DLL is not already resolvable, then verifies the DLL can be loaded.
    ///     The PATH is only treated as sufficient when one of its entries actually
    ///     contains the native DLL; a substring match (e.g. a folder whose name
    ///     merely contains "WinFsp") is not enough.
    /// </summary>
    /// <param name="reason">When the DLL cannot be made loadable, describes why.</param>
    /// <returns><c>true</c> if the native WinFsp DLL is loadable; otherwise <c>false</c>.</returns>
    internal static bool EnsureWinFspLoadable(out string? reason)
    {
        reason = null;

        try
        {
            var dllName = Environment.Is64BitProcess ? "winfsp-x64.dll" : "winfsp-x86.dll";
            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";

            if (TryProbeDll(dllName))
            {
                if (IsDllInPath(currentPath, dllName))
                    return true;

                // Not on PATH but loadable — the loader may have found it via the
                // application directory or system folders. Prepend the real bin dir
                // anyway so loadability does not depend on app-layout accidents.
                var binDir = FindWinFspBinDir();
                if (binDir is not null)
                {
                    var verified = Path.Combine(binDir, dllName);
                    if (File.Exists(verified)) PrependToPath(binDir, currentPath);
                }

                return true;
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
                return true;

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
        catch
        {
            return false;
        }
    }

    private static bool IsDllInPath(string path, string dllName)
    {
        foreach (var entry in path.Split(';', StringSplitOptions.RemoveEmptyEntries))
            if (File.Exists(Path.Combine(entry.Trim('"'), dllName)))
                return true;

        return false;
    }

    private static void PrependToPath(string binDir, string currentPath)
    {
        var separator = Path.DirectorySeparatorChar;
        if (currentPath.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Any(entry => string.Equals(entry.Trim('"').TrimEnd(separator),
                binDir.TrimEnd(separator), StringComparison.OrdinalIgnoreCase)))
            return;

        Environment.SetEnvironmentVariable("PATH", binDir + ";" + currentPath, EnvironmentVariableTarget.Process);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LoadLibraryEx(string lpszFile, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32.dll")]
    private static extern bool FreeLibrary(IntPtr hLibModule);
}