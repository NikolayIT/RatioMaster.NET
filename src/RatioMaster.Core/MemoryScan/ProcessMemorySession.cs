namespace RatioMaster.Core.MemoryScan
{
    /// <summary>An open handle to another process's memory.</summary>
    public abstract class ProcessMemorySession : IDisposable
    {
        /// <summary>Reads up to <paramref name="buffer"/>.Length bytes at <paramref name="address"/>. Returns 0 when unreadable.</summary>
        public abstract int Read(long address, byte[] buffer);

        public virtual void Dispose() => GC.SuppressFinalize(this);
    }
}
