using System;
using System.IO;

namespace BitTorrent
{
    internal class TorrentFile
	{
        FileInfo fileInfo;

        internal TorrentFile(Int64 len, string apath)  // : this()
		{
            fileInfo = new FileInfo(apath);
		}

        internal Int64 Length 
		{
            get { return fileInfo.Length; }
        }

        internal string Path
		{
            get { return fileInfo.FullName; }
        }

        internal string Name
		{
			get{ return fileInfo.Name; }
		}
	}
}
