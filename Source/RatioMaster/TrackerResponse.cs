namespace RatioMaster_source
{
    using System;
    using System.IO;
    using System.IO.Compression;

    using BitTorrent;

    internal class TrackerResponse
    {
        private bool chunkedEncoding;

        internal bool doRedirect;

        internal string RedirectionURL;

        internal bool response_status_302;

        internal TrackerResponse(MemoryStream responseStream)
        {
            string text2;
            this.Headers = string.Empty;
            this.Body = string.Empty;
            this.ContentEncoding = string.Empty;
            this.Charset = string.Empty;
            this.RedirectionURL = string.Empty;
            var stream1 = new MemoryStream();
            var reader1 = new StreamReader(responseStream);
            responseStream.Position = 0;
            string text1 = this.GetNewLineStr(reader1);
            this.Headers = string.Empty;
            do
            {
                text2 = reader1.ReadLine();
                int num1 = text2.IndexOf("302 Found");
                if (num1 >= 0)
                {
                    this.response_status_302 = true;
                }
                else
                {
                    num1 = text2.IndexOf("Location: ");
                    if (num1 >= 0)
                    {
                        this.RedirectionURL = text2.Substring(num1 + 10);
                    }
                    else
                    {
                        num1 = text2.IndexOf("Content-Encoding: ");
                        if (num1 >= 0)
                        {
                            this.ContentEncoding = text2.Substring(num1 + 0x12).ToLower();
                        }
                        else
                        {
                            num1 = text2.IndexOf("charset=");
                            if (num1 >= 0)
                            {
                                this.Charset = text2.Substring(num1 + 8).ToLower();
                            }
                            else
                            {
                                num1 = text2.IndexOf("Transfer-Encoding: chunked");
                                if (num1 >= 0)
                                {
                                    this.chunkedEncoding = true;
                                }
                            }
                        }
                    }
                }

                this.Headers = this.Headers + text2 + text1;
            }
            while (text2.Length != 0);
            responseStream.Position = this.Headers.Length;
            if (this.response_status_302 && (this.RedirectionURL != string.Empty))
            {
                this.doRedirect = true;
            }
            else
            {
                if (!this.chunkedEncoding)
                {
                    byte[] buffer2 = new byte[responseStream.Length - responseStream.Position];
                    responseStream.Read(buffer2, 0, buffer2.Length);
                    stream1.Write(buffer2, 0, buffer2.Length);
                }
                else
                {
                    string text3 = string.Empty;
                    text3 = reader1.ReadLine();
                    int num2 = Convert.ToInt32(text3.Split(new char[] { ' ' })[0], 0x10);
                    while (num2 > 0)
                    {
                        byte[] buffer1 = new byte[num2];
                        responseStream.Position = (responseStream.Position + text3.Length) + text1.Length;
                        responseStream.Read(buffer1, 0, num2);
                        stream1.Write(buffer1, 0, num2);
                        reader1.ReadLine();
                        text3 = reader1.ReadLine();
                        try
                        {
                            num2 = Convert.ToInt32(text3.Split(new[] { ' ' })[0], 0x10);
                            continue;
                        }
                        catch (Exception)
                        {
                            num2 = 0;
                            continue;
                        }
                    }
                }

                stream1.Position = 0;
                this.Dict = this.ParseBEncodeDict(stream1);
                stream1.Position = 0;
                var reader2 = new StreamReader(stream1);
                this.Body = reader2.ReadToEnd();
                stream1.Dispose();
                reader2.Dispose();
                reader1.Dispose();
            }
        }

        internal string Body { get; private set; }

        internal string Charset { get; private set; }

        internal string ContentEncoding { get; private set; }

        internal ValueDictionary Dict { get; private set; }

        internal string Headers { get; private set; }

        private string GetNewLineStr(StreamReader streamReader)
        {
            char ch1;
            long num1 = streamReader.BaseStream.Position;
            string text1 = "\r";
            do
            {
                ch1 = (char)((ushort)streamReader.BaseStream.ReadByte());
            }
            while ((ch1 != '\r') && (ch1 != '\n'));
            if ((ch1 == '\r') && (((ushort)streamReader.BaseStream.ReadByte()) == 10))
            {
                text1 = "\r\n";
            }

            streamReader.BaseStream.Position = num1;
            return text1;
        }

        private ValueDictionary ParseBEncodeDict(MemoryStream responseStream)
        {
            ValueDictionary dictionary1 = null;
            if ((this.ContentEncoding == "gzip") || (this.ContentEncoding == "x-gzip"))
            {
                var stream1 = new GZipStream(responseStream, CompressionMode.Decompress);
                try
                {
                    return (ValueDictionary)BEncode.Parse(stream1);
                }
                catch (Exception)
                {
                }
            }

            try
            {
                dictionary1 = (ValueDictionary)BEncode.Parse(responseStream);
            }
            catch (Exception exception1)
            {
                Console.Write(exception1.StackTrace);
            }

            return dictionary1;
        }
    }
}
