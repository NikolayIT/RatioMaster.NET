namespace RatioMaster_source
{
    internal class TorrentClient
    {
        // Fields
        private bool _HashUpperCase;
        private string _Headers;
        private string _HttpProtocol;
        private string _key;
        private string _Name;
        private string _PeerID;
        private string _Query;
        private bool _parse = false;
        private string _searchstring = "";
        private long _maxoffset = 25000000;
        private long _startoffset = 10000000;
        private string _processname = "";
        private int _defNumWant = 200;
        // Methods
        internal TorrentClient(string Name)
        {
            _Name = Name;
        }
        // Properties
        internal int DefNumWant
        {
            get
            {
                return _defNumWant;
            }
            set
            {
                _defNumWant = value;
            }
        }
        internal string ProcessName
        {
            get
            {
                return _processname;
            }
            set
            {
                _processname = value;
            }
        }
        internal long StartOffset
        {
            get
            {
                return _startoffset;
            }
            set
            {
                _startoffset = value;
            }
        }
        internal long MaxOffset
        {
            get
            {
                return _maxoffset;
            }
            set
            {
                _maxoffset = value;
            }
        }
        internal string SearchString
        {
            get
            {
                return _searchstring;
            }
            set
            {
                _searchstring = value;
            }
        }
        internal bool Parse
        {
            get
            {
                return _parse;
            }
            set
            {
                _parse = value;
            }
        }
        internal bool HashUpperCase
        {
            get
            {
                return _HashUpperCase;
            }
            set
            {
                _HashUpperCase = value;
            }
        }
        internal string Headers
        {
            get
            {
                return _Headers;
            }
            set
            {
                _Headers = value;
            }
        }
        internal string HttpProtocol
        {
            get
            {
                return _HttpProtocol;
            }
            set
            {
                _HttpProtocol = value;
            }
        }
        internal string Key
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
        internal string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
            }
        }
        internal string PeerID
        {
            get
            {
                return _PeerID;
            }
            set
            {
                _PeerID = value;
            }
        }
        internal string Query
        {
            get
            {
                return _Query;
            }
            set
            {
                _Query = value;
            }
        }
    }
}