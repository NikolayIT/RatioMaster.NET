namespace RatioMaster.Core.Tracker
{
    /// <summary>Announce value rounding, matching the old RM.RoundByDenominator (floor to a multiple).</summary>
    public static class AnnounceMath
    {
        /// <summary>Uploaded bytes are reported floored to a 16 KiB (16384) multiple.</summary>
        public const long UploadedDenominator = 0x4000;

        /// <summary>Downloaded bytes are reported floored to a 16-byte multiple.</summary>
        public const long DownloadedDenominator = 0x10;

        public static long RoundDown(long value, long denominator) =>
            value <= 0 ? value : denominator * (value / denominator);
    }
}
