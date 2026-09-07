using System.Net;
using System.Net.Sockets;
using RatioMaster.Core.Abstractions;
using RatioMaster.Core.Clients;
using RatioMaster.Core.Logging;
using RatioMaster.Core.Networking;
using RatioMaster.Core.Sessions;
using RatioMaster.Core.Tests.Fakes;
using RatioMaster.Core.Tracker;

namespace RatioMaster.Core.Tests.Sessions;

public class TorrentSessionTests
{
    private const long KiB = 1024;
    private static readonly ClientProfile Profile = ClientProfileCatalog.Load().GetByName("uTorrent 3.3.2");
    private static readonly byte[] InfoHash = Enumerable.Range(0, 20).Select(i => (byte)(i * 3 + 1)).ToArray();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ClientIdentity Identity(string port = "50000") => new()
    {
        PeerId = "-UT3320-abcdefghijkl",
        Key = "KEY12345",
        Port = port,
        NumWant = "200",
    };

    private static TorrentSettings QuietSettings() => new()
    {
        UploadRateBytes = 60 * KiB,
        DownloadRateBytes = 30 * KiB,
        UploadRandomEnabled = false,
        DownloadRandomEnabled = false,
        UseTcpListener = false,
        RequestScrape = true,
    };

    private static (TorrentSession Session, FakeTrackerClient Tracker) Create(
        TorrentSettings? settings = null,
        long totalLength = 1_000_000,
        IRandomSource? random = null,
        Action<FakeTrackerClient>? configureTracker = null,
        ClientIdentity? identity = null)
    {
        var tracker = new FakeTrackerClient();
        configureTracker?.Invoke(tracker);
        var descriptor = new TorrentDescriptor
        {
            InfoHash = InfoHash,
            TotalLength = totalLength,
            TrackerUrl = "http://tracker.test/announce",
            Name = "test.iso",
        };
        var session = new TorrentSession(
            descriptor,
            Profile,
            identity ?? Identity(),
            settings ?? QuietSettings(),
            tracker,
            new FakeLocalIpProvider(),
            random ?? new ScriptedRandomSource(),
            new FakeClock());
        return (session, tracker);
    }

    private static async Task TickAsync(TorrentSession session, int times)
    {
        for (var i = 0; i < times; i++)
        {
            await session.TickAsync(Ct);
        }
    }

    [Fact]
    public async Task StartSendsStartedAnnounceAndScrape()
    {
        var (session, tracker) = Create();

        await session.StartAsync(Ct);

        var (evt, values) = Assert.Single(tracker.Announces);
        Assert.Equal(TrackerEvent.Started, evt);
        Assert.Equal(0, values.Uploaded);
        Assert.Equal(0, values.Downloaded);
        Assert.Equal(1_000_000, values.Left);
        Assert.Equal("50000", values.Port);
        Assert.Equal("-UT3320-abcdefghijkl", values.PeerId);
        Assert.Equal("KEY12345", values.Key);
        Assert.Equal("200", values.NumWant);
        Assert.Equal("1.2.3.4", values.LocalIp);
        Assert.Equal(InfoHashEncoder.Encode(InfoHash, upperCase: false), values.InfoHashEncoded);
        Assert.Equal(1, tracker.ScrapeCount);
        Assert.Equal(TorrentSessionState.Downloading, session.State);
        Assert.Equal(10, session.Snapshot.Seeders);
        Assert.Equal(5, session.Snapshot.Leechers);
    }

    [Fact]
    public async Task CountersGrowEachTickByTheConfiguredRates()
    {
        var (session, _) = Create();
        await session.StartAsync(Ct);

        await TickAsync(session, 3);

        var stats = session.Snapshot;
        Assert.Equal(3 * 60 * KiB, stats.Uploaded);
        Assert.Equal(3 * 30 * KiB, stats.Downloaded);
        Assert.Equal(1_000_000 - 3 * 30 * KiB, stats.Left);
        Assert.Equal(TimeSpan.FromSeconds(3), stats.TotalRunningTime);
        Assert.Equal(3 * 30 * KiB / 1_000_000.0 * 100, stats.FinishedPercent, 6);
        Assert.Null(stats.Ratio); // below 100 KB downloaded
    }

    [Fact]
    public async Task DisabledRandomnessStillAddsUpToNineBytesOfNoise()
    {
        var random = new ScriptedRandomSource(ints: [7, 3]);
        var (session, _) = Create(random: random);
        await session.StartAsync(Ct);

        await TickAsync(session, 1);

        Assert.Equal(60 * KiB + 7, session.Snapshot.Uploaded);
        Assert.Equal(30 * KiB + 3, session.Snapshot.Downloaded);
    }

    [Fact]
    public async Task EnabledRandomnessAddsWholeKilobytesInRange()
    {
        var settings = QuietSettings() with { UploadRandomEnabled = true, UploadRandomMinKb = 1, UploadRandomMaxKb = 10 };
        var random = new ScriptedRandomSource(ints: [4]);
        var (session, _) = Create(settings, random: random);
        await session.StartAsync(Ct);

        await TickAsync(session, 1);

        Assert.Equal(60 * KiB + 4 * KiB, session.Snapshot.Uploaded);
    }

    [Fact]
    public async Task CompletesOnceWhenLeftReachesZero()
    {
        var settings = QuietSettings() with { DownloadRateBytes = 200_000 };
        var (session, tracker) = Create(settings, totalLength: 100_000);
        await session.StartAsync(Ct);

        await TickAsync(session, 2);

        var stats = session.Snapshot;
        Assert.Equal(100_000, stats.Downloaded);
        Assert.Equal(0, stats.Left);
        Assert.Equal(0, stats.DownloadRateBytes);
        Assert.Equal(100, stats.FinishedPercent);
        Assert.Equal(TorrentSessionState.Seeding, session.State);
        Assert.Equal(1, tracker.Announces.Count(a => a.Event == TrackerEvent.Completed));
        Assert.Equal(2, tracker.ScrapeCount); // start + completed
    }

    [Fact]
    public async Task StartsInSeedModeAtHundredPercent()
    {
        var settings = QuietSettings() with { FinishedPercent = 100 };
        var (session, tracker) = Create(settings);
        await session.StartAsync(Ct);

        Assert.Equal(TorrentSessionState.Seeding, session.State);
        Assert.Equal(0, tracker.Announces[0].Values.Left);

        await TickAsync(session, 2);

        Assert.Equal(2 * 60 * KiB, session.Snapshot.Uploaded);
        Assert.Equal(0, session.Snapshot.Downloaded);
        Assert.DoesNotContain(tracker.Announces, a => a.Event == TrackerEvent.Completed);
        Assert.Equal(100, session.Snapshot.FinishedPercent);
    }

    [Fact]
    public async Task PartialFinishedPercentSetsTheRemainingSize()
    {
        var settings = QuietSettings() with { FinishedPercent = 25 };
        var (session, tracker) = Create(settings, totalLength: 1000);
        await session.StartAsync(Ct);

        Assert.Equal(750, tracker.Announces[0].Values.Left);
        Assert.Equal(25, session.Snapshot.FinishedPercent, 6);
    }

    [Fact]
    public async Task AnnounceValuesAreFlooredToTheProtocolDenominators()
    {
        var settings = QuietSettings() with { UploadRateBytes = 20_000, DownloadRateBytes = 100 };
        var (session, tracker) = Create(settings);
        await session.StartAsync(Ct);
        await TickAsync(session, 1);
        Assert.Equal(20_000, session.Snapshot.Uploaded);

        session.RequestUpdate();
        await TickAsync(session, 1);

        var update = tracker.Announces.Last(a => a.Event == TrackerEvent.None).Values;
        Assert.Equal(32_768, update.Uploaded);     // 40 000 floored to a 16 KiB multiple
        Assert.Equal(192, update.Downloaded);      // 200 floored to a 16-byte multiple
        Assert.Equal(1_000_000 - 192, update.Left);
        Assert.Equal(32_768, session.Snapshot.Uploaded); // the stored counter is floored too, as in 0.43
    }

    [Theory]
    [InlineData(120, 120)]
    [InlineData(10, 60)]
    [InlineData(999_999, 86_400)]
    public async Task TrackerIntervalOverridesTheConfiguredOneWithinClamps(int trackerInterval, int expected)
    {
        var adjustments = new List<EngineAdjustment>();
        var (session, _) = Create(configureTracker: t => t.Interval = trackerInterval);
        session.Adjusted += (_, a) => adjustments.Add(a);

        await session.StartAsync(Ct);

        Assert.Equal(TimeSpan.FromSeconds(expected), session.Snapshot.NextUpdateIn);
        Assert.Contains(adjustments, a => a.IntervalSeconds == expected);
    }

    [Fact]
    public async Task ZeroLeechersPauseTheUploadByDefault()
    {
        // Issue #16 / #42: uploading when nobody is downloading is what gets accounts banned, so the
        // default is to pause, and nothing must be reported as uploaded while paused.
        var logs = new List<LogEntry>();
        var (session, tracker) = Create(configureTracker: t => t.Incomplete = 0);
        session.LogEmitted += (_, e) => logs.Add(e);
        Assert.True(session.Settings.StopUploadWhenNoLeechers);

        await session.StartAsync(Ct);
        await TickAsync(session, 3);
        session.RequestUpdate();
        await TickAsync(session, 1);

        Assert.Equal(0, session.Snapshot.UploadRateBytes);
        Assert.Equal(0, session.Snapshot.Uploaded);
        Assert.Equal(2, tracker.Announces.Count);
        Assert.All(tracker.Announces, a => Assert.Equal(0, a.Values.Uploaded));
        Assert.Equal(4 * 30 * KiB, session.Snapshot.Downloaded); // the download is not affected
        Assert.Contains(logs, l => l.Text.Contains("upload paused", StringComparison.Ordinal));
        Assert.Equal(60 * KiB, session.Settings.UploadRateBytes); // the user's speed is not overwritten
    }

    [Fact]
    public async Task ZeroLeechersDoNotPauseTheUploadWhenTheOptionIsOff()
    {
        var settings = QuietSettings() with { StopUploadWhenNoLeechers = false };
        var (session, _) = Create(settings, configureTracker: t => t.Incomplete = 0);

        await session.StartAsync(Ct);
        await TickAsync(session, 3);

        Assert.Equal(60 * KiB, session.Snapshot.UploadRateBytes);
        Assert.Equal(3 * 60 * KiB, session.Snapshot.Uploaded);
    }

    [Fact]
    public async Task UploadResumesWhenTheTrackerReportsLeechersAgain()
    {
        var logs = new List<LogEntry>();
        var (session, tracker) = Create(configureTracker: t => t.Incomplete = 0);
        session.LogEmitted += (_, e) => logs.Add(e);
        await session.StartAsync(Ct);
        await TickAsync(session, 2);
        Assert.Equal(0, session.Snapshot.Uploaded);

        tracker.Incomplete = 3;
        session.RequestUpdate();
        await TickAsync(session, 1); // this tick grows nothing (still paused) and then announces
        await TickAsync(session, 2);

        Assert.Equal(60 * KiB, session.Snapshot.UploadRateBytes);
        Assert.Equal(2 * 60 * KiB, session.Snapshot.Uploaded);
        Assert.Contains(logs, l => l.Text.Contains("Upload resumed: the tracker reports 3 leechers", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ZeroLeechersReportedByTheScrapePauseTheUploadToo()
    {
        var (session, _) = Create(configureTracker: t =>
        {
            t.Incomplete = 5;
            t.ScrapeComplete = 10;
            t.ScrapeIncomplete = 0;
        });

        await session.StartAsync(Ct); // the started announce says 5, the scrape right after it says 0
        await TickAsync(session, 2);

        Assert.Equal(0, session.Snapshot.Leechers);
        Assert.Equal(0, session.Snapshot.Uploaded);
    }

    [Fact]
    public async Task ARandomSpeedOnTheNextUpdateCannotBypassThePause()
    {
        // 0.43 zeroed the configured speed instead of gating the counter, so the "random speed on each
        // update" feature quietly switched the upload back on with no leechers.
        var settings = QuietSettings() with { NextUpdateRandomUpload = true, NextUpdateUploadMinKb = 10, NextUpdateUploadMaxKb = 50 };
        var (session, _) = Create(settings, configureTracker: t => t.Incomplete = 0);

        await session.StartAsync(Ct);
        session.RequestUpdate();
        await TickAsync(session, 3);

        Assert.Equal(0, session.Snapshot.UploadRateBytes);
        Assert.Equal(0, session.Snapshot.Uploaded);
    }

    [Fact]
    public async Task ALiveSpeedEditCannotBypassThePause()
    {
        var (session, _) = Create(configureTracker: t => t.Incomplete = 0);
        await session.StartAsync(Ct);

        session.UpdateLiveSettings(QuietSettings() with { UploadRateBytes = 500 * KiB });
        await TickAsync(session, 2);

        Assert.Equal(0, session.Snapshot.Uploaded);
    }

    [Fact]
    public async Task SwitchingThePauseOffWhileRunningResumesTheUpload()
    {
        var (session, _) = Create(configureTracker: t => t.Incomplete = 0);
        await session.StartAsync(Ct);
        await TickAsync(session, 2);
        Assert.Equal(0, session.Snapshot.Uploaded);

        session.UpdateLiveSettings(QuietSettings() with { StopUploadWhenNoLeechers = false });
        await TickAsync(session, 2);

        Assert.Equal(2 * 60 * KiB, session.Snapshot.Uploaded);
    }

    [Fact]
    public async Task RequestUpdateAnnouncesOnTheNextTick()
    {
        var (session, tracker) = Create();
        await session.StartAsync(Ct);

        session.RequestUpdate();
        await TickAsync(session, 1);

        Assert.Equal([TrackerEvent.Started, TrackerEvent.None], tracker.Announces.Select(a => a.Event));
        Assert.Equal(2, tracker.ScrapeCount);
        Assert.Equal(TorrentSessionState.Downloading, session.State);
    }

    [Fact]
    public async Task RegularAnnounceHappensAfterTheInterval()
    {
        var settings = QuietSettings() with { IntervalSeconds = 60 };
        var (session, tracker) = Create(settings, configureTracker: t => t.Interval = 60);
        await session.StartAsync(Ct);

        await TickAsync(session, 60);
        Assert.DoesNotContain(tracker.Announces, a => a.Event == TrackerEvent.None);
        Assert.Equal(TimeSpan.Zero, session.Snapshot.NextUpdateIn);

        await TickAsync(session, 1);
        Assert.Equal(1, tracker.Announces.Count(a => a.Event == TrackerEvent.None));
        Assert.Equal(TimeSpan.FromSeconds(60), session.Snapshot.NextUpdateIn);
    }

    [Fact]
    public async Task StopsAfterTheConfiguredSeconds()
    {
        var settings = QuietSettings() with { Stop = new StopCondition { Type = StopConditionType.AfterSeconds, Value = 3 } };
        var (session, tracker) = Create(settings);
        await session.StartAsync(Ct);

        await TickAsync(session, 2);
        Assert.Equal(TorrentSessionState.Downloading, session.State);
        Assert.Equal(TimeSpan.FromSeconds(1), session.Snapshot.StopAfterRemaining);

        await TickAsync(session, 1);
        Assert.Equal(TorrentSessionState.Stopped, session.State);
        Assert.Equal(TrackerEvent.Stopped, tracker.Announces.Last().Event);
        Assert.Contains("3 seconds", session.Snapshot.StopReason, StringComparison.Ordinal);
        Assert.Null(session.Snapshot.NextUpdateIn);
    }

    [Fact]
    public async Task StopsWhenSeedersDropBelowTheLimit()
    {
        var settings = QuietSettings() with { Stop = new StopCondition { Type = StopConditionType.SeedersBelow, Value = 5 } };
        var (session, tracker) = Create(settings, configureTracker: t => t.Complete = 2);
        await session.StartAsync(Ct);

        await TickAsync(session, 1);

        Assert.Equal(TorrentSessionState.Stopped, session.State);
        Assert.Equal(TrackerEvent.Stopped, tracker.Announces.Last().Event);
    }

    [Fact]
    public async Task StopsWhenUploadedExceedsTheLimit()
    {
        var settings = QuietSettings() with
        {
            UploadRateBytes = 2 * 1024 * 1024,
            Stop = new StopCondition { Type = StopConditionType.UploadedAboveMb, Value = 1 },
        };
        var (session, _) = Create(settings);
        await session.StartAsync(Ct);

        await TickAsync(session, 1);

        Assert.Equal(TorrentSessionState.Stopped, session.State);
        Assert.Contains("uploaded", session.Snapshot.StopReason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StopsWhenLeecherToSeederRatioIsBelowTheLimit()
    {
        var settings = QuietSettings() with { Stop = new StopCondition { Type = StopConditionType.LeecherSeederRatioBelow, Value = 0.5 } };
        var (session, _) = Create(settings, configureTracker: t =>
        {
            t.Complete = 10;
            t.Incomplete = 1;
        });
        await session.StartAsync(Ct);

        await TickAsync(session, 1);

        Assert.Equal(TorrentSessionState.Stopped, session.State);
    }

    [Fact]
    public async Task TrackerFailureStopsWithErrorUnlessIgnored()
    {
        var (session, tracker) = Create(configureTracker: t => t.FailureReason = "torrent not registered");
        await session.StartAsync(Ct);

        Assert.Equal(TorrentSessionState.Error, session.State);
        Assert.Contains("torrent not registered", session.Snapshot.StopReason, StringComparison.Ordinal);
        Assert.Equal([TrackerEvent.Started, TrackerEvent.Stopped], tracker.Announces.Select(a => a.Event));

        var ignoring = QuietSettings() with { IgnoreFailureReason = true };
        var (session2, _) = Create(ignoring, configureTracker: t => t.FailureReason = "torrent not registered");
        await session2.StartAsync(Ct);
        Assert.Equal(TorrentSessionState.Downloading, session2.State);
    }

    [Fact]
    public async Task NoConnectionKeepsTheSessionRunningAndCountersFrozenUntilTheFirstReply()
    {
        var (session, tracker) = Create(configureTracker: t => t.AnnounceError = new TrackerException("connection refused"));
        await session.StartAsync(Ct);

        Assert.Equal(TorrentSessionState.Downloading, session.State);
        await TickAsync(session, 2);
        Assert.Equal(0, session.Snapshot.Uploaded);

        tracker.AnnounceError = null;
        session.RequestUpdate();
        await TickAsync(session, 1);
        await TickAsync(session, 1);

        Assert.Equal(60 * KiB, session.Snapshot.Uploaded);
    }

    [Fact]
    public async Task NoConnectionRetriesWithinAMinuteInsteadOfWaitingTheFullInterval()
    {
        var (session, tracker) = Create(configureTracker: t => t.AnnounceError = new TrackerException("timed out"));
        await session.StartAsync(Ct);

        Assert.Equal(TimeSpan.FromSeconds(TorrentSession.RetryDelaySeconds), session.Snapshot.NextUpdateIn);

        // Once the tracker answers, the retry announce happens on its own and the counters start moving.
        tracker.AnnounceError = null;
        await TickAsync(session, TorrentSession.RetryDelaySeconds + 1);
        Assert.Contains(tracker.Announces, a => a.Event == TrackerEvent.None);
        await TickAsync(session, 1);
        Assert.True(session.Snapshot.Uploaded > 0);
    }

    [Fact]
    public async Task StopAsyncSendsTheStoppedAnnounce()
    {
        var (session, tracker) = Create();
        await session.StartAsync(Ct);

        await session.StopAsync(Ct);

        Assert.Equal(TrackerEvent.Stopped, tracker.Announces.Last().Event);
        Assert.Equal(TorrentSessionState.Stopped, session.State);
        Assert.Null(session.Snapshot.NextUpdateIn);
    }

    [Fact]
    public async Task ListenerIsNotOpenedWhenAProxyIsConfigured()
    {
        var settings = QuietSettings() with
        {
            UseTcpListener = true,
            Proxy = new ProxySettings { Type = ProxyType.Socks5, Host = "127.0.0.1", Port = 1080 },
        };
        var (session, _) = Create(settings);
        await session.StartAsync(Ct);

        Assert.False(session.IsListening);
    }

    [Fact]
    public async Task ListenerIsOpenedForDirectConnectionsAndClosedOnStop()
    {
        var port = GetFreePort();
        var settings = QuietSettings() with { UseTcpListener = true };
        var (session, _) = Create(settings, identity: Identity(port.ToString()));

        await session.StartAsync(Ct);
        Assert.True(session.IsListening);

        await session.StopAsync(Ct);
        Assert.False(session.IsListening);
    }

    [Fact]
    public async Task NextUpdateRandomSpeedsApplyBeforeTheAnnounce()
    {
        var settings = QuietSettings() with { NextUpdateRandomUpload = true, NextUpdateUploadMinKb = 10, NextUpdateUploadMaxKb = 50 };
        // Two zero jitters for the growth step, then 20 for the next-update upload speed.
        var random = new ScriptedRandomSource(ints: [0, 0, 20]);
        var (session, _) = Create(settings, random: random);
        await session.StartAsync(Ct);

        session.RequestUpdate();
        await TickAsync(session, 1);

        Assert.Equal(20 * KiB, session.Snapshot.UploadRateBytes);
    }

    [Fact]
    public async Task UpdateLiveSettingsChangesTheRatesImmediately()
    {
        var (session, _) = Create();
        await session.StartAsync(Ct);

        session.UpdateLiveSettings(QuietSettings() with { UploadRateBytes = 1024, DownloadRateBytes = 0 });
        await TickAsync(session, 1);

        Assert.Equal(1024, session.Snapshot.Uploaded);
        Assert.Equal(0, session.Snapshot.Downloaded);
    }

    [Fact]
    public async Task EmitsLogLinesAndTrackerExchanges()
    {
        var logs = new List<LogEntry>();
        var exchanges = new List<TrackerExchange>();
        var (session, _) = Create();
        session.LogEmitted += (_, e) => logs.Add(e);
        session.TrackerExchangeCompleted += (_, e) => exchanges.Add(e);

        await session.StartAsync(Ct);

        Assert.Contains(logs, l => l.Text.Contains("started event", StringComparison.Ordinal));
        Assert.Equal(2, exchanges.Count); // announce + scrape
        Assert.Equal("started", exchanges[0].Kind);
        Assert.Equal("scrape", exchanges[1].Kind);
    }

    [Fact]
    public async Task RatioBecomesAvailableAfter100KbDownloaded()
    {
        var settings = QuietSettings() with { UploadRateBytes = 200 * KiB, DownloadRateBytes = 100 * KiB };
        var (session, _) = Create(settings);
        await session.StartAsync(Ct);

        await TickAsync(session, 1);

        Assert.Equal(2.0, session.Snapshot.Ratio);
    }

    [Theory]
    [InlineData("udp://tracker.test:1337/announce", "udp://")]
    [InlineData("", "no tracker URL")]
    [InlineData("tracker.test/announce", "not a valid tracker URL")]
    public async Task AnUnsupportedTrackerUrlEndsInErrorWithoutAnyAnnounce(string trackerUrl, string expectedReason)
    {
        var tracker = new FakeTrackerClient();
        var descriptor = new TorrentDescriptor { InfoHash = InfoHash, TotalLength = 1000, TrackerUrl = trackerUrl };
        var session = new TorrentSession(descriptor, Profile, Identity(), QuietSettings(), tracker, new FakeLocalIpProvider(), new ScriptedRandomSource(), new FakeClock());

        await session.StartAsync(Ct);

        Assert.Equal(TorrentSessionState.Error, session.State);
        Assert.Contains(expectedReason, session.Snapshot.StopReason, StringComparison.Ordinal);
        Assert.Empty(tracker.Announces); // neither started nor stopped: there is nothing to talk to
        Assert.Equal(0, tracker.ScrapeCount);
    }

    [Fact]
    public async Task AnUnexpectedTrackerFailureEndsInErrorInsteadOfKillingTheRunLoop()
    {
        // Only TrackerException is expected from a tracker client; anything else used to fault the background
        // task silently, leaving the session "Starting" forever and rethrowing from StopAsync.
        var (session, tracker) = Create(configureTracker: t => t.AnnounceError = new InvalidOperationException("boom"));

        session.Start();
        await WaitUntilAsync(() => session.State == TorrentSessionState.Error, TimeSpan.FromSeconds(10));
        await session.StopAsync(Ct);

        Assert.Equal(TorrentSessionState.Error, session.State);
        Assert.Contains("boom", session.Snapshot.StopReason, StringComparison.Ordinal);
        Assert.Contains(tracker.Announces, a => a.Event == TrackerEvent.Stopped); // the stopped announce was still attempted
    }

    [Fact]
    public async Task CanBeStartedAgainAfterStoppingOnItsOwn()
    {
        var settings = QuietSettings() with { Stop = new StopCondition { Type = StopConditionType.AfterSeconds, Value = 1 } };
        var (session, tracker) = Create(settings);

        session.Start();
        await WaitUntilAsync(() => session.State == TorrentSessionState.Stopped, TimeSpan.FromSeconds(10));

        session.Start();
        await WaitUntilAsync(() => tracker.Announces.Count(a => a.Event == TrackerEvent.Started) == 2, TimeSpan.FromSeconds(10));
        await WaitUntilAsync(() => session.State == TorrentSessionState.Stopped, TimeSpan.FromSeconds(10));
        await session.StopAsync(Ct);

        Assert.Equal(2, tracker.Announces.Count(a => a.Event == TrackerEvent.Stopped));
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The condition was not met in time.");
            }

            await Task.Delay(50, Ct);
        }
    }

    private static int GetFreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }
}
