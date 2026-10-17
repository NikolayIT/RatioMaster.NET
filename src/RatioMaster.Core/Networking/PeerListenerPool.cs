namespace RatioMaster.Core.Networking
{
    using System.Globalization;

    using RatioMaster.Core.Resources;

    /// <summary>
    /// The ports the torrents listen on. A real client listens on one port for all of its torrents, so every torrent
    /// announced with the same port shares one <see cref="PeerListener"/>, which answers for each of them: the first
    /// torrent to start on a port opens it and the last one to stop closes it. Before, each torrent opened its own
    /// listener, and on a port another torrent already had it could not open one at all.
    /// </summary>
    public sealed class PeerListenerPool
    {
        private readonly Lock gate = new();
        private readonly Dictionary<int, PeerListener> listeners = [];

        /// <summary>Gets the pool the application's torrents share.</summary>
        public static PeerListenerPool Shared { get; } = new();

        /// <summary>
        /// Puts a torrent on the listener for its port, opening the port when no other torrent has. Returns null, and
        /// logs why, when the port cannot be opened, usually because another program has it.
        /// </summary>
        public PeerListenerRegistration? Join(int port, byte[] infoHash, string peerId, Action<string>? log = null)
        {
            ArgumentNullException.ThrowIfNull(infoHash);
            ArgumentNullException.ThrowIfNull(peerId);

            PeerListenerRegistration? registration = null;
            string? error;
            lock (this.gate)
            {
                if (!this.listeners.TryGetValue(port, out var listener))
                {
                    listener = new PeerListener(port);
                    error = listener.Start();
                    if (error is null)
                    {
                        this.listeners[port] = listener;
                    }
                }
                else
                {
                    error = null;
                }

                if (error is null)
                {
                    registration = new PeerListenerRegistration(this, listener, infoHash, peerId, log);
                    listener.Add(registration);
                }
            }

            // Outside the lock: the log callback raises an event that runs whatever is subscribed to it.
            log?.Invoke(error is null
                ? string.Format(CultureInfo.CurrentCulture, Strings.ListenerStarted, port)
                : string.Format(CultureInfo.CurrentCulture, Strings.ListenerStartFailed, port, error));
            return registration;
        }

        /// <summary>Takes a torrent off its port, closing the port when it was the last one there.</summary>
        internal async ValueTask LeaveAsync(PeerListenerRegistration registration)
        {
            PeerListener? closing = null;
            lock (this.gate)
            {
                var listener = registration.Listener;
                listener.Remove(registration);
                if (listener.IsEmpty && this.listeners.TryGetValue(listener.Port, out var current) && ReferenceEquals(current, listener))
                {
                    this.listeners.Remove(listener.Port);

                    // Closed here rather than after the lock, so a torrent that starts on this port next can open it.
                    listener.StopAccepting();
                    closing = listener;
                }
            }

            if (closing is not null)
            {
                await closing.DisposeAsync().ConfigureAwait(false);
            }

            registration.Log?.Invoke(Strings.ListenerClosed);
        }
    }
}
