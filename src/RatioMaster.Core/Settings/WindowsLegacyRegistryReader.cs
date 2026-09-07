using System.Runtime.Versioning;
using Microsoft.Win32;
using RatioMaster.Core.Networking;
using RatioMaster.Core.Sessions;

namespace RatioMaster.Core.Settings;

/// <summary>The real registry reader (Windows only).</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsLegacyRegistryReader : ILegacyRegistryReader, IDisposable
{
    public const string KeyPath = @"Software\RatioMaster.NET";

    private readonly RegistryKey? key;

    public WindowsLegacyRegistryReader()
    {
        try
        {
            this.key = Registry.CurrentUser.OpenSubKey(KeyPath);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            this.key = null;
        }
    }

    public bool Exists => this.key is not null;

    public string? GetString(string name) => this.key?.GetValue(name)?.ToString();

    public int? GetInt(string name) => this.key?.GetValue(name) is int value ? value : null;

    public void Dispose() => this.key?.Dispose();
}
