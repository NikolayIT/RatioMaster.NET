using System.Runtime.Versioning;
using Microsoft.Win32;
using RatioMaster.Core.Networking;
using RatioMaster.Core.Sessions;

namespace RatioMaster.Core.Settings;

/// <summary>Reads the values RatioMaster.NET 0.43 stored under HKCU\Software\RatioMaster.NET.</summary>
public interface ILegacyRegistryReader
{
    bool Exists { get; }

    string? GetString(string name);

    int? GetInt(string name);
}
