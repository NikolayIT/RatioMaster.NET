namespace BitTorrent
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Security.Cryptography;

    internal class IncompleteTorrentData : TorrentException
    {
        internal IncompleteTorrentData(string message)
            : base(message)
        {
        }
    }
}