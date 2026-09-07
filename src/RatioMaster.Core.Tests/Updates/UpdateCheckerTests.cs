namespace RatioMaster.Core.Tests.Updates
{
    using System.Net.Http;

    using RatioMaster.Core.Tests.Fakes;
    using RatioMaster.Core.Updates;

    public class UpdateCheckerTests
    {
        /// <summary>A build newer than this one, derived so the test does not need editing at every release.</summary>
        private static readonly string NewerVersion =
            (int.Parse(AppVersion.CheckId, System.Globalization.CultureInfo.InvariantCulture) + 100)
                .ToString("D4", System.Globalization.CultureInfo.InvariantCulture);

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        [Fact]
        public async Task ReportsAnUpdateWhenTheRemoteVersionIsHigher()
        {
            await using var server = FakeTracker.Start(FakeTracker.TextResponse(NewerVersion));
            var checker = new UpdateChecker(Client(), server.BaseUrl);

            var result = await checker.CheckAsync(Ct);

            Assert.True(result.Succeeded);
            Assert.True(result.UpdateAvailable);
            Assert.Equal(NewerVersion, result.RemoteVersion);
            Assert.Contains($"GET /vc.php?v={AppVersion.CheckId} ", server.Requests.First(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task ReportsNoUpdateForTheSameOrOlderVersion()
        {
            await using var same = FakeTracker.Start(FakeTracker.TextResponse(AppVersion.CheckId));
            var result = await new UpdateChecker(Client(), same.BaseUrl).CheckAsync(Ct);
            Assert.True(result.Succeeded);
            Assert.False(result.UpdateAvailable);

            await using var older = FakeTracker.Start(FakeTracker.TextResponse("0430"));
            var olderResult = await new UpdateChecker(Client(), older.BaseUrl).CheckAsync(Ct);
            Assert.True(olderResult.Succeeded);
            Assert.False(olderResult.UpdateAvailable);
        }

        [Fact]
        public async Task RejectsRepliesThatAreNotFourCharacters()
        {
            await using var server = FakeTracker.Start(FakeTracker.TextResponse("<html>oops</html>"));
            var result = await new UpdateChecker(Client(), server.BaseUrl).CheckAsync(Ct);

            Assert.False(result.Succeeded);
            Assert.Null(result.RemoteVersion);
            Assert.False(result.UpdateAvailable);
        }

        [Fact]
        public async Task ReturnsAnErrorInsteadOfThrowingWhenTheServerIsUnreachable()
        {
            var checker = new UpdateChecker(Client(), "http://127.0.0.1:1");
            var result = await checker.CheckAsync(Ct);

            Assert.False(result.Succeeded);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task UserAgentCarriesTheVersionAndNoUserName()
        {
            await using var server = FakeTracker.Start(FakeTracker.TextResponse(AppVersion.CheckId));
            await new UpdateChecker(Client(), server.BaseUrl).CheckAsync(Ct);

            var request = server.Requests.First();
            Assert.Contains($"User-Agent: RatioMaster.NET/{AppVersion.CheckId} (", request, StringComparison.Ordinal);
            Assert.DoesNotContain(Environment.UserName, request, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(Environment.UserName, UpdateChecker.UserAgent, StringComparison.OrdinalIgnoreCase);
        }

        private static HttpClient Client() => new() { Timeout = TimeSpan.FromSeconds(10) };
    }
}
