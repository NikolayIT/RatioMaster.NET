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
using BytesRoad.Net.Sockets.Advanced;

namespace BytesRoad.Net.Sockets
{
    /// <summary>
    /// Summary description for Socket_None.
    /// </summary>
    internal class Socket_None : SocketBase
    {
        #region Async classes
        class Bind_SO : AsyncResultBase
        {
            internal Bind_SO(AsyncCallback cb, object state) : base(cb, state)
            {
            }
        }

        class Connect_SO : AsyncResultBase
        {
            int _port;

            internal Connect_SO(int port, AsyncCallback cb, object state) : base(cb, state)
            {
                _port = port;
            }

            internal int Port { get { return _port;} }
        }
        #endregion

        internal Socket_None()
        {
        }

        internal Socket_None(Socket systemSocket) : base(systemSocket)
        {
        }

        #region Attributes

        override internal ProxyType ProxyType
        {
            get { return ProxyType.None; }
        }
        
        override internal EndPoint LocalEndPoint 
        {
            get { return _socket.LocalEndPoint; } 
        } 

        override internal EndPoint RemoteEndPoint 
        { 
            get { return _socket.RemoteEndPoint; } 
        }

        #endregion

        #region Helpers
        #endregion

        #region Accept functions (overriden)

        override internal SocketBase Accept()
        {
            CheckDisposed();
            return new Socket_None(_socket.Accept());
        }

        override internal IAsyncResult BeginAccept(AsyncCallback callback, object state)
        {
            CheckDisposed();
            return _socket.BeginAccept(callback, state);
        }

        override internal SocketBase EndAccept(IAsyncResult asyncResult)
        {
            return new Socket_None(_socket.EndAccept(asyncResult));
        }
        
        #endregion

        #region Connect functions (overriden)
        override internal void Connect(EndPoint remoteEP)
        {
            CheckDisposed();
            _socket.Connect(remoteEP);
        }

        override internal IAsyncResult BeginConnect(
            EndPoint remoteEP, 
            AsyncCallback callback, 
            object state)
        {
            CheckDisposed();
            Connect_SO stateObj = null;
            SetProgress(true);
            try
            {
                stateObj = new Connect_SO(-1, callback, state);

                _socket.BeginConnect(
                    remoteEP, 
                    new AsyncCallback(Connect_End),
                    stateObj);
            }
            catch(Exception e)
            {
                SetProgress(false);
                throw e;
            }

            return stateObj;
        }

        override internal IAsyncResult BeginConnect(
            string hostName, 
            int port, 
            AsyncCallback callback, 
            object state)
        {
            CheckDisposed();
            SetProgress(true);
            Connect_SO stateObj = null;
            try
            {
                stateObj = new Connect_SO(port, callback, state);
                Dns.BeginGetHostEntry(hostName, new AsyncCallback(GetHost_End), stateObj);
            }
            catch(Exception e)
            {
                SetProgress(false);
                throw e;
            }

            return stateObj;
        }

        void GetHost_End(IAsyncResult ar)
        {
            Connect_SO stateObj = (Connect_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                IPHostEntry host = Dns.EndGetHostEntry(ar);
                if(null == host)
                    throw new SocketException(SockErrors.WSAHOST_NOT_FOUND);

                    // throw new HostNotFoundException("Unable to resolve host name.");

                EndPoint remoteEP = ConstructEndPoint(host, stateObj.Port);
                _socket.BeginConnect(
                    remoteEP,
                    new AsyncCallback(Connect_End),
                    stateObj);
            }
            catch(Exception e)
            {
                stateObj.Exception = e;
                stateObj.SetCompleted();
            }
        }

        void Connect_End(IAsyncResult ar)
        {
            Connect_SO stateObj = (Connect_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                _socket.EndConnect(ar);
            }
            catch(Exception e)
            {
                stateObj.Exception = e;
            }

            stateObj.SetCompleted();
        }

        override internal void EndConnect(IAsyncResult asyncResult)
        {
            VerifyAsyncResult(asyncResult, typeof(Connect_SO), "EndConnect");
            HandleAsyncEnd(asyncResult, true);
        }
        #endregion

        #region Bind functions (overriden)
        override internal void Bind(SocketBase baseSocket)
        {
            CheckDisposed();
            IPEndPoint ep = (IPEndPoint)baseSocket.SystemSocket.LocalEndPoint;
            ep.Port = 0;
            _socket.Bind(ep);
        }

        override internal IAsyncResult BeginBind(
            SocketBase baseSocket, 
            AsyncCallback callback, 
            object state)
        {
            CheckDisposed();
            Bind_SO stateObj = new Bind_SO(callback, state);
            try
            {
                IPEndPoint ep = (IPEndPoint)baseSocket.SystemSocket.LocalEndPoint;
                ep.Port = 0;
                _socket.Bind(ep);
            }
            catch(Exception e)
            {
                stateObj.Exception = e;
            }

            stateObj.SetCompleted();
            return stateObj;
        }

        override internal void EndBind(IAsyncResult ar)
        {
            VerifyAsyncResult(ar, typeof(Bind_SO), "EndBind");
            HandleAsyncEnd(ar, false);
        }
        #endregion

        #region Listen function (overriden)
        override internal void Listen(int backlog)
        {
            CheckDisposed();
            _socket.Listen(backlog);
        }
        #endregion
    }
}
