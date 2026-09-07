using System.Net;
using System.Net.Sockets;
using System.Text;
using RatioMaster.Core.Networking;

namespace RatioMaster.Core.Tests.Networking;

public class PeerListenerTests
{
    private const string PeerId = "-RM1000-abcdefghijkl";

    [Fact]
    public async Task RepliesToAMatchingHandshake()
    {
        var ct = TestContext.Current.CancellationToken;
        var infoHash = MakeInfoHash(1);
        var port = GetFreePort();
        await using var listener = new PeerListener(port, infoHash, PeerId);
        Assert.True(listener.Start());

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port, ct);
        await using var stream = client.GetStream();
        await stream.WriteAsync(BuildHandshake(infoHash, "-OTHERPEER-1234567890"), ct);

        var response = new byte[68];
        await stream.ReadExactlyAsync(response, ct);

        Assert.Equal(19, response[0]);
        Assert.Equal("BitTorrent protocol", Encoding.ASCII.GetString(response, 1, 19));
        Assert.Equal(new byte[8], response[20..28]);
        Assert.Equal(infoHash, response[28..48]);
        Assert.Equal(PeerId, Encoding.ASCII.GetString(response, 48, 20));
    }

    [Fact]
    public async Task IgnoresAHandshakeForAnotherTorrent()
    {
        var infoHash = MakeInfoHash(2);
        var otherHash = MakeInfoHash(200);
        var port = GetFreePort();
        await using var listener = new PeerListener(port, infoHash, PeerId);
        Assert.True(listener.Start());

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port, TestContext.Current.CancellationToken);
        await using var stream = client.GetStream();
        await stream.WriteAsync(BuildHandshake(otherHash, "-OTHERPEER-1234567890"), TestContext.Current.CancellationToken);

        // The listener must not reply. The connection is closed instead, which surfaces either as a
        // zero-byte read or, on Windows, as a connection reset. Both mean no handshake was returned.
        var response = new byte[68];
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            var read = await stream.ReadAsync(response, cts.Token);
            Assert.Equal(0, read);
        }
        catch (IOException)
        {
            // Connection reset by the listener closing without replying: acceptable.
        }
    }

    [Fact]
    public async Task StartReturnsFalseWhenThePortIsInUse()
    {
        var port = GetFreePort();
        using var blocker = new TcpListener(IPAddress.Any, port);
        blocker.Start();
        try
        {
            var messages = new List<string>();
            await using var listener = new PeerListener(port, MakeInfoHash(3), PeerId, messages.Add);
            Assert.False(listener.Start());
            Assert.Contains(messages, m => m.Contains("could not start", StringComparison.Ordinal));
        }
        finally
        {
            blocker.Stop();
        }
    }

    [Fact]
    public async Task LogsWhenTheListenerIsClosed()
    {
        var messages = new List<string>();
        var listener = new PeerListener(GetFreePort(), MakeInfoHash(4), PeerId, messages.Add);
        Assert.True(listener.Start());

        await listener.DisposeAsync();

        Assert.False(listener.IsListening);
        Assert.Contains(messages, m => m.Contains("Started TCP listener", StringComparison.Ordinal));
        Assert.Contains(messages, m => m.Contains("TCP listener closed", StringComparison.Ordinal));
    }

    private static byte[] MakeInfoHash(byte seed)
    {
        var hash = new byte[20];
        for (var i = 0; i < hash.Length; i++)
        {
            hash[i] = (byte)(seed + i);
        }

        return hash;
    }

    private static byte[] BuildHandshake(byte[] infoHash, string peerId)
    {
        const string protocol = "BitTorrent protocol";
        var handshake = new List<byte> { (byte)protocol.Length };
        handshake.AddRange(Encoding.ASCII.GetBytes(protocol));
        handshake.AddRange(new byte[8]);
        handshake.AddRange(infoHash);
        handshake.AddRange(Encoding.ASCII.GetBytes(peerId));
        return handshake.ToArray();
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
