namespace RatioMaster.Core.MemoryScan;

/// <summary>Opens another process's memory so a running client's announce values can be copied.</summary>
public interface IProcessMemoryScanner
{
    /// <summary>False on platforms where reading another process's memory is not available.</summary>
    bool IsSupported { get; }

    /// <summary>Opens the first process with this name, or null when it is not running or cannot be opened.</summary>
    ProcessMemorySession? Open(string processName);
}
