namespace RatioMaster.Core.Tests.Tracker
{
    using RatioMaster.Core.Tracker;

    public class TrackerUrlTests
    {
        [Theory]
        [InlineData("http://tracker.example/announce")]
        [InlineData("https://tracker.example:2710/abc123/announce")]
        [InlineData("HTTP://TRACKER.EXAMPLE/announce.php?passkey=x")]
        [InlineData("  http://tracker.example/announce  ")]
        [InlineData("http://[2001:db8::1]:6969/announce")]
        public void AcceptsHttpAndHttpsTrackers(string url)
        {
            Assert.Null(TrackerUrl.Validate(url));
            Assert.True(TrackerUrl.IsValid(url));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void RejectsAMissingTracker(string? url)
        {
            var problem = TrackerUrl.Validate(url);
            Assert.NotNull(problem);
            Assert.Contains("no tracker", problem, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("udp://tracker.opentrackr.org:1337/announce", "udp://")]
        [InlineData("wss://tracker.example/announce", "wss://")]
        [InlineData("magnet:?xt=urn:btih:abc", "magnet://")]
        public void RejectsTrackersThatAreNotHttp(string url, string expectedScheme)
        {
            var problem = TrackerUrl.Validate(url);
            Assert.NotNull(problem);
            Assert.Contains("Only http and https", problem, StringComparison.Ordinal);
            Assert.Contains(expectedScheme, problem, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("tracker.example/announce")]
        [InlineData("http://")]
        [InlineData("http://exa mple/announce")]
        [InlineData("just some words")]
        public void RejectsMalformedUrls(string url)
        {
            var problem = TrackerUrl.Validate(url);
            Assert.NotNull(problem);
            Assert.Contains("not a valid tracker URL", problem, StringComparison.Ordinal);
        }
    }
}
