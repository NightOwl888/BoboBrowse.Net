//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
//*
//* Copyright (C) 2005-2015  John Wang
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//*   http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.Support;
    using Lucene.Net.Search;
    using Lucene.Net.Support;
    using Lucene.Net.Util;
    using System;

    /// <summary> 
    /// This class is written for a special purpose. No check is done in insertion and getting a value
    /// for performance reasons. Be careful if you are going to use this class.
    /// author femekci
    /// </summary>
    [Serializable]
    public sealed class BigIntArray : BigSegmentedArray
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private int[][] _array;

        // Remember that 2^SHIFT_SIZE = BLOCK_SIZE 
        private const int BLOCK_SIZE = 1024;
        private const int SHIFT_SIZE = 10;
        private const int MASK = BLOCK_SIZE - 1;

        public BigIntArray(int size)
            : base(size)
        {
            _array = new int[_numrows][];
            for (int i = 0; i < _numrows; i++)
            {
                _array[i] = new int[BLOCK_SIZE];
            }
        }

        public override sealed void Add(int docId, int val)
        {
            _array[docId >> SHIFT_SIZE][docId & MASK] = val;
        }

        public override sealed int Get(int docId)
        {
            return _array[docId >> SHIFT_SIZE][docId & MASK];
        }

        public override sealed int FindValue(int val, int docId, int maxId)
        {
            while (true)
            {
                if (_array[docId >> SHIFT_SIZE][docId & MASK] == val) return docId;
                if (docId++ >= maxId) break;
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        public override sealed int FindValues(OpenBitSet bitset, int docId, int maxId)
        {
            while (true)
            {
                if (bitset.FastGet(_array[docId >> SHIFT_SIZE][docId & MASK])) return docId;
                if (docId++ >= maxId) break;
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        public override sealed int FindValueRange(int minVal, int maxVal, int docId, int maxId)
        {
            while (true)
            {
                int val = _array[docId >> SHIFT_SIZE][docId & MASK];
                if (val >= minVal && val <= maxVal) return docId;
                if (docId++ >= maxId) break;
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        public override sealed int FindBits(int bits, int docId, int maxId)
        {
            while (true)
            {
                if ((_array[docId >> SHIFT_SIZE][docId & MASK] & bits) != 0) return docId;
                if (docId++ >= maxId) break;
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        public override sealed void Fill(int val)
        {
            foreach (int[] block in _array)
            {
                Arrays.Fill(block, val);
            }
        }

        public override void EnsureCapacity(int size)
        {
            int newNumrows = (size >> SHIFT_SIZE) + 1;
            if (newNumrows > _array.Length)
            {
                int[][] newArray = new int[newNumrows][]; // grow
                System.Array.Copy(_array, 0, newArray, 0, _array.Length);
                for (int i = _array.Length; i < newNumrows; ++i)
                {
                    newArray[i] = new int[BLOCK_SIZE];
                }
                _array = newArray;
            }
            _numrows = newNumrows;
        }

        protected override sealed int GetBlockSize()
        {
            return BLOCK_SIZE;
        }

        protected override sealed int GetShiftSize()
        {
            return SHIFT_SIZE;
        }

        public override int MaxValue
        {
            get { return int.MaxValue; }
        }

        public static BigSegmentedArray FromArray(int[] original)
        {
            BigIntArray result = new BigIntArray(original.Length);
            int i = 0;
            foreach (int c in original)
            {
                result.Add(i++, c);
            }
            return result;
        }

        public static int[] ToArray(BigSegmentedArray original)
        {
            int[] result = new int[original.Length];
            for (int i = 0; i < original.Length; i++)
            {
                result[i] = original.Get(i);
            }
            return result;
        }
    }
}
