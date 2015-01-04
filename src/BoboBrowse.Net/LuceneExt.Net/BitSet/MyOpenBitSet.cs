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
