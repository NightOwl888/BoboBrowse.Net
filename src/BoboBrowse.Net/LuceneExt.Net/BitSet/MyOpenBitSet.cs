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

﻿// Kamikaze version compatibility level: 3.0.6
namespace LuceneExt.BitSet
{
    using Lucene.Net.Util;
    using System;

    [Serializable]
    public class MyOpenBitSet : OpenBitSet
    {
        //private static long serialVersionUID = 1L; // NOT USED

        public MyOpenBitSet()
        {
        }

        public MyOpenBitSet(long numBits)
            : base(numBits)
        {
        }

        /// <summary>
        /// Set 0/1 at the specified index.
        /// Note: The value for the bitVal is not checked for 0/1, hence incorrect values passed 
        /// lead to unexpected results
        /// </summary>
        /// <param name="index"></param>
        /// <param name="bitVal"></param>
        public void FastSetAs(long index, int bitVal)
        {
            int wordNum = (int)(index >> 6);
            int bit = (int)index & 0x3f;
            long bitmask = ((long)bitVal) << bit;
            Bits[wordNum] |= bitmask;
        }
    }
}
