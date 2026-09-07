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

        /// <summary>Transmission's peer id pool, from tr_peerIdInit.</summary>
        private const string Base36 = "0123456789abcdefghijklmnopqrstuvwxyz";
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
            var value = spec.Type switch
            {
                RandomValueKind.TransmissionChecksum => this.GenerateTransmissionSuffix(spec.Length),
                RandomValueKind.HexRange => this.GenerateHexRange(),
                _ => this.GenerateBase(spec.Type, spec.Length),
            };

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
                    RandomValueKind.Random => (char)this.random.Next(1, 256),
                    _ => this.Pick(Alphanumeric),
                });
            }

            return builder.ToString();
        }

        /// <summary>
        /// Builds a Transmission peer id suffix: <paramref name="length"/> - 1 random base-36 characters
        /// followed by the check digit that brings their total to a multiple of 36. This is tr_peerIdInit
        /// (libtransmission/session.c) character for character, so the whole suffix has to be produced at
        /// once rather than one character at a time like the other kinds.
        /// </summary>
        private string GenerateTransmissionSuffix(int length)
        {
            var buffer = new char[length];
            var total = 0;
            for (var i = 0; i < length - 1; i++)
            {
                var value = this.random.Next(Base36.Length);
                total += value;
                buffer[i] = Base36[value];
            }

            var check = total % Base36.Length != 0 ? Base36.Length - (total % Base36.Length) : 0;
            buffer[length - 1] = Base36[check];
            return new string(buffer);
        }

        /// <summary>Transmission's announce key: tr_rand_int(INT_MAX) printed with "%x".</summary>
        private string GenerateHexRange() =>
            Convert.ToString(this.random.Next(int.MaxValue), 16);

        private char Pick(string set) => set[(int)(set.Length * this.random.NextDouble())];
    }
}
