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
using System.Threading;

using BytesRoad.Diag;

namespace BytesRoad.Net.Sockets.Advanced
{
    /// <summary>
    /// Summary description for AsyncResultBase.
    /// </summary>
    public class AsyncResultBase : IAsyncResult
    {
        object _callerState = null;
        bool _completedSync = true;
        ManualResetEvent _wait = null;
        AsyncCallback _callback = null;
        bool _isCompleted = false;
        bool _isHandled = false;
        Exception _exception = null;
        int _startThreadId = -1;



        internal AsyncResultBase(AsyncCallback cb, object callerState)
        {
            _callback = cb;
            _callerState = callerState;
            _startThreadId = Thread.CurrentThread.GetHashCode();
        }

        internal void UpdateContext()
        {
            if(Thread.CurrentThread.GetHashCode() != _startThreadId)
                _completedSync = false;
        }

        /*internal void SetCompleted(bool completedSync)
        {
            SetCompleted();
        }*/

        internal void SetCompleted()
        {
            lock(this)  // sync with 'AsyncWaitHandle' property
            {
                UpdateContext();
                _isCompleted = true;
                if(null != _wait)
                    _wait.Set();
            }

            dumpActivityException();

            try
            {
                if(null != CallBack)
                    CallBack(this);
            }
            catch(Exception e)
            {
                NSTrace.WriteLineError("Exception in CB: " + e.ToString());
                throw;
            }

            /*
            catch
            {
                NSTrace.WriteLineError("Non CLS exception in CB: " + Environment.StackTrace.ToString());
                throw;
            }
            */
        }

        void CloseWaitHandle()
        {
            lock(this)
            {
                if(null != _wait)
                {
                    _wait.Close();
                    _wait = null;
                }
            }
        }

        void dumpActivityException()
        {
            Exception e = Exception;
            if(null == e)
                return;
            
            int tid = Thread.CurrentThread.GetHashCode();
            string msg = string.Format("{0} ---------- Start Exception Info -----------------------------\n", tid);
            msg += string.Format("{0} Activity: {1}\n", tid, ActivityName);
            msg += string.Format("{0} Stack: {1}\n", tid, Environment.StackTrace.ToString());
            msg += string.Format("{0} Exception: {1}\n", tid, e.ToString());
            msg += string.Format("{0} ---------- End   Exception Info -----------------------------", tid);

            NSTrace.WriteLineError(msg);
        }


        #region Attributes
        virtual internal string ActivityName 
        { 
            get { return GetType().FullName; } 
        }

        internal AsyncCallback CallBack
        {
            get { return _callback; }
        }

        internal Exception Exception
        {
            get { return _exception; }
            set { _exception = value; }
        }

        internal virtual bool IsHandled
        {
            get { return _isHandled; }

            set 
            { 
                if(value)
                {
                    CloseWaitHandle();
                    _callerState = null;
                    _callback = null;
                }
                else
                {
                    NSTrace.WriteLineError("IsHandled assigned 'false'");
                }

                _isHandled = value; 
            }
        }
        #endregion

        #region IAsyncResult Members
        public object AsyncState
        {
            get { return _callerState; }
        }

        public bool CompletedSynchronously
        {
            get { return _completedSync; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                lock(this) // sync with 'SetCompleted' method
                {
                    if(null == _wait)
                        _wait = new ManualResetEvent(IsCompleted);
                }

                return _wait;
            }
        }

        public bool IsCompleted
        {
            get { return _isCompleted; }
        }
        #endregion
    }
}
