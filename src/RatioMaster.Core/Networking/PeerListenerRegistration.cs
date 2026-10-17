namespace RatioMaster.Core.Networking
{
    using RatioMaster.Core.Clients;

    /// <summary>
    /// One torrent's place on a shared <see cref="PeerListener"/>, handed out by <see cref="PeerListenerPool.Join"/>.
    /// Disposing it takes the torrent off the port; the last torrent to leave closes the port.
    /// </summary>
    public sealed class PeerListenerRegistration : IAsyncDisposable
    {
        private readonly PeerListenerPool pool;
        private int disposed;

        internal PeerListenerRegistration(PeerListenerPool pool, PeerListener listener, byte[] infoHash, string peerId, Action<string>? log)
        {
            this.pool = pool;
            this.Listener = listener;
            this.InfoHash = infoHash;

            // The identity keeps the peer id the way it goes into the announce URL, with the bytes the client escapes
            // as %xx; a handshake carries the bytes themselves.
            this.PeerId = PercentEncoding.Decode(peerId);
            this.Log = log;
        }

        public int Port => this.Listener.Port;

        internal PeerListener Listener { get; }

        internal byte[] InfoHash { get; }

        internal byte[] PeerId { get; }

        internal Action<string>? Log { get; }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) == 0)
            {
                await this.pool.LeaveAsync(this).ConfigureAwait(false);
            }
        }
    }
}
