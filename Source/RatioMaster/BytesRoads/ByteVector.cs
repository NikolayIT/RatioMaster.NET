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

namespace BytesRoad.Net.Sockets
{
    /// <summary>
    /// Summary description for ByteVector.
    /// </summary>
    internal class ByteVector
    {
        byte[] _data = new byte[0];
        int _capacity = 0;
        int _size = 0;
        /*
        internal ByteVector()
        {
        }
        */

        #region Attributes
        internal byte[] Data
        {
            get { return _data; }
        }

        internal int Size
        {
            get { return _size; }
        }

        internal int Capacity
        {
            get { return _capacity; }
        }
        #endregion

        void Reallocate(int requiredSize)
        {
            int newSize = (_capacity > 0) ? _capacity : 1;
            while(newSize < requiredSize)
                newSize <<= 1;

            byte[] data = new byte[newSize];
            if(null != _data)
                _data.CopyTo(data, 0);
            _data = data;
            _capacity = newSize;
        }

        void EnsureSpace(int needMore)
        {
            if(_size + needMore >= _capacity)
                Reallocate(_size + needMore);
        }

        internal void Add(byte[] data)
        {
            EnsureSpace(data.Length);
            Array.Copy(data, 0, _data, _size, data.Length);
            _size += data.Length;
        }

        internal void Add(
            byte[] data,
            int offset)
        {
            int copyNum = data.Length - offset;
            EnsureSpace(copyNum);
            Array.Copy(data, offset, _data, _size, copyNum);
            _size += copyNum;
        }

        internal void Add(byte[] data, int offset, int length)
        {
            EnsureSpace(length);
            Array.Copy(data, offset, _data, _size, length);
            _size += length;
        }

        internal void Add(ByteVector data)
        {
            Add(data.Data, 0, data.Size);
        }

        internal void CutTail(int count)
        {
            if(count < 0)
                throw new ArgumentException("Should be a positive value", "count");

            if(count > _size)
                _size = 0;
            else
                _size -= count;
        }

        internal void CutHead(int count)
        {
            if(count < 0)
                throw new ArgumentException("Should be a positive value", "count");

            if(count > _size)
            {
                _size = 0;
            }
            else
            {
                _size -= count;
                Array.Copy(_data, count, _data, 0, _size);
            }
        }

        internal void Clear()
        {
            _size = 0;
        }
    }
}
