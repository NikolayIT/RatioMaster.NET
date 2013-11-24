using BitTorrent;
using System;
using System.IO;
using System.IO.Compression;
namespace RatioMaster_source
{
    internal class TrackerResponse
    {
        internal TrackerResponse(MemoryStream responseStream)
        {
            string text2;
            this._headers = "";
            this._body = "";
            this._contentEncoding = "";
            this._charset = "";
            this.RedirectionURL = "";
            Stream stream1 = new MemoryStream();
            StreamReader reader1 = new StreamReader(responseStream);
            responseStream.Position = 0;
            string text1 = this.getNewLineStr(reader1);
            this._headers = "";
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
                            this._contentEncoding = text2.Substring(num1 + 0x12).ToLower();
                        }
                        else
                        {
                            num1 = text2.IndexOf("charset=");
                            if (num1 >= 0)
                            {
                                this._charset = text2.Substring(num1 + 8).ToLower();
                            }
                            else
                            {
                                num1 = text2.IndexOf("Transfer-Encoding: chunked");
                                if (num1 >= 0)
                                {
                                    this._chunkedEncoding = true;
                                }
                            }
                        }
                    }
                }
                this._headers = this._headers + text2 + text1;
            }
            while (text2.Length != 0);
            responseStream.Position = this._headers.Length;
            if (this.response_status_302 && (this.RedirectionURL != ""))
            {
                this.doRedirect = true;
            }
            else
            {
                if (!this._chunkedEncoding)
                {
                    byte[] buffer2 = new byte[responseStream.Length - responseStream.Position];
                    responseStream.Read(buffer2, 0, buffer2.Length);
                    stream1.Write(buffer2, 0, buffer2.Length);
                }
                else
                {
                    string text3 = "";
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
                            num2 = Convert.ToInt32(text3.Split(new char[] { ' ' })[0], 0x10);
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
                this._dict = this.parseBEncodeDict((MemoryStream)stream1);
                stream1.Position = 0;
                StreamReader reader2 = new StreamReader(stream1);
                this._body = reader2.ReadToEnd();
                stream1.Dispose();
                reader2.Dispose();
                reader1.Dispose();
            }
        }

        private string getNewLineStr(StreamReader streamReader)
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

        private ValueDictionary parseBEncodeDict(MemoryStream responseStream)
        {
            ValueDictionary dictionary1 = null;
            if ((this._contentEncoding == "gzip") || (this._contentEncoding == "x-gzip"))
            {
                GZipStream stream1 = new GZipStream(responseStream, CompressionMode.Decompress);
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

        private void saveArrayToFile(byte[] arr, string filename)
        {
            FileStream stream1 = File.OpenWrite(filename);
            stream1.Write(arr, 0, arr.Length);
            stream1.Close();
        }

        private void saveStreamToFile(MemoryStream ms, string filename)
        {
            FileStream stream1 = File.OpenWrite(filename);
            ms.WriteTo(stream1);
            stream1.Close();
        }


        internal string Body
        {
            get
            {
                return this._body;
            }
        }

        internal string Charset
        {
            get
            {
                return this._charset;
            }
        }

        internal string ContentEncoding
        {
            get
            {
                return this._contentEncoding;
            }
        }

        internal ValueDictionary Dict
        {
            get
            {
                return this._dict;
            }
        }

        internal string Headers
        {
            get
            {
                return this._headers;
            }
        }


        private string _body;
        private string _charset;
        private bool _chunkedEncoding;
        private string _contentEncoding;
        private ValueDictionary _dict;
        private string _headers;
        internal bool doRedirect;
        internal string RedirectionURL;
        internal bool response_status_302;
    }
}

