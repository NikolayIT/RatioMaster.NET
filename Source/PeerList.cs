namespace RatioMaster_source
{
    using System.Collections.Generic;

    internal class PeerList : List<Peer>
    {
        internal PeerList()
        {
            this.maxPeersToShow = 5;
        }
        public override string ToString()
        {
            string text1;
            text1 = "(" + this.Count + ") ";
            foreach (Peer peer1 in this)
            {
                if (this.peerCounter < this.maxPeersToShow)
                {
                    text1 = text1 + peer1 + ";";
                }
                this.peerCounter++;
            }
            return text1;
        }
        internal int maxPeersToShow;
        internal int peerCounter;
    }
}