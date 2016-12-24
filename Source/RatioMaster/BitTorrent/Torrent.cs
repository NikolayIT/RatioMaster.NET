namespace BitTorrent
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Security.Cryptography;

    internal class Torrent
    {
        // if the torrent is multiple files, an array of them
        private Collection<TorrentFile> torrentFiles;

        private ValueDictionary data; // contains all the dictionary entries for the data

        private string localTorrentFile; // path of the local torrent file

        private Int64 pieceLength; // length of each piece in bytes

        private Piece[] pieceArray; // an array of the torrent pieces

        private byte[] infohash; // a hash of the pieces

        private int pieces; // number of pieces the file is made up of

        private ulong _totalLength; // total length of all files in torrent

        internal Collection<TorrentFile> PhysicalFiles
        {
            get
            {
                return torrentFiles;
            }
        }

        internal Torrent()
        {
            data = new ValueDictionary();
            localTorrentFile = String.Empty;
            torrentFiles = new Collection<TorrentFile>();
        }

        internal Torrent(string localFilename)
        {
            torrentFiles = new Collection<TorrentFile>();
            OpenTorrent(localFilename);
        }

        internal ulong totalLength
        {
            get
            {
                return _totalLength;
            }
        }

        internal bool SingleFile
        {
            get
            {
                return (((ValueDictionary)data["info"]).Contains("length"));
            }
        }

        internal ValueDictionary Data
        {
            get
            {
                return data;
            }
        }

        internal ValueDictionary Info
        {
            get
            {
                return (ValueDictionary)data["info"];
            }
        }

        internal byte[] InfoHash
        {
            get
            {
                SHA1 sha = new SHA1CryptoServiceProvider();
                return sha.ComputeHash((data["info"]).Encode());
            }
        }

        internal string Name
        {
            get
            {
                // if (data.Contains("info") == false)
                // data.Add("info", new ValueDictionary());
                return BEncode.String(((ValueDictionary)data["info"])["name"]);
            }

            set
            {
                if (data.Contains("info") == false) data.Add("info", new ValueDictionary());
                ((ValueDictionary)data["info"]).SetStringValue("name", value);
            }
        }

        internal string Comment
        {
            get
            {
                return BEncode.String(data["comment"]);
            }

            set
            {
                data.SetStringValue("comment", value);
            }
        }

        internal string Announce
        {
            get
            {
                return BEncode.String(data["announce"]);
            }

            set
            {
                data.SetStringValue("announce", value);
            }
        }

        internal string CreatedBy
        {
            get
            {
                return BEncode.String(data["created by"]);
            }

            set
            {
                data.SetStringValue("created by", value);
            }
        }

        internal bool OpenTorrent(string localFilename)
        {
            data = null; // clear any old data
            bool hasOpened = false;
            localTorrentFile = localFilename;
            data = new ValueDictionary();
            FileStream fs = null;
            BinaryReader r = null;

            try
            {
                fs = File.OpenRead(localFilename);
                r = new BinaryReader(fs);

                // Parse the BEncode .torrent file
                data = (ValueDictionary)BEncode.Parse(r.BaseStream);

                // Check the torrent for its form, initialize this object
                LoadTorrent();

                hasOpened = true;
                r.Close();
                fs.Close();
            }
            catch (IOException)
            {
                hasOpened = false;
            }
            finally
            {
                if (r != null) r.Close();
                if (fs != null) fs.Close();
            }

            return hasOpened;
        }
        
        private void ParsePieceHashes(byte[] hashdata)
        {
            int targetPieces = hashdata.Length / 20;
            pieces = 0; // reset! careful
            pieceArray = null;
            pieceArray = new Piece[targetPieces];
            while (pieces < targetPieces)
            {
                Piece p = new Piece(this, pieces);
                pieceArray[pieces] = p;
                pieces++;
            }
        }

        internal int Pieces
        {
            get
            {
                return pieceArray.Length;
            }
        }
        
        private void LoadTorrent()
        {
            if (data.Contains("announce") == false) throw new IncompleteTorrentData("No tracker URL");

            if (data.Contains("info") == false) throw new IncompleteTorrentData("No internal torrent information");

            ValueDictionary info = (ValueDictionary)data["info"];
            pieceLength = ((ValueNumber)info["piece length"]).Integer;

            if (info.Contains("pieces") == false) throw new IncompleteTorrentData("No piece hash data");

            ValueString pieces = (ValueString)info["pieces"];

            if ((pieces.Length % 20) != 0) throw new IncompleteTorrentData("Missing or damaged piece hash codes");

            // Parse out the hash codes
            ParsePieceHashes(pieces.Bytes);

            // if (info.Contains("length") == true)

            // if (data.Contains("files") == true)
            // throw new Exception("This is not a single file");

            // SingleFile = true;

            // Determine what files are in the torrent
            if (SingleFile) ParseSingleFile();
            else ParseMultipleFiles();
            infohash = InfoHash;
        }

        private void ParseSingleFile()
        {
            ValueDictionary info = (ValueDictionary)data["info"];
            _totalLength = (ulong)((ValueNumber)info["length"]).Integer;
            TorrentFile f = new TorrentFile(((ValueNumber)info["length"]).Integer, ((ValueString)info["name"]).String);
            torrentFiles.Add(f);
        }

        private void ParseMultipleFiles()
        {
            ValueDictionary info = (ValueDictionary)data["info"];
            ValueList files = (ValueList)info["files"];
            torrentFiles = null;
            torrentFiles = new Collection<TorrentFile>();
            foreach (ValueDictionary o in files)
            {
                ValueList components = (ValueList)o["path"];
                bool first = true;
                string path = "";
                foreach (ValueString vs in components)
                {
                    if (!first) path += "/";
                    first = false;
                    path += vs.String;
                }

                _totalLength += (ulong)((ValueNumber)o["length"]).Integer;
                TorrentFile f = new TorrentFile(((ValueNumber)o["length"]).Integer, path);
                torrentFiles.Add(f);
            }
        }
    }
}
