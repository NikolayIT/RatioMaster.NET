namespace RatioMaster.Core.MemoryScan
{
    /// <summary>Used where reading another process is not possible (macOS, or when disabled).</summary>
    public sealed class NullProcessMemoryScanner : IProcessMemoryScanner
    {
        public static NullProcessMemoryScanner Instance { get; } = new();

        public bool IsSupported => false;

        public ProcessMemorySession? Open(string processName) => null;
    }
}
