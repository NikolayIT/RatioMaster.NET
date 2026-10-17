namespace RatioMaster.Core.Tests.Fakes
{
    using RatioMaster.Core.MemoryScan;

    /// <summary>A "running client" whose memory is the given bytes, or one that is not running.</summary>
    internal sealed class FakeProcessMemoryScanner(byte[] memory, bool canOpen = true) : IProcessMemoryScanner
    {
        public bool IsSupported => true;

        public ProcessMemorySession? Open(string processName) => canOpen ? new Session(memory) : null;

        private sealed class Session(byte[] memory) : ProcessMemorySession
        {
            public override int Read(long address, byte[] buffer)
            {
                if (address >= memory.Length)
                {
                    return 0;
                }

                var count = (int)Math.Min(buffer.Length, memory.Length - address);
                Array.Copy(memory, address, buffer, 0, count);
                return count;
            }
        }
    }
}
