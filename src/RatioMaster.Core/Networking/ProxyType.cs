namespace RatioMaster.Core.Networking;

/// <summary>Proxy protocols the tracker transport can dial through (mirrors the old ProxyType).</summary>
public enum ProxyType
{
    /// <summary>Connect directly to the tracker.</summary>
    None,

    /// <summary>HTTP CONNECT tunnel.</summary>
    HttpConnect,

    /// <summary>SOCKS4 (IP address only).</summary>
    Socks4,

    /// <summary>SOCKS4a (proxy resolves the host name).</summary>
    Socks4a,

    /// <summary>SOCKS5 (with optional username/password auth).</summary>
    Socks5,
}
