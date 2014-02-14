namespace RatioMaster_source
{
    using System;
    using System.Diagnostics;
    using System.IO;

    internal class ProcessMemoryReader
    {
        internal static void saveArrayToFile(byte[] arr, string filename)
        {
            FileStream stream1 = File.OpenWrite(filename);
            stream1.Write(arr, 0, arr.Length);
            stream1.Close();
        }
        internal ProcessMemoryReader()
        {
            this.m_hProcess = IntPtr.Zero;
        }
        internal void CloseHandle()
        {
            if (ProcessMemoryReaderApi.CloseHandle(this.m_hProcess) == 0)
            {
                throw new Exception("CloseHandle failed");
            }
        }
        internal void OpenProcess()
        {
            this.m_hProcess = ProcessMemoryReaderApi.OpenProcess(0x10, 1, (uint)this.m_ReadProcess.Id);
        }
        internal byte[] ReadProcessMemory(IntPtr MemoryAddress, uint bytesToRead, out int bytesReaded)
        {
            IntPtr ptr1;
            byte[] buffer1 = new byte[bytesToRead];
            ProcessMemoryReaderApi.ReadProcessMemory(this.m_hProcess, MemoryAddress, buffer1, bytesToRead, out ptr1);
            bytesReaded = ptr1.ToInt32();
            return buffer1;
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
        private IntPtr m_hProcess;
        private Process m_ReadProcess;
    }
}