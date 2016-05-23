namespace RatioMaster_source
{
    public static class TorrentClientFactory
    {
        private static readonly RandomStringGenerator stringGenerator = new RandomStringGenerator();

        public static TorrentClient GetClient(string name)
        {
            TorrentClient client = new TorrentClient(name);
            switch (name)
            {
                #region BitComet
                case "BitComet 1.20":
                    {
                        client.Name = "BitComet 1.20";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("numeric", 5, false, false);
                        client.Headers = "Host: {host}\r\nConnection: close\r\nAccpet: */*\r\nAccept-Encoding: gzip\r\nUser-Agent: BitComet/1.20.3.25\r\nPragma: no-cache\r\nCache-Control: no-cache\r\n";
                        client.PeerID = "-BC0120-" + GenerateIdString("random", 12, true, true);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&natmapped=1&localip={localip}&port_type=wan&uploaded={uploaded}&downloaded={downloaded}&left={left}&numwant={numwant}&compact=1&no_peer_id=1&key={key}{event}";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-BC0120-";
                        client.ProcessName = "BitComet";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                case "BitComet 1.03":
                    {
                        client.Name = "BitComet 1.03";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("numeric", 5, false, false);
                        client.Headers = "Host: {host}\r\nConnection: close\r\nAccpet: */*\r\nAccept-Encoding: gzip\r\nUser-Agent: BitComet/1.3.7.17\r\nPragma: no-cache\r\nCache-Control: no-cache\r\n";
                        client.PeerID = "-BC0103-" + GenerateIdString("random", 12, true, true);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&natmapped=1&localip={localip}&port_type=wan&uploaded={uploaded}&downloaded={downloaded}&left={left}&numwant={numwant}&compact=1&no_peer_id=1&key={key}{event}";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-BC0103-";
                        client.ProcessName = "BitComet";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                case "BitComet 0.98":
                    {
                        client.Name = "BitComet 0.98";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("numeric", 5, false, false);
                        client.Headers = "Accept: */*\r\nAccept-Encoding: gzip\r\nConnection: close\r\nHost: {host}\r\nUser-Agent: Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.0; .NET CLR 1.1.4322)\r\n";
                        client.PeerID = "-BC0098-" + GenerateIdString("random", 12, true, true);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&natmapped=1&localip={localip}&port_type=wan&uploaded={uploaded}&downloaded={downloaded}&left={left}&numwant={numwant}&compact=1&no_peer_id=1&key={key}{event}";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-BC0098-";
                        client.ProcessName = "BitComet";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                case "BitComet 0.96":
                    {
                        client.Name = "BitComet 0.96";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("numeric", 5, false, false);
                        client.Headers = "Accept: */*\r\nAccept-Encoding: gzip\r\nConnection: close\r\nHost: {host}\r\nUser-Agent: Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.0; .NET CLR 1.1.4322)\r\n";
                        client.PeerID = "-BC0096-" + GenerateIdString("random", 12, true, true);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&natmapped=1&localip={localip}&port_type=wan&uploaded={uploaded}&downloaded={downloaded}&left={left}&numwant={numwant}&compact=1&no_peer_id=1&key={key}{event}";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-BC0096-";
                        client.ProcessName = "BitComet";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                case "BitComet 0.93":
                    {
                        client.Name = "BitComet 0.93";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("numeric", 5, false, false);
                        client.Headers = "Accept: */*\r\nAccept-Encoding: gzip\r\nConnection: close\r\nHost: {host}\r\nUser-Agent: Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.0; .NET CLR 1.1.4322)\r\n";
                        client.PeerID = "-BC0093-" + GenerateIdString("random", 12, true, true);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&natmapped=1&localip={localip}&port_type=wan&uploaded={uploaded}&downloaded={downloaded}&left={left}&numwant={numwant}&compact=1&no_peer_id=1&key={key}{event}";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-BC0093-";
                        client.ProcessName = "BitComet";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                case "BitComet 0.92":
                    {
                        client.Name = "BitComet 0.92";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("numeric", 5, false, false);
                        client.Headers = "Accept: */*\r\nAccept-Encoding: gzip\r\nConnection: close\r\nHost: {host}\r\nUser-Agent: Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.0; .NET CLR 1.1.4322)\r\n";
                        client.PeerID = "-BC0092-" + GenerateIdString("random", 12, true, true);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&natmapped=1&localip={localip}&port_type=wan&uploaded={uploaded}&downloaded={downloaded}&left={left}&numwant={numwant}&compact=1&no_peer_id=1&key={key}{event}";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-BC0092-";
                        client.ProcessName = "BitComet";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                #endregion
                #region Vuze
                case "Vuze 4.2.0.8":
                    {
                        client.Name = "Vuze 4.2.0.8";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("alphanumeric", 8, false, false);
                        client.Headers = "User-Agent: Azureus 4.2.0.8;Windows XP;Java 1.6.0_05\r\nConnection: close\r\nAccept-Encoding: gzip\r\nHost: {host}\r\nAccept: text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2\r\n";
                        client.PeerID = "-AZ4208-" + GenerateIdString("alphanumeric", 12, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&supportcrypto=1&port={port}&azudp={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0{event}&numwant={numwant}&no_peer_id=1&compact=1&key={key}&azver=3";
                        client.DefNumWant = 50;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-AZ4208-";
                        client.ProcessName = "azureus";
                        client.StartOffset = 0;
                        client.MaxOffset = 100000000;
                        break;
                    }
                #endregion
                #region Azureus
                case "Azureus 3.1.1.0":
                    {
                        client.Name = "Azureus 3.1.1.0";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("alphanumeric", 8, false, false);
                        client.Headers = "User-Agent: Azureus 3.1.1.0;Windows XP;Java 1.6.0_07\r\nConnection: close\r\nAccept-Encoding: gzip\r\nHost: {host}\r\nAccept: text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2\r\n";
                        client.PeerID = "-AZ3110-" + GenerateIdString("alphanumeric", 12, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&supportcrypto=1&port={port}&azudp={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0{event}&numwant={numwant}&no_peer_id=1&compact=1&key={key}&azver=3";
                        client.DefNumWant = 50;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-AZ3110-";
                        client.ProcessName = "Azureus";
                        client.StartOffset = 0;
                        client.MaxOffset = 100000000;
                        break;
                    }
                case "Azureus 3.0.5.0":
                    {
                        client.Name = "Azureus 3.0.5.0";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("alphanumeric", 8, false, false);
                        client.Headers = "User-Agent: Azureus 3.0.5.0;Windows XP;Java 1.6.0_05\r\nConnection: close\r\nAccept-Encoding: gzip\r\nHost: {host}\r\nAccept: text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2\r\nContent-type: application/x-www-form-urlencoded\r\n";
                        client.PeerID = "-AZ3050-" + GenerateIdString("alphanumeric", 12, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&supportcrypto=1&port={port}&azudp={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0{event}&numwant={numwant}&no_peer_id=1&compact=1&key={key}&azver=3";
                        client.DefNumWant = 50;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-AZ3050-";
                        client.ProcessName = "Azureus";
                        client.StartOffset = 0;
                        client.MaxOffset = 100000000;
                        break;
                    }
                case "Azureus 3.0.4.2":
                    {
                        client.Name = "Azureus 3.0.4.2";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("alphanumeric", 8, false, false);
                        client.Headers = "User-Agent: Azureus 3.0.4.2;Windows XP;Java 1.5.0_07\r\nConnection: close\r\nAccept-Encoding: gzip\r\nHost: {host}\r\nAccept: text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2\r\nContent-type: application/x-www-form-urlencoded\r\n";
                        client.PeerID = "-AZ3042-" + GenerateIdString("alphanumeric", 12, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&supportcrypto=1&port={port}&azudp={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0{event}&numwant={numwant}&no_peer_id=1&compact=1&key={key}&azver=3";
                        client.DefNumWant = 50;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-AZ3042-";
                        client.ProcessName = "Azureus";
                        client.StartOffset = 0;
                        client.MaxOffset = 100000000;
                        break;
                    }
                case "Azureus 3.0.3.4":
                    {
                        client.Name = "Azureus 3.0.3.4";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("alphanumeric", 8, false, false);
                        client.Headers = "User-Agent: Azureus 3.0.3.4;Windows XP;Java 1.6.0_03\r\nConnection: close\r\nAccept-Encoding: gzip\r\nHost: {host}\r\nAccept: text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2\r\n";
                        client.PeerID = "-AZ3034-" + GenerateIdString("alphanumeric", 12, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&supportcrypto=1&port={port}&azudp={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0{event}&numwant={numwant}&no_peer_id=1&compact=1&key={key}&azver=3";
                        client.DefNumWant = 50;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-AZ3034-";
                        client.ProcessName = "Azureus";
                        client.StartOffset = 0;
                        client.MaxOffset = 100000000;
                        break;
                    }
                case "Azureus 3.0.2.2":
                    {
                        client.Name = "Azureus 3.0.2.2";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("alphanumeric", 8, false, false);
                        client.Headers = "User-Agent: Azureus 3.0.2.2;Windows XP;Java 1.6.0_01\r\nConnection: close\r\nAccept-Encoding: gzip\r\nHost: {host}\r\nAccept: text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2\r\n";
                        client.PeerID = "-AZ3022-" + GenerateIdString("alphanumeric", 12, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&supportcrypto=1&port={port}&azudp={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0{event}&numwant={numwant}&no_peer_id=1&compact=1&key={key}&azver=3";
                        client.DefNumWant = 50;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-AZ3022-";
                        client.ProcessName = "Azureus";
                        client.StartOffset = 0;
                        client.MaxOffset = 100000000;
                        break;
                    }
                case "Azureus 2.5.0.4":
                    {
                        client.Name = "Azureus 2.5.0.4";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("alphanumeric", 8, false, false);
                        client.Headers = "User-Agent: Azureus 2.5.0.4;Windows XP;Java 1.5.0_10\r\nConnection: close\r\nAccept-Encoding: gzip\r\nHost: {host}\r\nAccept: text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2\r\nContent-type: application/x-www-form-urlencoded\r\n";
                        client.PeerID = "-AZ2504-" + GenerateIdString("alphanumeric", 12, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&supportcrypto=1&port={port}&azudp={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}{event}&numwant={numwant}&no_peer_id=1&compact=1&key={key}&azver=3";
                        client.DefNumWant = 50;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-AZ2504-";
                        client.ProcessName = "Azureus";
                        client.StartOffset = 0;
                        client.MaxOffset = 100000000;
                        break;
                    }
                #endregion
                #region uTorrent
                case "uTorrent 3.3.2":
                    {
                        client.Name = "uTorrent 3.3.2";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "Host: {host}\r\nUser-Agent: uTorrent/3320\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "-UT3320-%18w" + GenerateIdString("random", 10, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-UT3320-";
                        client.ProcessName = "uTorrent";
                        client.StartOffset = 0;
                        client.MaxOffset = 200000000;
                        break;
                    }
                case "uTorrent 3.3.0":
                    {
                        client.Name = "uTorrent 3.3.0";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "Host: {host}\r\nUser-Agent: uTorrent/3300\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "-UT3300-%b9s" + GenerateIdString("random", 10, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-UT3300-";
                        client.ProcessName = "uTorrent";
                        client.StartOffset = 0;
                        client.MaxOffset = 200000000;
                        break;
                    }
                case "uTorrent 3.2.0":
                    {
                        client.Name = "uTorrent 3.2.0";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "Host: {host}\r\nUser-Agent: uTorrent/3200\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "-UT3200-z8\0." + GenerateIdString("random", 10, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-UT3200-";
                        client.ProcessName = "uTorrent";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                case "uTorrent 2.0.1 (build 19078)":
                    {
                        client.Name = "uTorrent 2.0.1 (build 19078)";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "Host: {host}\r\nUser-Agent: uTorrent/2010(19078)\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "-UT2010-%86J" + GenerateIdString("random", 10, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-UT2010-";
                        client.ProcessName = "uTorrent";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                case "uTorrent 1.8.5 (build 17414)":
                    {
                        client.Name = "uTorrent 1.8.5 (build 17414)";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "Host: {host}\r\nUser-Agent: uTorrent/1850(17414)\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "-UT1850-%06D" + GenerateIdString("random", 10, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-UT1850-";
                        client.ProcessName = "uTorrent";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                case "uTorrent 1.8.1-beta(11903)":
                    {
                        client.Name = "uTorrent 1.8.1-beta(11903)";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "Host: {host}\r\nUser-Agent: uTorrent/181B(11903)\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "-UT181B-%7f." + GenerateIdString("random", 10, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-UT181B-";
                        client.ProcessName = "uTorrent";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                case "uTorrent 1.8.0":
                    {
                        client.Name = "uTorrent 1.8.0";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "Host: {host}\r\nUser-Agent: uTorrent/1800\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "-UT1800-%25." + GenerateIdString("random", 10, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-UT1800-";
                        client.ProcessName = "uTorrent";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                case "uTorrent 1.7.7":
                    {
                        client.Name = "uTorrent 1.7.7";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "Host: {host}\r\nUser-Agent: uTorrent/1770\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "-UT1770-%f3%9f" + GenerateIdString("random", 10, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-UT1770-";
                        client.ProcessName = "uTorrent";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                case "uTorrent 1.7.6":
                    {
                        client.Name = "uTorrent 1.7.6";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "Host: {host}\r\nUser-Agent: uTorrent/1760\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "-UT1760-%b3%9e" + GenerateIdString("random", 10, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-UT1760-";
                        client.ProcessName = "uTorrent";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                case "uTorrent 1.7.5":
                    {
                        client.Name = "uTorrent 1.7.5";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "Host: {host}\r\nUser-Agent: uTorrent/1750\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "-UT1750-%fa%91" + GenerateIdString("random", 10, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-UT1750-";
                        client.ProcessName = "uTorrent";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                case "uTorrent 1.6.1":
                    {
                        client.Name = "uTorrent 1.6.1";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "Host: {host}\r\nUser-Agent: uTorrent/1610\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "-UT1610-%ea%81" + GenerateIdString("random", 10, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-UT1610-";
                        client.ProcessName = "uTorrent";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                case "uTorrent 1.6":
                    {
                        client.Name = "uTorrent 1.6";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "Host: {host}\r\nUser-Agent: uTorrent/1600\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "-UT1600-%d9%81" + GenerateIdString("random", 10, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-UT1600-";
                        client.ProcessName = "uTorrent";
                        client.StartOffset = 0;
                        client.MaxOffset = 60000000;
                        break;
                    }
                #endregion
                #region BitTorrent
                case "BitTorrent 6.0.3 (8642)":
                    {
                        client.Name = "BitTorrent 6.0.3 (8642)";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "Host: {host}\r\nUser-Agent: BitTorrent/6030\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "M6-0-3--" + GenerateIdString("random", 12, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = false;
                        client.SearchString = "";
                        client.ProcessName = "bittorrent";
                        client.StartOffset = 0;
                        client.MaxOffset = 100000000;
                        break;
                    }
                #endregion
                #region Transmission
                case "Transmission 2.82 (14160)":
                    {
                        client.Name = "Transmission 2.82 (14160)";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "User-Agent: Transmission/2.82\r\nHost: {host}\r\nAccept: */*\r\nAccept-Encoding: gzip;q=1.0, deflate, identity\r\n";
                        client.PeerID = "-TR2500-" + GenerateIdString("alphanumeric", 12, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&numwant={numwant}&key={key}&compact=1&supportcrypto=1{event}";
                        client.DefNumWant = 80;
                        // client.Parse = true;
                        client.SearchString = "&peer_id=-TR2500-";
                        client.ProcessName = "Transmission";
                        // client.StartOffset = 0;
                        // client.MaxOffset = 200000000;
                        break;
                    }
                case "Transmission 2.92 (14714)":
                    {
                        client.Name = "Transmission 2.92 (14714)";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "User-Agent: Transmission/2.92\r\nHost: {host}\r\nAccept: */*\r\nAccept-Encoding: gzip;q=1.0, deflate, identity\r\n";
                        client.PeerID = "-TR2920-" + GenerateIdString("alphanumeric", 12, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&numwant={numwant}&key={key}&compact=1&supportcrypto=1{event}";
                        client.DefNumWant = 80;
                        // client.Parse = true;
                        client.SearchString = "&peer_id=-TR2920-";
                        client.ProcessName = "Transmission";
                        // client.StartOffset = 0;
                        // client.MaxOffset = 200000000;
                        break;
                    }
                #endregion
                #region ABC
                case "ABC 3.1":
                    {
                        client.Name = "ABC 3.1";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("alphanumeric", 6, false, false);
                        client.Headers = "Host: {host}\r\nUser-Agent: ABC/ABC-3.1.0\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "A310--" + GenerateIdString("alphanumeric", 14, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&trackerid=48&no_peer_id=1&compact=1{event}&key={key}";
                        client.Parse = true;
                        client.SearchString = "&peer_id=A310--";
                        client.ProcessName = "abc";
                        break;
                    }
                #endregion
                #region BitLord
                case "BitLord 1.1":
                    {
                        client.Name = "BitLord 1.1";
                        client.HttpProtocol = "HTTP/1.0";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("numeric", 4, false, false);
                        client.Headers = "User-Agent: BitTorrent/3.4.2\r\nConnection: close\r\nAccept-Encoding: gzip, deflate\r\nHost: {host}\r\nCache-Control: no-cache\r\n";
                        client.PeerID = "exbc%01%01LORDCz%03%92" + GenerateIdString("random", 6, true, true);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&natmapped=1&uploaded={uploaded}&downloaded={downloaded}&left={left}&numwant=200&compact=1&no_peer_id=1&key={key}{event}";
                        break;
                    }
                #endregion
                #region BTuga
                case "BTuga 2.1.8":
                    {
                        client.Name = "BTuga 2.1.8";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("alphanumeric", 6, false, false);
                        client.Headers = "Host: {host}\r\nAccept-Encoding: gzip\r\nUser-Agent: BTuga/Revolution-2.6\r\n";
                        client.PeerID = "R26---" + GenerateIdString("alphanumeric", 14, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&no_peer_id=1&compact=1{event}&key={key}";
                        break;
                    }
                #endregion
                #region BitTornado
                case "BitTornado 0.3.17":
                    {
                        client.Name = "BitTornado 0.3.17";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("alphanumeric", 6, false, false);
                        client.Headers = "Host: {host}\r\nAccept-Encoding: gzip\r\nUser-Agent: BitTornado/T-0.3.17\r\n";
                        client.PeerID = "T03H-----" + GenerateIdString("alphanumeric", 11, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&no_peer_id=1&compact=1{event}&key={key}";
                        //client.Parse = true;
                        //client.SearchString = "&peer_id=T03H-----";
                        //client.ProcessName = "btdownloadgui";
                        break;
                    }
                #endregion
                #region Burst
                case "Burst 3.1.0b":
                    {
                        client.Name = "Burst 3.1.0b";
                        client.HttpProtocol = "HTTP/1.0";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("hex", 8, false, false);
                        client.Headers = "Host: {host}\r\nAccept-Encoding: gzip\r\nUser-Agent: BitTorrent/brst1.1.3\r\n";
                        client.PeerID = "Mbrst1-1-3" + GenerateIdString("hex", 10, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&key={key}&uploaded={uploaded}&downloaded={downloaded}&left={left}&compact=1{event}";
                        break;
                    }
                #endregion
                #region BitTyrant
                case "BitTyrant 1.1":
                    {
                        client.Name = "BitTyrant 1.1";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("alphanumeric", 8, false, false);
                        client.Headers = "User-Agent: AzureusBitTyrant 2.5.0.0BitTyrant;Windows XP;Java 1.5.0_10\n\rConnection: close\n\rAccept-Encoding: gzip\n\rHost: {host}\n\rAccept: text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2\n\rProxy-Connection: keep-alive\n\rContent-type: application/x-www-form-urlencoded\n\r";
                        client.PeerID = "AZ2500BT" + GenerateIdString("alphanumeric", 12, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&supportcrypto=1&port={port}&azudp={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}{event}&numwant={numwant}&no_peer_id=1&compact=1&key={key}";
                        client.DefNumWant = 50;
                        break;
                    }
                #endregion
                #region BitSpirit
                case "BitSpirit 3.6.0.200":
                    {
                        client.Name = "BitSpirit 3.6.0.200";
                        client.HttpProtocol = "HTTP/1.0";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "User-Agent: BTSP/3602\r\nHost: {host}\r\nAccept_Encoding: gzip\r\nConnection: close";
                        client.PeerID = "%2dSP3602" + GenerateIdString("random", 13, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}{event}&key={key}&compact=1&numwant={numwant}&no_peer_id=1";
                        client.DefNumWant = 200;
                        break;
                    }
                case "BitSpirit 3.1.0.077":
                    {
                        client.Name = "BitSpirit 3.1.0.077";
                        client.HttpProtocol = "HTTP/1.0";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("numeric", 3, false, false);
                        client.Headers = "User-Agent: BitTorrent/4.1.2\r\nHost: {host}\r\nAccept-Encoding: gzip\r\nConnection: close";
                        client.PeerID = "%00%03BS" + GenerateIdString("random", 16, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}{event}&key={key}&compact=1&numwant={numwant}";
                        client.DefNumWant = 200;
                        break;
                    }
                #endregion
                #region Deluge
                case "Deluge 1.2.0":
                    {
                        client.Name = "Deluge 1.2.0";
                        client.HttpProtocol = "HTTP/1.0";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("alphanumeric", 8, false, false);
                        client.Headers = "Host: {host}\r\nUser-Agent: Deluge 1.2.0\r\nConnection: close\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "-DE1200-" + GenerateIdString("alphanumeric", 12, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&event={event}&key={key}&compact=1&numwant={numwant}&supportcrypto=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = false;
                        client.SearchString = "-DE1200-";
                        client.ProcessName = "deluge";
                        client.StartOffset = 0;
                        client.MaxOffset = 100000000;
                        break;
                    }
                case "Deluge 0.5.8.7":
                    {
                        client.Name = "Deluge 0.5.8.7";
                        client.HttpProtocol = "HTTP/1.0";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("alphanumeric", 8, false, true);
                        client.Headers = "Host: {host}\r\nAccept-Encoding: gzip\r\nUser-Agent: Deluge 0.5.8.7\r\n";
                        client.PeerID = "-DE0587-" + GenerateIdString("alphanumeric", 12, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&event={event}&key={key}&compact=1&numwant={numwant}&supportcrypto=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = false;
                        client.SearchString = "-DE0587-";
                        client.ProcessName = "deluge";
                        client.StartOffset = 0;
                        client.MaxOffset = 100000000;
                        break;
                    }
                case "Deluge 0.5.8.6":
                    {
                        client.Name = "Deluge 0.5.8.6";
                        client.HttpProtocol = "HTTP/1.0";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("alphanumeric", 8, false, true);
                        client.Headers = "Host: {host}\r\nAccept-Encoding: gzip\r\nUser-Agent: Deluge 0.5.8.6\r\n";
                        client.PeerID = "-DE0586-" + GenerateIdString("alphanumeric", 12, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&event={event}&key={key}&compact=1&numwant={numwant}&supportcrypto=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = false;
                        client.SearchString = "-DE0586-";
                        client.ProcessName = "deluge";
                        client.StartOffset = 0;
                        client.MaxOffset = 100000000;
                        break;
                    }
                #endregion
                #region KTorrent
                case "KTorrent 2.2.1":
                    {
                        client.Name = "KTorrent 2.2.1";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("numeric", 10, false, false);
                        client.Headers = "User-Agent: ktorrent/2.2.1\r\nAccept: text/html, image/gif, image/jpeg, *; q=.2, */*; q=.2\r\nAccept-Encoding: x-gzip, x-deflate, gzip, deflate\r\nHost: {host}\r\nConnection: Keep-Alive\n\r";
                        client.PeerID = "-KT2210-" + GenerateIdString("numeric", 12, false, false);
                        client.Query = "peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&compact=1&numwant={numwant}&key={key}{event}&info_hash={infohash}";
                        client.DefNumWant = 100;
                        break;
                    }
                #endregion
                #region Gnome BT
                case "Gnome BT 0.0.28-1":
                    {
                        client.Name = "Gnome BT 0.0.28-1";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = true;
                        client.Key = GenerateIdString("alphanumeric", 8, false, false);
                        client.Headers = "Host: {host}\r\nUser-Agent: Python-urllib/2.5\r\nConnection: close\r\nAccept-Encoding: gzip";
                        client.PeerID = "M3-4-2--" + GenerateIdString("alphanumeric", 12, false, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&key={key}&uploaded={uploaded}&downloaded={downloaded}&left={left}&compact=1{event}";
                        client.DefNumWant = 100;
                        break;
                    }
                #endregion
                default:
                    {
                        client.Name = "uTorrent 3.3.2";
                        client.HttpProtocol = "HTTP/1.1";
                        client.HashUpperCase = false;
                        client.Key = GenerateIdString("hex", 8, false, true);
                        client.Headers = "Host: {host}\r\nUser-Agent: uTorrent/3320\r\nAccept-Encoding: gzip\r\n";
                        client.PeerID = "-UT3320-%18w" + GenerateIdString("random", 10, true, false);
                        client.Query = "info_hash={infohash}&peer_id={peerid}&port={port}&uploaded={uploaded}&downloaded={downloaded}&left={left}&corrupt=0&key={key}{event}&numwant={numwant}&compact=1&no_peer_id=1";
                        client.DefNumWant = 200;
                        client.Parse = true;
                        client.SearchString = "&peer_id=-UT3320-";
                        client.ProcessName = "uTorrent";
                        client.StartOffset = 0;
                        client.MaxOffset = 200000000;
                        break;
                    }
            }
            return client;
        }

        private static string GenerateIdString(string keyType, int keyLength, bool urlencoding, bool upperCase = false)
        {
            string text1;
            string text2 = keyType;
            if (text2 != null)
            {
                if (text2 == "alphanumeric")
                {
                    text1 = stringGenerator.Generate(keyLength);
                    goto Label_00A2;
                }
                if (text2 == "numeric")
                {
                    text1 = stringGenerator.Generate(keyLength, "0123456789".ToCharArray());
                    goto Label_00A2;
                }
                if (text2 == "random")
                {
                    text1 = stringGenerator.Generate(keyLength, true);
                    goto Label_00A2;
                }
                if (text2 == "hex")
                {
                    text1 = stringGenerator.Generate(keyLength, "0123456789ABCDEF".ToCharArray());
                    goto Label_00A2;
                }
            }
            text1 = stringGenerator.Generate(keyLength);
        Label_00A2:
            if (urlencoding)
            {
                return stringGenerator.Generate(text1, upperCase);
            }
            if (upperCase)
            {
                text1 = text1.ToUpper();
            }
            return text1;
        }
    }
}
