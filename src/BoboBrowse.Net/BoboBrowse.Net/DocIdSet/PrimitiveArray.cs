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

    [Serializable]
    public abstract class PrimitiveArray<T> : ICloneable
    {
        protected internal T[] Array { get; set; } // TODO: Change back to field ?

        protected internal int m_count;

        protected internal int Growth { get; set; } // TODO: Change back to field ?

        protected internal int Len { get; set; } // TODO: Change back to field ?

        private const int DEFAULT_SIZE = 1000;

        protected abstract T[] BuildArray(int len);

        protected PrimitiveArray(int len)
        {
            if (len <= 0)
            {
                throw new ArgumentException("len must be greater than 0: " + len);
            }
            Array = BuildArray(len);
            m_count = 0;
            Growth = 10;
            Len = len;
        }

        protected internal PrimitiveArray()
            : this(DEFAULT_SIZE)
        {
        }

        public virtual void Clear()
        {
            m_count = 0;
            Growth = 10;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected internal virtual void Expand()
        {
            Expand(Len + 100);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected internal virtual void Expand(int idx)
        {
            if (idx <= Len)
                return;
            int oldLen = Len;
            Len = idx + Growth;
            T[] newArray = BuildArray(Len);
            System.Array.Copy((Array)this.Array, 0, (Array)newArray, 0, oldLen);
            Growth += Len;
            Array = newArray;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual void EnsureCapacity(int idx)
        {
            Expand(idx);
        }

        public virtual int Count
        {
            get { return m_count; }
        }

        ///<summary>called to shrink the array size to the current # of elements to save memory.</summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual void Seal()
        {
            if (Len > m_count)
            {
                T[] newArray = BuildArray(m_count);
                System.Array.Copy((Array)this.Array, 0, (Array)newArray, 0, m_count);
                Array = newArray;
                Len = m_count;
            }
            Growth = 10;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual T[] ToArray(T[] array)
        {
            System.Array.Copy((Array)this.Array, 0, (Array)array, 0, m_count);
            return array;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual object ToArray()
        {
            object array = BuildArray(m_count);
            System.Array.Copy((Array)this.Array, 0, (Array)array, 0, m_count);
            return array;
        }

        public virtual object Clone()
        {
            return base.MemberwiseClone();
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
                buffer.Append(((Array)Array).GetValue(i));
            }
            buffer.Append(']');

            return buffer.ToString();
        }

        public virtual int Length
        {
            get { return this.Len; }
        }
    }
}
