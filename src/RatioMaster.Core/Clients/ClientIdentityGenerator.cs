namespace RatioMaster.Core.Clients
{
    using System.Globalization;
    using System.Text;

    using RatioMaster.Core.Abstractions;

    /// <summary>
    /// Generates client identity values (peer id, key, port, numwant) exactly as the old
    /// RandomStringGenerator / GenerateIdString did, so emulation is byte-for-byte compatible.
    /// </summary>
    public sealed class ClientIdentityGenerator
    {
        private const string Alphanumeric = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string Digits = "0123456789";
        private const string HexDigits = "0123456789ABCDEF";

        /// <summary>libtorrent's url_random set, minus the apostrophe it omits for buggy trackers.</summary>
        private const string UrlSafe = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_.!~*()";
        private const int MinPort = 1025;
        private const int MaxPort = 65535;

        private readonly IRandomSource random;

        public ClientIdentityGenerator(IRandomSource? random = null)
        {
            this.random = random ?? SystemRandomSource.Instance;
        }

        /// <summary>Generates a fresh identity for a profile.</summary>
        public ClientIdentity Generate(ClientProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);
            return new ClientIdentity
            {
                PeerId = profile.PeerIdPrefix + this.GenerateValue(profile.PeerId),
                Key = this.GenerateValue(profile.Key),
                Port = this.random.Next(MinPort, MaxPort).ToString(CultureInfo.InvariantCulture),
                NumWant = profile.DefaultNumWant.ToString(CultureInfo.InvariantCulture),
                Source = ClientIdentitySource.Generated,
            };
        }

        /// <summary>Generates one random value from a spec (the peer id suffix or the key).</summary>
        public string GenerateValue(RandomValueSpec spec)
        {
            ArgumentNullException.ThrowIfNull(spec);
            var value = this.GenerateBase(spec.Type, spec.Length);
            if (spec.UrlEncode)
            {
                return PercentEncoding.Encode(value, spec.UpperCase);
            }

            return spec.UpperCase ? value.ToUpperInvariant() : value;
        }

        private string GenerateBase(RandomValueKind kind, int length)
        {
            var builder = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                builder.Append(kind switch
                {
                    RandomValueKind.Numeric => this.Pick(Digits),
                    RandomValueKind.Hex => this.Pick(HexDigits),
                    RandomValueKind.UrlSafe => this.Pick(UrlSafe),
                    RandomValueKind.Random => (char)this.random.Next(255),
                    _ => this.Pick(Alphanumeric),
                });
            }

            return builder.ToString();
        }

        private char Pick(string set) => set[(int)(set.Length * this.random.NextDouble())];
    }
}
