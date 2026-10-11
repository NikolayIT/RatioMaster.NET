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

        /// <summary>BitTornado's mapbase64, from BitTornado/__init__.py and BT1/Rerequester.py.</summary>
        private const string MapBase64 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.-";
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
                PeerId = profile.PeerIdPrefix + this.GenerateValue(profile.PeerId, profile.Unreserved),
                Key = this.GenerateValue(profile.Key),
                Port = this.GeneratePort(profile.PortRanges).ToString(CultureInfo.InvariantCulture),
                NumWant = profile.DefaultNumWant.ToString(CultureInfo.InvariantCulture),
                Source = ClientIdentitySource.Generated,
            };
        }

        /// <summary>
        /// Generates one random value from a spec (the peer id suffix or the key). A value the spec says to
        /// percent-encode keeps letters, digits and the client's <paramref name="unreserved"/> characters as they are.
        /// </summary>
        public string GenerateValue(RandomValueSpec spec, string unreserved = "")
        {
            ArgumentNullException.ThrowIfNull(spec);
            var value = spec.Type switch
            {
                RandomValueKind.TransmissionChecksum => this.GenerateTransmissionSuffix(spec.Length),
                RandomValueKind.HexRange => this.GenerateHexRange(spec.Length),
                RandomValueKind.UInt16Decimal => this.random.Next(0, 65536).ToString(CultureInfo.InvariantCulture),
                _ => this.GenerateBase(spec.Type, spec.Length),
            };

            if (spec.UrlEncode)
            {
                return PercentEncoding.Encode(value, spec.UpperCase, unreserved);
            }

            return spec.UpperCase ? value.ToUpperInvariant() : value;
        }

        /// <summary>
        /// Picks a listen port uniformly from the client's ranges, which is what a client that draws from a range and
        /// re-draws the ports it skips ends up with; without ranges, from the old 1025-65534.
        /// </summary>
        private int GeneratePort(IReadOnlyList<PortRange> ranges)
        {
            if (ranges.Count == 0)
            {
                return this.random.Next(MinPort, MaxPort);
            }

            var index = this.random.Next(ranges.Sum(r => r.Count));
            foreach (var range in ranges)
            {
                if (index < range.Count)
                {
                    return range.Min + index;
                }

                index -= range.Count;
            }

            return ranges[^1].Max;
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
                    RandomValueKind.BitTornadoBase64 => this.Pick(MapBase64),
                    RandomValueKind.Random => (char)this.random.Next(1, 256),
                    RandomValueKind.AnyByte => (char)this.random.Next(0, 256),
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

        /// <summary>
        /// A 31-bit key in lower-case hex: Transmission's tr_rand_int(INT_MAX) printed with "%x" when
        /// <paramref name="length"/> is 0, rTorrent's random() zero-padded to 8 digits when it is 8.
        /// </summary>
        private string GenerateHexRange(int length) =>
            Convert.ToString(this.random.Next(int.MaxValue), 16).PadLeft(length, '0');

        private char Pick(string set) => set[(int)(set.Length * this.random.NextDouble())];
    }
}
