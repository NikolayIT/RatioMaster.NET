namespace RatioMaster.Core.Bencode
{
    /// <summary>Thrown when data is not valid bencode.</summary>
    public sealed class BencodeException : Exception
    {
        public BencodeException()
        {
        }

        public BencodeException(string message)
            : base(message)
        {
        }

        public BencodeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
