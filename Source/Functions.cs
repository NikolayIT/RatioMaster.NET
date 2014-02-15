namespace RatioMaster_source
{
    using System.Net;

    internal static class Functions
    {
        internal static string GetIp()
        {
            var ips = Dns.GetHostAddresses(Dns.GetHostName());
            return ips[0].ToString();
        }
    }
}
