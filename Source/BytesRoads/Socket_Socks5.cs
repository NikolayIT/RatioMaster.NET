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
using System.Text;
using BytesRoad.Net.Sockets.Advanced;

namespace BytesRoad.Net.Sockets
{
	internal enum AuthMethod
	{
		None,
		UsernamePassword,
		NoAcceptable
	}

	internal enum ATYP
	{
		IPv4 = 1,
		DomName = 3,
		IPv6 = 4
	}

	/// <summary>
	/// Summary description for Socket_Socks5.
	/// </summary>
	internal class Socket_Socks5 : SocketBase
	{
		#region Async classes
		class ReadVerifyReply_SO : AsyncResultBase
		{
			byte[] _phase1Data = new byte[5];
			byte[] _reply = null;

			internal ReadVerifyReply_SO(AsyncCallback cb, object state)
				: base(cb, state)
			{
			}

			internal byte[] Phase1Data
			{
				get { return _phase1Data; }
			}

			internal byte[] Reply
			{
				get { return _reply; }
				set { _reply = value; }
			}
		}
		class UsernamePassword_SO : AsyncResultBase
		{
			byte[] _reply = new byte[2];
			internal UsernamePassword_SO(AsyncCallback cb, object state)
				: base(cb, state)
			{
			}

			internal byte[] Reply
			{
				get { return _reply; }
			}
		}
		class DoAuthentication_SO : AsyncResultBase
		{
			internal DoAuthentication_SO(AsyncCallback cb, object state)
				: base(cb, state)
			{
			}
		}

		class ReadWhole_SO : AsyncResultBase
		{
			byte[] _buffer;
			int _offset;
			int _size;
			int _read = 0;

			internal ReadWhole_SO(byte[] buffer,
				int offset,
				int size,
				AsyncCallback cb, 
				object state) 
				: base(cb, state)
			{
				_buffer = buffer;
				_offset = offset;
				_size = size;
			}

			internal byte[] Buffer
			{
				get { return _buffer; }
			}

			internal int Read 
			{
				get { return _read; }
				set { _read = value; }
			}

			internal int Size
			{
				get { return _size; }
			}

			internal int Offset 
			{
				get { return _offset; }
			}
		}

		class Negotiation_SO : AsyncResultBase
		{
			byte[] _reply = new byte[2];
			bool _useCredentials;

			internal Negotiation_SO(bool useCredentials,
				AsyncCallback cb, 
				object state) : base(cb, state)
			{
				_useCredentials = useCredentials;
			}

			internal byte[] Reply
			{
				get { return _reply; }
			}

			internal bool UseCredentials
			{
				get { return _useCredentials; }
				set { _useCredentials = value; }
			}
		}

		class Connect_SO : AsyncResultBase
		{
			EndPoint _remoteEndPoint;
			int _readBytes = 0;
			int _hostPort = -1;
			string _hostName = null;

			internal Connect_SO(EndPoint remoteEndPoint,
				string hostName,
				int hostPort,
				AsyncCallback cb, 
				object state) : base(cb, state)
			{
				_remoteEndPoint = remoteEndPoint;
				_hostPort = hostPort;
				_hostName = hostName;
			}

			internal int HostPort
			{
				get { return _hostPort; }
			}

			internal string HostName
			{
				get { return _hostName; }
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
			Socket_Socks5 _baseSocket;
			int _readBytes = 0;
			IPAddress _proxyIP = null;

			internal Bind_SO(Socket_Socks5 baseSocket,
				AsyncCallback cb, 
				object state) : base(cb, state)
			{
				_baseSocket = baseSocket;
			}

			internal Socket_Socks5 BaseSocket
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

		//policy switches
		bool _resolveHostEnabled = true;

		//remote host information
		string _remoteHost = null;
		int _remotePort = -1;

		//End points
		EndPoint _localEndPoint = null;
		EndPoint _remoteEndPoint = null;

		internal Socket_Socks5(
			string proxyServer,
			int proxyPort,
			byte[] proxyUser,
			byte[] proxyPassword) 
			: base(proxyServer, proxyPort, proxyUser, proxyPassword)
	{
	}

		#region Attributes
		override internal ProxyType ProxyType 
		{ 
			get { return ProxyType.Socks5; } 
		}
		override internal EndPoint LocalEndPoint 
		{ 
			get { return _localEndPoint; } 
		}
		override internal EndPoint RemoteEndPoint 
		{ 
			get { return _remoteEndPoint; } 
		}
		#endregion

		#region Helper functions
		IPEndPoint ExtractReplyAddr(byte[] reply)
		{
			byte atyp = reply[3];
			if(atyp != (byte)ATYP.IPv4)
			{
				string msg = string.Format("Socks5: Address type in reply is unknown ({0}).", atyp);
				throw new ArgumentException(msg, "reply");
			}

			int port = (reply[8]<<8) | reply[9];
			long ip = (reply[7]<<24)|
				(reply[6]<<16)|
				(reply[5]<<8)|
				(reply[4]);
			ip &= 0xFFFFFFFF;

			return new IPEndPoint(new IPAddress(ip), port);
		}

		byte[] PrepareCmd(EndPoint remoteEP, byte cmdVal)
		{
			if(null == remoteEP)
				throw new ArgumentNullException("remoteEP", "The value cannot be null.");

			byte[] cmd = new byte[10];
			IPEndPoint ip = (IPEndPoint)remoteEP;

			//------------------------------
			// Compose header
			//
			cmd[0] = 5; //version
			cmd[1] = cmdVal; //command
			cmd[2] = 0; //reserved
			cmd[3] = 1; //ATYPE - 1 for address as IP4

			//------------------------------
			// Store IP address
			//
			long ipAddr = ip.Address.Address;
			cmd[4] = (byte)((ipAddr&0x000000FF));
			cmd[5] = (byte)((ipAddr&0x0000FF00)>>8);
			cmd[6] = (byte)((ipAddr&0x00FF0000)>>16);
			cmd[7] = (byte)((ipAddr&0xFF000000)>>24);

			//------------------------------
			// Store port
			cmd[8] = (byte)((ip.Port&0xFF00)>>8);
			cmd[9] = (byte)(ip.Port&0xFF);
		
			return cmd;
		}

		byte[] PrepareCmd(string remoteHost, int remotePort, byte cmdVal)
		{
			if(null == remoteHost)
				throw new ArgumentNullException("remoteHost", "The value cannot be null.");

			int hostLength = remoteHost.Length;
			if(hostLength > 255)
				throw new ArgumentException("Name of destination host cannot be more then 255 characters.", "remoteHost");

			byte[] cmd = new byte[4+1+hostLength+2];

			//----------------------------------
			// Compose header
			//
			cmd[0] = 5; //version
			cmd[1] = cmdVal; //command
			cmd[2] = 0; //reserved
			cmd[3] = 3; //domain name as address

			//----------------------------------
			// Store host name as address
			//
			cmd[4] = (byte)hostLength;
			byte[] name = Encoding.Default.GetBytes(remoteHost);
			Array.Copy(name, 0, cmd, 5, hostLength);

			//----------------------------------
			// Store the port
			//
			cmd[5+hostLength] = (byte)((remotePort&0xFF00)>>8);
			cmd[5+hostLength+1] = (byte)(remotePort&0xFF);

			return cmd;
		}

		byte[] PrepareBindCmd(Socket_Socks5 baseSocket)
		{
			if(null != baseSocket.RemoteEndPoint)
				return PrepareCmd(baseSocket.RemoteEndPoint, 2);
			else if(null != baseSocket._remoteHost)
				return PrepareCmd(baseSocket._remoteHost, baseSocket._remotePort, 2);
			else
				throw new InvalidOperationException("Unable to prepare bind command because of insufficient information.");
		}

		byte[] PrepareConnectCmd(EndPoint remoteEP, string hostName, int hostPort)
		{
			if(null != remoteEP)
				return PrepareCmd(remoteEP, 1);
			else if(null != hostName)
				return PrepareCmd(hostName, hostPort, 1);
			else
				throw new InvalidOperationException("Unable to prepare connect command because of insufficient information.");
		}


		#endregion

		#region ReadWhole functions
		void ReadWhole(byte[] buffer, int offset, int size)
		{
			int read = 0;
			while(read < size)
				read += NStream.Read(buffer, offset+read, size-read);
		}

		IAsyncResult BeginReadWhole(byte[] buffer, 
			int offset, 
			int size,
			AsyncCallback cb, 
			object state)
		{
			ReadWhole_SO stateObj = null;
			stateObj = new ReadWhole_SO(buffer, offset, size, cb, state);

			NStream.BeginRead(buffer, offset, size, 
				new AsyncCallback(ReadWhole_Read_End),
				stateObj);

			return stateObj;
		}

		void ReadWhole_Read_End(IAsyncResult ar)
		{
			ReadWhole_SO stateObj = (ReadWhole_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				stateObj.Read += NStream.EndRead(ar);
				if(stateObj.Read < stateObj.Size)
				{
					NStream.BeginRead(stateObj.Buffer,
						stateObj.Offset + stateObj.Read,
						stateObj.Size - stateObj.Read,
						new AsyncCallback(ReadWhole_Read_End),
						stateObj);
				}
				else
				{
					stateObj.SetCompleted();
				}
			}
			catch(Exception e)
			{
				stateObj.Exception = e;

				stateObj.SetCompleted();
			}
		}

		void EndReadWhole(IAsyncResult ar)
		{
			VerifyAsyncResult(ar, typeof(ReadWhole_SO));
			HandleAsyncEnd(ar, false);
		}
		#endregion

		#region Negotiation functions

		#region Username/password negotiation
	
		void Validate_UsernamePasswordReply(byte[] reply)
		{
			if(1 != reply[0])
			{
				string msg = string.Format("Socks5: Unknown reply format for username/password authentication ({0}).", reply[0]);
				throw new ProtocolViolationException(msg);
			}

			if(0 != reply[1])
			{
				string msg = string.Format("Socks5: Username/password authentication failed ({0}).", reply[1]);
                msg.ToString();
			    throw new SocketException(SockErrors.WSAECONNREFUSED);
			}
		}

		byte[] Prepare_UsernamePasswordCmd()
		{
			if(null == _proxyUser)
				throw new ArgumentNullException("ProxyUser", "The value cannot be null.");

			int userLength = _proxyUser.Length;
			if(userLength > 255)
				throw new ArgumentException("Proxy user name cannot be more then 255 characters.", "ProxyUser");

			int passwordLength = 0;
			if(null != _proxyPassword)
			{
				passwordLength = _proxyPassword.Length;
				if(passwordLength > 255)
					throw new ArgumentException("Proxy password cannot be more then 255 characters.", "ProxyPassword");
			}

			byte[] cmd = new byte[1+1+userLength+1+passwordLength];
			
			//------------------------------
			// Compose the header
			cmd[0] = 1; //version

			//------------------------------
			// Store user name
			cmd[1] = (byte)userLength;
			Array.Copy(_proxyUser, 0, cmd, 2, userLength);

			//------------------------------
			// Store password if exists
			cmd[2+userLength] = (byte)passwordLength;
			if(passwordLength > 0)
				Array.Copy(_proxyPassword, 0, cmd, 3+userLength, passwordLength);

			return cmd;
		}

		void SubNegotiation_UsernamePassword()
		{
			//---------------------------------------
			// Prepare authentication information
			byte[] cmd = Prepare_UsernamePasswordCmd();

			//---------------------------------------
			// Send authentication information
			NStream.Write(cmd, 0, cmd.Length);

			//---------------------------------------
			// Read the response
			byte[] res = new byte[2];
			ReadWhole(res, 0, 2);

			//---------------------------------------
			// Validate server response
			Validate_UsernamePasswordReply(res);
		}


		IAsyncResult BeginSubNegotiation_UsernamePassword(AsyncCallback cb,
			object state)
		{
			//---------------------------------------
			// Prepare authentication information
			byte[] cmd = Prepare_UsernamePasswordCmd();

			UsernamePassword_SO stateObj = new UsernamePassword_SO(cb, state);

			//---------------------------------------
			// Send authentication information
			NStream.BeginWrite(cmd, 
				0, 
				cmd.Length,
				new AsyncCallback(SubUsernamePassword_Write_End),
				stateObj);

			return stateObj;
		}

		void SubUsernamePassword_Write_End(IAsyncResult ar)
		{
			UsernamePassword_SO stateObj = (UsernamePassword_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				NStream.EndWrite(ar);

				//---------------------------------------
				// Send authentication information
				BeginReadWhole(stateObj.Reply, 
					0, 
					2,
					new AsyncCallback(SubUsernamePassword_Read_End),
					stateObj);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
				stateObj.SetCompleted();
			}
		}

		void SubUsernamePassword_Read_End(IAsyncResult ar)
		{
			UsernamePassword_SO stateObj = (UsernamePassword_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				EndReadWhole(ar);

				//---------------------------------------
				// Validate server response
				Validate_UsernamePasswordReply(stateObj.Reply);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
			}
			stateObj.SetCompleted();
		}

		void EndSubNegotiation_UsernamePassword(IAsyncResult ar)
		{
			VerifyAsyncResult(ar, typeof(UsernamePassword_SO));
			HandleAsyncEnd(ar, true);
		}

		#endregion

		AuthMethod ExtractAuthMethod(byte[] reply)
		{
			//------------------------------------
			// Check the reply header
			if(reply[0] != 5)
			{
				string msg = string.Format("Socks5 server returns reply with unknown version ({0}).", reply[0]);
				throw new ProtocolViolationException(msg);
			}

			//------------------------------------
			// Dispatch method chosen to particular
			// sub negotiation or throw the 
			// exception.
			//
			byte method = reply[1];
			if(0 == method) //no authentication required
			{
				return AuthMethod.None;
			}
			else if(2 == method) //Username/password
			{
				return AuthMethod.UsernamePassword;
			}
			else if(0xFF == method) //no acceptable methods
			{
				return AuthMethod.NoAcceptable;
			}
			else
			{
				//it is a violation of protocol
				string msg = string.Format("Socks5 server requires not declared authentication method ({0}).", method);
				throw new ProtocolViolationException(msg);
			}
		}

		void DoAuthentication(AuthMethod method)
		{
			if(AuthMethod.None == method)
				return;

			if(AuthMethod.UsernamePassword == method)
			{
				SubNegotiation_UsernamePassword();
			}
			else if(AuthMethod.NoAcceptable == method)
			{
				//throw new AuthFailedException("No acceptable methods.");
				throw new SocketException(SockErrors.WSAECONNREFUSED);
			}
			else
			{
				//throw invalid operation because 
				//method is unknown and execution should be stoped proir
				//this point
				throw new InvalidOperationException("Unknown authentication requested.");
			}
		}

		IAsyncResult BeginDoAuthentication(AuthMethod method,
			AsyncCallback cb, 
			object state)
		{
			DoAuthentication_SO stateObj = new DoAuthentication_SO(cb, state);
			if(AuthMethod.UsernamePassword == method)
			{
				BeginSubNegotiation_UsernamePassword(
					new AsyncCallback(SubNegotiation_UsernamePassword_End),
					stateObj);
			}
			else if(AuthMethod.NoAcceptable == method)
			{
				//throw new AuthFailedException("No acceptable methods.");
				throw new SocketException(SockErrors.WSAECONNREFUSED);
			}
			else if(AuthMethod.None != method)
			{
				throw new InvalidOperationException("Unknown authentication requested.");
			}
			else
			{
				stateObj.SetCompleted();
			}
			return stateObj;
		}

		void SubNegotiation_UsernamePassword_End(IAsyncResult ar)
		{
			DoAuthentication_SO stateObj = (DoAuthentication_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				EndSubNegotiation_UsernamePassword(ar);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
			}
			stateObj.SetCompleted();
		}

		void EndDoAuthentication(IAsyncResult ar)
		{
			VerifyAsyncResult(ar, typeof(DoAuthentication_SO));
			HandleAsyncEnd(ar, false);
		}

		byte[] PrepareNegotiationCmd(bool useCredentials)
		{
			byte[] cmd = null;

			if(useCredentials)
			{
				cmd = new byte[4];
				cmd[0] = 5; //version
				cmd[1] = 2; //number of supported methods
				cmd[2] = 0; //no authentication
				cmd[3] = 2; //username/password
			}
			else
			{
				cmd = new byte[3];
				cmd[0] = 5; //version
				cmd[1] = 1; //number of supported methods
				cmd[2] = 0; //no authentication
			}
			return cmd;
		}

		void Negotiate()
		{
		    bool useCredentials = PreAuthenticate;
			if(null == _proxyUser)
				useCredentials = false;

			AuthMethod authMethod;
			while(true)
			{
				//----------------------------------------------
				// Send negotiation request
				byte[] cmd = PrepareNegotiationCmd(useCredentials);
				NStream.Write(cmd, 0, cmd.Length);

				//----------------------------------------------
				// Read negotiation reply with supported methods
				byte[] reply = new byte[2];
				ReadWhole(reply, 0, 2);

				//----------------------------------------------
				// Extract demanded authentication method
				authMethod = ExtractAuthMethod(reply);
				if((AuthMethod.NoAcceptable == authMethod) &&
					!useCredentials &&
					(null != _proxyUser))
				{
					useCredentials = true;
					continue;
				}
				break;	
			}

			//-------------------------------------------
			// Run appropriate authentication if required
			DoAuthentication(authMethod);
		}

		IAsyncResult BeginNegotiate(AsyncCallback callback, object state)
		{
			bool useCredentials = PreAuthenticate;
			if(null == _proxyUser)
				useCredentials = false;

			Negotiation_SO stateObj = new Negotiation_SO(useCredentials, 
				callback, 
				state);

			//-----------------------------------
			// Send negotiation request
			byte[] cmd = PrepareNegotiationCmd(stateObj.UseCredentials);
			NStream.BeginWrite(cmd, 
				0, 
				cmd.Length,
				new AsyncCallback(Negotiate_Write_End),
				stateObj);

			return stateObj;
		}

		void Negotiate_Write_End(IAsyncResult ar)
		{
			Negotiation_SO stateObj = (Negotiation_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				NStream.EndWrite(ar);

				//-----------------------------------
				// Read negotiation reply with 
				// supported methods
				//
				BeginReadWhole(stateObj.Reply, 
					0, 
					2,
					new AsyncCallback(Negotiate_ReadWhole_End),
					stateObj);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
				stateObj.SetCompleted();
			}
		}

		void Negotiate_ReadWhole_End(IAsyncResult ar)
		{
			Negotiation_SO stateObj = (Negotiation_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				EndReadWhole(ar);

				//----------------------------------------------
				// Extract demanded authentication method
				AuthMethod authMethod = ExtractAuthMethod(stateObj.Reply);
				if((AuthMethod.NoAcceptable == authMethod) &&
					!stateObj.UseCredentials &&
					(null != _proxyUser))
				{
					stateObj.UseCredentials = true;

					//-----------------------------------
					// Send negotiation request
					byte[] cmd = PrepareNegotiationCmd(stateObj.UseCredentials);
					NStream.BeginWrite(cmd, 
						0, 
						cmd.Length,
						new AsyncCallback(Negotiate_Write_End),
						stateObj);
				}
				else
				{
					//-----------------------------------
					// Run appropriate authentication 
					// method
					BeginDoAuthentication(authMethod, 
						new AsyncCallback(Negotiate_DoAuth_End),
						stateObj);
				}
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
				stateObj.SetCompleted();
			}
		}

		void Negotiate_DoAuth_End(IAsyncResult ar)
		{
			Negotiation_SO stateObj = (Negotiation_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				EndDoAuthentication(ar);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
			}
			stateObj.SetCompleted();
		}

		void EndNegotiate(IAsyncResult ar)
		{
			VerifyAsyncResult(ar, typeof(Negotiation_SO));
			HandleAsyncEnd(ar, false);
		}

		#endregion

		#region ReadVerifyReply functions

		void CheckReplyVer(byte[] reply)
		{
			if(5 != reply[0])
			{
				string msg = string.Format("Socks5: Unknown format of reply ({0}).", reply[0]);
				throw new ProtocolViolationException(msg);
			}
		}

		void CheckReplyForError(byte[] reply)
		{
			byte rep = reply[1];
			if(0 == rep)
				return;

			string msg;
			if(1 == rep)
				msg = "Socks5: General SOCKS server failure.";
			else if(2 == rep)
				msg = "Socks5: Connection not allowed by rule set.";
			else if(3 == rep)
				msg = "Socks5: Network unreachable.";
			else if(4 == rep)
				msg = "Socks5: Host unreachable.";
			else if(5 == rep)
				msg = "Socks5: Connection refused.";
			else if(6 == rep)
				msg = "Socks5: TTL expired.";
			else if(7 == rep)
				msg = "Socks5: Command not supported.";
			else if(8 == rep)
				msg = "Socks5: Address type not supported.";
			else
				msg = string.Format("Socks5: Reply code is unknown ({0}).", rep);
            msg.ToString();
			throw new SocketException(SockErrors.WSAECONNREFUSED);
			//throw new ProxyErrorException(msg);
		}

		int GetAddrFieldLength(byte[] reply)
		{
			byte atyp = reply[3];
			if(1 == atyp) //IP4 address?
				return 4;
			else if(3 == atyp) //domain name?
				return 1 + reply[4];
			else if(4 == atyp)
				return 16;
			else
			{
				string msg = string.Format("Socks5: Unknown address type in reply ({0}).", atyp);
				throw new ProtocolViolationException(msg);
			}
		}

		int VerifyReplyAndGetLeftBytes(byte[] reply)
		{
			//----------------------------------
			// Check reply version
			CheckReplyVer(reply);

			//------------------------------------------
			// Check for error condition
			CheckReplyForError(reply);

			//------------------------------------------
			// Calculate number of bytes left to read:
			// address length - 1(because one byte from
			// address field was read in phase 1) + port
			return GetAddrFieldLength(reply) - 1 + 2;
		}

		byte[] ReadVerifyReply()
		{
			//-----------------------------------
			// Phase 1. Read 5 bytes
			byte[] phase1Data = new byte[5];
			ReadWhole(phase1Data, 0, 5);


			int leftBytes = VerifyReplyAndGetLeftBytes(phase1Data);

			//-----------------------------------
			// Phase 2. Read left data 
			byte[] reply = new byte[5 + leftBytes];
			phase1Data.CopyTo(reply, 0);
			ReadWhole(reply, 5, leftBytes);
			return reply;
		}

		IAsyncResult BeginReadVerifyReply(AsyncCallback cb, object state)
		{
			ReadVerifyReply_SO stateObj = new ReadVerifyReply_SO(cb, state);
			BeginReadWhole(stateObj.Phase1Data, 0, 5, 
				new AsyncCallback(Phase1_End), stateObj);
			return stateObj;
		}

		void Phase1_End(IAsyncResult ar)
		{
			ReadVerifyReply_SO stateObj = (ReadVerifyReply_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				EndReadWhole(ar);

				int leftBytes = VerifyReplyAndGetLeftBytes(stateObj.Phase1Data);

				//-----------------------------------
				// Phase 2. Read left data 
				stateObj.Reply = new byte[5 + leftBytes];
				stateObj.Phase1Data.CopyTo(stateObj.Reply, 0);
				BeginReadWhole(stateObj.Reply, 5, leftBytes,
					new AsyncCallback(Phase2_End), stateObj);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
				stateObj.SetCompleted();
			}
		}

		void Phase2_End(IAsyncResult ar)
		{
			ReadVerifyReply_SO stateObj = (ReadVerifyReply_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				EndReadWhole(ar);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
			}
			stateObj.SetCompleted();
		}

		byte[] EndReadVerifyReply(IAsyncResult ar)
		{
			VerifyAsyncResult(ar, typeof(ReadVerifyReply_SO));
			HandleAsyncEnd(ar, false);
			return ((ReadVerifyReply_SO)ar).Reply;
		}

		#endregion

		#region Accept functions (overriden)

		override internal SocketBase Accept()
		{
			CheckDisposed();
			SetProgress(true);
			try
			{
				byte[] reply = ReadVerifyReply();
				_remoteEndPoint = ExtractReplyAddr(reply);
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
				BeginReadVerifyReply(new AsyncCallback(Accept_Read_End), stateObj);
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
				byte[] reply = EndReadVerifyReply(ar);
				_remoteEndPoint = ExtractReplyAddr(reply);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
			}
			stateObj.SetCompleted();
		}

		override internal SocketBase EndAccept(IAsyncResult asyncResult)
		{
			VerifyAsyncResult(asyncResult, typeof(Accept_SO), "EndAccept");
			HandleAsyncEnd(asyncResult, true);
			return this;
		}
		
		#endregion

		#region Connect functions (overriden)
		override internal void Connect(string hostName, int hostPort)
		{
			if(null == hostName)
				throw new ArgumentNullException("hostName", "The value cannot be null.");

			if(hostPort < IPEndPoint.MinPort || hostPort > IPEndPoint.MaxPort)
				throw new ArgumentOutOfRangeException("hostPort", "Value, specified for the port is out of the valid range."); 

			Connect(null, hostName, hostPort);
		}

		override internal void Connect(EndPoint remoteEP)
		{
			if(null == remoteEP)
				throw new ArgumentNullException("remoteEP", "The value cannot be null.");

			Connect(remoteEP, null, -1);
		}

		void Connect(EndPoint remoteEP, string hostName, int hostPort)
		{
			CheckDisposed();
			SetProgress(true);
			try
			{
				if(null == remoteEP)
				{
					if(_resolveHostEnabled)
					{
						IPHostEntry host = GetHostByName(hostName);
						if(null != host)
							remoteEP = ConstructEndPoint(host, hostPort);
					}

					if((null == hostName) && (null == remoteEP))
						throw new ArgumentNullException("hostName", "The value cannot be null.");
				}

				//------------------------------------
				// Get end point for the proxy server
				//
				IPHostEntry  proxyEntry = GetHostByName(_proxyServer);
				if(null == proxyEntry)
					throw new SocketException(SockErrors.WSAHOST_NOT_FOUND);
					//throw new HostNotFoundException("Unable to resolve proxy name.");

				IPEndPoint proxyEndPoint = ConstructEndPoint(proxyEntry, _proxyPort);

				//------------------------------------------
				// Connect to proxy server
				//
				_socket.Connect(proxyEndPoint);

				//------------------------------------------
				// Negotiate user
				Negotiate();

				//------------------------------------------
				// Send CONNECT command
				//
				byte[] cmd = PrepareConnectCmd(remoteEP, hostName, hostPort);
				NStream.Write(cmd, 0, cmd.Length);

				//------------------------------------------
				// Read and verify reply from proxy the server. 
				//
				byte[] reply = ReadVerifyReply();
				_localEndPoint = ExtractReplyAddr(reply);
				_remoteEndPoint = remoteEP;

				//---------------------------------------
				//I we unable to resolve remote host then
				//store information - it will required
				//later for BIND command.
				if(null == remoteEP)
				{
					_remotePort = hostPort;
					_remoteHost = hostName;
				}
			}
			finally
			{
				SetProgress(false);
			}
		}
	
		override internal IAsyncResult BeginConnect(string hostName,
			int hostPort, 
			AsyncCallback callback,
			object state)
		{
			CheckDisposed();

			if(null == hostName)
				throw new ArgumentNullException("hostName", "The value cannot be null.");

			if(hostPort < IPEndPoint.MinPort || hostPort > IPEndPoint.MaxPort)
				throw new ArgumentOutOfRangeException("hostPort", "Value, specified for the port is out of the valid range."); 

			Connect_SO stateObj = null;
			SetProgress(true);
			try
			{
				stateObj = new Connect_SO(null, hostName, hostPort, callback, state);
				if(_resolveHostEnabled)
				{
					//--------------------------------------
					// Trying to resolve host name locally
					BeginGetHostByName(hostName,
						new AsyncCallback(Connect_GetHost_Host_End),
						stateObj);
				}
				else
				{
					//-------------------------------------
					// Get end point for the proxy server
					//
					BeginGetHostByName(_proxyServer,
						new AsyncCallback(Connect_GetHost_Proxy_End),
						stateObj);
				}
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
				if(null != host)
					stateObj.RemoteEndPoint = ConstructEndPoint(host, stateObj.HostPort);

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
			if(null == remoteEP)
				throw new ArgumentNullException("remoteEP", "The value cannot be null.");

			Connect_SO stateObj = null;
			SetProgress(true);
			try
			{
				stateObj = new Connect_SO(remoteEP, null, -1, callback, state);

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

				//------------------------------------------
				// Negotiate user
				BeginNegotiate(new AsyncCallback(Connect_Negotiate_End), stateObj);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
				stateObj.SetCompleted();
			}
		}

		void Connect_Negotiate_End(IAsyncResult ar)
		{
			Connect_SO stateObj = (Connect_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				EndNegotiate(ar);

				//------------------------------------
				// Send CONNECT command
				//
				byte[] cmd = PrepareConnectCmd(stateObj.RemoteEndPoint,
					stateObj.HostName,
					stateObj.HostPort);

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
				BeginReadVerifyReply(new AsyncCallback(Connect_Read_End), stateObj);
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
				byte[] reply = EndReadVerifyReply(ar);

				_localEndPoint = ExtractReplyAddr(reply);
				_remoteEndPoint = stateObj.RemoteEndPoint;

				//---------------------------------------
				//I we unable to resolve remote host then
				//store information - it will required
				//later for BIND command.
				if(null == stateObj.RemoteEndPoint)
				{
					_remotePort = stateObj.HostPort;
					_remoteHost = stateObj.HostName;
				}
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
		override internal void Bind(SocketBase socket)
		{
			CheckDisposed();
			SetProgress(true);
			try
			{
				//-----------------------------------------
				// Get end point for the proxy server
				//
				IPHostEntry host = GetHostByName(_proxyServer);
				if(host == null)
					throw new SocketException(SockErrors.WSAHOST_NOT_FOUND);
					//throw new HostNotFoundException("Unable to resolve proxy host name.");

				IPEndPoint proxyEndPoint = ConstructEndPoint(host, _proxyPort);

				//-----------------------------------------
				// Connect to proxy server
				//
				_socket.Connect(proxyEndPoint);

				//------------------------------------------
				// Negotiate user
				Negotiate();

				//-----------------------------------------
				// Send BIND command
				//
				byte[] cmd = PrepareBindCmd((Socket_Socks5)socket);
				NStream.Write(cmd, 0, cmd.Length);

				//-----------------------------------------
				// Read the reply from the proxy server. 
				byte[] reply = ReadVerifyReply();
				_localEndPoint = ExtractReplyAddr(reply);

				//remote end point is unknown till accept
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

			if(null == baseSocket)
				throw new ArgumentNullException("baseSocket", "The value cannot be null");

			Bind_SO stateObj = null;
			SetProgress(true);
			try
			{
				stateObj = new Bind_SO((Socket_Socks5)baseSocket, callback, state);

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

				//------------------------------------------
				// Negotiate user
				BeginNegotiate(new AsyncCallback(Bind_Negotiate_End), 
					stateObj);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
				stateObj.SetCompleted();
			}
		}

		void Bind_Negotiate_End(IAsyncResult ar)
		{
			Bind_SO stateObj = (Bind_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				EndNegotiate(ar);

				//------------------------------------
				// Send BIND command
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
				BeginReadVerifyReply(new AsyncCallback(Bind_Read_End), stateObj);

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
				byte[] reply = EndReadVerifyReply(ar);
				_localEndPoint = ExtractReplyAddr(reply);
				_remoteEndPoint = null;
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
			}
			stateObj.SetCompleted();
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
