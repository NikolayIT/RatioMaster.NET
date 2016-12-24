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

namespace BytesRoad.Net.Sockets.Advanced
{
    /// <summary>
    /// Summary description for AsyncBase.
    /// </summary>
    public class AsyncBase
    {
        protected bool inProgress = false;
        /*
        internal AsyncBase()
        {
        }
        */
        
        virtual internal void SetProgress(bool progress)
        {
            // prevent from nested calls
            lock(this)
            {
                if(progress)
                {
                    if(inProgress)
                        throw new InvalidOperationException("Attempt to start operation which is already in the progress");
                    inProgress = true;
                }
                else
                {
                    inProgress = false;
                }
            }
        }

        virtual internal void CheckProgress()
        {
            lock(this)
            {
                if(inProgress)
                    throw new InvalidOperationException("Attempt to start operation which is already in the progress");
            }
        }

/*        virtual internal void HandleAsyncEnd(IAsyncResult ar, Type arType, bool turnProgressOff)
        {
            HandleAsyncEnd(ar, arType, null, turnProgressOff);
        }*/

        static internal void VerifyAsyncResult(
            IAsyncResult ar, 
            Type arType)
        {
            VerifyAsyncResult(ar, arType, null);
        }

        static internal void VerifyAsyncResult(
            IAsyncResult ar, 
            Type arType,
            string metName)
        {
            if(null == ar)
                throw new ArgumentNullException("asyncResult", "The value cannot be null.");

            if(null == metName)
                metName = "End*";

            if(false == ar.GetType().Equals(arType))
                throw new ArgumentException(
                    "asyncResult was not returned by a call to the " + 
                    metName + " method.", "asyncResult");

            AsyncResultBase stateObj = (AsyncResultBase)ar;
            if(stateObj.IsHandled)
                throw new InvalidOperationException(metName + " was previously called for the asynchronous operation.");
        }

        virtual internal void HandleAsyncEnd(IAsyncResult ar, bool turnProgressOff)
        {
            if((false == ar.GetType().IsSubclassOf(typeof(AsyncResultBase))) &&
                (false == ar.GetType().Equals(typeof(AsyncResultBase))))
                throw new ArgumentException("asyncResult was not returned by a call to End* method.", "asyncResult");

            AsyncResultBase stateObj = (AsyncResultBase)ar;
            if(stateObj.IsHandled)
                throw new InvalidOperationException("End* method was previously called for the asynchronous operation.");

            if(false == stateObj.IsCompleted)
                stateObj.AsyncWaitHandle.WaitOne();

            stateObj.IsHandled = true;

            if(turnProgressOff)
                SetProgress(false);    

            if(null != stateObj.Exception)
            {
                // dumpActivityException(stateObj);
                throw stateObj.Exception;
            }
        }

        /*
        void dumpActivityException(AsyncResultBase ar)
        {
            Exception e = ar.Exception;
            int tid = Thread.CurrentThread.GetHashCode();
            string msg = string.Format("{0} -----------------------------", tid);
            NSTrace.WriteLineError(msg);

            msg = string.Format("{0} Activity: {1}", tid, ar.ActivityName);
            NSTrace.WriteLineError(msg);

            msg = string.Format("{0} Exception: {1}", tid, e.GetType().FullName);
            NSTrace.WriteLineError(msg);

            msg = string.Format("{0} Message: {1}", tid, e.Message);
            NSTrace.WriteLineError(msg);

            msg = string.Format("{0} Stack: {1}", tid, e.StackTrace);
            NSTrace.WriteLineError(msg);

            msg = string.Format("{0} -----------------------------", tid);
            NSTrace.WriteLineError(msg);
        }
        */
    }
}
