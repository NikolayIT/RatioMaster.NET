namespace RatioMaster.Core.Clients;

/// <summary>Where a client identity came from.</summary>
public enum ClientIdentitySource
{
    /// <summary>Generated at random from the profile.</summary>
    Generated,

    /// <summary>Copied from a running instance of the real client via memory scan.</summary>
    CopiedFromProcess,

    /// <summary>Entered by the user.</summary>
    Custom,
}

/// <summary>The concrete peer id, key, port and numwant used for one announce session.</summary>
public sealed record ClientIdentity
{
    public required string PeerId { get; init; }

    public required string Key { get; init; }

    public required string Port { get; init; }

    public required string NumWant { get; init; }

    public ClientIdentitySource Source { get; init; } = ClientIdentitySource.Generated;
}
