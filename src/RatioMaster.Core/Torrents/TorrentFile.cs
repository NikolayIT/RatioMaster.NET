namespace RatioMaster.Core.Torrents
{
    using System.Security.Cryptography;

    using RatioMaster.Core.Bencode;

    /// <summary>The parsed content of a .torrent file (metainfo).</summary>
    public sealed class TorrentFile
    {
        private readonly byte[] infoHash;

        private TorrentFile(
            string? path,
            BencodeDictionary root,
            BencodeDictionary info,
            byte[] infoHash,
            string announce,
            IReadOnlyList<string> announceList,
            string name,
            IReadOnlyList<TorrentFileEntry> files,
            long pieceLength,
            int pieceCount)
        {
            this.Path = path;
            this.Root = root;
            this.Info = info;
            this.infoHash = infoHash;
            this.InfoHashHex = Convert.ToHexString(infoHash);
            this.Announce = announce;
            this.AnnounceList = announceList;
            this.Name = name;
            this.Files = files;
            this.TotalLength = files.Sum(f => f.Length);
            this.PieceLength = pieceLength;
            this.PieceCount = pieceCount;
            this.IsSingleFile = info.ContainsKey("length");
            this.Comment = root.GetUtf8Text("comment");
            this.CreatedBy = root.GetUtf8Text("created by");
            this.IsPrivate = info.GetInteger("private") == 1;
            var creation = root.GetInteger("creation date");
            if (creation is > 0 and < 253402300800)
            {
                this.CreationDate = DateTimeOffset.FromUnixTimeSeconds(creation.Value);
            }
        }

        /// <summary>Gets the local path the file was loaded from, when known.</summary>
        public string? Path { get; }

        public BencodeDictionary Root { get; }

        public BencodeDictionary Info { get; }

        /// <summary>Gets SHA-1 of the raw "info" bytes exactly as they appear in the file.</summary>
        public ReadOnlySpan<byte> InfoHash => this.infoHash;

        /// <summary>Gets the upper-case hex of <see cref="InfoHash"/>.</summary>
        public string InfoHashHex { get; }

        /// <summary>Gets the "announce" URL, or the first "announce-list" entry, or an empty string.</summary>
        public string Announce { get; }

        /// <summary>Gets all tracker URLs: "announce" first, then every "announce-list" entry, without duplicates.</summary>
        public IReadOnlyList<string> AnnounceList { get; }

        public string Name { get; }

        public IReadOnlyList<TorrentFileEntry> Files { get; }

        public long TotalLength { get; }

        public long PieceLength { get; }

        public int PieceCount { get; }

        public bool IsSingleFile { get; }

        public bool IsPrivate { get; }

        public string? Comment { get; }

        public string? CreatedBy { get; }

        public DateTimeOffset? CreationDate { get; }

        public static TorrentFile Load(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            byte[] data;
            try
            {
                data = File.ReadAllBytes(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new TorrentFormatException($"Cannot read '{path}': {ex.Message}", ex);
            }

            return Parse(data, System.IO.Path.GetFullPath(path));
        }

        public static TorrentFile Parse(ReadOnlySpan<byte> data, string? path = null)
        {
            BencodeDictionary root;
            try
            {
                root = BencodeParser.ParseDictionary(data);
            }
            catch (BencodeException ex)
            {
                throw new TorrentFormatException("Not a valid torrent file: " + ex.Message, ex);
            }

            var info = root.GetDictionary("info") ?? throw new TorrentFormatException("No internal torrent information (\"info\" dictionary).");
            var infoHash = SHA1.HashData(data.Slice(info.RawOffset, info.RawLength));

            var pieceLength = info.GetInteger("piece length") ?? throw new TorrentFormatException("No piece length.");
            var pieces = info.GetString("pieces") ?? throw new TorrentFormatException("No piece hash data.");
            if (pieces.Length % 20 != 0)
            {
                throw new TorrentFormatException("Missing or damaged piece hash codes.");
            }

            var name = info.GetUtf8Text("name.utf-8") ?? info.GetUtf8Text("name") ?? string.Empty;
            var files = ReadFiles(info, name);

            var announce = root.GetUtf8Text("announce")?.Trim() ?? string.Empty;
            var announceList = new List<string>();
            if (announce.Length > 0)
            {
                announceList.Add(announce);
            }

            if (root.GetList("announce-list") is { } tiers)
            {
                foreach (var tier in tiers)
                {
                    if (tier is not BencodeList urls)
                    {
                        continue;
                    }

                    foreach (var url in urls)
                    {
                        if (url is BencodeString s)
                        {
                            var text = s.Utf8Text.Trim();
                            if (text.Length > 0 && !announceList.Contains(text, StringComparer.Ordinal))
                            {
                                announceList.Add(text);
                            }
                        }
                    }
                }
            }

            if (announce.Length == 0 && announceList.Count > 0)
            {
                announce = announceList[0];
            }

            return new TorrentFile(path, root, info, infoHash, announce, announceList, name, files, pieceLength, pieces.Length / 20);
        }

        public byte[] GetInfoHashBytes() => (byte[])this.infoHash.Clone();

        private static List<TorrentFileEntry> ReadFiles(BencodeDictionary info, string name)
        {
            if (info.GetInteger("length") is { } length)
            {
                return [new TorrentFileEntry(name, length)];
            }

            var list = info.GetList("files") ?? throw new TorrentFormatException("The torrent has neither a file length nor a file list.");
            var files = new List<TorrentFileEntry>(list.Count);
            foreach (var item in list)
            {
                if (item is not BencodeDictionary file)
                {
                    throw new TorrentFormatException("Invalid entry in the file list.");
                }

                var fileLength = file.GetInteger("length") ?? throw new TorrentFormatException("A file entry has no length.");
                var components = file.GetList("path.utf-8") ?? file.GetList("path") ?? throw new TorrentFormatException("A file entry has no path.");
                var path = string.Join('/', components.OfType<BencodeString>().Select(c => c.Utf8Text));
                files.Add(new TorrentFileEntry(path, fileLength));
            }

            return files;
        }
    }
}
