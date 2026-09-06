using System.Security.Cryptography;
using System.Text;
using RatioMaster.Core.Bencode;
using RatioMaster.Core.Torrents;

namespace RatioMaster.Core.Tests.Torrents;

public class TorrentFileTests
{
    [Fact]
    public void ParsesSingleFileTorrent()
    {
        var info = TorrentBuilder.SingleFileInfo("ubuntu.iso", 100_000, 16_384);
        var torrent = TorrentFile.Parse(TorrentBuilder.Build(info));

        Assert.Equal("ubuntu.iso", torrent.Name);
        Assert.Equal(100_000, torrent.TotalLength);
        Assert.Equal(16_384, torrent.PieceLength);
        Assert.Equal(7, torrent.PieceCount);
        Assert.True(torrent.IsSingleFile);
        Assert.Single(torrent.Files);
        Assert.Equal(new TorrentFileEntry("ubuntu.iso", 100_000), torrent.Files[0]);
        Assert.Equal(TorrentBuilder.DefaultAnnounce, torrent.Announce);
        Assert.Equal([TorrentBuilder.DefaultAnnounce], torrent.AnnounceList);
        Assert.Null(torrent.Path);
        Assert.False(torrent.IsPrivate);

        var expectedHash = SHA1.HashData(info.ToBytes());
        Assert.Equal(expectedHash, torrent.InfoHash.ToArray());
        Assert.Equal(Convert.ToHexString(expectedHash), torrent.InfoHashHex);
        Assert.Equal(40, torrent.InfoHashHex.Length);
        Assert.Equal(expectedHash, torrent.GetInfoHashBytes());
    }

    [Fact]
    public void ParsesMultiFileTorrent()
    {
        var info = TorrentBuilder.MultiFileInfo("Album", (["01.mp3"], 3_000), (["cd2", "02.mp3"], 5_000));
        var torrent = TorrentFile.Parse(TorrentBuilder.Build(info));

        Assert.Equal("Album", torrent.Name);
        Assert.False(torrent.IsSingleFile);
        Assert.Equal(8_000, torrent.TotalLength);
        Assert.Equal(["01.mp3", "cd2/02.mp3"], torrent.Files.Select(f => f.Path));
        Assert.Equal([3_000L, 5_000L], torrent.Files.Select(f => f.Length));
    }

    [Fact]
    public void InfoHashIsComputedOverRawBytesEvenWhenKeysAreUnsorted()
    {
        // Keys inside "info" are deliberately out of order; re-encoding would sort them and change the hash.
        var pieces = TorrentBuilder.Pieces(1);
        var infoBytes = Concat("d6:pieces20:", pieces, "4:name3:abc6:lengthi5e12:piece lengthi16384ee");
        var data = Concat("d8:announce3:url4:info", infoBytes, "e");

        var torrent = TorrentFile.Parse(data);

        Assert.Equal(SHA1.HashData(infoBytes), torrent.InfoHash.ToArray());
        Assert.NotEqual(SHA1.HashData(torrent.Info.ToBytes()), torrent.InfoHash.ToArray());
        Assert.Equal("abc", torrent.Name);
        Assert.Equal(5, torrent.TotalLength);
    }

    [Fact]
    public void FlattensAnnounceListWithoutDuplicates()
    {
        var data = TorrentBuilder.Build(root => root.Set("announce-list", new BencodeList(
        [
            new BencodeList([new BencodeString(TorrentBuilder.DefaultAnnounce), new BencodeString("http://b/announce")]),
            new BencodeList([new BencodeString("udp://c:6969/announce")]),
            new BencodeList([new BencodeString("  http://b/announce  ")]),
        ])));

        var torrent = TorrentFile.Parse(data);

        Assert.Equal([TorrentBuilder.DefaultAnnounce, "http://b/announce", "udp://c:6969/announce"], torrent.AnnounceList);
    }

    [Fact]
    public void UsesFirstAnnounceListEntryWhenAnnounceIsMissing()
    {
        var root = TorrentBuilder.Root(TorrentBuilder.SingleFileInfo(), announce: null)
            .Set("announce-list", new BencodeList([new BencodeList([new BencodeString("http://only/announce")])]));

        var torrent = TorrentFile.Parse(root.ToBytes());

        Assert.Equal("http://only/announce", torrent.Announce);
    }

    [Fact]
    public void AnnounceIsEmptyWhenTheFileHasNoTrackers()
    {
        var torrent = TorrentFile.Parse(TorrentBuilder.Build(TorrentBuilder.SingleFileInfo(), announce: null));
        Assert.Equal(string.Empty, torrent.Announce);
        Assert.Empty(torrent.AnnounceList);
    }

    [Fact]
    public void ReadsOptionalMetadata()
    {
        var data = TorrentBuilder.Build(
            root => root.Set("comment", "hello").Set("created by", "tool 1.0").Set("creation date", 1_700_000_000),
            info => info.Set("private", 1));

        var torrent = TorrentFile.Parse(data);

        Assert.Equal("hello", torrent.Comment);
        Assert.Equal("tool 1.0", torrent.CreatedBy);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000), torrent.CreationDate);
        Assert.True(torrent.IsPrivate);
    }

    [Fact]
    public void PrefersUtf8NameAndPaths()
    {
        var info = TorrentBuilder.MultiFileInfo("legacy", (["old.txt"], 1));
        info.Set("name.utf-8", "näme");
        var file = (BencodeDictionary)info.GetList("files")![0];
        file.Set("path.utf-8", new BencodeList([new BencodeString("dir"), new BencodeString("für.txt")]));

        var torrent = TorrentFile.Parse(TorrentBuilder.Build(info));

        Assert.Equal("näme", torrent.Name);
        Assert.Equal("dir/für.txt", torrent.Files[0].Path);
    }

    [Fact]
    public void RejectsTorrentWithoutInfo()
    {
        var data = new BencodeDictionary().Set("announce", "x").ToBytes();
        var ex = Assert.Throws<TorrentFormatException>(() => TorrentFile.Parse(data));
        Assert.Contains("info", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsTorrentWithoutPieces()
    {
        var data = TorrentBuilder.Build(configureInfo: info => info.Remove("pieces"));
        Assert.Throws<TorrentFormatException>(() => TorrentFile.Parse(data));
    }

    [Fact]
    public void RejectsDamagedPieceHashes()
    {
        var data = TorrentBuilder.Build(configureInfo: info => info.Set("pieces", new BencodeString(new byte[21])));
        Assert.Throws<TorrentFormatException>(() => TorrentFile.Parse(data));
    }

    [Fact]
    public void RejectsTorrentWithoutLengthOrFiles()
    {
        var data = TorrentBuilder.Build(configureInfo: info => info.Remove("length"));
        Assert.Throws<TorrentFormatException>(() => TorrentFile.Parse(data));
    }

    [Fact]
    public void RejectsDataThatIsNotBencode()
    {
        Assert.Throws<TorrentFormatException>(() => TorrentFile.Parse(Encoding.ASCII.GetBytes("<html>")));
        Assert.Throws<TorrentFormatException>(() => TorrentFile.Parse(Encoding.ASCII.GetBytes("le")));
    }

    [Fact]
    public void LoadReadsTheFileAndRecordsItsPath()
    {
        var path = Path.Combine(Path.GetTempPath(), "rm-test-" + Guid.NewGuid().ToString("N") + ".torrent");
        File.WriteAllBytes(path, TorrentBuilder.Build());
        try
        {
            var torrent = TorrentFile.Load(path);
            Assert.Equal(Path.GetFullPath(path), torrent.Path);
            Assert.Equal("ubuntu.iso", torrent.Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadWrapsIoErrors()
    {
        var path = Path.Combine(Path.GetTempPath(), "rm-missing-" + Guid.NewGuid().ToString("N") + ".torrent");
        Assert.Throws<TorrentFormatException>(() => TorrentFile.Load(path));
    }

    private static byte[] Concat(params object[] parts)
    {
        using var stream = new MemoryStream();
        foreach (var part in parts)
        {
            var bytes = part switch
            {
                string s => Encoding.Latin1.GetBytes(s),
                byte[] b => b,
                _ => throw new InvalidOperationException(),
            };
            stream.Write(bytes);
        }

        return stream.ToArray();
    }
}
