// =============================================================
// BytesRoad.NetSuit : A free network library for .NET platform 
// =============================================================
//
// Copyright (C) 2004-2005 BytesRoad Software
// 
// Project Info: http://www.bytesroad.com/NetSuit
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
using System.Diagnostics;
using System.Collections;

namespace BytesRoad.Diag
{
    /// <summary>
    /// Provides a thread-safe list of 
    /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>
    /// objects.
    /// </summary>
    /// 
    /// <remarks>
    /// The <b>TraceListenerSet</b> class provides the same set of 
    /// functionality as
    /// <see cref="System.Diagnostics.TraceListenerCollection">TraceListenerCollection</see>
    /// provides. The reason for creating this class is inpossibility of
    /// using <b>TraceListenerCollection</b> due to hidden constructor.
    /// </remarks>
    public class NSTraceListeners : IList, ICollection,
        IEnumerable
    {
        ArrayList _listeners = new ArrayList();

        internal NSTraceListeners()
        {
        }

    
        #region thunks for compilator

        /// <summary>
        /// This member supports the .NET Framework infrastructure 
        /// and is not intended to be used directly from your code.
        /// </summary>
        /// <exclude/>
        virtual public bool IsSynchronized 
        {
            get { return false; }
        }

        /// <summary>
        /// This member supports the .NET Framework infrastructure 
        /// and is not intended to be used directly from your code.
        /// </summary>
        /// <exclude/>
        virtual public bool IsFixedSize 
        {
            get { return false; }
        }

        /// <summary>
        /// This member supports the .NET Framework infrastructure 
        /// and is not intended to be used directly from your code.
        /// </summary>
        /// <exclude/>
        virtual public bool IsReadOnly 
        {
            get { return false; }
        }

        /// <summary>
        /// This member supports the .NET Framework infrastructure 
        /// and is not intended to be used directly from your code.
        /// </summary>
        /// <exclude/>
        object ICollection.SyncRoot 
        {
            get { return this; }
        }

        /// <summary>
        /// This member supports the .NET Framework infrastructure 
        /// and is not intended to be used directly from your code.
        /// </summary>
        /// <exclude/>
        int IList.Add(object val)
        {
            return Add((TraceListener)val);
        }

        /// <summary>
        /// This member supports the .NET Framework infrastructure 
        /// and is not intended to be used directly from your code.
        /// </summary>
        /// <exclude/>
        void IList.Remove(object val)
        {
            Remove((TraceListener)val);
        }


        /// <summary>
        /// This member supports the .NET Framework infrastructure 
        /// and is not intended to be used directly from your code.
        /// </summary>
        /// <exclude/>
        bool IList.Contains(object val)
        {
            return Contains((TraceListener)val);
        }

        /// <summary>
        /// This member supports the .NET Framework infrastructure 
        /// and is not intended to be used directly from your code.
        /// </summary>
        /// <exclude/>
        int IList.IndexOf(object val)
        {
            return IndexOf((TraceListener)val);
        }

        /// <summary>
        /// This member supports the .NET Framework infrastructure 
        /// and is not intended to be used directly from your code.
        /// </summary>
        /// <exclude/>
        void IList.Insert(int index, object val)
        {
            Insert(index, (TraceListener)val);
        }

        /// <summary>
        /// This member supports the .NET Framework infrastructure 
        /// and is not intended to be used directly from your code.
        /// </summary>
        /// <exclude/>
        object IList.this[int index] 
        {
            get { return this[index]; }
            set { this[index] = (TraceListener)value; }
        }

        #endregion

        /// <summary>
        /// Gets the number of listeners in the list.
        /// </summary>
        /// <value>
        /// The number of listeners in the list.
        /// </value>
        public virtual int Count // ICollection member
        {
            get { return _listeners.Count; }
        }

        /// <overloads>
        /// Gets or sets the specified 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>.  
        /// </overloads>
        /// <summary>
        /// Gets or sets the 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>
        /// at the specified index.
        /// </summary>
        /// <value>
        /// A 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see> 
        /// with the specified index.
        /// </value>
        /// <remarks>
        /// Note that the index is zero-based. 
        /// </remarks>
        public TraceListener this[int index] 
        {
            get { return (TraceListener)_listeners[index]; }
            set { _listeners[index] = value; }
        }

        /// <summary>
        /// Gets the first 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>  
        /// in the list with the specified name.
        /// </summary>
        /// <value>
        /// The first 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>   
        /// in the list with the given 
        /// <see cref="System.Diagnostics.TraceListener.Name">TraceListener.Name</see>.
        /// This item returns a null reference (<b>Nothing</b> in Visual Basic) 
        /// if no <b>TraceListener</b> with the given name can be found.
        /// </value>
        /// <remarks>
        /// Note that the <b>Item</b> property is case-sensitive 
        /// when searching for names.
        /// </remarks>
        public TraceListener this[string name] 
        {
            get 
            { 
                lock(_listeners)
                {
                    foreach(TraceListener tl in _listeners)
                    {
                        if(name.Equals(tl.Name))
                            return tl;
                    }
                }

                return null; 
            }
        }

        /// <summary>
        /// Adds a 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>    
        /// to the list.
        /// </summary>
        /// <param name="listener">
        /// A 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>
        /// to add to the list.
        /// </param>
        /// <returns>
        /// The position at which the new listener was inserted.
        /// </returns>
        public int Add(TraceListener listener)
        {
            return _listeners.Add(listener);
        }

        /// <overloads>
        /// Adds multiple 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>
        /// objects to the collection.
        /// </overloads>
        /// <summary>
        /// Adds an array of 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>
        /// objects to the list.
        /// </summary>
        /// <param name="listeners">
        /// An array of 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>
        /// objects to add to the list.
        /// </param>
        public void AddRange(TraceListener[] listeners)
        {
            _listeners.AddRange(listeners);
        }

        /// <summary>
        /// Adds the contents of another
        /// <see cref="BytesRoad.Diag.NSTraceListeners">NSTraceListeners</see>
        /// to the list.
        /// </summary>
        /// <param name="listeners">
        /// Another 
        /// <see cref="BytesRoad.Diag.NSTraceListeners">NSTraceListeners</see>
        /// whose contents are added to the list.
        /// </param>
        public void AddRange(NSTraceListeners listeners)
        {
            _listeners.AddRange(listeners);
        }

        /// <summary>
        /// Adds the contents of another
        /// <see cref="System.Diagnostics.TraceListenerCollection">TraceListenerCollection</see>
        /// to the list.
        /// </summary>
        /// <param name="listeners">
        /// Another 
        /// <see cref="System.Diagnostics.TraceListenerCollection">TraceListenerCollection</see>
        /// whose contents are added to the list.
        /// </param>
        public void AddRange(TraceListenerCollection listeners)
        {
            _listeners.AddRange(listeners);
        }

        /// <summary>
        /// Clears all the listeners from the list.
        /// </summary>
        public virtual void Clear() // IList member
        {
            _listeners.Clear();
        }
 
        /// <summary>
        /// Checks whether the list contains the specified listener.
        /// </summary>
        /// <param name="listener">
        /// A <see cref="System.Diagnostics.TraceListener">TraceListener</see>    
        /// to find in the list.
        /// </param>
        /// <returns>
        /// <b>true</b> if the listener is in the list; otherwise, <b>false</b>.
        /// </returns>
        /// <remarks>
        /// The <b>Contains</b> method can confirm the existence of a 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>    
        /// before you perform further operations.
        /// </remarks>
        public bool Contains(TraceListener listener) // IList 
        {
            return _listeners.Contains(listener);
        }

        /// <summary>
        /// Copies a section of the current 
        /// <see cref="BytesRoad.Diag.NSTraceListeners">NSTraceListeners</see>
        /// list to the specified array at the specified index.
        /// </summary>
        /// <param name="listeners">
        /// An array of type 
        /// <see cref="System.Array">Array</see>
        /// to copy the elements into.
        /// </param>
        /// <param name="index">
        /// The starting index number in the current list to copy from. 
        /// </param>
        public void CopyTo(TraceListener[] listeners, int index) // ICollection memeber
        {
            _listeners.CopyTo(index, listeners, 0, _listeners.Count);
        }

        /// <summary>
        /// This member supports the .NET Framework infrastructure 
        /// and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        /// <exclude/>
        public void CopyTo(Array array, int index)
        {}

        /// <summary>
        /// Gets an enumerator for this list.
        /// </summary>
        /// <returns>
        /// An enumerator of type 
        /// <see cref="System.Collections.IEnumerator">IEnumerator</see>.
        /// </returns>
        public virtual IEnumerator GetEnumerator() // IEnumerable
        {
            return _listeners.GetEnumerator();
        }

        /// <summary>
        /// Gets the index of the specified listener.
        /// </summary>
        /// <param name="listener">
        /// A 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>    
        /// to find in the list. 
        /// </param>
        /// <returns>
        /// The index of the listener, if it can be found
        /// in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(TraceListener listener)
        {
            return _listeners.IndexOf(listener);
        }

        /// <summary>
        /// Inserts the listener at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which the new
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>.
        /// should be inserted.
        /// </param>
        /// <param name="listener">
        /// A 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>
        /// to insert in the list. 
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The <i>index</i> is not a valid index in the list.
        /// </exception>
        public void Insert(int index, TraceListener listener)
        {
            _listeners.Insert(index, listener);
        }

        /// <overloads>
        /// Removes a specific
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>
        /// from the collection.
        /// </overloads>
        /// <summary>
        /// Removes from the collection the specified 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see>.
        /// </summary>
        /// <param name="listener">
        /// A 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see> 
        /// to remove from the list.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// The <i>listener</i> does not exist in the list.
        /// </exception>
        public void Remove(TraceListener listener)
        {
            if(false == Contains(listener))
                throw new ArgumentException("The listener does not exist in the list.", "listener");
            _listeners.Remove(listener);
        }

        /// <summary>
        /// Removes from the collection the first 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see> 
        /// with the specified name.
        /// </summary>
        /// <param name="name">
        /// The case-sensitive name of the 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see> 
        /// to remove from the list.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// A listener with the given <i>name</i> does not exist in the list.
        /// </exception>
        public void Remove(string name)
        {
            TraceListener tl = this[name];
            if(null == tl)
                throw new ArgumentException("A listener with the given name does not exist in the list.", "name");
            _listeners.Remove(tl);
        }

        /// <summary>
        /// Removes from the collection the 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see> 
        /// at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the 
        /// <see cref="System.Diagnostics.TraceListener">TraceListener</see> 
        /// to remove from the list.
        /// </param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The <i>index</i> is not a valid index in the list.
        /// </exception>
        public virtual void RemoveAt(int index)
        {
            _listeners.RemoveAt(index);
        }
    }
}
