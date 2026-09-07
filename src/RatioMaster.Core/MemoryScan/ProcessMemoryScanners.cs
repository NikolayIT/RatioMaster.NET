namespace RatioMaster.Core.MemoryScan;

/// <summary>Picks the scanner for the current operating system.</summary>
public static class ProcessMemoryScanners
{
    /// <summary>Windows uses kernel32, Linux uses /proc, macOS is unsupported (needs task_for_pid entitlements).</summary>
    public static IProcessMemoryScanner ForCurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsProcessMemoryScanner();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxProcessMemoryScanner();
        }

        return NullProcessMemoryScanner.Instance;
    }
}
