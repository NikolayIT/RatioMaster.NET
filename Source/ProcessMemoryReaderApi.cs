namespace RatioMaster_source
{
    using System;
    using System.Runtime.InteropServices;

    internal class ProcessMemoryReaderApi
    {
        [DllImport("kernel32.dll")]
        internal static extern int CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, uint dwProcessId);
        [DllImport("kernel32.dll")]
        internal static extern int ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] buffer, uint size, out IntPtr lpNumberOfBytesRead);

        internal const uint PROCESS_VM_READ = 0x10;
    }
}