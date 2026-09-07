namespace RatioMaster.Core.Tracker
{
    using System.Globalization;

    /// <summary>Builds a tracker announce URL from a client query template, reproducing RM.getUrlString.</summary>
    public static class AnnounceUrlBuilder
    {
        public static string Build(string trackerUrl, string queryTemplate, AnnounceValues values, TrackerEvent trackerEvent)
        {
            ArgumentNullException.ThrowIfNull(trackerUrl);
            ArgumentNullException.ThrowIfNull(queryTemplate);
            ArgumentNullException.ThrowIfNull(values);

            var url = trackerUrl + (trackerUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?");

            var query = queryTemplate;
            if (trackerEvent == TrackerEvent.Started)
            {
                query = query.Replace("&natmapped=1&localip={localip}", string.Empty, StringComparison.Ordinal);
            }

            if (trackerEvent != TrackerEvent.Stopped)
            {
                query = query.Replace("&trackerid=48", string.Empty, StringComparison.Ordinal);
            }

            url += query;

            // A client that is leaving has no use for peers, so every real one asks for none on the last
            // announce. Otherwise an unset numwant means "as many as usual", which is 200.
            var numWant = trackerEvent == TrackerEvent.Stopped
                ? "0"
                : values.NumWant == "0" ? "200" : values.NumWant;

            return url
                .Replace("{infohash}", values.InfoHashEncoded, StringComparison.Ordinal)
                .Replace("{peerid}", values.PeerId, StringComparison.Ordinal)
                .Replace("{port}", values.Port, StringComparison.Ordinal)
                .Replace("{uploaded}", values.Uploaded.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
                .Replace("{downloaded}", values.Downloaded.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
                .Replace("{left}", values.Left.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
                .Replace("{event}", trackerEvent.ToQueryValue(), StringComparison.Ordinal)
                .Replace("{numwant}", numWant, StringComparison.Ordinal)
                .Replace("{key}", values.Key, StringComparison.Ordinal)
                .Replace("{localip}", values.LocalIp, StringComparison.Ordinal);
        }
    }
}
