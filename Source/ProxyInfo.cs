namespace RatioMaster_source
{
    using BytesRoad.Net.Sockets;

    internal struct ProxyInfo
    {
        public ProxyType ProxyType;
        public string ProxyServer;
        public int ProxyPort;
        public byte[] ProxyUser;
        public byte[] ProxyPassword;
    }
}
