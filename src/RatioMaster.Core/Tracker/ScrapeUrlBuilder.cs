namespace RatioMaster.Core.Tracker
{
    /// <summary>Derives a scrape URL from an announce URL, reproducing RM.getScrapeUrlString.</summary>
    public static class ScrapeUrlBuilder
    {
        /// <summary>Returns the scrape URL, or null when the tracker's path does not end in "announce".</summary>
        public static string? TryBuild(string trackerUrl, string infoHashEncoded)
        {
            ArgumentNullException.ThrowIfNull(trackerUrl);
            ArgumentNullException.ThrowIfNull(infoHashEncoded);

            var slash = trackerUrl.LastIndexOf('/');
            if (slash < 0 || slash + 1 + 8 > trackerUrl.Length)
            {
                return null;
            }

            if (!trackerUrl.Substring(slash + 1, 8).Equals("announce", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var scrapeUrl = trackerUrl[..(slash + 1)] + "scrape" + trackerUrl[(slash + 9)..];
            scrapeUrl += scrapeUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            return scrapeUrl + "info_hash=" + infoHashEncoded;
        }
    }
}
