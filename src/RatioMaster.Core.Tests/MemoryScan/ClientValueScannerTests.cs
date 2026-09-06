using System.Text;
using RatioMaster.Core.Clients;
using RatioMaster.Core.MemoryScan;

namespace RatioMaster.Core.Tests.MemoryScan;

public class ClientValueScannerTests
{
    private static readonly ClientProfileCatalog Catalog = ClientProfileCatalog.Load();

    private static ClientIdentity Fallback => new()
    {
        PeerId = "-FALLBACK-",
        Key = "FALLBACK",
        Port = "1",
        NumWant = "200",
    };

    private static byte[] MemoryWith(string announce, int padding = 70_000)
    {
        var text = new string('.', padding) + announce;
        return Encoding.ASCII.GetBytes(text);
    }

    [Fact]
    public void CopiesTheValuesOutOfTheRunningClient()
    {
        var memory = MemoryWith(
            "GET /announce?info_hash=%aa&peer_id=-UT3320-abc123&port=54321&uploaded=0&downloaded=0&key=DEADBEEF&numwant=175&compact=1 HTTP/1.1");
        var scanner = new ClientValueScanner(new FakeProcessMemoryScanner(memory));

        var identity = scanner.TryScan(Catalog.GetByName("uTorrent 3.3.2"), Fallback);

        Assert.NotNull(identity);
        Assert.Equal("-UT3320-abc123", identity!.PeerId);
        Assert.Equal("DEADBEEF", identity.Key);
        Assert.Equal("54321", identity.Port);
        Assert.Equal("175", identity.NumWant);
        Assert.Equal(ClientIdentitySource.CopiedFromProcess, identity.Source);
    }

    [Fact]
    public void FallsBackForFieldsThatAreNotPresent()
    {
        var memory = MemoryWith("&peer_id=-UT3320-onlypeer&compact=1 x", padding: 0);
        var scanner = new ClientValueScanner(new FakeProcessMemoryScanner(memory));

        var identity = scanner.TryScan(Catalog.GetByName("uTorrent 3.3.2"), Fallback);

        Assert.NotNull(identity);
        Assert.Equal("-UT3320-onlypeer", identity!.PeerId);
        Assert.Equal("FALLBACK", identity.Key);
        Assert.Equal("1", identity.Port);
        Assert.Equal("200", identity.NumWant);
    }

    [Fact]
    public void UsesTheProfileDefaultWhenNumWantIsNotANumber()
    {
        var memory = MemoryWith("&peer_id=-UT3320-abc&numwant=lots&compact=1 x", padding: 0);
        var scanner = new ClientValueScanner(new FakeProcessMemoryScanner(memory));

        var identity = scanner.TryScan(Catalog.GetByName("uTorrent 3.3.2"), Fallback);

        Assert.Equal("200", identity!.NumWant);
    }

    [Fact]
    public void ReturnsNullWhenTheMarkerIsNotFound()
    {
        var scanner = new ClientValueScanner(new FakeProcessMemoryScanner(Encoding.ASCII.GetBytes("nothing here")));
        Assert.Null(scanner.TryScan(Catalog.GetByName("uTorrent 3.3.2"), Fallback));
    }

    [Fact]
    public void ReturnsNullWhenTheProcessIsNotRunning()
    {
        var scanner = new ClientValueScanner(new FakeProcessMemoryScanner([], canOpen: false));
        var messages = new List<string>();

        Assert.Null(scanner.TryScan(Catalog.GetByName("uTorrent 3.3.2"), Fallback, messages.Add));
        Assert.Contains(messages, m => m.Contains("No uTorrent process found", StringComparison.Ordinal));
    }

    [Fact]
    public void ReturnsNullForProfilesThatCannotBeScanned()
    {
        var scanner = new ClientValueScanner(new FakeProcessMemoryScanner(Encoding.ASCII.GetBytes("&peer_id=-DE1200-x&")));
        Assert.Null(scanner.TryScan(Catalog.GetByName("Deluge 1.2.0"), Fallback));
    }

    [Fact]
    public void ReturnsNullWhenThePlatformDoesNotSupportScanning()
    {
        var scanner = new ClientValueScanner(NullProcessMemoryScanner.Instance);
        Assert.False(scanner.IsSupported);
        Assert.Null(scanner.TryScan(Catalog.GetByName("uTorrent 3.3.2"), Fallback));
    }

    private sealed class FakeProcessMemoryScanner(byte[] memory, bool canOpen = true) : IProcessMemoryScanner
    {
        public bool IsSupported => true;

        public ProcessMemorySession? Open(string processName) => canOpen ? new Session(memory) : null;

        private sealed class Session(byte[] memory) : ProcessMemorySession
        {
            public override int Read(long address, byte[] buffer)
            {
                if (address >= memory.Length)
                {
                    return 0;
                }

                var count = (int)Math.Min(buffer.Length, memory.Length - address);
                Array.Copy(memory, address, buffer, 0, count);
                return count;
            }
        }
    }
}
