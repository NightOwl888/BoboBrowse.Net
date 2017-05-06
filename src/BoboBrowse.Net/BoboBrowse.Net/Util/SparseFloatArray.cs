//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
//*
//* Copyright (C) 2005-2015  Spackle
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
    using Lucene.Net.Util;
    using System;

    /// <summary>
    /// NOTE: This was SparseFloatArray in bobo-browse
    /// </summary>
    public class SparseSingleArray
    {
        protected float[] m_floats;
        protected OpenBitSet m_bits;

        // the number of bits set BEFORE the given reference point index*REFERENCE_POINT_EVERY.
        protected int[] m_referencePoints;
        private int m_capacity;
        protected const float ON_RATIO_CUTOFF = 0.75f;

        // 32 is 32 bits per 256 floats, which is the same as the 32 bits per 32 floats that are needed
        // in _bits.  
        protected const int REFERENCE_POINT_EVERY = 32;//256;

        /// <summary>
        /// Good for saving memory with sparse float arrays, when those arrays no longer need to be mutable.
        /// 
        /// requires: floats never changes after this method is called returns.
        /// in fact, you should lose all references to it, since this object 
        /// might save you a lot of memory.
        /// </summary>
        /// <param name="floats">The float array.</param>
        public SparseSingleArray(float[] floats)
        {
            m_capacity = floats.Length;
            Condense(floats);
        }

        /// <summary>
        /// Short-cut to quickly create a sparse float array representing 
        /// <code>this(new float[capacity]);</code>, but without reading through said array.
        /// The advantage here is that the constructor is lightning-fast in the case that 
        /// all values in the float array are known to 
        /// <c>== 0f</c>.
        /// </summary>
        /// <param name="capacity">The capacity of the array.</param>
        public SparseSingleArray(int capacity)
        {
            m_capacity = capacity;
            m_floats = null;
            m_bits = null;
            m_referencePoints = null;
        }

        protected virtual void Condense(float[] floats)
        {
            if (floats.Length != m_capacity)
            {
                throw new ArgumentException("bad input float array of length " + floats.Length + " for capacity: " + m_capacity);
            }
            var bits = new OpenBitSet(floats.Length);
            int on = 0;
            for (int i = 0; i < floats.Length; i++)
            {
                if (floats[i] != 0f)
                {
                    bits.Set(i);
                    on++;
                }
            }
            if (((float)on) / ((float)floats.Length) < ON_RATIO_CUTOFF)
            {
                // it's worth compressing
                if (0 == on)
                {
                    // it's worth super-compressing
                    m_floats = null;
                    m_bits = null;
                    m_referencePoints = null;
                    // capacity is good.
                }
                else
                {
                    m_bits = bits;
                    m_floats = new float[m_bits.Cardinality()];
                    m_referencePoints = new int[floats.Length / REFERENCE_POINT_EVERY];
                    int i = 0;
                    int floatsIdx = 0;
                    int refIdx = 0;
                    while (i < floats.Length && (i = m_bits.NextSetBit(i)) >= 0)
                    {
                        m_floats[floatsIdx] = floats[i];
                        while (refIdx < i / REFERENCE_POINT_EVERY)
                        {
                            m_referencePoints[refIdx++] = floatsIdx;
                        }
                        floatsIdx++;
                        i++;
                    }
                    while (refIdx < m_referencePoints.Length)
                    {
                        m_referencePoints[refIdx++] = floatsIdx;
                    }
                }
            }
            else
            {
                // it's not worth compressing
                m_floats = floats;
                m_bits = null;
            }
        }

        /// <summary>
        /// warning: DO NOT modify the return value at all.
        /// the assumption is that these arrays are QUITE LARGE and that we would not want 
        /// to unnecessarily copy them.  this method in many cases returns an array from its
        /// internal representation.  doing anything other than READING these values 
        /// results in UNDEFINED operations on this, from that point on.
        /// </summary>
        /// <returns></returns>
        public virtual float[] Expand()
        {
            if (null == m_bits)
            {
                if (null == m_floats)
                {
                    // super-compressed, all zeros
                    return new float[m_capacity];
                }
                else
                {
                    return m_floats;
                }
            }
            float[] all = new float[m_capacity];
            int floatsidx = 0;
            for (int idx = m_bits.NextSetBit(0); idx >= 0 && idx < m_capacity; idx = m_bits.NextSetBit(idx + 1))
            {
                all[idx] = m_floats[floatsidx++];
            }
            return all;
        }

        public virtual float Get(int index)
        {
            if (null == m_bits)
            {
                if (null == m_floats)
                {
                    // super-compressed, all zeros
                    if (index < 0 || index >= m_capacity)
                    {
                        throw new IndexOutOfRangeException("bad index: " + index + " for SparseFloatArray representing array of length " + m_capacity);
                    }
                    return 0f;
                }
                else
                {
                    return m_floats[index];
                }
            }
            else
            {
                if (m_bits.Get(index))
                {
                    // count the number of bits that are on BEFORE this index
                    int count;
                    int @ref = index / REFERENCE_POINT_EVERY - 1;
                    if (@ref >= 0)
                    {
                        count = m_referencePoints[@ref];
                    }
                    else
                    {
                        count = 0;
                    }
                    int i = index - index % REFERENCE_POINT_EVERY;
                    while ((i = m_bits.NextSetBit(i)) >= 0 && i < index)
                    {
                        count++;
                        i++;
                    }
                    return m_floats[count];
                }
                else
                {
                    return 0f;
                }
            }
        }
    }
}
