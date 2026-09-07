using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RatioMaster.Core.Networking;

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
