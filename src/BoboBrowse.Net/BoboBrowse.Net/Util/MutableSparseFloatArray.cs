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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.Support;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public class MutableSparseFloatArray : SparseFloatArray
    {
        private IDictionary<int, float> _map;
        private bool _isDirty;

        public MutableSparseFloatArray(float[] floats)
            : base(floats)
        {
            _map = new Dictionary<int, float>();
            _isDirty = false;
        }

        public bool IsDirty
        {
            get { return _isDirty; }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override float Get(int index)
        {
            var val = base.Get(index);
            if (val != 0f)
            {
                return val;
            }
            // else, check here!
            float? stored = null;
            if (_map.ContainsKey(index))
            {
                stored = _map[index];
            }
            if (stored != null)
            {
                return (float)stored;
            }
            return 0f;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Set(int idx, float val)
        {
            _isDirty = true;
            if (null == _bits && null != _floats)
            {
                _floats[idx] = val;
            }
            else
            {
                if (null != _bits && _bits.Get(idx))
                {
                    // count the number of bits that are on BEFORE this idx
                    int count;
                    int @ref = idx / REFERENCE_POINT_EVERY - 1;
                    if (@ref >= 0)
                    {
                        count = _referencePoints[@ref];
                    }
                    else
                    {
                        count = 0;
                    }
                    int i = idx - idx % REFERENCE_POINT_EVERY;
                    while ((i = _bits.NextSetBit(i)) >= 0 && i < idx)
                    {
                        count++;
                        i++;
                    }
                    _floats[count] = val;
                }
                else
                {
                    if (val != 0f)
                    {
                        _map.Put(idx, val);
                    }
                    else
                    {
                        float? stored = null;
                        if (_map.ContainsKey(idx))
                        {
                            stored = _map[idx];
                        }
                        if (stored != null)
                        {
                            _map.Remove(idx);
                        }
                    }
                    int sz = _map.Count;
                    // keep something on the order of 32KB, or 0.4*compressed size, in _map
                    // if _floats is null, then that's the same as it existing but being of length 0
                    // if sz > 512, and _floats is null, that's the same as checking if sz > 0.4f*0 and that sz > 2f*0, which is true
                    // in other words, if _floats is null, and sz > 512, then our expansion rule says to condense()
                    if (sz > 512 && (null == _floats || (sz > 4096 ? sz > 0.4f * _floats.Length : sz > 2f * _floats.Length)))
                    {
                        Condense();
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
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override float[] Expand()
        {
            float[] all = base.Expand();
            foreach (int key in _map.Keys)
            {
                float val = _map[key];
                all[key] = val;
            }
            return all;
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
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Condense()
        {
            base.Condense(this.Expand());
            _map = new Dictionary<int, float>();
        }
    }
}
