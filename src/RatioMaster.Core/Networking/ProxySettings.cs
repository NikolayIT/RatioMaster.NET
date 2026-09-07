using System.Text.Json.Serialization;

namespace RatioMaster.Core.Networking;

/// <summary>Proxy configuration for one announce.</summary>
public sealed record ProxySettings
{
    [JsonConstructor]
    public ProxySettings()
    {
    }

    public static ProxySettings None { get; } = new() { Type = ProxyType.None };

    public ProxyType Type { get; set; } = ProxyType.None;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool IsDirect => this.Type == ProxyType.None;

    public bool HasCredentials => this.Username.Length > 0 || this.Password.Length > 0;
}
