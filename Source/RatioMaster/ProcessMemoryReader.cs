namespace RatioMaster_source
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;

    internal class ProcessMemoryReader
    {
        [DllImport("kernel32.dll")]
        internal static extern int CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        internal static extern int ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] buffer, uint size, out IntPtr lpNumberOfBytesRead);

        internal const uint PROCESS_VM_READ = 0x10;

        private IntPtr m_hProcess;

        private Process m_ReadProcess;

        internal ProcessMemoryReader()
        {
            this.m_hProcess = IntPtr.Zero;
        }

        internal Process ReadProcess
        {
            get
            {
                return this.m_ReadProcess;
            }

            set
            {
                this.m_ReadProcess = value;
            }
        }

        internal static void SaveArrayToFile(byte[] arr, string filename)
        {
            FileStream stream1 = File.OpenWrite(filename);
            stream1.Write(arr, 0, arr.Length);
            stream1.Close();
        }

        internal void CloseHandle()
        {
            if (CloseHandle(this.m_hProcess) == 0)
            {
                throw new Exception("CloseHandle failed");
            }
        }

        internal void OpenProcess()
        {
            this.m_hProcess = OpenProcess(0x10, 1, (uint)this.m_ReadProcess.Id);
        }

        internal byte[] ReadProcessMemory(IntPtr memoryAddress, uint bytesToRead, out int bytesRead)
        {
            IntPtr pointer;
            var buffer = new byte[bytesToRead];
            ReadProcessMemory(this.m_hProcess, memoryAddress, buffer, bytesToRead, out pointer);
            bytesRead = pointer.ToInt32();
            return buffer;
        }
    }
}