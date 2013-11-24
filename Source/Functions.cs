using System;
using System.Net;

namespace RatioMaster_source
{
    internal static class Functions
    {
        internal static string GetIp()
        {
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
            return ips[0].ToString();
        }
    }
}
