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
using System.IO;

using BytesRoad.Net.Sockets.Advanced;

namespace BytesRoad.Net.Sockets
{
    /// <summary>
    /// Provides the stream of data for network communication.
    /// </summary>
    /// <remarks>
    /// The <b>NetworkStreamEx</b> class provides methods for sending and receiving data over
    /// network. <b>NetworkStreamEx</b> may be used for both synchronous and asynchronous data transfer. 
    /// In order to create an instance of the <b>NetworkStreamEx</b> class, you must provide a connected 
    /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>. 
    /// You can also specify what 
    /// <see cref="System.IO.FileAccess"/> permission the <b>NetworkStreamEx</b> 
    /// has over the provided <b>SocketEx</b>.
    /// By default, closing the <b>NetworkStreamEx</b> does not 
    /// close the provided <b>SocketEx</b>. If you want the <b>NetworkStreamEx</b> to have
    /// permission to close the provided <b>SocketEx</b>, you must specify <b>true</b> for the 
    /// value of the <i>ownsSocket</i> constructor parameter.
    ///  
    /// <para>
    /// Use the <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.Write"/> and 
    /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.Read"/> methods for simple single thread
    /// synchronous blocking I/O. If you want to process your I/O using separate threads, consider
    /// using the 
    /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.BeginWrite"/>/<see cref="BytesRoad.Net.Sockets.NetworkStreamEx.EndWrite"/>
    /// and 
    /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.BeginRead"/>/<see cref="BytesRoad.Net.Sockets.NetworkStreamEx.EndRead"/>
    /// methods for communication.
    /// </para>
    /// 
    /// <para>
    /// The <b>NetworkStreamEx</b> does not support random access to the network data stream.
    /// The value of the
    /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.CanSeek"/>
    /// property, which indicates whether the stream supports seeking, is always <b>false</b>;
    /// reading the
    /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.Position"/> 
    /// property, reading the 
    /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.Length"/>  
    /// property, or calling the 
    /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.Seek"/> 
    /// method will throw a <b>NotSupportedException</b>.
    /// </para>
    /// 
    /// </remarks>
    public class NetworkStreamEx : Stream
    {
        #region Async classes
        class Read_SO : AsyncResultBase
        {
            int _read = 0;
            int _offset = 0;
            int _size = 0;
            byte[] _buffer = null;

            internal Read_SO(
                byte[] buffer,
                int offset, 
                int size, 
                AsyncCallback cb, 
                object state) : base(cb, state)
            {
                _buffer = buffer;
                _offset = offset;
                _size = size;
            }

            internal int Read
            {
                get { return _read; }
                set { _read = value; }
            }

            internal int Offset
            {
                get { return _offset; }
            }

            internal int Size
            {
                get { return _size; }
            }

            internal byte[] Buffer
            {
                get { return _buffer; }
            }
        }

        class Write_SO : AsyncResultBase
        {
            int _sent = 0;
            int _offset = 0;
            int _size = 0;
            byte[] _buffer = null;

            internal Write_SO(
                byte[] buffer,
                int offset, 
                int size, 
                AsyncCallback cb, 
                object state) : base(cb, state)
            {
                _buffer = buffer;
                _offset = offset;
                _size = size;
            }

            internal int Sent
            {
                get { return _sent; }
                set { _sent = value; }
            }

            internal int Offset
            {
                get { return _offset; }
            }

            internal int Size
            {
                get { return _size; }
            }

            internal byte[] Buffer
            {
                get { return _buffer; }
            }
        }

        #endregion

        SocketEx _socket = null;
        bool _ownsSocket = false;
        AsyncBase _asyncCtx = new AsyncBase();
        bool _disposed = false;
        FileAccess _access = FileAccess.ReadWrite;

        #region Constructors

        /// <overloads>
        /// Create an instance of the <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> class.
        /// </overloads>
        /// <summary>
        /// Creates the network stream for the specified <see cref="BytesRoad.Net.Sockets.SocketEx"/>.
        /// </summary>
        /// <param name="socket">
        /// The 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// that the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> 
        /// will use to send and receive data over network.
        /// </param>
        /// <remarks>
        /// The 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> 
        /// is created with read/write access to the specified
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>. 
        /// The <b>NetworkStreamEx</b> does not own the underlying <b>SocketEx</b>, so calling the
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.Close"/> method will not 
        /// close the <b>SocketEx</b>.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// <i>socket</i> is null reference (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// specified <i>socket</i> is not connected.
        /// </exception>
        public NetworkStreamEx(SocketEx socket)
        {
            _socket = socket;
        }

        /// <summary>
        /// Creates the network stream for the <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// with the specified ownership.
        /// </summary>
        /// <param name="socket">
        /// The 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// that the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> 
        /// will use to send and receive data over network.
        /// </param>
        /// <param name="ownsSocket">
        /// <b>true</b> to indicate that the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>
        /// will take ownership of the <see cref="BytesRoad.Net.Sockets.SocketEx"/>; otherwise, <b>false</b>. 
        /// </param>
        /// <remarks>
        /// The 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> 
        /// is created with read/write access to the specified
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>. 
        /// If <i>ownsSocket</i> is <b>true</b>, the <b>NetworkStreamEx</b> takes ownership of the 
        /// underlying <see cref="BytesRoad.Net.Sockets.SocketEx"/>, 
        /// and calling the NetworkStreamEx's 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.Close"/>
        /// method will also close the underlying <b>SocketEx</b>.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// <i>socket</i> is null reference (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// specified <i>socket</i> is not connected.
        /// </exception>
        public NetworkStreamEx(SocketEx socket, bool ownsSocket)
        {
            _socket = socket;
            _ownsSocket = ownsSocket;
        }

        /// <summary>
        /// Creates the network stream for the <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// with the specified access rights.
        /// </summary>
        /// <param name="socket">
        /// The 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// that the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> 
        /// will use to send and receive data over network.
        /// </param>
        /// <param name="access">
        /// A bitwise combination of the 
        /// <see cref="System.IO.FileAccess"/>
        /// values, specifying the type of access given to the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>
        /// over the provided <see cref="BytesRoad.Net.Sockets.SocketEx"/>.
        /// </param>
        /// <remarks>
        /// When <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> created with this constructor,
        /// it does not own the underlying <see cref="BytesRoad.Net.Sockets.SocketEx"/>, 
        /// so calling the NetworkStreamEx's 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.Close"/> 
        /// method will not close the underlying <b>SocketEx</b>.
        /// <para>
        /// The <i>access</i> parameter sets the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.CanRead"/> and 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.CanWrite"/> 
        /// properties of the <b>NetworkStreamEx</b>.
        /// If you specify <see cref="System.IO.FileAccess.Write"/>, then the <b>NetworkStreamEx</b> will 
        /// allow writing operations only. If you specify 
        /// <see cref="System.IO.FileAccess.Read"/>, then the <b>NetworkStreamEx</b> will allow read operations only. 
        /// If you specify <see cref="System.IO.FileAccess.ReadWrite"/>, both types of operations will be allowed.
        /// </para>
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// <i>socket</i> is null reference (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// specified <i>socket</i> is not connected.
        /// </exception>
        public NetworkStreamEx(SocketEx socket, FileAccess access)
        {
            _socket = socket;
            _access = access;
        }

        /// <summary>
        /// Creates the network stream for the <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// with the specified access rights and ownership.
        /// </summary>
        /// <param name="socket">
        /// The 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// that the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> 
        /// will use to send and receive data over network.
        /// </param>
        /// <param name="access">
        /// A bitwise combination of the 
        /// <see cref="System.IO.FileAccess"/>
        /// values, specifying the type of access given to the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>
        /// over the provided <see cref="BytesRoad.Net.Sockets.SocketEx"/>.
        /// </param>
        /// <param name="ownsSocket">
        /// <b>true</b> to indicate that the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>
        /// will take ownership of the <see cref="BytesRoad.Net.Sockets.SocketEx"/>; otherwise, <b>false</b>.
        /// </param>
        /// <remarks>
        ///  If <i>ownsSocket</i> is <b>true</b>, the <b>NetworkStreamEx</b> takes ownership of the 
        /// underlying <see cref="BytesRoad.Net.Sockets.SocketEx"/>, and calling the NetworkStreamEx's 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.Close"/>
        /// method will also close the underlying <b>SocketEx</b>.
        /// <para>
        /// The <i>access</i> parameter sets the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.CanRead"/> and 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.CanWrite"/> 
        /// properties of the <b>NetworkStreamEx</b>.
        /// If you specify <see cref="System.IO.FileAccess.Write"/>, then the <b>NetworkStreamEx</b> will 
        /// allow writing operations only. If you specify 
        /// <see cref="System.IO.FileAccess.Read"/>, then the <b>NetworkStreamEx</b> will allow read operations only. 
        /// If you specify <see cref="System.IO.FileAccess.ReadWrite"/>, both types of operations will be allowed.
        /// </para>
        /// 
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// <i>socket</i> is null reference (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// specified <i>socket</i> is not connected.
        /// </exception>
        public NetworkStreamEx(SocketEx socket, FileAccess access, bool ownsSocket)
        {
            _socket = socket;
            _ownsSocket = ownsSocket;
            _access = access;
        }
        #endregion

        #region Attributes
        /// <summary>
        /// Gets a value indicating whether the read operations are allowed.
        /// </summary>
        /// <value>
        /// <b>true</b> if data can be read from the stream; otherwise, <b>false</b>. The default value is <b>true</b>.
        /// </value>
        /// <remarks>
        /// If <b>CanRead</b> is <b>true</b>, 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> will allow calls to the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.Read"/> and 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.BeginRead"/> methods. 
        /// 
        /// Access rights assigned to <b>NetworkStreamEx</b> instance in the constructor. 
        /// There is no way to change them after instance of the 
        /// <b>NetworkStreamEx</b> initialized.
        /// </remarks>
        public override bool CanRead 
        { 
            get 
            { 
                return (_access & FileAccess.Read) == FileAccess.Read; 
            } 
        }

        /// <summary>
        /// Gets a value indicating whether the stream supports seeking. This property always returns <b>false</b>.
        /// </summary>
        /// <value>
        /// <b>false</b> to indicate that 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> not support seeking.
        /// </value>
        /// <remarks>
        /// Always return <b>false</b>.
        /// </remarks>
        public override bool CanSeek 
        { 
            get { return false; } 
        }

        /// <summary>
        /// Gets a value indicating whether the write operations are allowed.
        /// </summary>
        /// <value>
        /// <b>true</b> if data can be written to the stream; otherwise, <b>false</b>. The default value is <b>true</b>.
        /// </value>
        /// <remarks>
        /// If <b>CanWrite</b> is <b>true</b>, 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> will allow calls to the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.Write"/> and 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.BeginWrite"/> methods. 
        /// 
        /// Access rights assigned to <b>NetworkStreamEx</b> instance in the constructor. 
        /// There is no way to change them after instance of the 
        /// <b>NetworkStreamEx</b> initialized.
        /// </remarks>
        public override bool CanWrite 
        { 
            get 
            { 
                return (_access & FileAccess.Write) == FileAccess.Write;  
            } 
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>
        /// has data available for reading.
        /// </summary>
        /// <value>
        /// <b>true</b> if there is data available for reading; otherwise, <b>false</b>.
        /// </value>
        /// <remarks>
        /// Use the <b>DataAvailable</b> method to determine the stream has data available for reading. 
        /// If the remote host shuts down or closes the connection, 
        /// <b>DataAvailable</b> throws a <see cref="System.Net.Sockets.SocketException"/>.
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.IO.IOException">
        /// The underlying <see cref="BytesRoad.Net.Sockets.SocketEx"/> is closed.
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        public virtual bool DataAvailable 
        {
            get 
            { 
                CheckDisposed();
                try
                {
                    return _socket.Available > 0; 
                }
                catch
                {
                    CheckDisposed();
                    throw;
                }
            } 
        }

        /// <summary>
        /// Gets the length of the data available on the stream. 
        /// This property always throws a <see cref="System.NotSupportedException"/>.
        /// </summary>
        /// <value>
        /// This property is not supported by <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>,
        /// and will throw a <see cref="System.NotSupportedException"/>.
        /// </value>
        /// <exception cref="System.NotSupportedException">
        /// any access to the property.
        /// </exception>
        public override long Length
        {
            get
            {
                ThrowPropUnsupported("Length");
                return 0;
            }
        }

        /// <summary>
        /// Gets or sets the current position in the stream. 
        /// This property always throws a <see cref="System.NotSupportedException"/>.
        /// </summary>
        /// <value>
        /// This property is not supported by <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>,
        /// and will throw a <see cref="System.NotSupportedException"/>.
        /// </value>
        /// <exception cref="System.NotSupportedException">
        /// any access to the property.
        /// </exception>        
        public override long Position 
        {
            get { ThrowPropUnsupported("Position"); return 0; }
            set { ThrowPropUnsupported("Position"); }
        }
        #endregion

        #region Helpers
        void ThrowPropUnsupported(string propName)
        {
            string msg = string.Format("'{0}' property is not supported.", propName);
            throw new NotSupportedException(msg);
        }

        void ThrowMetUnsupported(string metName)
        {
            string msg = string.Format("'{0}' method is not supported.", metName);
            throw new NotSupportedException(msg);
        }


        Exception GetDisposedException()
        {
            return new ObjectDisposedException(GetType().FullName, "Object was disposed.");
        }

        void CheckDisposed()
        {
            if(_disposed)
                throw GetDisposedException();
        }
        #endregion

        #region Read

        /// <summary>
        /// Reads data from the network stream.
        /// </summary>
        /// <param name="buffer">The buffer to store data to.</param>
        /// <param name="offset">The location in the <i>buffer</i> where to start storing the received data.</param>
        /// <param name="size">The number of bytes to read.</param>
        /// <returns>The number of bytes read from the network stream.</returns>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> object was disposed.
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
        /// <remarks>
        /// If no data is available for reading, the <b>Read</b> method will block
        /// until data is available. You can use the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.DataAvailable"/> 
        /// property to determine if data is available for reading. When 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.DataAvailable"/>
        /// is non-zero, retry the receive operation.
        /// <para>
        /// The <b>Read</b> method will 
        /// read as much data as specified by <i>size</i> parameter. If 
        /// the remote host shuts down the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// connection with the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Shutdown"/> 
        /// method, and all available data has been received, the <b>Read</b> method 
        /// will complete and return number of bytes was read.
        /// </para>
        /// <note>
        /// The <b>NetworkStreamEx</b> should have access right to read data from the network stream.
        /// You may use <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.CanRead"/> property to check this.
        /// </note>
        /// </remarks>
        public override int Read(byte[] buffer, int offset, int size)
        {
            CheckDisposed();
            _asyncCtx.SetProgress(true);
            try
            {
                int read = 0;
                int num = 1;
                while((num > 0) && (read < size))
                {
                    num = _socket.Receive(buffer, offset + read, size - read);
                    read += num;
                }

                return read;
            }
            catch
            {
                CheckDisposed();
                throw;
            }
            finally
            {
                _asyncCtx.SetProgress(false);
            }
        }


        /// <summary>
        /// Begins an asynchronous reading from the network stream.
        /// </summary>
        /// <param name="buffer">The buffer to store data to.</param>
        /// <param name="offset">The location in the <i>buffer</i> where to start storing the received data.</param>
        /// <param name="size">The number of bytes to read.</param>
        /// <param name="callback">
        /// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
        /// </param>
        /// <param name="state">
        /// An object containing state information for this request.
        /// </param>
        /// <returns>
        /// An <see cref="System.IAsyncResult">IAsyncResult</see>
        /// that references the asynchronous read.
        /// </returns>
        /// <remarks>
        /// The <b>BeginRead</b> method starts an asynchronous
        /// read operation from the network stream.
        /// It returns immediately and does not wait for 
        /// the asynchronous call to complete.
        /// <para>
        /// The 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.EndRead"/>
        /// method is used to retrieve the results of 
        /// the asynchronous call. It can be called 
        /// any time after <b>BeginRead</b>; if the asynchronous 
        /// call has not completed,
        /// <b>EndRead</b>
        /// will block until it completes.
        /// </para>
        /// <para>
        /// The read operation will not completed until the number of bytes specified by <i>size</i>
        /// parameter is read from the stream. If the remote host shuts down the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// connection with the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Shutdown"/> 
        /// method, and all available data has been received, the <b>Read</b> method 
        /// will complete and return number of bytes was read.
        /// </para>
        /// <note>
        /// The <b>NetworkStreamEx</b> should have access right to read data from the network stream.
        /// You may use <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.CanRead"/> property to check this.
        /// </note>
        /// </remarks>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> object was disposed.
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
        public override IAsyncResult BeginRead(
            byte[] buffer,
            int offset,
            int size,
            AsyncCallback callback,
            object state
            )
        {
            CheckDisposed();
            _asyncCtx.SetProgress(true);
            try
            {
                Read_SO stateObj = new Read_SO(
                    buffer, 
                    offset, 
                    size,
                    callback,
                    state);

                return _socket.BeginReceive(
                    buffer, 
                    offset, 
                    size, 
                    new AsyncCallback(Read_End),
                    stateObj); 
            }
            catch
            {
                _asyncCtx.SetProgress(false);
                CheckDisposed();
                throw;
            }
        }


        void Read_End(IAsyncResult ar)
        {
            Read_SO stateObj = (Read_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                int read = _socket.EndReceive(ar);

                stateObj.Read += read;
                if((read > 0) && (stateObj.Read < stateObj.Size))
                {
                    _socket.BeginReceive(
                        stateObj.Buffer, 
                        stateObj.Offset + stateObj.Read, 
                        stateObj.Size - stateObj.Read, 
                        new AsyncCallback(Read_End),
                        stateObj); 
                }
                else
                {
                    stateObj.SetCompleted();
                }
            }
            catch(Exception e)
            {
                if(_disposed)
                    stateObj.Exception = GetDisposedException();
                else
                    stateObj.Exception = e;
                stateObj.SetCompleted();
            }

            /*
            catch
            {
                if(_disposed)
                    stateObj.Exception = GetDisposedException();
                else
                    stateObj.Exception = new SocketException(SockErrors.WSAECONNRESET);
                stateObj.SetCompleted();
            }
            */
        }


        /// <summary>
        /// Ends a pending asynchronous read.
        /// </summary>
        /// <param name="asyncResult">
        /// An 
        /// <see cref="System.IAsyncResult">IAsyncResult</see>
        /// that stores state information for this asynchronous operation.
        /// </param>
        /// <returns>The number of bytes read.</returns>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// <i>asyncResult</i> is a null reference 
        /// (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <i>asyncResult</i> was not returned by a call to the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.BeginRead"/>
        /// method.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <b>EndRead</b> was previously called for the 
        /// asynchronous read.
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        /// 
        /// <remarks>
        /// <b>EndRead</b> is a blocking method that completes the 
        /// asynchronous read operation started in the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.BeginRead"/> method.
        /// 
        /// <para>
        /// The read operation will not completed until the number of bytes specified by <i>size</i>
        /// parameter (to <b>BeginRead</b> method) is read from the stream. If the remote host shuts down the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// connection with the 
        /// <see cref="BytesRoad.Net.Sockets.SocketEx.Shutdown"/> 
        /// method, and all available data has been received, the <b>Read</b> method 
        /// will complete and return number of bytes was read.
        /// </para>
        /// 
        /// <note>
        /// The <b>NetworkStreamEx</b> should have access right to read data from the network stream.
        /// You may use <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.CanRead"/> property to check this.
        /// </note>
        /// 
        /// </remarks>        
        public override int EndRead(IAsyncResult asyncResult)
        {
            AsyncBase.VerifyAsyncResult(asyncResult, typeof(Read_SO), "EndRead");
            _asyncCtx.HandleAsyncEnd(asyncResult, true);
            Read_SO stateObj = (Read_SO)asyncResult;
            return stateObj.Read;
        }

        #endregion

        #region Write

        /// <summary>
        /// Writes data to the network stream.
        /// </summary>
        /// <param name="buffer">Data to write.</param>
        /// <param name="offset">The position in the data <i>buffer</i> from which to begin writing.</param>
        /// <param name="size">The number of bytes to write.</param>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> object was disposed.
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
        /// 
        /// <remarks>
        /// <b>Write</b> method will block until all data, specified by <i>size</i> parameter, are sent.
        /// 
        /// <note>
        /// The <b>NetworkStreamEx</b> should have access right to write data to the network stream.
        /// You may use <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.CanWrite"/> property to check this.
        /// </note>
        /// </remarks>
        public override void Write(byte[] buffer, int offset, int size)
        {
            CheckDisposed();
            _asyncCtx.SetProgress(true);
            try
            {
                int sent = 0;
                while(sent < size)
                {
                    sent += _socket.Send(
                        buffer, 
                        offset + sent, 
                        size - sent);
                }
            }
            catch
            {
                CheckDisposed();
                throw;
            }
            finally
            {
                _asyncCtx.SetProgress(false);
            }
        }

        /// <summary>
        /// Begins an asynchronous write to the network stream.
        /// </summary>
        /// <param name="buffer">Data to write.</param>
        /// <param name="offset">The position in the data <i>buffer</i> from which to begin writing.</param>
        /// <param name="size">The number of bytes to write.</param>
        /// <param name="callback">
        /// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
        /// </param>
        /// <param name="state">
        /// An object containing state information for this request.
        /// </param>
        /// <returns>
        /// An <see cref="System.IAsyncResult">IAsyncResult</see>
        /// that references the asynchronous write.
        /// </returns>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> object was disposed.
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
        /// <remarks>
        /// The <b>BeginWrite</b> method starts an asynchronous
        /// write operation.
        /// It returns immediately and does not wait for 
        /// the asynchronous call to complete.
        /// <para>
        /// The 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.EndWrite"/>
        /// method is used to retrieve the results of 
        /// the asynchronous call. It can be called 
        /// any time after <b>BeginWrite</b>; if the asynchronous 
        /// call has not completed,
        /// <b>EndWrite</b>
        /// will block until it completes.
        /// </para>
        /// <para>
        /// Write operation will not completed until all data, specified by <i>size</i> parameter, are sent.
        /// </para>
        /// 
        /// <note>
        /// The <b>NetworkStreamEx</b> should have access right to write data to the network stream.
        /// You may use <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.CanWrite"/> property to check this.
        /// </note>
        /// 
        /// </remarks>
        public override IAsyncResult BeginWrite(
            byte[] buffer,
            int offset,
            int size,
            AsyncCallback callback,
            object state
            )
        {
            CheckDisposed();
            _asyncCtx.SetProgress(true);
            try
            {
                Write_SO stateObj =  new Write_SO(
                    buffer,
                    offset,
                    size, 
                    callback, 
                    state);

                return _socket.BeginSend(
                    buffer, 
                    offset, 
                    size,
                    new AsyncCallback(Send_End),
                    stateObj);
            }
            catch
            {
                _asyncCtx.SetProgress(false);
                CheckDisposed();
                throw;
            }
        }


        void Send_End(IAsyncResult ar)
        {
            Write_SO stateObj = (Write_SO)ar.AsyncState;
            try
            {
                stateObj.UpdateContext();
                int sent = _socket.EndSend(ar);

                stateObj.Sent += sent;
                if(stateObj.Sent < stateObj.Size)
                {
                    _socket.BeginSend(
                        stateObj.Buffer, 
                        stateObj.Offset + stateObj.Sent, 
                        stateObj.Size - stateObj.Sent, 
                        new AsyncCallback(Send_End),
                        stateObj); 
                }
                else
                {
                    stateObj.SetCompleted();
                }
            }
            catch(Exception e)
            {
                if(_disposed)
                    stateObj.Exception = GetDisposedException();
                else
                    stateObj.Exception = e;
                stateObj.SetCompleted();
            }

            /*
            catch
            {
                if(_disposed)
                    stateObj.Exception = GetDisposedException();
                else
                    stateObj.Exception = new SocketException(SockErrors.WSAECONNRESET);
                stateObj.SetCompleted();
            }
            */
        }

        /// <summary>
        /// Ends a pending asynchronous write.
        /// </summary>
        /// <param name="asyncResult">
        /// An 
        /// <see cref="System.IAsyncResult">IAsyncResult</see>
        /// that stores state information for this asynchronous operation.
        /// </param>
        /// 
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/> object was disposed.
        /// </exception>
        /// 
        /// <exception cref="System.ArgumentNullException">
        /// <i>asyncResult</i> is a null reference 
        /// (<b>Nothing</b> in Visual Basic).
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// <i>asyncResult</i> was not returned by a call to the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.BeginWrite"/>
        /// method.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// <b>EndWrite</b> was previously called for the 
        /// asynchronous write.
        /// </exception>
        /// 
        /// <exception cref="System.Net.Sockets.SocketException">
        /// An error occurred when attempting to access
        /// the socket which is used to complete the requested operation.
        /// </exception>
        /// 
        /// <remarks>
        /// <b>EndWrite</b> is a blocking method that completes the 
        /// asynchronous write operation started in the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.BeginWrite"/> method.
        /// 
        /// <para>
        /// <b>EndWrite</b> will block until all data, specified by <i>size</i> parameter to
        /// <b>BeginWrite</b> method, are sent.
        /// </para>
        /// 
        /// <note>
        /// The <b>NetworkStreamEx</b> should have access right to write data to the network stream.
        /// You may use <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.CanWrite"/> property to check this.
        /// </note>
        /// 
        /// </remarks>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            AsyncBase.VerifyAsyncResult(asyncResult, typeof(Write_SO), "EndWrite");
            _asyncCtx.HandleAsyncEnd(asyncResult, true);
        }
        #endregion

        /// <summary>
        /// Flushes data from the stream. Has no effect when applied to <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>.
        /// </summary>
        /// <remarks>
        /// Has no effect when applied to <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>.
        /// </remarks>
        public override void Flush()
        {
            CheckDisposed();
        }

        /// <summary>
        /// Changes the current position in the stream. This method always throws a <see cref="System.NotSupportedException"/>.
        /// </summary>
        /// <param name="offset">not used.</param>
        /// <param name="origin">not used.</param>
        /// <returns>not used.</returns>
        /// <remarks>
        /// Always throws a <see cref="System.NotSupportedException"/>.
        /// </remarks>
        /// <exception cref="System.NotSupportedException">
        /// any access to the method.
        /// </exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();
            ThrowMetUnsupported("Seek");
            return 0;
        }

        /// <summary>
        /// Sets the length of the stream. This method always throws a <see cref="System.NotSupportedException"/>.
        /// </summary>
        /// <param name="value">not used.</param>
        /// <remarks>
        /// Always throws a <see cref="System.NotSupportedException"/>.
        /// </remarks>
        /// <exception cref="System.NotSupportedException">
        /// any access to the method.
        /// </exception>
        public override void SetLength(long value)
        {
            CheckDisposed();
            ThrowMetUnsupported("SetLength");
        }


        /// <summary>
        /// Releases the resources used by the
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>.
        /// </summary>
        /// <remarks>
        /// Internally simply calls <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.Dispose"/>, which
        /// depending on the ownership of underlying <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// will call <see cref="BytesRoad.Net.Sockets.SocketEx.Close"/> method.
        /// </remarks>
        public override void Close()
        {
            mDispose();
        }

        #region IDisposable Members

        /// <summary>
        /// Frees resources used by the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>
        /// class.
        /// </summary>
        /// <remarks>
        /// The 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>
        /// class finalizer calls the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.Dispose"/>
        /// method to free resources associated with the <b>NetworkStreamEx</b>
        /// object.
        /// If the <b>NetworkStreamEx</b> owns the underlying <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// the <b>Dispose</b> method will call <see cref="BytesRoad.Net.Sockets.SocketEx.Close"/>
        /// method to release <b>SocketEx</b> object used for network communications. 
        /// </remarks>
        ~NetworkStreamEx()
        {
            mDispose(false);
        }

        /// <overloads>
        /// Releases the resources used by the
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>.
        /// </overloads>
        /// 
        /// <summary>
        /// Releases all resources used by the
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>.
        /// </summary>
        /// <remarks>
        /// Call <b>Dispose</b> when you are finished using the
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>. 
        /// If the <b>NetworkStreamEx</b> owns the underlying <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// the <b>Dispose</b> method will call <see cref="BytesRoad.Net.Sockets.SocketEx.Close"/>
        /// method to release <b>SocketEx</b> object used for network communications. 
        /// The <b>Dispose</b> method leaves the <b>NetworkStreamEx</b> in an 
        /// unusable state. After calling <b>Dispose</b>, you must release 
        /// all references to the <b>NetworkStreamEx</b> so the garbage collector
        /// can reclaim the memory that the <b>NetworkStreamEx</b> was occupying.
        /// </remarks>
        public void mDispose()
        {
            GC.SuppressFinalize(this);
            mDispose(true);
        }



        /// <summary>
        /// Releases the unmanaged resources used by the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx"/>
        /// and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <b>true</b> to release both managed and unmanaged resources;
        /// <b>false</b> to release only unmanaged resources.
        /// </param>
        /// <remarks>
        /// This method is called by the public 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.Dispose">Dispose()</see>
        /// method and the 
        /// <see cref="BytesRoad.Net.Sockets.NetworkStreamEx.Finalize">Finalize</see>
        /// method. <b>Dispose()</b> invokes the protected 
        /// <b>Dispose(Boolean)</b>
        /// method with the <i>disposing</i> parameter set to <b>true</b>.
        /// <b>Finalize</b> invokes <b>Dispose</b> with <i>disposing</i>
        /// set to <b>false</b>.
        /// When the <i>disposing</i> parameter is <b>true</b>,
        /// this method releases all resources held by any managed 
        /// objects that this <b>NetworkStreamEx</b> references. 
        /// This method invokes the <b>Dispose()</b> method of each 
        /// referenced object.
        /// 
        /// <note>
        /// If the <b>NetworkStreamEx</b> owns the underlying <see cref="BytesRoad.Net.Sockets.SocketEx"/>
        /// the <b>Dispose</b> method will call <see cref="BytesRoad.Net.Sockets.SocketEx.Close"/>
        /// method to release <b>SocketEx</b> object used for network communications. 
        /// </note>
        /// 
        /// </remarks>
        protected virtual void mDispose(bool disposing)
        {
            lock(this)
            {
                if(!_disposed)
                {
                    _disposed = true;
                    if(disposing)
                    {
                    }

                    if(_ownsSocket)
                        _socket.Dispose();
                }
            }
        }
        #endregion
    }
}
