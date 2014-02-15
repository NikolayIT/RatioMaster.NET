namespace RatioMaster_source
{
    using System.Net;

    internal class Peer
    {
        internal IPAddress IpAddress;

        internal string PeerID;

        internal ushort Port;

        internal Peer(byte[] ip, short port)
        {
            this.PeerID = string.Empty;
            this.IpAddress = new IPAddress(ip);
            this.Port = (ushort)IPAddress.NetworkToHostOrder(port);
            this.PeerID = string.Empty;
        }

        internal Peer(string ip, string port, string peerId)
        {
            this.PeerID = string.Empty;
            try
            {
                this.IpAddress = IPAddress.Parse(ip);
                this.Port = (ushort)IPAddress.NetworkToHostOrder(short.Parse(port));
                this.PeerID = peerId;
            }
            catch
            {
            }
        }

        public override string ToString()
        {
            if (this.PeerID.Length > 0)
            {
                return this.IpAddress + ":" + this.Port + "(PeerID=" + this.PeerID + ")";
            }

            return this.IpAddress + ":" + this.Port;
        }
    }
}