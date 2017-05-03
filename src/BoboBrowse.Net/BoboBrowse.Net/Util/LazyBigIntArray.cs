//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// BigSegmentedArray that creates segments only when the corresponding index is
    /// being accessed.
    /// author jko
    /// </summary>
    [Serializable]
    public class LazyBigIntArray : BigSegmentedArray
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private int[][] m_array;
        /* Remember that 2^SHIFT_SIZE = BLOCK_SIZE */
        private const int BLOCK_SIZE = 1024;
        private const int SHIFT_SIZE = 10;
        private const int MASK = BLOCK_SIZE - 1;

        private int m_fillValue = 0;

        public LazyBigIntArray(int size)
            : base(size)
        {
            // initialize empty blocks
            m_array = new int[m_numrows][];
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#getBlockSize()
        /// </summary>
        /// <returns></returns>
        protected override int GetBlockSize()
        {
            return BLOCK_SIZE;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#getShiftSize()
        /// </summary>
        /// <returns></returns>
        protected override int GetShiftSize()
        {
            return SHIFT_SIZE;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#get(int)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override int Get(int id)
        {
            int i = id >> SHIFT_SIZE;
            if (m_array[i] == null)
                return m_fillValue; // return _fillValue to mimic int[] behavior
            else
                return m_array[i][id & MASK];
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#add(int, int)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="val"></param>
        public override void Add(int id, int val)
        {
            int i = id >> SHIFT_SIZE;
            if (m_array[i] == null)
            {
                m_array[i] = new int[BLOCK_SIZE];
                if (m_fillValue != 0)
                    Arrays.Fill(m_array[i], m_fillValue);
            }
            m_array[i][id & MASK] = val;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#fill(int)
        /// </summary>
        /// <param name="val"></param>
        public override void Fill(int val)
        {
            foreach (int[] block in m_array)
            {
                if (block == null) continue;
                Arrays.Fill(block, val);
            }

            m_fillValue = val;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#ensureCapacity(int)
        /// </summary>
        /// <param name="size"></param>
        public override void EnsureCapacity(int size)
        {
            int newNumrows = (size >> SHIFT_SIZE) + 1;
            if (newNumrows > m_array.Length)
            {
                int[][] newArray = new int[newNumrows][];           // grow
                System.Array.Copy(m_array, 0, newArray, 0, m_array.Length);
                // don't allocate new rows
                m_array = newArray;
            }
            m_numrows = newNumrows;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#maxValue()
        /// </summary>
        public override int MaxValue
        {
            get { return int.MaxValue; }
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#findValue(int, int, int)
        /// </summary>
        /// <param name="val"></param>
        /// <param name="id"></param>
        /// <param name="maxId"></param>
        /// <returns></returns>
        public override int FindValue(int val, int id, int maxId)
        {
            while (id <= maxId)
            {
                int i = id >> SHIFT_SIZE;
                if (m_array[i] == null)
                {
                    if (val == m_fillValue)
                        return id;
                    else
                        id = (i + 1) << SHIFT_SIZE; // jump to next segment
                }
                else
                {
                    if (m_array[i][id & MASK] == val)
                        return id;
                    else
                        id++;
                }
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#findValues(org.apache.lucene.util.OpenBitSet, int, int)
        /// </summary>
        /// <param name="bitset"></param>
        /// <param name="id"></param>
        /// <param name="maxId"></param>
        /// <returns></returns>
        public override int FindValues(OpenBitSet bitset, int id, int maxId)
        {
            while (id <= maxId)
            {
                int i = id >> SHIFT_SIZE;
                if (m_array[i] == null)
                {
                    if (bitset.FastGet(m_fillValue))
                        return id;
                    else
                        id = (i + 1) << SHIFT_SIZE; // jump to next segment
                }
                else
                {
                    if (bitset.FastGet(m_array[i][id & MASK]))
                        return id;
                    else
                        id++;
                }
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#findValueRange(int, int, int, int)
        /// </summary>
        /// <param name="minVal"></param>
        /// <param name="maxVal"></param>
        /// <param name="id"></param>
        /// <param name="maxId"></param>
        /// <returns></returns>
        public override int FindValueRange(int minVal, int maxVal, int id, int maxId)
        {
            while (id <= maxId)
            {
                int i = id >> SHIFT_SIZE;
                if (m_array[i] == null)
                {
                    if (m_fillValue >= minVal && m_fillValue <= maxVal)
                        return id;
                    else
                        id = (i + 1) << SHIFT_SIZE; // jump to next segment
                }
                else
                {
                    int val = m_array[i][id & MASK];
                    if (val >= minVal && val <= maxVal)
                        return id;
                    else
                        id++;
                }
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#findBits(int, int, int)
        /// </summary>
        /// <param name="bits"></param>
        /// <param name="id"></param>
        /// <param name="maxId"></param>
        /// <returns></returns>
        public override int FindBits(int bits, int id, int maxId)
        {
            while (id <= maxId)
            {
                int i = id >> SHIFT_SIZE;
                if (m_array[i] == null)
                {
                    if ((m_fillValue & bits) != 0)
                        return id;
                    else
                        id = (i + 1) << SHIFT_SIZE; // jump to next segment
                }
                else
                {
                    int val = m_array[i][id & MASK];
                    if ((val & bits) != 0)
                        return id;
                    else
                        id++;
                }
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }
    }
}