namespace RatioMaster_source
{
    using System.Collections.Generic;

    internal class PeerList : List<Peer>
    {
        internal int maxPeersToShow;

        internal int peerCounter;

        internal PeerList()
        {
            this.maxPeersToShow = 5;
        }

        public override string ToString()
        {
            string result = string.Format("({0}) ", this.Count);
            foreach (Peer peer in this)
            {
                if (this.peerCounter < this.maxPeersToShow)
                {
                    result = result + peer + ";";
                }

                this.peerCounter++;
            }

            return result;
        }
    }
}