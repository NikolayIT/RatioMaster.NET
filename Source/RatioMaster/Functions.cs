namespace RatioMaster_source
{
    using System.Net;
    using System.Net.Sockets;

    internal static class Functions
    {
        internal static string GetIp()
        {
            foreach (var address in Dns.GetHostEntry(string.Empty).AddressList)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return address.ToString();
                }
            }

            return "127.0.0.1";
        }
    }
}
