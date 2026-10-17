namespace RatioMaster.Core.Clients
{
    using System.Globalization;

    /// <summary>
    /// The port each emulated client listens on. A real client listens on one port for all of its torrents and keeps it
    /// from one run to the next, so every torrent emulating the same client announces the same port: the first torrent
    /// that needs one picks it, by the client's own rules, and the application saves the book with its settings (issue
    /// #56). Each torrent used to make up a port of its own, so several torrents of one client from one address all
    /// announced different ports, which no real client does.
    /// </summary>
    public sealed class ClientPortBook
    {
        private readonly Lock gate = new();
        private readonly Dictionary<string, string> ports = new(StringComparer.Ordinal);

        /// <summary>Raised, on the thread that picked it, when a client gets its port, so the book can be saved.</summary>
        public event EventHandler? Changed;

        /// <summary>Replaces the book with the ports saved last time, skipping any that are not a port number.</summary>
        public void Load(IReadOnlyDictionary<string, string>? saved)
        {
            lock (this.gate)
            {
                this.ports.Clear();
                foreach (var (client, port) in saved ?? new Dictionary<string, string>())
                {
                    if (IsPort(port))
                    {
                        this.ports[client] = port;
                    }
                }
            }
        }

        /// <summary>Gets the port the client listens on, picking it with <paramref name="pick"/> the first time.</summary>
        public string GetOrPick(string clientName, Func<string> pick)
        {
            ArgumentNullException.ThrowIfNull(clientName);
            ArgumentNullException.ThrowIfNull(pick);

            string port;
            lock (this.gate)
            {
                if (this.ports.TryGetValue(clientName, out var known))
                {
                    return known;
                }

                port = pick();
                this.ports[clientName] = port;
            }

            // Outside the lock: whoever saves the book runs from this event.
            this.Changed?.Invoke(this, EventArgs.Empty);
            return port;
        }

        /// <summary>Gets a copy of the book, to save.</summary>
        public IReadOnlyDictionary<string, string> Snapshot()
        {
            lock (this.gate)
            {
                return new Dictionary<string, string>(this.ports, StringComparer.Ordinal);
            }
        }

        private static bool IsPort(string? text) =>
            int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var port) && port is > 0 and <= 65535;
    }
}
