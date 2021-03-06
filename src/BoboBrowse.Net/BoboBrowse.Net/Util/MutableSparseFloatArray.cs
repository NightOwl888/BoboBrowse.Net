﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
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
    using Lucene.Net.Support;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// NOTE: This was MutableSparseFloatArray in bobo-browse
    /// </summary>
    public class MutableSparseSingleArray : SparseSingleArray
    {
        private IDictionary<int, float> m_map;
        private bool m_isDirty;

        private object syncLock = new object();

        public MutableSparseSingleArray(float[] floats)
            : base(floats)
        {
            m_map = new Dictionary<int, float>();
            m_isDirty = false;
        }

        public bool IsDirty
        {
            get { return m_isDirty; }
        }

        public override float Get(int index)
        {
            lock (syncLock)
            {
                var val = base.Get(index);
                if (val != 0f)
                {
                    return val;
                }
                // else, check here!
                float? stored = null;
                if (m_map.ContainsKey(index))
                {
                    stored = m_map[index];
                }
                if (stored != null)
                {
                    return (float)stored;
                }
                return 0f;
            }
        }

        public void Set(int idx, float val)
        {
            lock (syncLock)
            {
                m_isDirty = true;
                if (null == m_bits && null != m_floats)
                {
                    m_floats[idx] = val;
                }
                else
                {
                    if (null != m_bits && m_bits.Get(idx))
                    {
                        // count the number of bits that are on BEFORE this idx
                        int count;
                        int @ref = idx / REFERENCE_POINT_EVERY - 1;
                        if (@ref >= 0)
                        {
                            count = m_referencePoints[@ref];
                        }
                        else
                        {
                            count = 0;
                        }
                        int i = idx - idx % REFERENCE_POINT_EVERY;
                        while ((i = m_bits.NextSetBit(i)) >= 0 && i < idx)
                        {
                            count++;
                            i++;
                        }
                        m_floats[count] = val;
                    }
                    else
                    {
                        if (val != 0f)
                        {
                            m_map.Put(idx, val);
                        }
                        else
                        {
                            float? stored = null;
                            if (m_map.ContainsKey(idx))
                            {
                                stored = m_map[idx];
                            }
                            if (stored != null)
                            {
                                m_map.Remove(idx);
                            }
                        }
                        int sz = m_map.Count;
                        // keep something on the order of 32KB, or 0.4*compressed size, in _map
                        // if _floats is null, then that's the same as it existing but being of length 0
                        // if sz > 512, and _floats is null, that's the same as checking if sz > 0.4f*0 and that sz > 2f*0, which is true
                        // in other words, if _floats is null, and sz > 512, then our expansion rule says to condense()
                        if (sz > 512 && (null == m_floats || (sz > 4096 ? sz > 0.4f * m_floats.Length : sz > 2f * m_floats.Length)))
                        {
                            Condense();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Future modifications to this may or may not affect values stored in the 
        /// returned array.  Modifications to the returned array, cause this object instance to become unusable, as
        /// from this point on all operations are UNDEFINED.
        /// </summary>
        /// <returns>the expanded primitive float array rep. of this.</returns>
        public override float[] Expand()
        {
            lock (syncLock)
            {
                float[] all = base.Expand();
                foreach (int key in m_map.Keys)
                {
                    float val = m_map[key];
                    all[key] = val;
                }
                return all;
            }
        }

        /// <summary>
        /// An expensive, but necessary, operation to internally conserve space as things grow.
        /// Might be useful to call outside of the automatic maintenance, 
        /// when you expect very few new non-zero values, just changes to existing values,
        /// in the future.
        /// 
        /// Uses an expanded form of the float array as scratch space in memory, so be careful 
        /// that you have enough memory, and try doing this one at a time.
        /// </summary>
        public void Condense()
        {
            lock (syncLock)
            {
                base.Condense(this.Expand());
                m_map = new Dictionary<int, float>();
            }
        }
    }
}
