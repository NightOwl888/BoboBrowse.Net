//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Written in Java.
//* 
//* Copyright (C) 2005-2006  John Wang
//*
//* This library is free software; you can redistribute it and/or
//* modify it under the terms of the GNU Lesser General Public
//* License as published by the Free Software Foundation; either
//* version 2.1 of the License, or (at your option) any later version.
//*
//* This library is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Util
{
    using Lucene.Net.Util;

    public abstract class BigSegmentedArray
    {
        private readonly int _size;
        private readonly int _blockSize;
        private readonly int _shiftSize;

        protected internal int _numrows;

        protected BigSegmentedArray(int size)
        {
            _size = size;
            _blockSize = GetBlockSize();
            _shiftSize = GetShiftSize();
            _numrows = (size >> _shiftSize) + 1;
        }

        public virtual int Size()
        {
            return _size;
        }

        protected abstract int GetBlockSize();

        // TODO: maybe this should be automatically calculated
        protected abstract int GetShiftSize();

        public abstract int Get(int docId);

        public virtual int Capacity()
        {
            return _numrows * _blockSize;
        }

        public abstract void Add(int docId, int val);

        public abstract void Fill(int val);

        public abstract void EnsureCapacity(int size);

        public abstract int MaxValue { get; }

        public abstract int FindValue(int val, int docId, int maxId);

        public abstract int FindValues(OpenBitSet bitset, int docId, int maxId);

        public abstract int FindValues(BitVector bitset, int docId, int maxId);

        public abstract int FindValueRange(int minVal, int maxVal, int docId, int maxId);

        public abstract int FindBits(int bits, int docId, int maxId);
    }
}
