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
namespace BoboBrowse.Net.DocIdSet
{
    using BoboBrowse.Net.Support;
    using System;
    using System.Runtime.CompilerServices;
    using System.Text;

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class PrimitiveArray<T> //: ICloneable
    {
        protected internal T[] m_array;

        protected internal int m_count;

        protected internal int m_growth;

        protected internal int m_len;

        private const int DEFAULT_SIZE = 1000;

        private object syncLock = new object();

        protected abstract T[] BuildArray(int len);

        protected PrimitiveArray(int len)
        {
            if (len <= 0)
            {
                throw new ArgumentException("len must be greater than 0: " + len);
            }
            m_array = BuildArray(len);
            m_count = 0;
            m_growth = 10;
            m_len = len;
        }

        protected internal PrimitiveArray()
            : this(DEFAULT_SIZE)
        {
        }

        public virtual void Clear()
        {
            m_count = 0;
            m_growth = 10;
        }

        protected internal virtual void Expand()
        {
            lock (syncLock)
            {
                Expand(m_len + 100);
            }
        }

        protected internal virtual void Expand(int idx)
        {
            lock (syncLock)
            {
                if (idx <= m_len)
                    return;
                int oldLen = m_len;
                m_len = idx + m_growth;
                T[] newArray = BuildArray(m_len);
                System.Array.Copy((Array)this.m_array, 0, (Array)newArray, 0, oldLen);
                m_growth += m_len;
                m_array = newArray;
            }
        }

        public virtual void EnsureCapacity(int idx)
        {
            lock (syncLock)
            {
                Expand(idx);
            }
        }

        public virtual int Count
        {
            get { return m_count; }
        }

        ///<summary>called to shrink the array size to the current # of elements to save memory.</summary>
        public virtual void Seal()
        {
            lock (syncLock)
            {
                if (m_len > m_count)
                {
                    T[] newArray = BuildArray(m_count);
                    System.Array.Copy((Array)this.m_array, 0, (Array)newArray, 0, m_count);
                    m_array = newArray;
                    m_len = m_count;
                }
                m_growth = 10;
            }
        }

        public virtual T[] ToArray(T[] array)
        {
            lock (syncLock)
            {
                System.Array.Copy(this.m_array, 0, array, 0, m_count);
                return array;
            }
        }

        public virtual object ToArray()
        {
            lock (syncLock)
            {
                var array = BuildArray(m_count);
                System.Array.Copy(this.m_array, 0, array, 0, m_count);
                return array;
            }
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder("[");
            for (int i = 0; i < m_count; ++i)
            {
                if (i != 0)
                {
                    buffer.Append(", ");
                }
                buffer.Append(m_array[i]);
            }
            buffer.Append(']');

            return buffer.ToString();
        }

        public virtual int Length
        {
            get { return this.m_len; }
        }
    }
}
