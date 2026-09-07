namespace RatioMaster.Core.Networking
{
    /// <summary>Thrown when a proxy refuses or fails to establish a tunnel.</summary>
    public sealed class ProxyException : Exception
    {
        public ProxyException(string message)
            : base(message)
        {
        }

        public ProxyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
