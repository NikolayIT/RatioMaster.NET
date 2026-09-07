namespace RatioMaster.Core.MemoryScan
{
    using System.Diagnostics;
    using System.Runtime.Versioning;

    /// <summary>
    /// Best-effort reader using /proc/&lt;pid&gt;/mem. Needs the same user (or ptrace permission), so it can
    /// legitimately fail; callers fall back to generated values.
    /// </summary>
    [SupportedOSPlatform("linux")]
    public sealed class LinuxProcessMemoryScanner : IProcessMemoryScanner
    {
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
                try
                {
                    var stream = new FileStream($"/proc/{process.Id}/mem", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    return new Session(stream);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
                {
                    return null;
                }
            }
        }

        private sealed class Session(FileStream stream) : ProcessMemorySession
        {
            public override int Read(long address, byte[] buffer)
            {
                try
                {
                    stream.Seek(address, SeekOrigin.Begin);
                    return stream.Read(buffer, 0, buffer.Length);
                }
                catch (Exception ex) when (ex is IOException or ArgumentOutOfRangeException or NotSupportedException)
                {
                    // Unmapped region: skip it.
                    return 0;
                }
            }

            public override void Dispose()
            {
                stream.Dispose();
                base.Dispose();
            }
        }
    }
}
