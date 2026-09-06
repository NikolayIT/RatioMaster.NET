namespace RatioMaster.Core.Networking;

/// <summary>Opens a byte stream to a tracker, directly or through a proxy, optionally over TLS.</summary>
public interface ITrackerTransport
{
    /// <summary>
    /// Connects to <paramref name="host"/>:<paramref name="port"/>, tunnelling through the proxy when one is
    /// configured and wrapping the stream in TLS when <paramref name="useTls"/> is set. The caller owns and
    /// disposes the returned stream.
    /// </summary>
    Task<Stream> ConnectAsync(
        string host,
        int port,
        bool useTls,
        bool ignoreCertificateErrors,
        ProxySettings proxy,
        CancellationToken cancellationToken);
}
