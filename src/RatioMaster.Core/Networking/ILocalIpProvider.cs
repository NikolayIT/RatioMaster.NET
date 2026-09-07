using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RatioMaster.Core.Networking;

/// <summary>Finds the machine's primary local IPv4 address for the tracker {localip} placeholder.</summary>
public interface ILocalIpProvider
{
    /// <summary>The best local IPv4 address, or 127.0.0.1 when none is found.</summary>
    ValueTask<string> GetLocalIpAsync(CancellationToken cancellationToken = default);
}
