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
using System.Net;
using System.Net.Sockets;
using BytesRoad.Diag;
using BytesRoad.Net.Sockets.Advanced;

namespace BytesRoad.Net.Sockets
{
    /// <summary>
    /// Summary description for SocketBase.
    /// </summary>
    public abstract class SocketBase : AsyncBase, IDisposable
    {
        protected Socket _socket = null;
        
        NetworkStream _stream = null;
        static Random _rand = new Random(unchecked((int)DateTime.Now.Ticks)); 

        bool _disposed = false;

        // Proxy attributes
        protected string _proxyServer = null;
        protected int _proxyPort = -1;
        protected byte[] _proxyUser = null;
        protected byte[] _proxyPassword = null;
        bool _preAuthenticate = true;

        protected SocketBase()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);
        }

        protected SocketBase(
            string proxyServer, 
            int proxyPort, 
            byte[] proxyUser, 
            byte[] proxyPassword)
        {
            _proxyServer = proxyServer;
            _proxyPort = proxyPort;
            _proxyUser = proxyUser;
            _proxyPassword = proxyPassword;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        protected SocketBase(Socket systemSocket)
        {
            if(null == systemSocket)
                throw new ArgumentNullException("systemSocket");

            _socket = systemSocket;
        }

        #region Attributes

        virtual internal int Available
        {
            get { return _socket.Available; }
        }

        internal bool Connected
        {
            get { return _socket.Connected; }
        }

        protected NetworkStream NStream
        {
            get
            {
                if(null == _stream)
                    _stream = new NetworkStream(_socket, false);
                return _stream;
            }
        }

        internal Socket SystemSocket
        {
            get { return _socket; }
        }

        internal bool PreAuthenticate
        {
            get { return _preAuthenticate; }
            set { _preAuthenticate = value; }
        }

        abstract internal ProxyType ProxyType { get; }

        abstract internal EndPoint LocalEndPoint { get; } 

        abstract internal EndPoint RemoteEndPoint { get; }

        #endregion

        #region DNS helpers
        static internal IPHostEntry GetHostByName(string hostName)
        {
            IPHostEntry host = null;
            try
            {
                host = Dns.GetHostEntry(hostName);
            }
            catch (Exception)
            {
                host = null;
            }

            return host;
        }

        static internal IAsyncResult BeginGetHostByName(string hostName, AsyncCallback cb, object state)
        {
            return Dns.BeginGetHostEntry(hostName, cb, state);
        }

        static internal IPHostEntry EndGetHostByName(IAsyncResult ar)
        {
            IPHostEntry host = null;
            try
            {
                host = Dns.EndGetHostEntry(ar);
            }
            catch (Exception)
            {
                host = null;
            }

            return host;
        }
        #endregion

        #region Helpers
        static internal IPEndPoint ConstructEndPoint(IPHostEntry host, int port)
        {
            if(0 >= host.AddressList.Length)
            {
                NSTrace.WriteLineError("Provided host structure do not contains addresses.");
                throw new ArgumentException("Provided host structure do not contains addresses.", "host");
            }

            foreach (var addr in host.AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                    return new IPEndPoint(addr, port);
            }
            
            return new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
        }

        protected void CheckDisposed()
        {
            if(_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }
        #endregion

        #region Accept functions (abstract)
        abstract internal SocketBase Accept();

        abstract internal IAsyncResult BeginAccept(AsyncCallback callback, object state);

        abstract internal SocketBase EndAccept(IAsyncResult asyncResult);
        #endregion

        #region Connect functions (virtual and abstract)
        virtual internal void Connect(string hostName, int port)
        {

            CheckDisposed();

            string msg = string.Format("S: Resolving name '{0}'...", hostName);
            NSTrace.WriteLineInfo(msg);

            IPHostEntry host = GetHostByName(hostName);
            if(null == host)
            {
                NSTrace.WriteLineInfo("S: Hostname not found, throwing exception...");
                throw new SocketException(SockErrors.WSAHOST_NOT_FOUND);
            }

            NSTrace.WriteLineInfo("S: Hostname found, connecting ...");
            Connect(ConstructEndPoint(host, port));
            NSTrace.WriteLineInfo("S: Connection established.");
        }

        abstract internal void Connect(EndPoint remoteEP);

        abstract internal IAsyncResult BeginConnect(string hostName, int port, AsyncCallback callback, object state);

        abstract internal IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback callback, object state);

        abstract internal void EndConnect(IAsyncResult asyncResult);
        #endregion

        #region Bind functions (abstract)
        abstract internal void Bind(SocketBase socket);

        abstract internal IAsyncResult BeginBind(SocketBase socket, AsyncCallback callback, object state);

        abstract internal void EndBind(IAsyncResult ar); 
        #endregion

        #region Listen function (abstract)
        abstract internal void Listen(int backlog);
        #endregion

        #region Shutdown function (virtual)
        virtual internal void Shutdown(SocketShutdown how)
        {
            CheckDisposed();
            _socket.Shutdown(how);
        }
        #endregion

        #region Receive functions (virtual)
        virtual internal int Receive(byte[] buffer)
        {
            CheckDisposed();
            return _socket.Receive(buffer);
        }

        virtual internal int Receive(byte[] buffer, int size)
        {
            CheckDisposed();
            return _socket.Receive(buffer, size, SocketFlags.None);
        }

        virtual internal int Receive(byte[] buffer, int offset, int size)
        {
            CheckDisposed();
            return _socket.Receive(buffer, offset, size, SocketFlags.None);
        }

        virtual internal IAsyncResult BeginReceive(
            byte[] buffer,
            int offset,
            int size,
            AsyncCallback callback,
            object state
            )
        {
            CheckDisposed();
            return _socket.BeginReceive(buffer, offset, size, SocketFlags.None, callback, state);
        }

        virtual internal int EndReceive(IAsyncResult asyncResult)
        {
            return _socket.EndReceive(asyncResult);
        }

        #endregion

        #region Send functions (virtual)
        virtual internal int Send(byte[] buffer)
        {
            CheckDisposed();
            return _socket.Send(buffer);
        }

        virtual internal int Send(byte[] buffer, int size)
        {
            CheckDisposed();
            return _socket.Send(buffer, size, SocketFlags.None);
        }

        virtual internal int Send(byte[] buffer, int offset, int size)
        {
            CheckDisposed();
            return _socket.Send(buffer, offset, size, SocketFlags.None);
        }

        virtual internal IAsyncResult BeginSend(
            byte[] buffer,
            int offset,
            int size,
            AsyncCallback callback,
            object state
            )
        {
            CheckDisposed();
            return _socket.BeginSend(buffer, offset, size, 
                SocketFlags.None, callback, state);
        }

        virtual internal int EndSend(IAsyncResult asyncResult)
        {
            return _socket.EndSend(asyncResult);
        }

        #endregion

        #region SetSocketOption functions (virtual)
        virtual internal void SetSocketOption(
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            byte[] optionValue
            )
        {
            CheckDisposed();
            _socket.SetSocketOption(optionLevel, optionName, optionValue);
        }


        virtual internal void SetSocketOption(
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            int optionValue
            )
        {
            CheckDisposed();
            _socket.SetSocketOption(optionLevel, optionName, optionValue);
        }

        virtual internal void SetSocketOption(
            SocketOptionLevel optionLevel,
            SocketOptionName optionName,
            object optionValue
            )
        {
            CheckDisposed();
            _socket.SetSocketOption(optionLevel, optionName, optionValue);
        }
        #endregion


        #region Disposing pattern
        ~SocketBase()
        {
            Dispose(false);
        }

        internal void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock(this)
            {
                if(disposing)
                {
                }

                _socket.Close();
            }
        }
        #endregion
    }
}
