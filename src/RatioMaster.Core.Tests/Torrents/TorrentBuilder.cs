namespace RatioMaster.Core.Tests.Torrents
{
    using RatioMaster.Core.Bencode;

    /// <summary>Builds .torrent metainfo bytes for tests.</summary>
    internal static class TorrentBuilder
    {
        public const string DefaultAnnounce = "http://tracker.example.com/announce";

        public static BencodeDictionary SingleFileInfo(string name = "ubuntu.iso", long length = 100_000, int pieceLength = 16_384)
        {
            var pieceCount = (int)Math.Max(1, (length + pieceLength - 1) / pieceLength);
            return new BencodeDictionary()
                .Set("name", name)
                .Set("length", length)
                .Set("piece length", pieceLength)
                .Set("pieces", new BencodeString(Pieces(pieceCount)));
        }

        public static BencodeDictionary MultiFileInfo(string name, params (string[] Path, long Length)[] files)
        {
            var list = new BencodeList();
            foreach (var (path, length) in files)
            {
                list.Add(new BencodeDictionary()
                    .Set("length", length)
                    .Set("path", new BencodeList(path.Select(p => new BencodeString(p)))));
            }

            var total = files.Sum(f => f.Length);
            var pieceCount = (int)Math.Max(1, (total + 16_384 - 1) / 16_384);
            return new BencodeDictionary()
                .Set("name", name)
                .Set("files", list)
                .Set("piece length", 16_384)
                .Set("pieces", new BencodeString(Pieces(pieceCount)));
        }

        public static BencodeDictionary Root(BencodeDictionary info, string? announce = DefaultAnnounce)
        {
            var root = new BencodeDictionary().Set("info", info);
            if (announce is not null)
            {
                root.Set("announce", announce);
            }

            return root;
        }

        public static byte[] Build(BencodeDictionary info, string? announce = DefaultAnnounce) => Root(info, announce).ToBytes();

        public static byte[] Build(Action<BencodeDictionary>? configureRoot = null, Action<BencodeDictionary>? configureInfo = null)
        {
            var info = SingleFileInfo();
            configureInfo?.Invoke(info);
            var root = Root(info);
            configureRoot?.Invoke(root);
            return root.ToBytes();
        }

        public static byte[] Pieces(int count)
        {
            var bytes = new byte[count * 20];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)((i * 7) + 3);
            }

            return bytes;
        }
    }
}
