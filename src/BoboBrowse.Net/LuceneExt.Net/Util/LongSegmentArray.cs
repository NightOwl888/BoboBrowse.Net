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

namespace LuceneExt.Util
{
    using Lucene.Net.Util;
    using System;

    [Serializable]
    public class LongSegmentArray : PrimitiveArray<OpenBitSet>
    {
        public LongSegmentArray(int len)
            : base(len)
        {
        }

        public LongSegmentArray()
        {
        }

        protected internal override object BuildArray(int len)
        {
            return new long[len][];
        }

        public virtual void Add(long[] val)
        {
            EnsureCapacity(Count + 1);
            long[][] array = (long[][])base.Array;
            array[Count] = val;
            Count++;
        }

        public virtual void Get(int index, long[] @ref)
        {
            EnsureCapacity(index);
            ((long[][])Array)[index] = @ref;
            Count = Math.Max(Count, index + 1);
        }

        public virtual long[] Get(int index)
        {
            return ((long[][])Array)[index];
        }
    }
}
