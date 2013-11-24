using System;

namespace RatioMaster_source
{
    internal struct TorrentInfo
    {
        // Variables
        private string _tracker;
        private string _hash;
        private Int64 _uploadRate;
        private Int64 _downloadRate;
        private int _interval;
        private Int64 _uploaded;
        private Int64 _downloaded;
        private Int64 _left;
        private Int64 _totalsize;
        private string _filename;
        private string _key;
        private string _port;
        private Random random;
        private string _numberOfPeers;
        private string _peerID;
        private Uri _trackerUri;
        // Main
        internal TorrentInfo(Int64 uploaded, Int64 downloaded)
        {
            _uploaded = uploaded;
            _downloaded = downloaded;
            _tracker = "";
            _hash = "";
            _left = 10000;
            _totalsize = 10000;
            _filename = "";
            _uploadRate = 60 * 1024;
            _downloadRate = 30 * 1024;
            _interval = 1800;
            random = new Random();
            _key = random.Next(1000).ToString();
            _port = random.Next(1025, 65535).ToString();
            _numberOfPeers = "200";
            _peerID = "";
            _trackerUri = null;
        }
        // Functions
        internal long downloaded
        {
            get
            {
                return _downloaded;
            }
            set
            {
                _downloaded = value;
            }
        }
        internal long downloadRate
        {
            get
            {
                return _downloadRate;
            }
            set
            {
                _downloadRate = value;
            }
        }
        internal string filename
        {
            get
            {
                return _filename;
            }
            set
            {
                _filename = value;
            }
        }
        internal string hash
        {
            get
            {
                return _hash;
            }
            set
            {
                _hash = value;
            }
        }
        internal int interval
        {
            get
            {
                return _interval;
            }
            set
            {
                _interval = value;
            }
        }
        internal string key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;
            }
        }
        internal long left
        {
            get
            {
                return _left;
            }
            set
            {
                _left = value;
            }
        }
        internal string numberOfPeers
        {
            get
            {
                return _numberOfPeers;
            }
            set
            {
                _numberOfPeers = value;
            }
        }
        internal string peerID
        {
            get
            {
                return _peerID;
            }
            set
            {
                _peerID = value;
            }
        }
        internal string port
        {
            get
            {
                return _port;
            }
            set
            {
                _port = value;
            }
        }
        internal long totalsize
        {
            get
            {
                return _totalsize;
            }
            set
            {
                _totalsize = value;
            }
        }
        internal string tracker
        {
            get
            {
                return _tracker;
            }
            set
            {
                _tracker = value;
            }
        }
        internal long uploaded
        {
            get
            {
                return _uploaded;
            }
            set
            {
                _uploaded = value;
            }
        }
        internal long uploadRate
        {
            get
            {
                return _uploadRate;
            }
            set
            {
                _uploadRate = value;
            }
        }
        internal Uri trackerUri
        {
            get
            {
                return _trackerUri;
            }
            set
            {
                _trackerUri = value;
            }
        }
    }
}
