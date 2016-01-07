namespace RatioMaster_source
{
    using System.Net;
    using System.Net.Sockets;

    internal static class Functions
    {
        internal static string GetIp()
        {
            foreach (var addr in Dns.GetHostEntry(string.Empty).AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                    return addr.ToString();
            }
            return "127.0.0.1";
        }
    }
}
