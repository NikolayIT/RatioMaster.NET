using RatioMaster.Core.Abstractions;
using RatioMaster.Core.Networking;

namespace RatioMaster.Core.Tests.Fakes;

/// <summary>A clock whose time is set by the test.</summary>
internal sealed class FakeClock : ISystemClock
{
    public DateTimeOffset Now { get; set; } = new(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan by) => Now += by;
}

/// <summary>Returns a fixed local IP.</summary>
internal sealed class FakeLocalIpProvider(string ip = "1.2.3.4") : ILocalIpProvider
{
    public ValueTask<string> GetLocalIpAsync(CancellationToken cancellationToken = default) => new(ip);
}
