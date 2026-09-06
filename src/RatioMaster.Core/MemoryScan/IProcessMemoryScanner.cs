namespace RatioMaster.Core.MemoryScan;

/// <summary>An open handle to another process's memory.</summary>
public abstract class ProcessMemorySession : IDisposable
{
    /// <summary>Reads up to <paramref name="buffer"/>.Length bytes at <paramref name="address"/>. Returns 0 when unreadable.</summary>
    public abstract int Read(long address, byte[] buffer);

    public virtual void Dispose() => GC.SuppressFinalize(this);
}

/// <summary>Opens another process's memory so a running client's announce values can be copied.</summary>
public interface IProcessMemoryScanner
{
    /// <summary>False on platforms where reading another process's memory is not available.</summary>
    bool IsSupported { get; }

    /// <summary>Opens the first process with this name, or null when it is not running or cannot be opened.</summary>
    ProcessMemorySession? Open(string processName);
}
