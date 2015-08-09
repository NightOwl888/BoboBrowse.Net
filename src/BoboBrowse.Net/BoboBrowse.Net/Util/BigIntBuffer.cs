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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.Support;
    using System.Collections.Generic;

    ///<summary>@author ymatsuda</summary>
    public class BigIntBuffer
    {
        private const int PAGESIZE = 1024;
        private const int MASK = 0x3FF;
        private const int SHIFT = 10;

        private readonly List<int[]> _buffer;
        private int _allocSize;
        private int _mark;

        public BigIntBuffer()
        {
            _buffer = new List<int[]>();
            _allocSize = 0;
            _mark = 0;
        }

        public virtual int Alloc(int size)
        {
            if (size > PAGESIZE)
                throw new System.ArgumentException("size too big");

            if ((_mark + size) > _allocSize)
            {
                int[] page = new int[PAGESIZE];
                _buffer.Add(page);
                _allocSize += PAGESIZE;
            }
            int ptr = _mark;
            _mark += size;

            return ptr;
        }

        public virtual void Reset()
        {
            _mark = 0;
        }

        public virtual void Set(int ptr, int val)
        {
            int[] page = _buffer.Get(ptr >> SHIFT);
            page[ptr & MASK] = val;
        }

        public virtual int Get(int ptr)
        {
            int[] page = _buffer.Get(ptr >> SHIFT);
            return page[ptr & MASK];
        }
    }
}
