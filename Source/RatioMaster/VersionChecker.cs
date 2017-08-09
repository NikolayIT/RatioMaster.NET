namespace RatioMaster_source
{
    using System;
    using System.IO;
    using System.Net;

    public class VersionChecker
    {
        public const string LocalVersion = "0430";
        public const string PublicVersion = "0.43";
        public const string ReleaseDate = "08-01-2016";
        private const string ProgramPageVersion = "https://github.com/NikolayIT/RatioMaster.NET/releases/latest";

        private readonly string userAgent;

        public VersionChecker(string log)
        {
            this.userAgent = "RatioMaster.NET"
                             + $"/{LocalVersion} ({Environment.OSVersion}; .NET CLR {Environment.Version}; {Environment.UserName}.{Environment.ProcessorCount})";
            this.Log = log;
        }

        // TODO: Replace with StringBuilder
        public string Log { get; private set; }

        internal string RemoteVersion { get; private set; }

        public bool CheckNewVersion()
        {
            try
            {
                bool result = false;
                this.Log = this.Log + ("Local Version: " + LocalVersion + "\n");
                this.Log = this.Log + ("Checking for new version..." + "\n");
                this.RemoteVersion = this.GetServerVersionId();
                //// mainForm.txtRemote.Text = remoteVersion;
                if (this.RemoteVersion.Length != 4)
                {
                    this.RemoteVersion = "error";
                    this.Log = this.Log + ("Error checking new version!!!" + "\n" + "\n");
                    return false;
                }

                this.Log = this.Log + ("Remote Version: " + this.RemoteVersion + "\n" + "\n");
                if (string.Compare(this.RemoteVersion, LocalVersion, StringComparison.Ordinal) > 0)
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

        public string GetServerVersionId()
        {
            var url = ProgramPageVersion;
            try
            {
                var request1 = (HttpWebRequest)WebRequest.Create(url);
                request1.UserAgent = this.userAgent;
                request1.Timeout = 2500;
                var response1 = request1.GetResponse();
                var data = response1.ResponseUri.ToString();
                data = data.Substring(data.Length - 4, 4);
                return data;
            }
            catch (Exception exception1)
            {
                this.Log = this.Log + "Error in GetVersion(string url):\n" + exception1.Message + "\n";
            }

            return string.Empty;
        }
    }
}
