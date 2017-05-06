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
    using System.Collections.Generic;

    /// <summary>
    /// NOTE: This was BigIntBuffer in bobo-browse
    /// <para/>
    /// @author ymatsuda
    /// </summary>
    public class BigInt32Buffer
    {
        private const int PAGESIZE = 1024;
        private const int MASK = 0x3FF;
        private const int SHIFT = 10;

        private readonly List<int[]> m_buffer;
        private int m_allocSize;
        private int m_mark;

        public BigInt32Buffer()
        {
            m_buffer = new List<int[]>();
            m_allocSize = 0;
            m_mark = 0;
        }

        public virtual int Alloc(int size)
        {
            if (size > PAGESIZE)
                throw new System.ArgumentException("size too big");

            if ((m_mark + size) > m_allocSize)
            {
                int[] page = new int[PAGESIZE];
                m_buffer.Add(page);
                m_allocSize += PAGESIZE;
            }
            int ptr = m_mark;
            m_mark += size;

            return ptr;
        }

        public virtual void Reset()
        {
            m_mark = 0;
        }

        public virtual void Set(int ptr, int val)
        {
            int[] page = m_buffer.Get(ptr >> SHIFT);
            page[ptr & MASK] = val;
        }

        public virtual int Get(int ptr)
        {
            int[] page = m_buffer.Get(ptr >> SHIFT);
            return page[ptr & MASK];
        }
    }
}
