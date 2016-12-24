// =============================================================
// BytesRoad.NetSuit : A free network library for .NET platform 
// =============================================================
//
// Copyright (C) 2004-2005 BytesRoad Software
// 
// Project Info: http://www.bytesroad.com/NetSuit/
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// ========================================================================== 
// Changed by: NRPG
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using BytesRoad.Net.Sockets.Advanced;

namespace BytesRoad.Net.Sockets
{
    /// <summary>
    /// Summary description for Socket_HttpConnect.
    /// </summary>
    internal class Socket_HttpConnect : SocketBase
    {
        #region Async classes
        class Receive_SO : AsyncResultBase
        {
            int _read = 0;

            internal Receive_SO(AsyncCallback cb, object state) : base(cb, state)
            {
            }

            internal int Read
            {
                get { return _read; }
                set { _read = value; }
            }
        }

        class Connect_SO : AsyncResultBase
        {
            bool _useCredentials;
            string _hostName;
            int _hostPort;

            internal Connect_SO(
                string hostName,
                int hostPort,
                bool useCredentials, 
                AsyncCallback cb, 
                object state) : base(cb, state)
            {
                _useCredentials = useCredentials;
                _hostName = hostName;
                _hostPort = hostPort;
            }

            internal bool UseCredentials
            {
                get { return _useCredentials; }
                set { _useCredentials = value; }
            }

            internal string HostName
            {
                get { return _hostName; }
            }

            internal int HostPort
            {
                get { return _hostPort; }
            }
        }

        class ReadReply_SO : AsyncResultBase
        {
            byte[] _buffer = new byte[512];
            ByteVector _reply = new ByteVector();

            internal ReadReply_SO(
                AsyncCallback cb, 
                object state) : base(cb, state)
            {
            }

            internal byte[] Buffer
            {
                get { return _buffer; }
            }

            internal ByteVector Reply
            {
                get { return _reply; }
            }
        }
        #endregion

        ByteVector _recvBuffer = new ByteVector();
        int _maxReplySize = 4096;

        EndPoint _remoteEndPoint = null;

        Regex _replyRegEx = new Regex(
            @"^HTTP/\d+\.\d+ (?<code>\d\d\d) ?(?<reason>[^\r\n]*)?(\r)?\n",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);


        internal Socket_HttpConnect(
            string proxyServer,
            int proxyPort,
            byte[] proxyUser, 
            byte[] proxyPassword)
            : base (proxyServer, proxyPort, proxyUser, proxyPassword)
        {
        }

        #region Attributes

        override internal int Available
        {
            get
            {
                int len = _recvBuffer.Size;
                if(len > 0)
                    return len;
                return _socket.Available;
            }
        }

        override internal ProxyType ProxyType 
        { 
            get { return ProxyType.HttpConnect; } 
        }
        
        override internal EndPoint LocalEndPoint 
        { 
            get { return null; } 
        } 

        override internal EndPoint RemoteEndPoint 
        { 
            get { return _remoteEndPoint; } 
        }

        #endregion

        #region Helpers
        int AnalyzeReply(ByteVector reply, out string reason)
        {
            if(0 == reply.Size)
                throw new SocketException(SockErrors.WSAECONNREFUSED);

                // throw new ProxyErrorException("Web proxy close the connection.");

            // reply should be in following form:
            // "HTTP/x.x xxx reason(\r)?\n"
            string replyStr = Encoding.ASCII.GetString(reply.Data, 0, reply.Size);
            Match m = _replyRegEx.Match(replyStr);

            if((reply.Size < 14) || (m.Groups.Count != 4))
                throw new ProtocolViolationException("Web proxy reply is incorrect.");

            int code = int.Parse(m.Groups["code"].Value);
            reason = m.Groups["reason"].Value;

            return code;
        }

        void ThrowUnsupportException(string method)
        {
            string msg = string.Format("'{0}' command is not possible with Web proxy.", method);
            throw new InvalidOperationException(msg);
        }

        string GetBasicCredentials()
        {
            int length = 1; // ':'
            if(null != _proxyUser)
                length += _proxyUser.Length;

            if(null != _proxyPassword)
                length += _proxyPassword.Length;

            byte[] cmd = new byte[length];
            length = 0;

            if(null != _proxyUser)
            {
                Array.Copy(_proxyUser, 0, cmd, 0, _proxyUser.Length);
                length += _proxyUser.Length;
            }

            cmd[length++] = (byte)':';
            if(null != _proxyPassword)
            {
                Array.Copy(_proxyPassword, 0, cmd, length, _proxyPassword.Length);
                length += _proxyPassword.Length;
            }

            return Convert.ToBase64String(cmd);
        }

        byte[] GetConnectCmd(string hostName, int hostPort, bool useCredentials)
        {
            string cmd = string.Format("CONNECT {0}:{1} HTTP/1.1\r\n", hostName, hostPort);
            cmd += string.Format("Host: {0}:{1}\r\n", hostName, hostPort);
            if(useCredentials)
            {
                string credentials = GetBasicCredentials();
                cmd += "Authorization: basic " + credentials + "\r\n";
                cmd += "Proxy-Authorization: basic " + credentials + "\r\n";
            }

            cmd += "\r\n";
            return Encoding.ASCII.GetBytes(cmd);
        }

        int FetchBufferData(byte[] buffer, int offset, int size)
        {
            int length = _recvBuffer.Size;
            if(length <= 0)
                return 0;

            if(offset < 0)
                offset = 0;
            
            if(size < 0)
                size = buffer.Length;
            
            int num = (length > size) ? size : length;
            Array.Copy(_recvBuffer.Data, 0, buffer, offset, num);
            _recvBuffer.CutHead(num);
            return num;
        }

        void PutBufferData(byte[] buffer, int offset, int size)
        {
            if(0 != _recvBuffer.Size)
                throw new InvalidOperationException("PutBufferData: buffer is not empty.");

            _recvBuffer.Add(buffer, offset, size);
        }

        int FindReplyEnd(byte[] buf, int size)
        {
            const byte CR = 13;
            const byte LF = 10;

            // the end of the buffer can't be there
            if(size < 2)
                return -1;

            // here we need to find the end of http response
            // it identified either by <CRLF><CRLF> or by <LF><LF>
            for(int i = 0;i < size;i++)
            {
                bool checkLong = false;
                bool checkShort = false;
                int stillCheck = size - i;

                if(stillCheck >= 2)
                {
                    checkShort = true;
                    if(stillCheck >= 4)
                        checkLong = true;
                }

                if(checkLong)
                {
                    if((buf[i] == CR) &&
                        (buf[i + 1] == LF) &&
                        (buf[i + 2] == CR) &&
                        (buf[i + 3] == LF))
                    {
                        return i + 4;
                    }
                }
                else if(checkShort)
                {
                    if((buf[i] == LF) &&
                        (buf[i + 1] == LF))
                    {
                        return i + 2;
                    }
                }
                else
                    break;
            }

            return -1;
        }

        #endregion

        #region ReadReply functions
        ByteVector ReadReply()
        {
            ByteVector reply = new ByteVector();
            byte[] buf = new byte[512];
            while(true)
            {
                int num = Receive(buf);
                if(0 == num)
                    break;

                reply.Add(buf, 0, num);

                // handle the end of reply
                int afterEndPos = FindReplyEnd(reply.Data, reply.Size);
                if(afterEndPos > 0)
                {
                    if(afterEndPos < num) // read after reply finished?
                    {
                        // put data back into the buffer for further
                        // processing in receive functions
                        PutBufferData(buf, afterEndPos, num - afterEndPos);
                        reply.CutTail(num - afterEndPos);
                    }

                    break;
                }

                if(reply.Size > _maxReplySize)
                    throw new ProtocolViolationException("Web proxy reply exceed maximum length.");
            }

            return reply;
        }

        IAsyncResult BeginReadReply(AsyncCallback cb, object state)
        {
            ReadReply_SO stateObj = new ReadReply_SO(cb, state);
            BeginReceive(
                stateObj.Buffer, 
                0, 
                stateObj.Buffer.Length,
                new AsyncCallback(ReadReply_Recv_End),
                stateObj);
            return stateObj;
        }

        void ReadReply_Recv_End(IAsyncResult ar)
        {
            ReadReply_SO stateObj = (ReadReply_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                int num = EndReceive(ar);
                if(0 == num)
                {
                    stateObj.SetCompleted();
                }
                else
                {
                    stateObj.Reply.Add(stateObj.Buffer, 0, num);

                    // handle the end of reply
                    int afterEndPos = FindReplyEnd(
                        stateObj.Reply.Data, 
                        stateObj.Reply.Size);

                    if(afterEndPos > 0)
                    {
                        if(afterEndPos < num) // read after reply finished?
                        {
                            // put data back into the buffer for further
                            // processing in receive functions
                            PutBufferData(stateObj.Buffer, afterEndPos, num - afterEndPos);
                            stateObj.Reply.CutTail(num - afterEndPos);
                        }

                        stateObj.SetCompleted();
                    }
                    else
                    {
                        if(stateObj.Reply.Size > _maxReplySize)
                            throw new ProtocolViolationException("Web proxy reply exceed maximum length.");

                        BeginReceive(
                            stateObj.Buffer, 
                            0, 
                            stateObj.Buffer.Length,
                            new AsyncCallback(ReadReply_Recv_End),
                            stateObj);
                    }
                }
            }
            catch(Exception e)
            {
                stateObj.Exception = e;
                stateObj.SetCompleted();
            }
        }
        
        ByteVector EndReadReply(IAsyncResult ar)
        {
            VerifyAsyncResult(ar, typeof(ReadReply_SO));
            HandleAsyncEnd(ar, false);
            return ((ReadReply_SO)ar).Reply;
        }
        #endregion

        #region Connect functions (overriden)
        override internal void Connect(string hostName, int hostPort)
        {
            CheckDisposed();

            SetProgress(true);
            try
            {
                if(null == hostName)
                    throw new ArgumentNullException("hostName", "The value cannot be null.");

                if(hostPort < IPEndPoint.MinPort || hostPort > IPEndPoint.MaxPort)
                    throw new ArgumentOutOfRangeException("hostPort", "Value, specified for the port is out of the valid range."); 

                //------------------------------------
                // Get end point for the proxy server
                IPHostEntry  proxyEntry = GetHostByName(_proxyServer);
                if(null == proxyEntry)
                    throw new SocketException(SockErrors.WSAHOST_NOT_FOUND);

                    // throw new HostNotFoundException("Unable to resolve proxy name.");

                IPEndPoint proxyEndPoint;
                if (_proxyServer.Equals("127.0.0.1"))
                    proxyEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), _proxyPort);
                else
                    proxyEndPoint = ConstructEndPoint(proxyEntry, _proxyPort);

                //------------------------------------------
                // Connect to proxy server
                _socket.Connect(proxyEndPoint);

                bool useCredentials = PreAuthenticate;
                while(true)
                {
                    //------------------------------------------
                    // Send CONNECT command
                    byte[] cmd = GetConnectCmd(hostName, hostPort, useCredentials);
                    NStream.Write(cmd, 0, cmd.Length);

                    //-----------------------------------------
                    // Read the reply
                    ByteVector reply = ReadReply();

                    //------------------------------------------
                    // Analyze reply
                    string reason = null;
                    int code = AnalyzeReply(reply, out reason);

                    //------------------------------------------
                    // is good return code?
                    if(code >= 200 && code <= 299)
                        return;

                    //------------------------------------------
                    // If Proxy Authentication Required
                    // but we do not issued it, then try again
                    if((407 == code) && 
                        !useCredentials && 
                        (_proxyUser != null))
                    {
                        useCredentials = true;
                        continue;
                    }

                    // string msg = string.Format("Connection refused by web proxy: {0} ({1}).", reason, code);
                    // throw new ProxyErrorException(msg);
                    throw new SocketException(SockErrors.WSAECONNREFUSED);
                }
            }
            finally
            {
                SetProgress(false);
            }
        }

        override internal void Connect(EndPoint remoteEP)
        {
            if(null == remoteEP)
                throw new ArgumentNullException("remoteEP", "The value cannot be null.");

            IPEndPoint ipEP = (IPEndPoint)remoteEP;
            string hostName = ipEP.Address.ToString();
            int port = ipEP.Port;
            Connect(hostName, port);
        }

        override internal IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback callback, object state)
        {
            if(null == remoteEP)
                throw new ArgumentNullException("remoteEP", "The value cannot be null.");

            IPEndPoint ipEP = (IPEndPoint)remoteEP;
            string hostName = ipEP.Address.ToString();
            int port = ipEP.Port;
            return BeginConnect(hostName, port, callback, state);
        }

        internal override IAsyncResult BeginConnect(
            string hostName, 
            int hostPort, 
            AsyncCallback callback, 
            object state)
        {
            Connect_SO stateObj = null;
            CheckDisposed();

            if(null == hostName)
                throw new ArgumentNullException("hostName", "The value cannot be null.");

            if(hostPort < IPEndPoint.MinPort || hostPort > IPEndPoint.MaxPort)
                throw new ArgumentOutOfRangeException("hostPort", "Value, specified for the port is out of the valid range."); 

            SetProgress(true);
            try
            {
                stateObj = new Connect_SO(
                    hostName,
                    hostPort,
                    PreAuthenticate,
                    callback,
                    state);

                //------------------------------------
                // Get end point for the proxy server
                BeginGetHostByName(
                    _proxyServer, 
                    new AsyncCallback(Connect_GetPrxHost_End),
                    stateObj);
            }
            catch(Exception e)
            {
                SetProgress(false);
                throw e;
            }

            return stateObj;
        }

        void Connect_GetPrxHost_End(IAsyncResult ar)
        {
            Connect_SO stateObj = (Connect_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                IPHostEntry  proxyEntry = EndGetHostByName(ar);
                if(null == proxyEntry)
                    throw new SocketException(SockErrors.WSAHOST_NOT_FOUND);

                    // throw new HostNotFoundException("Unable to resolve proxy name.");

                IPEndPoint proxyEndPoint = ConstructEndPoint(proxyEntry, _proxyPort);

                //------------------------------------------
                // Connect to proxy server
                _socket.BeginConnect(
                    proxyEndPoint, 
                    new AsyncCallback(Connect_Connect_End),
                    stateObj);
            }
            catch(Exception e)
            {
                stateObj.Exception = e;
                stateObj.SetCompleted();
            }
        }

        void Connect_Connect_End(IAsyncResult ar)
        {
            Connect_SO stateObj = (Connect_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                _socket.EndConnect(ar);

                //------------------------------------------
                // Send CONNECT command
                byte[] cmd = GetConnectCmd(
                    stateObj.HostName, 
                    stateObj.HostPort, 
                    stateObj.UseCredentials);

                NStream.BeginWrite(cmd, 0, cmd.Length,
                    new AsyncCallback(Connect_Write_End),
                    stateObj);
            }
            catch(Exception e)
            {
                stateObj.Exception = e;
                stateObj.SetCompleted();
            }
        }

        void Connect_Write_End(IAsyncResult ar)
        {
            Connect_SO stateObj = (Connect_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                NStream.EndWrite(ar);

                //-----------------------------------------
                // Read the reply
                BeginReadReply(new AsyncCallback(Connect_ReadReply_End), stateObj);
            }
            catch(Exception e)
            {
                stateObj.Exception = e;
                stateObj.SetCompleted();
            }
        }

        void Connect_ReadReply_End(IAsyncResult ar)
        {
            Connect_SO stateObj = (Connect_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                ByteVector reply = EndReadReply(ar);

                //------------------------------------------
                // Analyze reply
                string reason = null;
                int code = AnalyzeReply(reply, out reason);

                //------------------------------------------
                // is good return code?
                if(code >= 200 && code <= 299)
                {
                    stateObj.SetCompleted();
                }
                else if((407 == code) && 
                    !(stateObj.UseCredentials) && 
                    (_proxyUser != null))
                {
                    //------------------------------------------
                    // If Proxy Authentication Required
                    // but we do not issued it, then try again

                    stateObj.UseCredentials = true;

                    //------------------------------------------
                    // Send CONNECT command
                    byte[] cmd = GetConnectCmd(
                        stateObj.HostName, 
                        stateObj.HostPort, 
                        stateObj.UseCredentials);

                    NStream.BeginWrite(cmd, 0, cmd.Length,
                        new AsyncCallback(Connect_Write_End),
                        stateObj);
                }
                else
                {
                    // string msg = string.Format("Connection refused by web proxy: {0} ({1}).", reason, code);
                    // throw new ProxyErrorException(msg);
                    throw new SocketException(SockErrors.WSAECONNREFUSED);
                }
            }
            catch(Exception e)
            {
                stateObj.Exception = e;
                stateObj.SetCompleted();
            }
        }

        override internal void EndConnect(IAsyncResult asyncResult)
        {
            VerifyAsyncResult(asyncResult, typeof(Connect_SO), "EndConnect");
            HandleAsyncEnd(asyncResult, true);
        }
        #endregion

        #region Receive functions (override)

        override internal int Receive(byte[] buffer)
        {
            CheckDisposed();

            int num = FetchBufferData(buffer, -1, -1);
            if(num > 0)
                return num;
            
            return base.Receive(buffer);
        }

        override internal int Receive(byte[] buffer, int size)
        {
            CheckDisposed();

            int num = FetchBufferData(buffer, -1, size);
            if(num > 0)
                return num;

            return base.Receive(buffer, size);
        }

        override internal int Receive(byte[] buffer, int offset, int size)
        {
            CheckDisposed();

            int num = FetchBufferData(buffer, offset, size);
            if(num > 0)
                return num;

            return base.Receive(buffer, offset, size);
        }

        override internal IAsyncResult BeginReceive(
            byte[] buffer,
            int offset,
            int size,
            AsyncCallback callback,
            object state
            )
        {
            Receive_SO stateObj = null;

            CheckDisposed();
            stateObj = new Receive_SO(callback, state);
            if((stateObj.Read = FetchBufferData(buffer, offset, size)) > 0)
            {
                stateObj.SetCompleted();
            }
            else
            {
                _socket.BeginReceive(
                    buffer, 
                    offset, 
                    size,
                    SocketFlags.None,
                    new AsyncCallback(Receive_End), 
                    stateObj);
            }

            return stateObj;
        }

        void Receive_End(IAsyncResult ar)
        {
            Receive_SO stateObj = (Receive_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                stateObj.Read = _socket.EndReceive(ar);
            }
            catch(Exception e)
            {
                stateObj.Exception = e;
            }

            stateObj.SetCompleted();
        }

        override internal int EndReceive(IAsyncResult asyncResult)
        {
            VerifyAsyncResult(asyncResult, typeof(Receive_SO), "EndReceive");
            HandleAsyncEnd(asyncResult, false);
            return ((Receive_SO)asyncResult).Read;
        }

        #endregion

        #region Accept functions (overriden) - not supported
        override internal SocketBase Accept()
        {
            ThrowUnsupportException("Accept");
            return null;
        }

        override internal IAsyncResult BeginAccept(AsyncCallback callback, object state)
        {
            ThrowUnsupportException("BeginAccept");
            return null;
        }

        override internal SocketBase EndAccept(IAsyncResult asyncResult)
        {
            ThrowUnsupportException("EndAccept");
            return null;
        }
        #endregion

        #region Bind functions (overriden) - not supported
        override internal void Bind(SocketBase baseSocket)
        {
            ThrowUnsupportException("Bind");
        }

        override internal IAsyncResult BeginBind(SocketBase baseSocket, AsyncCallback callback, object state)
        {
            ThrowUnsupportException("BeginBind");
            return null;
        }

        override internal void EndBind(IAsyncResult ar)
        {
            ThrowUnsupportException("EndBind");
        }
        #endregion

        #region Listen function (overriden) - not supported
        override internal void Listen(int backlog)
        {
            ThrowUnsupportException("Listen");
        }
        #endregion
    }
}
