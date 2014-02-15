namespace RatioMaster_source
{
    using System;
    using System.IO;
    using System.Net;

    internal class VersionChecker
    {
        private readonly string userAgent;
        
        internal VersionChecker(string log)
        {
            this.LocalVersion = "0430";
            this.PublicVersion = "0.43";
            this.ReleaseDate = "??-02-2014";
            this.userAgent = "RatioMaster.NET" + "/" + this.LocalVersion + " (" + Environment.OSVersion + "; .NET CLR " + Environment.Version + "; " + Environment.UserName + "." + Environment.ProcessorCount + ")";
            this.Log = log;
        }

        // TODO: Replace with StringBuilder
        public string Log { get; private set; }

        internal string PublicVersion { get; private set; }

        internal string LocalVersion { get; private set; }

        internal string RemoteVersion { get; private set; }

        internal string ReleaseDate { get; private set; }

        internal bool CheckNewVersion()
        {
            try
            {
                bool result = false;
                string local = this.LocalVersion;
                this.Log = this.Log + ("Local Version: " + local + "\n");
                this.Log = this.Log + ("Checking for new version..." + "\n");
                this.RemoteVersion = this.GetVersion("http://ratiomaster.net/vc.php?v=" + this.LocalVersion);
                //// mainForm.txtRemote.Text = remoteVersion;
                if (this.RemoteVersion.Length != 4)
                {
                    this.RemoteVersion = "error";
                    this.Log = this.Log + ("Error checking new version!!!" + "\n" + "\n");
                    return false;
                }

                this.Log = this.Log + ("Remote Version: " + this.RemoteVersion + "\n" + "\n");
                if (string.Compare(this.RemoteVersion, local, StringComparison.Ordinal) > 0)
                {
                    result = true;
                }

                return result;
            }
            catch (Exception exception1)
            {
                this.Log = this.Log + ("Error checking for new version:\n" + exception1.Message + "\n");
                return false;
            }
        }

        private string GetVersion(string url)
        {
            try
            {
                var request1 = (HttpWebRequest)WebRequest.Create(url);
                request1.UserAgent = this.userAgent;
                request1.Timeout = 2500;
                var response1 = (HttpWebResponse)request1.GetResponse();
                var reader1 = new StreamReader(response1.GetResponseStream());
                string data = reader1.ReadToEnd();
                reader1.Close();
                response1.Close();
                return data;
            }
            catch (Exception exception1)
            {
                this.Log = this.Log + ("Error in GetVersion(string url):\n" + exception1.Message + "\n");
            }

            return string.Empty;
        }
    }
}
