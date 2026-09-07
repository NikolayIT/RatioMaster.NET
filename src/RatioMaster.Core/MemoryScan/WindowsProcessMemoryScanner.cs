using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace RatioMaster.Core.MemoryScan;

/// <summary>Reads another process's memory with kernel32, as the old ProcessMemoryReader did.</summary>
[SupportedOSPlatform("windows")]
public sealed partial class WindowsProcessMemoryScanner : IProcessMemoryScanner
{
    private const uint ProcessVmRead = 0x0010;

    public bool IsSupported => true;

    public ProcessMemorySession? Open(string processName)
    {
        ArgumentException.ThrowIfNullOrEmpty(processName);
        var process = Process.GetProcessesByName(processName).FirstOrDefault();
        if (process is null)
        {
            return null;
        }

        using (process)
        {
            var handle = OpenProcess(ProcessVmRead, 1, (uint)process.Id);
            return handle == IntPtr.Zero ? null : new Session(handle);
        }
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr OpenProcess(uint desiredAccess, int inheritHandle, uint processId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ReadProcessMemory(IntPtr process, IntPtr baseAddress, [Out] byte[] buffer, nuint size, out nuint bytesRead);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr handle);

    private sealed class Session(IntPtr handle) : ProcessMemorySession
    {
        private IntPtr handle = handle;

        public override int Read(long address, byte[] buffer)
        {
            if (this.handle == IntPtr.Zero)
            {
                return 0;
            }

            return ReadProcessMemory(this.handle, (IntPtr)address, buffer, (nuint)buffer.Length, out var read)
                ? (int)read
                : 0;
        }

        public override void Dispose()
        {
            if (this.handle != IntPtr.Zero)
            {
                CloseHandle(this.handle);
                this.handle = IntPtr.Zero;
            }

            base.Dispose();
        }
    }
}
