using System.Text;
using RatioMaster.Core.Bencode;

namespace RatioMaster.Core.Tracker;

/// <summary>The interpreted content of a tracker scrape response for one torrent.</summary>
public sealed class ScrapeResponse
{
    private ScrapeResponse(string? failureReason, int? complete, int? downloaded, int? incomplete)
    {
        FailureReason = failureReason;
        Complete = complete;
        Downloaded = downloaded;
        Incomplete = incomplete;
    }

    public string? FailureReason { get; }

    /// <summary>Seeders.</summary>
    public int? Complete { get; }

    public int? Downloaded { get; }

    /// <summary>Leechers.</summary>
    public int? Incomplete { get; }

    public bool HasFailure => !string.IsNullOrEmpty(FailureReason);

    public static ScrapeResponse Parse(BencodeDictionary dictionary, ReadOnlySpan<byte> infoHash)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        var failure = dictionary.GetDisplayText("failure reason");
        if (failure is not null)
        {
            return new ScrapeResponse(failure, null, null, null);
        }

        var files = dictionary.GetDictionary("files");
        var key = Encoding.Latin1.GetString(infoHash);
        if (files is null || !files.TryGetValue(key, out var entry) || entry is not BencodeDictionary stats)
        {
            return new ScrapeResponse(null, null, null, null);
        }

        return new ScrapeResponse(
            null,
            (int?)stats.GetInteger("complete"),
            (int?)stats.GetInteger("downloaded"),
            (int?)stats.GetInteger("incomplete"));
    }
}
