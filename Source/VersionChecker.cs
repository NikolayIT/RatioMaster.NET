using System;
using System.Net;
using System.IO;
namespace RatioMaster_source
{
    internal class VersionChecker
    {
        // Fields
        internal string Log;
        private readonly string publicVer;
        private readonly string version;
        private string remoteVersion;
        private readonly string releaseDate;
        private readonly string _userAgent;
        // Methods
        internal VersionChecker(string log)
        {
            version = "0430";
            publicVer = "0.43";
            releaseDate = "??-02-2014";
            _userAgent = "RatioMaster.NET" + "/" + version + " (" + Environment.OSVersion + "; .NET CLR " + Environment.Version + "; " + Environment.UserName + "." + Environment.ProcessorCount + ")";
            Log = log;
        }
        internal bool CheckNewVersion()
        {
            try
            {
                bool ret = false;
                string local = LocalVersion;
                Log += "Local Version: " + local + "\n";
                Log += "Checking for new version..." + "\n";
                remoteVersion = GetVersion("http://ratiomaster.net/vc.php?v=" + version);
                // mainForm.txtRemote.Text = remoteVersion;
                if (remoteVersion.Length != 4)
                {
                    remoteVersion = "error";
                    Log += "Error checking new version!!!" + "\n" + "\n";
                    return false;
                }
                Log += "Remote Version: " + remoteVersion + "\n" + "\n";
                if (remoteVersion.CompareTo(local) > 0) ret = true;
                return ret;
            }
            catch (Exception exception1)
            {
                Log += "Error checking for new version:\n" + exception1.Message + "\n";
                return false;
            }
        }
        // Properties
        internal string PublicVersion
        {
            get
            {
                return publicVer;
            }
        }
        internal string LocalVersion
        {
            get
            {
                return version;
            }
        }
        internal string RemoteVersion
        {
            get
            {
                return remoteVersion;
            }
        }
        internal string ReleaseDate
        {
            get
            {
                return releaseDate;
            }
        }
        private string GetVersion(string url)
        {
            try
            {
                HttpWebRequest request1 = (HttpWebRequest)WebRequest.Create(url);
                request1.UserAgent = _userAgent;
                request1.Timeout = 2500;
                HttpWebResponse response1 = (HttpWebResponse)request1.GetResponse();
                StreamReader reader1 = new StreamReader(response1.GetResponseStream());
                string data = reader1.ReadToEnd();
                reader1.Close();
                response1.Close();
                return data;
            }
            catch (Exception exception1)
            {
                Log+="Error in GetVersion(string url):\n" + exception1.Message + "\n";
            }
            return "";
        }
    }
}
