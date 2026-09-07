namespace RatioMaster.Core.Tracker
{
    /// <summary>Thrown when a tracker request cannot be completed.</summary>
    public sealed class TrackerException : Exception
    {
        public TrackerException(string message)
            : base(message)
        {
        }

        public TrackerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
