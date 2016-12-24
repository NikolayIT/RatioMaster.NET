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
using System.Threading;
using BytesRoad.Diag;

namespace BytesRoad.Net.Sockets
{

    /// <summary>
    /// Specifies the type of the proxy server.
    /// </summary>
    public enum ProxyType
    {
        /// <summary>
        /// No proxy server. Directly connecting to remote host.
        /// </summary>
        None,

        /// <summary>
        /// SOCKS4 proxy server.
        /// </summary>
        Socks4,

        /// <summary>
        /// SOCKS4a proxy server. Extension of the SOCKS4 proxy server.
        /// </summary>
        Socks4a,

        /// <summary>
        /// SOCKS5 proxy server.
        /// </summary>
        Socks5,

        /// <summary>
        /// Web proxy server. HTTP CONNECT method is used for communication.
        /// </summary>
        HttpConnect
    }

    /// <summary>
    /// Encompasses socket's error codes
    /// </summary>
    public class SockErrors
    {

        /// <summary>
        /// Software caused connection abort
        /// </summary>
        public const int WSAECONNABORTED = 10053;
        /// <summary>
        /// Connection reset by peer. 
        /// </summary>
        public const int WSAECONNRESET = 10054;
        /// <summary>
        /// Connection timed out. 
        /// </summary>
        public const int WSAETIMEDOUT = 10060;

        /// <summary>
        /// Connection refused. 
        /// </summary>
        public const int WSAECONNREFUSED = 10061;

        /// <summary>
        /// Host not found. 
        /// </summary>
        public const int WSAHOST_NOT_FOUND = 11001;

        SockErrors(){}
    }

    #region Commented exceptions
    /*    internal class HostNotFoundException : Exception
        {
            internal HostNotFoundException(string message) : base(message)
            {
            }
        }

        internal class SocketTimeoutException : Exception
        {
            internal SocketTimeoutException()
            {
            }

            internal SocketTimeoutException(string message) : base(message)
            {
            }

            internal SocketTimeoutException(string message, 
                Exception innerEx) : base(message, innerEx)
            {
            }
        }

        internal class ProxyErrorException : Exception
        {
            int _code;
            internal ProxyErrorException(string message) : base (message)
            {
            }

            internal ProxyErrorException(string message, int code) : base (message)
            {
                _code = code;
            }
        }

        internal class AuthFailedException : ProxyErrorException
        {
            internal AuthFailedException(string message) : base(message)
            {
            }
        }*/
    #endregion

    internal enum OpState
    {
        Working,
        Timedout,
        Finished
    }
;

    internal abstract class IOp
    {
        abstract internal object Execute();

        abstract internal object BeginExecute(AsyncCallback cb, object state);

        abstract internal object EndExecute(IAsyncResult ar);
    }

    /// <summary>
    /// Provide methods for TCP/IP communication through the various types of proxies.
    /// </summary>
    /// <remarks>
    /// <b>SocketEx</b> class built on top of the <see cref="System.Net.Sockets.Socket"/>
    /// class provided by the .NET Framework. 
    /// 
    /// The main advantages of the SocketEx class is that it might be used to
    /// communicate with the peer host through the various types of proxies.
    /// Thus giving your application the simple way to add so called 
    /// 'firewall friendly features'. 
    /// <para>
    /// Following proxy servers are supported:
    /// <list type="bullet">
    /// <item>
    /// <description>Socks4</description>
    /// </item>
    /// <item>
    /// <description>Socks4a</description>
    /// </item>
    /// <item>
    /// <description>Socks5, username/password authentication method supported</description>
    /// </item>
    /// <item>
    /// <description>Web proxy (HTTP CONNECT method), basic authentication method supported</description>
    /// </item>
    /// </list>
    /// </para>
    /// 
    /// </remarks>
    /// 
    public class SocketEx : IDisposable
    {
        SocketBase _baseSocket = null;
        OpState _opState = OpState.Finished;
        bool _disposed = false;
        Timer  _timer = null;

        int _recvTimeout = Timeout.Infinite;
        int _sendTimeout = Timeout.Infinite;
        int _acceptTimeout = Timeout.Infinite;
        int _connectTimeout = Timeout.Infinite;

        /// <summary>
        /// Initializes  new instance of the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/> class for 
        /// direct connection.
        /// </summary>
        /// <remarks>
        /// With this constructor the instance of the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/> class would
        /// be connected to remote host directly. 
        /// To force connection through the proxy server you need to use
        /// other constructor.
        /// </remarks>
        public SocketEx()
        {
            NSTrace.WriteLineVerbose("-> SocketEx()");

            _baseSocket = new Socket_None();
            Init();

            NSTrace.WriteLineVerbose("<- SocketEx()");
        }

        /// <summary>
        /// Initializes  new instance of the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/> class for 
        /// direct connection or connection through the proxy servers.
        /// </summary>
        /// <param name="proxyType">
        /// Specifies the type of the proxy server to 
        /// be used for communication with remote end point. 
        /// One of the <see cref="BytesRoad.Net.Sockets.ProxyType"/> values.
        /// </param>
        /// <param name="proxyServer">The host name of the proxy server.</param>
        /// <param name="proxyPort">The port number of the proxy server.</param>
        /// <param name="proxyUser">
        /// The user name which would be used with proxy server 
        /// in authentication procedure.
        /// </param>
        /// <param name="proxyPassword">
        /// The password which would be used with proxy server 
        /// in authentication procedure.
        /// </param>
        /// <remarks>
        ///  If proxy server doesn't support anonymous users and the <i>proxyUser</i>
        ///  parameter equals to <b>null</b> (<b>Nothing</b> in Visual Basic) then the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Connect"/>
        /// method will fail.
        /// </remarks>
        public SocketEx(
            ProxyType proxyType, 
            string proxyServer, 
            int proxyPort,
            byte[] proxyUser,
            byte[] proxyPassword)
        {
            NSTrace.WriteLineVerbose("-> SocketEx(full)");

            if(ProxyType.None == proxyType)
                _baseSocket = new Socket_None();
            else if(ProxyType.Socks4 == proxyType)
                _baseSocket = new Socket_Socks4(proxyServer, proxyPort, proxyUser);
            else if(ProxyType.Socks4a == proxyType)
                _baseSocket = new Socket_Socks4a(proxyServer, proxyPort, proxyUser);
            else if(ProxyType.Socks5 == proxyType)
                _baseSocket = new Socket_Socks5(proxyServer, proxyPort, proxyUser, proxyPassword);
            else if(ProxyType.HttpConnect == proxyType)
                _baseSocket = new Socket_HttpConnect(proxyServer, proxyPort, proxyUser, proxyPassword);
            else
            {
                string msg = string.Format("Proxy type is not supported ({0}).", proxyType.ToString());
                NSTrace.WriteLineError("EX: " + msg + " " + Environment.StackTrace);
                throw new NotSupportedException(msg);
            }

            Init();

            NSTrace.WriteLineVerbose("<- SocketEx(full)");
        }

        #region comments
        /*
        /// <summary>
        /// Initializes new, bindable instance of the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/> class for 
        /// connection through the proxy servers.
        /// </summary>
        /// <param name="baseSocket">
        /// Instance of the <b>SocketEx</b> class which already connected 
        /// to the remote end point through the proxy server.
        /// </param>
        /// <remarks>
        /// You need to use this constructor if you plan to call
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Bind"/>
        /// method to listen for incoming connection from the
        /// remote end point. See <see cref="BytesRoad.Net.Sockets.SocketEx.Bind"/>
        /// command for more details. Bind command supported only by the following 
        /// type of proxy servers: <b>Socks4</b>, <b>Socks4a</b>, <b>Socks5</b>.
        /// </remarks>
        public SocketEx(SocketEx baseSocket)
        {
            NSTrace.WriteLineVerbose("-> SocketEx(handle)");
            if(null == baseSocket)
            {
                NSTrace.WriteLineError("EX: SocketEx(handle), handle == null. " + Environment.StackTrace);
                throw new ArgumentNullException("baseSocket");
            }

            _baseSocket = baseSocket._baseSocket;
            Init();

            NSTrace.WriteLineVerbose("<- SocketEx(handle)");
        }
*/
#endregion

        /// <summary>
        /// Used in Accept methods
        /// </summary>
        /// <param name="baseSocket"></param>
        internal SocketEx(SocketBase baseSocket)
        {
            NSTrace.WriteLineVerbose("-> SocketEx(handle)");
            if(null == baseSocket)
            {
                NSTrace.WriteLineError("EX: SocketEx(handle), handle == null. " + Environment.StackTrace);
                throw new ArgumentNullException("baseSocket");
            }

            _baseSocket = baseSocket;
            Init();

            NSTrace.WriteLineVerbose("<- SocketEx(handle)");
        }

        
        #region Helpers
        void Init()
        {
            _timer = new Timer(new TimerCallback(OnTimer), null, Timeout.Infinite, Timeout.Infinite);
        }

        void CheckDisposed()
        {
            if(_disposed)
                throw new ObjectDisposedException(GetType().FullName, "Object disposed.");
        }
        #endregion

        #region Attributes

        /// <summary>
        /// Gets the amount of data that has been received from the network and is available to be read.
        /// </summary>
        /// <value>
        /// The number of bytes queued in the the internal network buffer and available for reading.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        public int Available
        {
            get 
            { 
                CheckDisposed();
                return _baseSocket.Available; 
            }
        }

        /// <summary>
        /// Gets a value indicating whether a <b>SocketEx</b> is connected to a remote host.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// <value>
        /// Value <b>true</b> means that the <b>SocketEx</b> is connected to the remote end point as of the most
        /// recent operation; otherwise, <b>false</b>.
        /// </value>
        /// <remarks>
        /// In case connection to remote end point made through the proxy server the value of the property 
        /// indicates whether the last network operation between local host and proxy server was successful or not.
        /// </remarks>
        public bool Connected
        {
            get 
            { 
                CheckDisposed();
                return _baseSocket.Connected; 
            }
        }

        /// <summary>
        /// Gets or sets value which specify whether to preauthenticate
        /// connection with proxy server.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// <value>
        /// <b>true</b> to preauthenticate; <b>false</b>, otherwise.
        /// </value>
        /// 
        /// <remarks>
        /// <para>
        /// The <b>PreAuthenticate</b> property
        /// is very important due to depending of its value the credentials would be 
        /// transfered to the proxy server with the fist packet sent. If you don't want 
        /// credentials to be sent to the proxy server with the first packet, but want
        /// to try to pass authentication as anonymous user instead then you need to assign 
        /// <i>false</i> value to this property.
        /// </para>
        /// And vise versa,
        /// if you want credentials to be sent along with the first packet
        /// and thus save one round trip then you need to assign value <b>true</b> to
        /// this property.
        /// </remarks>
        public bool PreAuthenticate
        {
            get 
            { 
                CheckDisposed();
                return _baseSocket.PreAuthenticate; 
            }

            set 
            { 
                CheckDisposed();
                _baseSocket.PreAuthenticate = value;    
            }
        }

        /// <summary>
        /// Gets the instance of the 
        /// <see cref="System.Net.Sockets.Socket"/>
        /// class which is used to communicate with the proxy server.
        /// </summary>
        /// <value>
        /// An instance of the <see cref="System.Net.Sockets.Socket"/> class.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        public Socket SystemSocket
        {
            get 
            { 
                CheckDisposed();
                return _baseSocket.SystemSocket; 
            }
        }

        /// <summary>
        /// Gets the type of the proxy used by the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// class.
        /// </summary>
        /// <value>
        /// One of the <see cref="BytesRoad.Net.Sockets.ProxyType"/> values.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// <remarks>
        /// This property is read only. You may specify what type of the proxy you want to use
        /// in constructor of the <see cref="BytesRoad.Net.Sockets.SocketEx"/> class.
        /// </remarks>
        public ProxyType ProxyType
        { 
            get 
            { 
                CheckDisposed();
                return _baseSocket.ProxyType; 
            }
         }

        /// <summary>
        /// Gets the local end point the instance of the <b>SocketEx</b> class is bind to.
        /// </summary>
        /// <value>
        /// An instance of the <see cref="System.Net.EndPoint"/> class.
        /// </value>
        /// <remarks>
        /// This property represents the end point which are used as local end point 
        /// at the proxy server to connect to the remote host.
        /// </remarks>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        public EndPoint LocalEndPoint 
        { 
            get 
            { 
                CheckDisposed();
                return _baseSocket.LocalEndPoint; 
            } 
        } 

        /// <summary>
        /// Gets the remote end point the instance of the <b>SocketEx</b> class connect to.
        /// </summary>
        /// <value>
        /// An instance of the <see cref="System.Net.EndPoint"/> class.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        public EndPoint RemoteEndPoint 
        { 
            get 
            { 
                CheckDisposed();
                return _baseSocket.RemoteEndPoint; 
            } 
        }

        /// <summary>
        /// Gets or sets timeout value for the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Send"/> and
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.BeginSend"/>
        /// operation.
        /// </summary>
        /// <value>
        /// Integer value which indicate timeout in milliseconds.
        /// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
        /// or zero specify no timeout.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The specified value is negative and is not equal to 
        /// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
        /// </exception>
        public int SendTimeout
        {
            get 
            { 
                CheckDisposed();
                return _sendTimeout; 
            }

            set 
            { 
                CheckDisposed();
                _sendTimeout = GetTimeoutValue(value, "SendTimeout"); 
            }
        }

        /// <summary>
        /// Gets or sets timeout value for the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Receive"/> and
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.BeginReceive"/>
        /// operation.
        /// </summary>
        /// <value>
        /// Integer value which indicate timeout in milliseconds.
        /// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
        /// or zero specify no timeout.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The specified value is negative and is not equal to 
        /// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
        /// </exception>
        public int ReceiveTimeout
        {
            get 
            { 
                CheckDisposed();
                return _recvTimeout; 
            }

            set 
            { 
                CheckDisposed();
                _recvTimeout = GetTimeoutValue(value, "ReceiveTimeout"); 
            }
        }

        /// <summary>
        /// Gets or sets timeout value for the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Accept"/> and
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.BeginAccept"/>
        /// operation.
        /// </summary>
        /// <value>
        /// Integer, positive value which indicate timeout in milliseconds.
        /// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
        /// or zero specify no timeout.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The specified value is negative and is not equal to 
        /// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
        /// </exception>
        public int AcceptTimeout
        {
            get 
            { 
                CheckDisposed();
                return _acceptTimeout; 
            }

            set 
            { 
                CheckDisposed();
                _acceptTimeout = GetTimeoutValue(value, "AcceptTimeout"); 
            }
        }


        /// <summary>
        /// Gets or sets the timeout period for connect operations.
        /// </summary>
        /// <value>
        /// Positive value for connect operations timeout (in milliseconds). 
        /// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
        /// or zero specify no timeout.
        /// </value>
        /// <remarks>
        /// This timeout applied for both 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Connect">Connect</see>
        /// and
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.BeginConnect">BeginConnect</see>
        /// methods.
        /// </remarks>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The specified value is negative and is not equal to 
        /// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
        /// </exception>
        public int ConnectTimeout
        {
            get 
            { 
                CheckDisposed();
                return _connectTimeout; 
            }

            set 
            {
                CheckDisposed();
                _connectTimeout = GetTimeoutValue(value, "ConnectTimeout"); 
            }
        }

        Exception NonCLSException
        {
            get { return new Exception("Non CLS-compliant exception was thrown."); }
        }
        #endregion

        #region Timeouts functions
        int GetTimeoutValue(int val, string propName)
        {
            if(val < 0 && (Timeout.Infinite != val))
                throw new ArgumentOutOfRangeException(propName, val, "Timeout value should not be less then zero (exception is only Timeout.Infinite");

            if(0 == val)
                return Timeout.Infinite;
            else
                return val; 
        }

        internal void SetTimeout(int timeout)
        {
            ConnectTimeout = timeout;
            AcceptTimeout = timeout;
            ReceiveTimeout = timeout;
            SendTimeout = timeout;
        }

        internal void SetReceiveTimeout(int timeout)
        {
            ReceiveTimeout = timeout;
        }

        internal void SetSendTimeout(int timeout)
        {
            SendTimeout = timeout;
        }        

        void StartTimeoutTrack(int timeout)
        {
            _opState = OpState.Working;
            _timer.Change(timeout, Timeout.Infinite);
        }

        void StopTimeoutTrack(Exception e)
        {
            lock(this)
            {
                if(_opState == OpState.Timedout)
                {
                    throw new SocketException(SockErrors.WSAETIMEDOUT);

                    // throw new SocketTimeoutException(null, e);
                }
                else
                {
                    CheckDisposed();
                    _opState = OpState.Finished;
                }

                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        void OnTimer(object state)
        {
            lock(this)
            {
                if(!_disposed)
                {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    if(_opState == OpState.Working)
                    {
                        _opState = OpState.Timedout;
                        Dispose();
                    }
                }
            }
        }

        object DoTimeoutOp(int timeout, IOp op)
        {
            CheckDisposed();

            object ret = null;
            StartTimeoutTrack(timeout);
            try
            {
                ret = op.Execute();
            }
            catch(Exception e)
            {
                NSTrace.WriteLineError("SocketEx (ex): " + e.ToString());
                StopTimeoutTrack(e);
                throw;
            }

            /*
            catch
            {
                NSTrace.WriteLineError("SocketEx (non clas ex): " + Environment.StackTrace);
                StopTimeoutTrack(NonCLSException);
                throw;
            }
            */
            StopTimeoutTrack(null);
            return ret;
        }

        object BeginTimeoutOp(int timeout, IOp op, AsyncCallback cb, object state)
        {
            CheckDisposed();

            object ret = null;
            StartTimeoutTrack(timeout);
            try
            {
                ret = op.BeginExecute(cb, state);
            }
            catch(Exception e)
            {
                NSTrace.WriteLineError("SocketEx.B (ex): " + e.ToString());
                StopTimeoutTrack(e);
                throw;
            }

            /*
            catch
            {
                NSTrace.WriteLineError("SocketEx.B (non cls ex): " + Environment.StackTrace);
                StopTimeoutTrack(NonCLSException);
                throw;
            }
            */
            return ret;
        }

        object EndTimeoutOp(IOp op, IAsyncResult ar)
        {
            object ret = null;
            try
            {
                ret = op.EndExecute(ar);
            }
            catch(Exception e)
            {
                NSTrace.WriteLineError("SocketEx.E (ex): " + e.ToString());
                StopTimeoutTrack(e);
                throw;
            }

            /*
            catch
            {
                NSTrace.WriteLineError("SocketEx.E (Non CLS ex): " + Environment.StackTrace);
                StopTimeoutTrack(NonCLSException);
                throw;
            }
            */
            StopTimeoutTrack(null);
            return ret;
        }
        #endregion


        #region Accept functions

        #region Op class
        class Accept_Op : IOp
        {
            SocketBase _baseSocket;

            internal Accept_Op(SocketBase baseSocket)
            {
                _baseSocket = baseSocket;
            }

            #region IOp Members

            internal override object Execute()
            {
                return new SocketEx(_baseSocket.Accept());
            }

            internal override object BeginExecute(AsyncCallback cb, object state)
            {
                return _baseSocket.BeginAccept(cb, state);
            }

            internal override object EndExecute(IAsyncResult ar)
            {
                return new SocketEx(_baseSocket.EndAccept(ar));
            }

            #endregion
        }
        #endregion

        /// <summary>
        /// Creates the new instance of the <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// class which represents newly created connection.
        /// </summary>
        /// <returns>
        /// An instance of the <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// for the created connection.
        /// </returns>
        /// <remarks>
        /// <b>Accept</b> method accept incoming connection and create new instance of the 
        /// <b>SocketEx</b> class for it. The <b>SocketEx</b> should be bound before
        /// calling this method thus you need to call 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Bind"/>
        /// and
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Listen"/>
        /// methods first (or their asynchronous versions).
        /// <note>
        /// Web proxy doesn't able to accept incoming connection. So calling
        /// this method on the instance of the <b>SocketEx</b> class which is configured
        /// to work with the web proxy server
        /// (<see cref="BytesRoad.Net.Sockets.ProxyType.HttpConnect"/> proxy type) 
        /// will throw the 
        /// <see cref="System.InvalidOperationException"/> exception.
        /// </note>
        /// </remarks>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// The operation is unsupported.
        /// </exception>
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        public SocketEx Accept()
        {
            return (SocketEx)DoTimeoutOp(_acceptTimeout, new Accept_Op(_baseSocket));
        }

        /// <summary>
        /// Begins an asynchronous operation to accept an incoming connection attempt.
        /// </summary>
        /// <param name="callback">
        /// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
        /// </param>
        /// <param name="state">
        /// An object containing state information for this request.
        /// </param>
        /// <returns>
        /// An <see cref="System.IAsyncResult">IAsyncResult</see>
        /// that references the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// <b>BeginAccept</b> method asynchronously accept incoming connection and 
        /// create new instance of the <b>SocketEx</b> class for it. 
        /// The <b>SocketEx</b> should be bound before
        /// calling this method thus you need to call 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Bind"/>
        /// and
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Listen"/>
        /// methods first (or their asynchronous versions).
        /// <note>
        /// Web proxy doesn't able to accept incoming connection. So calling
        /// this method on the instance of the <b>SocketEx</b> class which is configured
        /// to work with the web proxy server
        /// (<see cref="BytesRoad.Net.Sockets.ProxyType.HttpConnect"/> proxy type) 
        /// will throw the 
        /// <see cref="System.InvalidOperationException"/> exception.
        /// </note>
        /// </remarks>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// The operation is unsupported.
        /// </exception>
        public IAsyncResult BeginAccept(AsyncCallback callback, object state)
        {
            return (IAsyncResult)BeginTimeoutOp(
                _acceptTimeout, 
                new Accept_Op(_baseSocket), 
                callback, 
                state);
        }

        /// <summary>
        /// Completes the asynchronous operation to accept incoming connection.
        /// </summary>
        /// <param name="asyncResult">
        /// An 
        /// <see cref="System.IAsyncResult">IAsyncResult</see>
        /// that stores state information for 
        /// this asynchronous operation.
        /// </param>
        /// <returns>
        /// An instance of the <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// for the created connection.
        /// </returns>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        public SocketEx EndAccept(IAsyncResult asyncResult)
        {
            return (SocketEx)EndTimeoutOp(new Accept_Op(_baseSocket), asyncResult);
        }
        #endregion

        #region Connect functions 

        #region Op class
        class ConnectOp : IOp
        {
            string _hostName = null;
            int _hostPort = -1;
            SocketBase _baseSocket = null;
            EndPoint _remoteEP = null;

            internal ConnectOp(SocketBase baseSocket)
            {
                _baseSocket = baseSocket;
            }

            internal ConnectOp(
                SocketBase baseSocket, 
                string hostName, 
                int hostPort)
            {
                _baseSocket = baseSocket;
                _hostName = hostName;
                _hostPort = hostPort;
            }

            internal ConnectOp(SocketBase baseSocket, EndPoint remoteEP)
            {
                _baseSocket = baseSocket;
                _remoteEP = remoteEP;
            }
        
            #region IOp Members

            internal override object Execute()
            {
                if(null != _remoteEP)
                    _baseSocket.Connect(_remoteEP);
                else
                    _baseSocket.Connect(_hostName, _hostPort);
                return null;
            }

            internal override object BeginExecute(AsyncCallback cb, object state)
            {
                if(null != _remoteEP)
                    return _baseSocket.BeginConnect(_remoteEP, cb, state);

                return _baseSocket.BeginConnect(_hostName, _hostPort, cb, state);
            }

            internal override object EndExecute(IAsyncResult ar)
            {
                _baseSocket.EndConnect(ar);
                return null;
            }

            #endregion
        }
        #endregion
        
        /// <overloads>
        /// Establishes a connection to a remote host.
        /// </overloads>
        /// <summary>
        /// Establishes a connection to a remote host by using host name and port number specified.
        /// </summary>
        /// <param name="hostName">
        /// The name of the remote host.
        /// </param>
        /// <param name="hostPort">
        /// The port number on the remote host.
        /// </param>
        /// 
        /// <remarks>
        /// <b>Connect</b> method will block until the connection with remote host is
        /// established or error occurs.
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// The <i>hostName</i> parameter is <b>null</b> (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <i>port</i> is less than <see cref="System.Net.IPEndPoint.MinPort">MinPort</see>. 
        ///<para>-or-</para>
        ///    <i>port</i> is greater than <see cref="System.Net.IPEndPoint.MaxPort">MaxPort</see>.
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket used to complete requested operation.
        /// </exception>
        public void Connect(string hostName, int hostPort)
        {
            DoTimeoutOp(
                _connectTimeout, 
                new ConnectOp(_baseSocket, hostName, hostPort));
        }

        /// <summary>
        /// Establishes a connection to a remote host by using specified instance of the
        /// <see cref="System.Net.EndPoint"/> class.
        /// </summary>
        /// 
        /// <param name="remoteEP">
        /// An instance of the <see cref="System.Net.EndPoint"/> class which 
        /// represents remote end point to connect to.
        /// </param>
        /// 
        /// <remarks>
        /// <b>Connect</b> method will block until the connection with remote host is
        /// established or error occurs.
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// The <i>remoteEP</i> parameter is <b>null</b> (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket used to complete requested operation.
        /// </exception>
        public void Connect(EndPoint remoteEP)
        {
            DoTimeoutOp(
                _connectTimeout, 
                new ConnectOp(_baseSocket, remoteEP));
        }

        /// <overloads>
        /// Begins an asynchronous connection to the remote host.
        /// </overloads>
        /// <summary>
        /// Begins an asynchronous connection to the remote host by using host name and port number specified.
        /// </summary>
        /// <param name="hostName">The name of the remote host.</param>
        /// <param name="port">Port number on the remote host.</param>
        /// <param name="callback">
        /// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
        /// </param> 
        /// <param name="state">
        /// An object containing state information for this request.
        /// </param>
        /// <returns>
        /// An <see cref="System.IAsyncResult">IAsyncResult</see>
        /// that references the asynchronous connection.
        /// </returns>
        /// <remarks>
        /// The <b>BeginConnect</b> method starts an asynchronous
        /// request for a remote host connection.
        /// It returns immediately and does not wait for 
        /// the asynchronous call to complete.
        /// <para>
        /// The 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.EndConnect"/>
        /// method is used to retrieve the results of 
        /// the asynchronous call. It can be called 
        /// any time after <b>BeginConnect</b>; if the asynchronous 
        /// call has not completed,
        /// <b>EndConnect</b>
        /// will block until it completes.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// The <i>hostName</i> parameter is <b>null</b> (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <i>port</i> is less than <see cref="System.Net.IPEndPoint.MinPort">MinPort</see>. 
        ///<para>-or-</para>
        ///    <i>port</i> is greater than <see cref="System.Net.IPEndPoint.MaxPort">MaxPort</see>.
        /// </exception>
        public IAsyncResult BeginConnect(
            string hostName, 
                                        int port, 
                                        AsyncCallback callback, 
                                        object state)
        {
            return (IAsyncResult)BeginTimeoutOp(
                _connectTimeout, 
                new ConnectOp(_baseSocket, hostName, port), 
                callback, state);
        }

        /// <summary>
        /// Begins an asynchronous connection to the remote host by using specified instance of the
        /// <see cref="System.Net.EndPoint"/> class.
        /// </summary>
        /// <param name="remoteEP">
        /// An instance of the <see cref="System.Net.EndPoint"/> class which 
        /// represents remote end point to connect to.
        /// </param>
        /// <param name="callback">
        /// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
        /// </param> 
        /// <param name="state">
        /// An object containing state information for this request.
        /// </param>
        /// <returns>
        /// An <see cref="System.IAsyncResult">IAsyncResult</see>
        /// that references the asynchronous connection.
        /// </returns>
        /// <remarks>
        /// The <b>BeginConnect</b> method starts an asynchronous
        /// request for a remote host connection.
        /// It returns immediately and does not wait for 
        /// the asynchronous call to complete.
        /// <para>
        /// The 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.EndConnect"/>
        /// method is used to retrieve the results of 
        /// the asynchronous call. It can be called 
        /// any time after <b>BeginConnect</b>; if the asynchronous 
        /// call has not completed,
        /// <b>EndConnect</b>
        /// will block until it completes.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// The <i>remoteEP</i> parameter is <b>null</b> (<b>Nothing</b> in Visual Basic).
        /// </exception>
        public IAsyncResult BeginConnect(
            EndPoint remoteEP, 
                                        AsyncCallback callback, 
                                        object state)
        {
            return (IAsyncResult)BeginTimeoutOp(
                _connectTimeout, 
                new ConnectOp(_baseSocket, remoteEP), 
                callback, 
                state);
        }
        
        /// <summary>
        /// Completes the asynchronous connect to remote host.
        /// </summary>
        /// <param name="asyncResult">
        /// An 
        /// <see cref="System.IAsyncResult">IAsyncResult</see>
        /// that stores state information for 
        /// this asynchronous operation.
        /// </param>
        /// 
        /// <remarks>
        /// <b>EndConnect</b> is a blocking method that completes the 
        /// asynchronous connection to remote host 
        /// started in the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.BeginConnect"/> method.
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        public void EndConnect(IAsyncResult asyncResult)
        {
            EndTimeoutOp(new ConnectOp(_baseSocket), asyncResult);
        }
        #endregion

        #region Bind functions
        
        #region Op class
        class Bind_Op: IOp
        {
            SocketBase _baseSocket;
            SocketEx   _primConnSock; // primary connection (used for socks proxy)

            // constructor used for async end
            internal Bind_Op(SocketBase baseSocket)
            {
                _baseSocket = baseSocket;
                _primConnSock = null;
            }

            internal Bind_Op(SocketBase baseSocket, SocketEx primConnSock)
            {
                _baseSocket = baseSocket;
                _primConnSock = primConnSock;
            }

            #region IOp Members

            internal override object Execute()
            {
                _baseSocket.Bind(_primConnSock._baseSocket);
                return null;
            }

            internal override object BeginExecute(AsyncCallback cb, object state)
            {
                return _baseSocket.BeginBind(_primConnSock._baseSocket, cb, state);
            }

            internal override object EndExecute(IAsyncResult ar)
            {
                _baseSocket.EndBind(ar);
                return null;
            }
            #endregion
        }

        #endregion

        /// <summary>
        /// Associates a <see cref="BytesRoad.Net.Sockets.SocketEx"/> with
        /// local end point at the proxy server.
        /// </summary>
        /// <param name="socket">
        /// An instance of the <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// class which already connected to the remote host through the proxy
        /// server. See remarks for more details.
        /// </param>
        /// <remarks>
        /// The behavior of the <b>Bind</b> method depends on the proxy type specified
        /// while instance of the <b>SocketEx</b> class constructed. Table below 
        /// represents the behavior of the method for different types of proxy server. 
        /// 
        /// <list type="table">
        /// <listheader>
        /// <term>Type of proxy server</term>
        /// <description>Behavior of the <b>Bind</b> method</description>
        /// </listheader>
        /// 
        /// <item>
        /// <term>
        /// <see cref="BytesRoad.Net.Sockets.ProxyType.None"/>
        /// </term>
        /// <description>
        /// The socket will be bound locally to the same IP address 
        /// as the instance of the <b>SocketEx</b> class specified by 
        /// <i>socket</i> parameter.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>
        /// <see cref="BytesRoad.Net.Sockets.ProxyType.HttpConnect"/>
        /// </term>
        /// <description>
        /// Web proxy not support binding command. The 
        /// <see cref="System.InvalidOperationException"/> would be thrown.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>
        /// <see cref="BytesRoad.Net.Sockets.ProxyType.Socks4"/>
        /// <para>-or-</para>
        /// <see cref="BytesRoad.Net.Sockets.ProxyType.Socks4a"/>
        /// <para>-or-</para>
        /// <see cref="BytesRoad.Net.Sockets.ProxyType.Socks5"/>
        /// </term>
        /// <description>
        /// The socket will be bound at the proxy server.
        /// Port and IP address may be retrieved via 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.LocalEndPoint"/> property.
        /// An instance specified by <i>socket</i> parameter must be connected 
        /// with the remote host through physically the same proxy server as 
        /// the instance of the <b>SocketEx</b> class on which 
        /// <b>Bind</b> method are called.
        /// </description>
        /// </item>
        /// 
        /// </list>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.InvalidOperationException">
        /// The operation is unsupported.
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        public void Bind(SocketEx socket)
        {
            DoTimeoutOp(_connectTimeout, new Bind_Op(_baseSocket, socket));
        }

        /// <summary>
        /// Begins an asynchronous binding.
        /// </summary>
        /// <param name="socket">
        /// An instance of the <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// class which already connected to the remote host through the proxy
        /// server. See remarks for more details.
        /// </param>
        /// <param name="callback">
        /// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
        /// </param>
        /// <param name="state">
        /// An object containing state information for this request.
        /// </param>
        /// <returns>
        /// An <see cref="System.IAsyncResult">IAsyncResult</see>
        /// that references the asynchronous bind.
        /// </returns>
        /// 
        /// <remarks>
        /// The <b>BeginBind</b> method starts an asynchronous
        /// binding request.
        /// It returns immediately and does not wait for 
        /// the asynchronous call to complete.
        /// <para>
        /// The 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.EndBind"/>
        /// method is used to retrieve the results of 
        /// the asynchronous call. It can be called 
        /// any time after <b>BeginBind</b>; if the asynchronous 
        /// call has not completed,
        /// <b>EndBind</b>
        /// will block until it completes.
        /// </para>
        /// 
        /// The behavior of the <b>BeginBind</b> method depends on the proxy type specified
        /// while instance of the <b>SocketEx</b> class constructed. Table below 
        /// represents the behavior of the method for different types of proxy server. 
        /// 
        /// <list type="table">
        /// <listheader>
        /// <term>Type of proxy server</term>
        /// <description>Behavior of the <b>BeginBind</b> method</description>
        /// </listheader>
        /// 
        /// <item>
        /// <term>
        /// <see cref="BytesRoad.Net.Sockets.ProxyType.None"/>
        /// </term>
        /// <description>
        /// The socket will be bound locally to the same IP address 
        /// as the instance of the <b>SocketEx</b> class specified by 
        /// <i>socket</i> parameter.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>
        /// <see cref="BytesRoad.Net.Sockets.ProxyType.HttpConnect"/>
        /// </term>
        /// <description>
        /// Web proxy not support binding command. The 
        /// <see cref="System.InvalidOperationException"/> would be thrown.
        /// </description>
        /// </item>
        /// 
        /// <item>
        /// <term>
        /// <see cref="BytesRoad.Net.Sockets.ProxyType.Socks4"/>
        /// <para>-or-</para>
        /// <see cref="BytesRoad.Net.Sockets.ProxyType.Socks4a"/>
        /// <para>-or-</para>
        /// <see cref="BytesRoad.Net.Sockets.ProxyType.Socks5"/>
        /// </term>
        /// <description>
        /// The socket will be bound at the proxy server.
        /// Port and IP address may be retrieved via 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.LocalEndPoint"/> property.
        /// An instance specified by <i>socket</i> parameter must be 
        /// connected with the remote host through physically the same proxy server 
        /// as the instance of the <b>SocketEx</b> class on which <b>BeginBind</b>
        /// method are called.
        /// </description>
        /// </item>
        /// 
        /// </list>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.InvalidOperationException">
        /// The operation is unsupported.
        /// </exception>
        public IAsyncResult BeginBind(
            SocketEx socket, 
                                        AsyncCallback callback, 
                                        object state)
        {
            return (IAsyncResult)BeginTimeoutOp(
                _connectTimeout,
                new Bind_Op(_baseSocket, socket),
                callback,
                state);
        }

        /// <summary>
        /// Ends asynchronous binding.
        /// </summary>
        /// <param name="asyncResult">
        /// An 
        /// <see cref="System.IAsyncResult">IAsyncResult</see>
        /// that stores state information for 
        /// this asynchronous operation.
        /// </param>
        /// 
        /// <remarks>
        /// <b>EndBind</b> is a blocking method that completes the 
        /// asynchronous binding operation started in 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.BeginBind"/>.
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        public void EndBind(IAsyncResult asyncResult)
        {
            EndTimeoutOp(new Bind_Op(_baseSocket), asyncResult);
        }
        #endregion

        #region Listen function

        #region Op class
        class Listen_Op : IOp
        {
            int _backlog;
            SocketBase _baseSocket;

            internal Listen_Op(SocketBase baseSocket, int backlog)
            {
                _backlog = backlog;
                _baseSocket = baseSocket;
            }

            #region IOp Members

            internal override object Execute()
            {
                _baseSocket.Listen(_backlog);
                return null;
            }

            internal override object BeginExecute(AsyncCallback cb, object state)
            {
                throw new NotSupportedException("BeginListen is not supported.");
            }

            internal override object EndExecute(IAsyncResult ar)
            {
                throw new NotSupportedException("EndListen is not supported.");
            }

            #endregion
        }
        #endregion
        

        /// <summary>
        /// Places a <see cref="BytesRoad.Net.Sockets.SocketEx"/> in a listening state.
        /// </summary>
        /// <param name="backlog">Lenght of the buffer where incoming connections are queued.</param>
        /// <remarks>
        /// <b>Listen</b> causes <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// to listen for incoming connection attempts. 
        /// The <i>backlog</i> parameter specifies the number of incoming connections
        /// that can be queued for acceptance.
        /// <para>
        /// <b>Listen</b> function is meaningful only when called on instance of the
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/> class which configured
        /// not to use any type of proxy servers. That is 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.ProxyType"/>
        /// property equals to
        /// <see cref="BytesRoad.Net.Sockets.ProxyType.None"/>.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        public void Listen(int backlog)
        {
            DoTimeoutOp(_connectTimeout, new Listen_Op(_baseSocket, backlog));
        }
        #endregion

        #region Shutdown function

        /// <summary>
        /// Disables sends and receives.
        /// </summary>
        /// <param name="how">
        /// One of the <see cref="System.Net.Sockets.SocketShutdown"/>
        /// values that specifies the operation that will no longer be allowed.
        /// </param>
        /// <remarks>
        /// Calls 
        /// <see cref="System.Net.Sockets.Socket.Shutdown"/>
        /// method on a socket which is used in underlying layer
        /// for communication with proxy server.
        /// </remarks>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        public void Shutdown(SocketShutdown how)
        {
            _baseSocket.Shutdown(how);
        }
        #endregion

        #region Receive functions 

        #region Op class
        class Receive_Op : IOp
        {
            byte[] _buffer;
            int _offset;
            int _size;
            SocketBase _baseSocket;

            internal Receive_Op(SocketBase baseSocket)
            {
                _baseSocket = baseSocket;
            }

            internal Receive_Op(
                SocketBase baseSocket,
                byte[] buffer, int offset, int size)
            {
                _baseSocket = baseSocket;
                _buffer = buffer;
                _offset = offset;
                _size = size;
            }

            #region IOp Members

            internal override object Execute()
            {
                return _baseSocket.Receive(_buffer, _offset, _size);
            }

            internal override object BeginExecute(AsyncCallback cb, object state)
            {
                return _baseSocket.BeginReceive(
                    _buffer,
                    _offset,
                    _size,
                    cb,
                    state);
            }

            internal override object EndExecute(IAsyncResult ar)
            {
                return _baseSocket.EndReceive(ar);
            }

            #endregion
        }
        #endregion

    
        /// <overloads>
        /// Receives data from the remote host.
        /// </overloads>
        /// <summary>
        /// Receives data from the remote host and store it in the specified buffer.
        /// </summary>
        /// <param name="buffer">
        /// Buffer to store the received data.
        /// </param>
        /// <returns>
        /// The number of bytes received.
        /// </returns>
        /// <remarks>
        /// If no data is available for reading, the <b>Receive</b> method will block
        /// until data is available. You can use the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Available"/> 
        /// property to determine if data is available for reading. When 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Available"/>
        /// is non-zero, retry the receive operation.
        /// <para>
        /// The <b>Receive</b> method will 
        /// read as much data as is available, up to the size of the buffer. If 
        /// the remote host shuts down the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// connection with the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Shutdown"/> 
        /// method, and all available data has been received, the <b>Receive</b> method 
        /// will complete immediately and return zero bytes.
        /// </para>
        /// </remarks>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// <i>buffer</i> is a null reference (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        public int Receive(byte[] buffer)
        {
            return (int)DoTimeoutOp(
                _recvTimeout, 
                new Receive_Op(_baseSocket, buffer, 0, buffer.Length));
        }

        /// <summary>
        /// Receives specified amount of data from the remote host and store it in the supplied buffer.
        /// </summary>
        /// <param name="buffer">Buffer to store the received data.</param>
        /// <param name="size">The number of bytes to receive.</param>
        /// <returns>The number of bytes received.</returns>
        /// 
        /// <remarks>
        /// If no data is available for reading, the <b>Receive</b> method will block
        /// until data is available. You can use the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Available"/> 
        /// property to determine if data is available for reading. When 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Available"/>
        /// is non-zero, retry the receive operation.
        /// <para>
        /// The <b>Receive</b> method will 
        /// read as much data as is available, up to the size of the buffer. If 
        /// the remote host shuts down the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// connection with the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Shutdown"/> 
        /// method, and all available data has been received, the <b>Receive</b> method 
        /// will complete immediately and return zero bytes.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// <i>buffer</i> is a null reference (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <i>size</i> exceeds the size of <i>buffer</i>.
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        public int Receive(byte[] buffer, int size)
        {
            return (int)DoTimeoutOp(
                _recvTimeout, 
                new Receive_Op(_baseSocket, buffer, 0, size));
        }

        /// <summary>
        /// Receives specified amount of data from the remote host and store it starting from
        /// the specified offset in the supplied buffer.
        /// </summary>
        /// <param name="buffer">Buffer to store the received data.</param>
        /// <param name="offset">The location in <i>buffer</i> to store the received data.</param>
        /// <param name="size">The number of bytes to receive.</param>
        /// <returns>The number of bytes received.</returns>
        /// 
        /// <remarks>
        /// If no data is available for reading, the <b>Receive</b> method will block
        /// until data is available. You can use the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Available"/> 
        /// property to determine if data is available for reading. When 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Available"/>
        /// is non-zero, retry the receive operation.
        /// <para>
        /// The <b>Receive</b> method will 
        /// read as much data as is available, up to the size of the buffer. If 
        /// the remote host shuts down the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// connection with the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Shutdown"/> 
        /// method, and all available data has been received, the <b>Receive</b> method 
        /// will complete immediately and return zero bytes.
        /// </para>
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// <i>buffer</i> is a null reference (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <i>offset</i> is less than 0. 
        /// <para>-or-</para>
        /// <i>offset</i> is greater than the length of <i>buffer</i>.
        /// <para>-or-</para>
        /// <i>size</i> is less than 0.
        /// <para>-or-</para>
        /// <i>size</i> is greater than the length of <i>buffer</i> minus 
        /// the value of the <i>offset</i> parameter.
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        public int Receive(byte[] buffer, int offset, int size)
        {
            return (int)DoTimeoutOp(
                _recvTimeout, 
                new Receive_Op(_baseSocket, buffer, offset, size));
        }

        /// <summary>
        /// Begins an asynchronous receive operation.
        /// </summary>
        /// <param name="buffer">Buffer to store the received data.</param>
        /// <param name="offset">The location in <i>buffer</i> to store the received data.</param>
        /// <param name="size">The number of bytes to receive.</param>
        /// <param name="callback">
        /// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
        /// </param>
        /// <param name="state">
        /// An object containing state information for this request.
        /// </param>
        /// <returns>
        /// An <see cref="System.IAsyncResult">IAsyncResult</see>
        /// that references the asynchronous receive.
        /// </returns>
        /// 
        /// <remarks>
        /// The <b>BeginReceive</b> method starts an asynchronous
        /// receive operation.
        /// It returns immediately and does not wait for 
        /// the asynchronous call to complete.
        /// <para>
        /// The 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.EndReceive"/>
        /// method is used to retrieve the results of 
        /// the asynchronous call. It can be called 
        /// any time after <b>BeginReceive</b>; if the asynchronous 
        /// call has not completed,
        /// <b>EndReceive</b>
        /// will block until it completes.
        /// </para>
        /// <para>
        /// The receive operation will not completed until the data is available for reading or
        /// error occurs. You can use the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Available"/> 
        /// property to determine if data is available for reading. When 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Available"/>
        /// is non-zero, retry the receive operation.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// <i>buffer</i> is a null reference (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <i>offset</i> is less than 0. 
        /// <para>-or-</para>
        /// <i>offset</i> is greater than the length of <i>buffer</i>.
        /// <para>-or-</para>
        /// <i>size</i> is less than 0.
        /// <para>-or-</para>
        /// <i>size</i> is greater than the length of <i>buffer</i> minus 
        /// the value of the <i>offset</i> parameter.
        /// </exception>
        public IAsyncResult BeginReceive(
            byte[] buffer,
                                            int offset,
                                            int size,
                                            AsyncCallback callback,
                                            object state)
        {
            return (IAsyncResult)BeginTimeoutOp(
                _recvTimeout, 
                new Receive_Op(_baseSocket, buffer, offset, size),
                callback, state);
        }

        /// <summary>
        /// Ends a pending asynchronous receive.
        /// </summary>
        /// <param name="asyncResult">
        /// An 
        /// <see cref="System.IAsyncResult">IAsyncResult</see>
        /// that stores state information for this asynchronous operation.
        /// </param>
        /// <returns>The number of bytes received.</returns>
        /// 
        /// <remarks>
        /// <b>EndReceive</b> is a blocking method that completes the 
        /// asynchronous receive operation started in the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.BeginReceive"/> method.
        /// 
        /// The <b>EndReceive</b> method will read as much data as is available
        /// up to the number of bytes you specified in the <i>size</i> parameter of the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.BeginReceive"/> method. 
        /// If the remote host shuts down the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/> 
        /// connection with the <see cref="BytesRoad.Net.Sockets.SocketEx.Shutdown"/> 
        /// method, and all available data has been received, the <b>EndReceive</b> 
        /// method will complete immediately and return zero bytes.
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// <i>asyncResult</i> is a null reference 
        /// (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <i>asyncResult</i> was not returned by a call to the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.BeginReceive"/>
        /// method.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <b>EndReceive</b> was previously called for the 
        /// asynchronous receiving.
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>        
        public int EndReceive(IAsyncResult asyncResult)
        {
            return (int)EndTimeoutOp(new Receive_Op(_baseSocket), asyncResult);
        }

        #endregion

        #region Send functions

        #region Op class
        class Send_Op : IOp
        {
            byte[] _buffer;
            int _offset;
            int _size;
            SocketBase _baseSocket;

            internal Send_Op(SocketBase baseSocket)
            {
                _baseSocket = baseSocket;
            }

            internal Send_Op(SocketBase baseSocket, byte[] buffer,
                int offset, int size)
            {
                _baseSocket = baseSocket;
                _buffer = buffer;
                _offset = offset;
                _size = size;
            }

            #region IOp Members

            internal override object Execute()
            {
                return _baseSocket.Send(_buffer, _offset, _size);
            }

            internal override object BeginExecute(AsyncCallback cb, object state)
            {
                return _baseSocket.BeginSend(_buffer, _offset, _size, cb, state);
            }

            internal override object EndExecute(IAsyncResult ar)
            {
                return _baseSocket.EndSend(ar);
            }
            #endregion
        }
        #endregion

        /// <overloads>
        /// Sends data to the remote host.
        /// </overloads>
        /// <summary>
        /// Sends all data from the specified buffer to the remote host.
        /// </summary>
        /// <param name="buffer">Data to send.</param>
        /// <returns>
        /// Number of bytes sent.
        /// </returns>
        /// 
        /// <remarks>
        /// <b>Send</b> will block until all of the bytes in the buffer are sent.
        /// There is also no guarantee that the data you send will appear on the network immediately. 
        /// To increase network efficiency, the underlying system may delay transmission until 
        /// a significant amount of outgoing data is collected. A successful completion of the Send 
        /// method means that the underlying system has had room to buffer your data for a network send.
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// <i>buffer</i> is a null reference (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        public int Send(byte[] buffer)
        {
            return (int)DoTimeoutOp(
                _sendTimeout, 
                new Send_Op(_baseSocket, buffer, 0, buffer.Length));
        }

        /// <summary>
        /// Sends specified amount of data to the remote host.
        /// </summary>
        /// <param name="buffer">Data to send.</param>
        /// <param name="size">The number of bytes to send.</param>
        /// <returns>
        /// Number of bytes sent.
        /// </returns>
        /// 
        /// <remarks>
        /// <b>Send</b> will block until all of the bytes in the buffer are sent.
        /// There is also no guarantee that the data you send will appear on the network immediately. 
        /// To increase network efficiency, the underlying system may delay transmission until 
        /// a significant amount of outgoing data is collected. A successful completion of the Send 
        /// method means that the underlying system has had room to buffer your data for a network send.
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// <i>buffer</i> is a null reference (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <i>size</i> is less than 0 or exceeds the size of <i>buffer</i>.
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        public int Send(byte[] buffer, int size)
        {
            return (int)DoTimeoutOp(
                _sendTimeout, 
                new Send_Op(_baseSocket, buffer, 0, size));
        }

        /// <summary>
        /// Sends specified amount of data starting from the specified position 
        /// in the supplied buffer to the remote host.
        /// </summary>
        /// <param name="buffer">Data to send.</param>
        /// <param name="offset">The position in the data buffer at which to begin sending data.</param>
        /// <param name="size">The number of bytes to send.</param>
        /// <returns>
        /// Number of bytes sent.
        /// </returns>
        /// 
        /// <remarks>
        /// <b>Send</b> will block until all of the bytes in the buffer are sent.
        /// There is also no guarantee that the data you send will appear on the network immediately. 
        /// To increase network efficiency, the underlying system may delay transmission until 
        /// a significant amount of outgoing data is collected. A successful completion of the Send 
        /// method means that the underlying system has had room to buffer your data for a network send.
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// <i>buffer</i> is a null reference (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <i>offset</i> is less than 0.
        /// <para>-or-</para>
        /// <i>offset</i> is greater than the length of <i>buffer</i>.
        /// <para>-or-</para>
        /// <i>size</i> is less than 0.
        /// <para>-or-</para>
        /// <i>size</i> is greater than the length of <i>buffer</i> minus
        /// the value of the <i>offset</i> parameter.
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        public int Send(byte[] buffer, int offset, int size)
        {
            return (int)DoTimeoutOp(
                _sendTimeout, 
                new Send_Op(_baseSocket, buffer, offset, size));
        }

        /// <summary>
        /// Begins an asynchronous send operation.
        /// </summary>
        /// <param name="buffer">Data to send.</param>
        /// <param name="offset">The position in the data buffer at which to begin sending data.</param>
        /// <param name="size">The number of bytes to send.</param>
        /// <param name="callback">
        /// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
        /// </param>
        /// <param name="state">
        /// An object containing state information for this request.
        /// </param>
        /// <returns>
        /// An <see cref="System.IAsyncResult">IAsyncResult</see>
        /// that references the asynchronous send.
        /// </returns>
        /// 
        /// <remarks>
        /// The <b>BeginSend</b> method starts an asynchronous
        /// send operation.
        /// It returns immediately and does not wait for 
        /// the asynchronous call to complete.
        /// <para>
        /// The 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.EndSend"/>
        /// method is used to retrieve the results of 
        /// the asynchronous call. It can be called 
        /// any time after <b>BeginSend</b>; if the asynchronous 
        /// call has not completed,
        /// <b>EndSend</b>
        /// will block until it completes.
        /// </para>
        /// <para>
        /// Send operation will not completed until all of the bytes in the buffer are sent.
        /// There is also no guarantee that the data you send will appear on the network immediately. 
        /// To increase network efficiency, the underlying system may delay transmission until 
        /// a significant amount of outgoing data is collected. A successful completion of the send 
        /// operation means that the underlying system has had room to buffer your data for a network send.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// <i>buffer</i> is a null reference (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <i>offset</i> is less than 0.
        /// <para>-or-</para>
        /// <i>offset</i> is greater than the length of <i>buffer</i>.
        /// <para>-or-</para>
        /// <i>size</i> is less than 0.
        /// <para>-or-</para>
        /// <i>size</i> is greater than the length of <i>buffer</i> minus
        /// the value of the <i>offset</i> parameter.
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        public IAsyncResult BeginSend(
            byte[] buffer,
                                        int offset,
                                        int size,
                                        AsyncCallback callback,
                                        object state)
        {
            return (IAsyncResult)BeginTimeoutOp(
                _sendTimeout, 
                new Send_Op(_baseSocket, buffer, offset, size),
                callback, state);
        }

        /// <summary>
        /// Ends a pending asynchronous send.
        /// </summary>
        /// <param name="asyncResult">
        /// An 
        /// <see cref="System.IAsyncResult">IAsyncResult</see>
        /// that stores state information for this asynchronous operation.
        /// </param>
        /// <returns>The number of bytes sent.</returns>
        /// 
        /// <remarks>
        /// <b>EndSend</b> is a blocking method that completes the 
        /// asynchronous send operation started in the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.BeginSend"/> method.
        /// 
        /// <para>
        /// <b>EndSend</b> will block until the requested number of bytes are sent. 
        /// There is no guarantee that the data you send will appear on the 
        /// network immediately. To increase network efficiency, the underlying 
        /// system may delay transmission until a significant amount of outgoing 
        /// data is collected. A successful completion of the <b>EndSend</b> method 
        /// means that the underlying system has had room to buffer your 
        /// data for a network send.
        /// </para>
        /// 
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.SocketEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// <i>asyncResult</i> is a null reference 
        /// (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <i>asyncResult</i> was not returned by a call to the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.BeginSend"/>
        /// method.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <b>EndSend</b> was previously called for the 
        /// asynchronous receiving.
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>        
        public int EndSend(IAsyncResult asyncResult)
        {
            return (int)EndTimeoutOp(new Send_Op(_baseSocket), asyncResult);
        }

        #endregion


        #region Disposing pattern
        /// <summary>
        /// Frees resources used by the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// class.
        /// </summary>
        /// <remarks>
        /// The 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// class finalizer calls the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Dispose">Dispose</see>
        /// method to free resources associated with the <b>SocketEx</b>
        /// object.
        /// </remarks>
        ~SocketEx()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases the resources used by the
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>.
        /// </summary>
        /// <remarks>
        /// Internally simply calls <see cref="BytesRoad.Net.Sockets.SocketEx.Dispose"/>.
        /// </remarks>
        public void Close()
        {
            Dispose();
        }

        /// <overloads>
        /// Releases the resources used by the
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>.
        /// </overloads>
        /// 
        /// <summary>
        /// Releases all resources used by the
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>.
        /// </summary>
        /// <remarks>
        /// Call <b>Dispose</b> when you are finished using the
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>. 
        /// The <b>Dispose</b> method leaves the <b>SocketEx</b> in an 
        /// unusable state. After calling <b>Dispose</b>, you must release 
        /// all references to the <b>SocketEx</b> so the garbage collector
        /// can reclaim the memory that the <b>SocketEx</b> was occupying.
        /// </remarks>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <b>true</b> to release both managed and unmanaged resources;
        /// <b>false</b> to release only unmanaged resources.
        /// </param>
        /// <remarks>
        /// This method is called by the public 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Dispose">Dispose()</see>
        /// method and the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Finalize">Finalize</see>
        /// method. <b>Dispose()</b> invokes the protected 
        /// <b>Dispose(Boolean)</b>
        /// method with the <i>disposing</i> parameter set to <b>true</b>.
        /// <b>Finalize</b> invokes <b>Dispose</b> with <i>disposing</i>
        /// set to <b>false</b>.
        /// When the <i>disposing</i> parameter is <b>true</b>,
        /// this method releases all resources held by any managed 
        /// objects that this <b>SocketEx</b> references. 
        /// This method invokes the <b>Dispose()</b> method of each 
        /// referenced object.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            lock(this)
            {
                if(disposing)
                {
                }

                if(!_disposed)
                {
                    _baseSocket.Dispose();
                    _timer.Dispose();
                    _disposed = true;
                }
            }
        }
        #endregion
    }
}
