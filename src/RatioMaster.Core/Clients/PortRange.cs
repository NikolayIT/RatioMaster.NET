namespace RatioMaster.Core.Clients
{
    using System.Globalization;

    /// <summary>An inclusive range of listen ports a client picks its random port from.</summary>
    /// <param name="Min">The lowest port in the range.</param>
    /// <param name="Max">The highest port in the range.</param>
    public sealed record PortRange(int Min, int Max)
    {
        /// <summary>Gets the number of ports in the range.</summary>
        public int Count => this.Max - this.Min + 1;

        /// <summary>
        /// Parses ranges written as "10000-45099,45111-65535". Parts that are not a valid range of ports are left out,
        /// so a list with nothing valid in it comes back empty.
        /// </summary>
        public static IReadOnlyList<PortRange> ParseList(string? text)
        {
            var ranges = new List<PortRange>();
            foreach (var part in (text ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var bounds = part.Split('-', StringSplitOptions.TrimEntries);
                if (bounds.Length == 2
                    && int.TryParse(bounds[0], NumberStyles.None, CultureInfo.InvariantCulture, out var min)
                    && int.TryParse(bounds[1], NumberStyles.None, CultureInfo.InvariantCulture, out var max)
                    && min is >= 1 and <= 65535
                    && max >= min
                    && max <= 65535)
                {
                    ranges.Add(new PortRange(min, max));
                }
            }

            return ranges;
        }
    }
}
