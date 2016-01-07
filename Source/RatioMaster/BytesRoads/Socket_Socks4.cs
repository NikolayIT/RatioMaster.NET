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
//========================================================================== 
// Changed by: NRPG
using System;
using System.Net;
using System.Net.Sockets;
using BytesRoad.Net.Sockets.Advanced;

namespace BytesRoad.Net.Sockets
{
    /// <summary>
    /// Summary description for SocketEx_Socks4.
    /// </summary>
    internal class Socket_Socks4 : SocketBase
    {
        #region Async classes
        class Connect_SO : AsyncResultBase
        {
            EndPoint _remoteEndPoint;
            int _readBytes = 0;
            int _port = -1;

            internal Connect_SO(EndPoint remoteEndPoint,
                int port,
                AsyncCallback cb, 
                object state) : base(cb, state)
            {
                _remoteEndPoint = remoteEndPoint;
                _port = port;
            }

            internal int Port
            {
                get { return _port; }
            }
            internal EndPoint RemoteEndPoint
            {
                get { return _remoteEndPoint; }
                set { _remoteEndPoint = value; }
            }

            internal int ReadBytes
            {
                get { return _readBytes; }
                set { _readBytes = value; }
            }
        }

        class Bind_SO : AsyncResultBase
        {
            SocketBase _baseSocket;
            int _readBytes = 0;
            IPAddress _proxyIP = null;

            internal Bind_SO(SocketBase baseSocket,
                AsyncCallback cb, 
                object state) : base(cb, state)
            {
                _baseSocket = baseSocket;
            }

            internal SocketBase BaseSocket
            {
                get { return _baseSocket; }
            }

            internal int ReadBytes
            {
                get { return _readBytes; }
                set { _readBytes = value; }
            }

            internal IPAddress ProxyIP
            {
                get { return _proxyIP; }
                set { _proxyIP = value; }
            }
        }

        class Accept_SO : AsyncResultBase
        {
            int _readBytes = 0;

            internal Accept_SO(AsyncCallback cb, 
                object state) : base(cb, state)
            {
            }

            internal int ReadBytes
            {
                get { return _readBytes; }
                set { _readBytes = value; }
            }
        }

        #endregion

        byte[] _response = new byte[8];

        //End points
        EndPoint _localEndPoint = null;
        EndPoint _remoteEndPoint = null;

        internal Socket_Socks4(
            string proxyServer,
            int proxyPort,
            byte[] proxyUser) 
            : base(proxyServer, 
            proxyPort, 
            proxyUser,
            null)
        {
        }

        #region Attributes
        override internal ProxyType ProxyType 
        { 
            get { return ProxyType.Socks4; } 
        }
        override internal EndPoint LocalEndPoint { get { return _localEndPoint; } }
        override internal EndPoint RemoteEndPoint { get { return _remoteEndPoint; } }
        #endregion

        #region Helper functions

        void VerifyResponse()
        {
            string msg = null;

            //------------------------------------
            // Verify reply format. Pass 4 even it
            // against the RFC.
            //
            if((_response[0] != 0) && (_response[0] != 4))
            {
                msg = string.Format("Socks4: Reply format is unknown ({0}).", _response[0]);
                throw new ProtocolViolationException(msg);
            }

            //------------------------------------
            // Verify the response
            //
            if(_response[1] != 90)
            {
                byte err = _response[1];
                if(91 == err)
                    msg = string.Format("Socks4: Request rejected or failed ({0}).", err);
                else if(92 == err)
                    msg = string.Format("Socks4: Request rejected because SOCKS server cannot connect to identd on the client ({0}).", err);
                else if(93 == err)
                    msg = string.Format("Socks4: Request rejected because the client program and identd report different user-ids ({0}).", err);
                else
                    msg = string.Format("Socks4: Socks server return unknown error code ({0}).", err);
            }

            if(null != msg)
                throw new SocketException(SockErrors.WSAECONNREFUSED);
                //throw new ProxyErrorException(msg);
        }

        byte[] PrepareCmd(EndPoint remoteEP, byte cmdVal)
        {
            int userLength = 0;
            if(_proxyUser != null)
                userLength = _proxyUser.Length;

            IPEndPoint ip = (IPEndPoint)remoteEP;
            byte[] cmd = new byte[8+userLength+1];
            cmd[0] = 4;
            cmd[1] = cmdVal;
            cmd[2] = (byte)((ip.Port&0xFF00)>>8);
            cmd[3] = (byte)(ip.Port&0xFF);

            long ipAddr = ip.Address.Address;
            cmd[7] = (byte)((ipAddr&0xFF000000)>>24);
            cmd[6] = (byte)((ipAddr&0x00FF0000)>>16);
            cmd[5] = (byte)((ipAddr&0x0000FF00)>>8);
            cmd[4] = (byte)((ipAddr&0x000000FF));

            if(userLength > 0)
                Array.Copy(_proxyUser, 0, cmd, 8, userLength);
            cmd[8 + userLength] = 0;
            return cmd;
        }

        byte[] PrepareBindCmd(SocketBase baseSocket)
        {
            return PrepareCmd(baseSocket.RemoteEndPoint, 2);
        }

        byte[] PrepareConnectCmd(EndPoint remoteEP)
        {
            return PrepareCmd(remoteEP, 1);
        }

        IPEndPoint ConstructBindEndPoint(IPAddress proxyIP)
        {
            int port = (_response[2]<<8) | _response[3] ;
            long ip = (_response[7]<<24)|
                (_response[6]<<16)|
                (_response[5]<<8)|
                (_response[4]);
            ip &= 0xFFFFFFFF;

            //------------------------------------
            //if ip addr all zeros we need to 
            //substitute address of the proxy
            //server
            if(0 == ip)
                return new IPEndPoint(proxyIP, port);

            return new IPEndPoint(new IPAddress(ip), port);
        }
        #endregion

        #region Accept functions (overriden)

        override internal SocketBase Accept()
        {
            CheckDisposed();
            SetProgress(true);
            try
            {
                int read = 0;
                while(read < 8)
                {
                    read += NStream.Read(_response, 
                        read, 
                        _response.Length - read);
                }

                VerifyResponse();
            }
            finally
            {
                SetProgress(false);
            }
            return this;
        }

        override internal IAsyncResult BeginAccept(AsyncCallback callback, 
            object state)
        {
            CheckDisposed();

            Accept_SO stateObj = null;
            SetProgress(true);
            try
            {
                stateObj = new Accept_SO(callback, state);

                //------------------------------------
                // Read the second response from proxy server. 
                //
                NStream.BeginRead(_response, 
                    0, 
                    8, 
                    new AsyncCallback(Accept_Read_End),
                    stateObj);
            }
            catch
            {
                SetProgress(false);
                throw;
            }
            return stateObj;
        }

        void Accept_Read_End(IAsyncResult ar)
        {
            Accept_SO stateObj = (Accept_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                stateObj.ReadBytes += NStream.EndRead(ar);

                if(stateObj.ReadBytes < 8)
                {
                    //------------------------------------
                    // Continue read the response from proxy server. 
                    //
                    NStream.BeginRead(_response, 
                        stateObj.ReadBytes, 
                        8 - stateObj.ReadBytes, 
                        new AsyncCallback(Accept_Read_End),
                        stateObj);
                }
                else
                {
                    VerifyResponse();
                    stateObj.SetCompleted();
                }

            }
            catch(Exception e)
            {
                stateObj.Exception = e;
                stateObj.SetCompleted();
            }
        }

        override internal SocketBase EndAccept(IAsyncResult asyncResult)
        {
            VerifyAsyncResult(asyncResult, typeof(Accept_SO), "EndAccept");
            HandleAsyncEnd(asyncResult, true);
            return this;
        }
        
        #endregion

        #region Connect functions (overriden)

        override internal void Connect(EndPoint remoteEP)
        {
            CheckDisposed();
            SetProgress(true);
            try
            {
                //------------------------------------
                // Get end point for the proxy server
                //
                IPHostEntry  proxyEntry = GetHostByName(_proxyServer);
                if(null == proxyEntry)
                    //throw new HostNotFoundException("Unable to resolve proxy name.");
                    throw new SocketException(SockErrors.WSAHOST_NOT_FOUND);

                IPEndPoint proxyEndPoint = ConstructEndPoint(proxyEntry, _proxyPort);

                //------------------------------------------
                // Connect to proxy server
                //
                _socket.Connect(proxyEndPoint);

                _localEndPoint = null; //CONNECT command doesn't provide us with local end point
                _remoteEndPoint = remoteEP;

                //------------------------------------------
                // Send CONNECT command
                //
                byte[] cmd = PrepareConnectCmd(remoteEP);
                NStream.Write(cmd, 0, cmd.Length);

                //------------------------------------------
                // Read the response from proxy the server. 
                //
                int read = 0;
                while(read < 8)
                {
                    read += NStream.Read(_response, 
                        read, 
                        _response.Length - read);
                }

                VerifyResponse();
            }
            finally
            {
                SetProgress(false);
            }
        }

    
        override internal IAsyncResult BeginConnect(string hostName,
            int port, 
            AsyncCallback callback,
            object state)
        {
            CheckDisposed();
            Connect_SO stateObj = null;

            SetProgress(true);
            try
            {
                stateObj = new Connect_SO(null, port, callback, state);
                BeginGetHostByName(hostName,
                    new AsyncCallback(Connect_GetHost_Host_End),
                    stateObj);
            }
            catch(Exception e)
            {
                SetProgress(false);
                throw e;
            }
            return stateObj;
        }

        void Connect_GetHost_Host_End(IAsyncResult ar)
        {
            Connect_SO stateObj = (Connect_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                IPHostEntry host = EndGetHostByName(ar);
                if(null == host)
                    throw new SocketException(SockErrors.WSAHOST_NOT_FOUND);
                    //throw new HostNotFoundException("Unable to resolve host name.");

                stateObj.RemoteEndPoint = ConstructEndPoint(host, stateObj.Port);

                //------------------------------------
                // Get end point for the proxy server
                //
                BeginGetHostByName(_proxyServer,
                    new AsyncCallback(Connect_GetHost_Proxy_End),
                    stateObj);
            }
            catch(Exception e)
            {
                stateObj.Exception = e;
                stateObj.SetCompleted();
            }
        }

        override internal IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback callback, object state)
        {
            CheckDisposed();

            Connect_SO stateObj = null;

            SetProgress(true);
            try
            {
                stateObj = new Connect_SO(remoteEP, -1, callback, state);

                //------------------------------------
                // Get end point for the proxy server
                //
                BeginGetHostByName(_proxyServer,
                    new AsyncCallback(Connect_GetHost_Proxy_End),
                    stateObj);
            }
            catch(Exception ex)
            {
                SetProgress(false);
                throw ex;
            }
            return stateObj;
        }

        void Connect_GetHost_Proxy_End(IAsyncResult ar)
        {
            Connect_SO stateObj = (Connect_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                IPHostEntry host = EndGetHostByName(ar);
                if(null == host)
                    throw new SocketException(SockErrors.WSAHOST_NOT_FOUND);
                    //throw new HostNotFoundException("Unable to resolve proxy name.");

                IPEndPoint proxyEndPoint = ConstructEndPoint(host, _proxyPort);

                //------------------------------------
                // Connect to proxy server
                //
                _socket.BeginConnect(proxyEndPoint, 
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

                _localEndPoint = null;
                _remoteEndPoint = stateObj.RemoteEndPoint;

                //------------------------------------
                // Send CONNECT command
                //
                byte[] cmd = PrepareConnectCmd(stateObj.RemoteEndPoint);

                NStream.BeginWrite(cmd, 
                    0, 
                    cmd.Length,
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

                //------------------------------------
                // Read the response from proxy server. 
                //
                NStream.BeginRead(_response, 
                    0, 
                    8, 
                    new AsyncCallback(Connect_Read_End),
                    stateObj);
            }
            catch(Exception e)
            {
                stateObj.Exception = e;
                stateObj.SetCompleted();
            }
        }

        void Connect_Read_End(IAsyncResult ar)
        {
            Connect_SO stateObj = (Connect_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                int num = NStream.EndRead(ar);
                stateObj.ReadBytes += num;

                if(stateObj.ReadBytes < 8)
                {
                    //------------------------------------
                    // Read the response from proxy server. 
                    //
                    NStream.BeginRead(_response, 
                        stateObj.ReadBytes, 
                        8 - stateObj.ReadBytes, 
                        new AsyncCallback(Connect_Read_End),
                        stateObj);
                }
                else
                {
                    VerifyResponse();
                    stateObj.SetCompleted();
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

        #region Bind functions (overriden)
        override internal void Bind(SocketBase socket)
        {
            CheckDisposed();
            SetProgress(true);
            try
            {
                //-----------------------------------------
                // Get end point for the proxy server
                IPHostEntry host = GetHostByName(_proxyServer);
                if(host == null)
                    throw new SocketException(SockErrors.WSAHOST_NOT_FOUND);
                    //throw new HostNotFoundException("Unable to resolve proxy host name.");

                IPEndPoint proxyEndPoint = ConstructEndPoint(host, _proxyPort);

                //-----------------------------------------
                // Connect to proxy server
                //
                _socket.Connect(proxyEndPoint);

                //-----------------------------------------
                // Send BIND command
                //
                byte[] cmd = PrepareBindCmd(socket);
                NStream.Write(cmd, 0, cmd.Length);

                //-----------------------------------------
                // Read the response from the proxy server. 
                //
                int read = 0;
                while(read < 8)
                {
                    read += NStream.Read(_response, 
                        read, 
                        _response.Length - read);
                }

                VerifyResponse();
                _localEndPoint = ConstructBindEndPoint(proxyEndPoint.Address);
            
                //remote end point doesn't provided for BIND command
                _remoteEndPoint = null;
            }
            finally
            {
                SetProgress(false);
            }
        }


        override internal IAsyncResult BeginBind(SocketBase baseSocket, 
            AsyncCallback callback, 
            object state)
        {
            CheckDisposed();

            Bind_SO stateObj = null;

            SetProgress(true);
            try
            {
                stateObj = new Bind_SO(baseSocket, callback, state);

                //------------------------------------
                // Get end point for the proxy server
                //
                BeginGetHostByName(_proxyServer,
                    new AsyncCallback(Bind_GetHost_End),
                    stateObj);
            }
            catch(Exception ex)
            {
                SetProgress(false);
                throw ex;
            }
            return stateObj;
        }

        void Bind_GetHost_End(IAsyncResult ar)
        {
            Bind_SO stateObj = (Bind_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                IPHostEntry host = EndGetHostByName(ar);
                if(host == null)
                    throw new SocketException(SockErrors.WSAHOST_NOT_FOUND);
                    //throw new HostNotFoundException("Unable to resolve proxy host name.");

                IPEndPoint proxyEndPoint = ConstructEndPoint(host, _proxyPort);
                stateObj.ProxyIP = proxyEndPoint.Address;

                //------------------------------------
                // Connect to proxy server
                //
                _socket.BeginConnect(proxyEndPoint, 
                    new AsyncCallback(Bind_Connect_End),
                    stateObj);
            }
            catch(Exception e)
            {
                stateObj.Exception = e;
                stateObj.SetCompleted();
            }
        }

        void Bind_Connect_End(IAsyncResult ar)
        {
            Bind_SO stateObj = (Bind_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                _socket.EndConnect(ar);

                //------------------------------------
                // Send CONNECT command
                //
                byte[] cmd = PrepareBindCmd(stateObj.BaseSocket);

                NStream.BeginWrite(cmd, 
                    0, 
                    cmd.Length,
                    new AsyncCallback(Bind_Write_End),
                    stateObj);
            }
            catch(Exception e)
            {
                stateObj.Exception = e;
                stateObj.SetCompleted();
            }
        }

        void Bind_Write_End(IAsyncResult ar)
        {
            Bind_SO stateObj = (Bind_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                NStream.EndWrite(ar);

                //------------------------------------
                // Read the response from proxy server. 
                //
                NStream.BeginRead(_response, 
                    0, 
                    8, 
                    new AsyncCallback(Bind_Read_End),
                    stateObj);
            }
            catch(Exception e)
            {
                stateObj.Exception = e;
                stateObj.SetCompleted();
            }
        }

        void Bind_Read_End(IAsyncResult ar)
        {
            Bind_SO stateObj = (Bind_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                int num = NStream.EndRead(ar);
                stateObj.ReadBytes += num;

                if(stateObj.ReadBytes < 8)
                {
                    //------------------------------------
                    // Read the response from proxy server. 
                    //
                    NStream.BeginRead(_response, 
                        stateObj.ReadBytes, 
                        8 - stateObj.ReadBytes, 
                        new AsyncCallback(Bind_Read_End),
                        stateObj);
                }
                else
                {
                    VerifyResponse();
                    _localEndPoint = ConstructBindEndPoint(stateObj.ProxyIP);
                    _remoteEndPoint = null;
                    stateObj.SetCompleted();
                }
            }
            catch(Exception e)
            {
                stateObj.Exception = e;
                stateObj.SetCompleted();
            }
        }

        override internal void EndBind(IAsyncResult asyncResult)
        {
            VerifyAsyncResult(asyncResult, typeof(Bind_SO), "EndBind");
            HandleAsyncEnd(asyncResult, true);
        }

        #endregion

        #region Listen functions (overriden)
        override internal void Listen(int backlog)
        {
            CheckDisposed();
            if(null == _localEndPoint)
                throw new ArgumentException("Attempt to listen on socket which has not been bound with Bind.");
        }
        #endregion
    }
}
