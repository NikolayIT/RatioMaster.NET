namespace RatioMaster_source
{
    using System.Net;

    internal class Peer
    {
        internal IPAddress IpAddress;
        internal string Peer_ID;
        internal ushort Port;
        internal Peer(byte[] ip, short port)
        {
            this.Peer_ID = "";
            this.IpAddress = new IPAddress(ip);
            this.Port = (ushort)IPAddress.NetworkToHostOrder(port);
            this.Peer_ID = "";
        }
        internal Peer(string ip, string port, string peer_id)
        {
            this.Peer_ID = "";
            try
            {
                this.IpAddress = IPAddress.Parse(ip);
                this.Port = (ushort)IPAddress.NetworkToHostOrder(short.Parse(port));
                this.Peer_ID = peer_id;
            }
            catch { }
        }
        public override string ToString()
        {
            if (this.Peer_ID.Length > 0)
            {
                return (this.IpAddress + ":" + this.Port + "(PeerID=" + this.Peer_ID + ")");
            }
            return (this.IpAddress + ":" + this.Port);
        }
    }
}