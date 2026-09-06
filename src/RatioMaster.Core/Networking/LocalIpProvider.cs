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

public sealed class LocalIpProvider : ILocalIpProvider
{
    public const string Fallback = "127.0.0.1";

    public static LocalIpProvider Instance { get; } = new();

    public ValueTask<string> GetLocalIpAsync(CancellationToken cancellationToken = default) => new(Resolve());

    private static string Resolve()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up
                    || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                foreach (var address in nic.GetIPProperties().UnicastAddresses)
                {
                    if (address.Address.AddressFamily == AddressFamily.InterNetwork
                        && !IPAddress.IsLoopback(address.Address))
                    {
                        return address.Address.ToString();
                    }
                }
            }
        }
        catch (NetworkInformationException)
        {
            // Fall through to the loopback fallback.
        }

        return Fallback;
    }
}
