namespace RatioMaster.Core.Tracker
{
    using System.Text;

    using RatioMaster.Core.Bencode;

    /// <summary>The interpreted content of a tracker scrape response for one torrent.</summary>
    public sealed class ScrapeResponse
    {
        private ScrapeResponse(string? failureReason, int? complete, int? downloaded, int? incomplete)
        {
            this.FailureReason = failureReason;
            this.Complete = complete;
            this.Downloaded = downloaded;
            this.Incomplete = incomplete;
        }

        public string? FailureReason { get; }

        /// <summary>Gets the seeders.</summary>
        public int? Complete { get; }

        public int? Downloaded { get; }

        /// <summary>Gets the leechers.</summary>
        public int? Incomplete { get; }

        public bool HasFailure => !string.IsNullOrEmpty(this.FailureReason);

        public static ScrapeResponse Parse(BencodeDictionary dictionary, ReadOnlySpan<byte> infoHash)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            var failure = dictionary.GetDisplayText("failure reason");
            if (failure is not null)
            {
                return new ScrapeResponse(failure, null, null, null);
            }

            var files = dictionary.GetDictionary("files");
            var key = Encoding.Latin1.GetString(infoHash);
            if (files is null || !files.TryGetValue(key, out var entry) || entry is not BencodeDictionary stats)
            {
                return new ScrapeResponse(null, null, null, null);
            }

            return new ScrapeResponse(
                null,
                (int?)stats.GetInteger("complete"),
                (int?)stats.GetInteger("downloaded"),
                (int?)stats.GetInteger("incomplete"));
        }
    }
}
