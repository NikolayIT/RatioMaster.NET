namespace RatioMaster_source
{
    public class TorrentClient
    {
        internal TorrentClient(string name)
        {
            this.DefNumWant = 200;
            this.StartOffset = 10000000;
            this.SearchString = string.Empty;
            this.Parse = false;
            this.MaxOffset = 25000000;
            this.ProcessName = string.Empty;
            this.Name = name;
        }

        internal int DefNumWant { get; set; }

        internal string ProcessName { get; set; }

        internal long StartOffset { get; set; }

        internal long MaxOffset { get; set; }

        internal string SearchString { get; set; }

        internal bool Parse { get; set; }

        internal bool HashUpperCase { get; set; }

        internal string Headers { get; set; }

        internal string HttpProtocol { get; set; }

        internal string Key { get; set; }

        internal string Name { get; set; }

        internal string PeerID { get; set; }

        internal string Query { get; set; }
    }
}
