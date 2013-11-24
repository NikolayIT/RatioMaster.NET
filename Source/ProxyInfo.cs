using BytesRoad.Net.Sockets;

namespace RatioMaster_source
{
    internal struct ProxyInfo
    {
        public ProxyType proxyType;
        public string proxyServer;
        public int proxyPort;
        public byte[] proxyUser;
        public byte[] proxyPassword;
    }
}