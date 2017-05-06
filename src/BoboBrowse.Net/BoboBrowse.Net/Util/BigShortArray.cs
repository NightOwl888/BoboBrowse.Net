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

    /// <summary>
    /// NOTE: This was BigShortArray in bobo-browse
    /// </summary>
    public class BigInt16Array : BigSegmentedArray
    {
        private short[][] m_array;

        // Remember that 2^SHIFT_SIZE = BLOCK_SIZE 
        private const int BLOCK_SIZE = 2048;
        private const int SHIFT_SIZE = 11;
        private const int MASK = BLOCK_SIZE - 1;

        public BigInt16Array(int size)
            : base(size)
        {
            m_array = new short[m_numrows][];
            for (int i = 0; i < m_numrows; i++)
            {
                m_array[i] = new short[BLOCK_SIZE];
            }
        }

        public override sealed void Add(int docId, int val)
        {
            m_array[docId >> SHIFT_SIZE][docId & MASK] = (short)val;
        }

        public override sealed int Get(int docId)
        {
            return m_array[docId >> SHIFT_SIZE][docId & MASK];
        }

        public override sealed int FindValue(int val, int docId, int maxId)
        {
            while (true)
            {
                if (m_array[docId >> SHIFT_SIZE][docId & MASK] == val) return docId;
                if (docId++ >= maxId) break;
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        public override sealed int FindValues(OpenBitSet bitset, int docId, int maxId)
        {
            while (true)
            {
                if (bitset.FastGet(m_array[docId >> SHIFT_SIZE][docId & MASK])) return docId;
                if (docId++ >= maxId) break;
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        public override sealed int FindValueRange(int minVal, int maxVal, int docId, int maxId)
        {
            while (true)
            {
                int val = m_array[docId >> SHIFT_SIZE][docId & MASK];
                if (val >= minVal && val <= maxVal) return docId;
                if (docId++ >= maxId) break;
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        public override sealed int FindBits(int bits, int docId, int maxId)
        {
            while (true)
            {
                if ((m_array[docId >> SHIFT_SIZE][docId & MASK] & bits) != 0) return docId;
                if (docId++ >= maxId) break;
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        public override sealed void Fill(int val)
        {
            short shortVal = (short)val;
            foreach (short[] block in m_array)
            {
                Arrays.Fill(block, shortVal);
            }
        }

        public override void EnsureCapacity(int size)
        {
            int newNumrows = (size >> SHIFT_SIZE) + 1;
            if (newNumrows > m_array.Length)
            {
                short[][] newArray = new short[newNumrows][]; // grow
                System.Array.Copy(m_array, 0, newArray, 0, m_array.Length);
                for (int i = m_array.Length; i < newNumrows; ++i)
                {
                    newArray[i] = new short[BLOCK_SIZE];
                }
                m_array = newArray;
            }
            m_numrows = newNumrows;
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
            get { return short.MaxValue; }
        }
    }
}
