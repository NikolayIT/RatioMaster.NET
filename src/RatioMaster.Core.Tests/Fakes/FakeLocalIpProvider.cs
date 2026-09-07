namespace RatioMaster.Core.Tests.Fakes
{
    using RatioMaster.Core.Abstractions;
    using RatioMaster.Core.Networking;

    /// <summary>Returns a fixed local IP.</summary>
    internal sealed class FakeLocalIpProvider(string ip = "1.2.3.4") : ILocalIpProvider
    {
        public ValueTask<string> GetLocalIpAsync(CancellationToken cancellationToken = default) => new(ip);
    }
}
