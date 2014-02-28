// Copyright (c) COMPANY. All rights reserved. 

namespace BoboBrowse.Net.Utils
{
    using System;
    using Lucene.Net.Util;

    public class BigShortArray : BigSegmentedArray
    {
        private short[][] array;

        // Remember that 2^SHIFT_SIZE = BLOCK_SIZE 
        private const int BLOCK_SIZE = 2048;
        private const int SHIFT_SIZE = 11;
        private const int MASK = BLOCK_SIZE - 1;

        public BigShortArray(int size)
            : base(size, BLOCK_SIZE, SHIFT_SIZE)
        {
            array = new short[numrows][];
            for (int i = 0; i < numrows; i++)
            {
                array[i] = new short[BLOCK_SIZE];
            }
        }

        public override sealed void Add(int docId, int val)
        {
            array[docId >> SHIFT_SIZE][docId & MASK] = (sbyte)val;
        }

        public override sealed int Get(int docId)
        {
            return array[docId >> SHIFT_SIZE][docId & MASK];
        }

        public override sealed int FindValue(int val, int docId, int maxId)
        {
            while (docId <= maxId && array[docId >> SHIFT_SIZE][docId & MASK] != val)
            {
                docId++;
            }
            return docId;
        }

        public override sealed int FindValues(OpenBitSet bitset, int docId, int maxId)
        {
            while (docId <= maxId && !bitset.FastGet(array[docId >> SHIFT_SIZE][docId & MASK]))
            {
                docId++;
            }
            return docId;
        }

        public override sealed int FindValueRange(int minVal, int maxVal, int docId, int maxId)
        {
            while (docId <= maxId)
            {
                int val = array[docId >> SHIFT_SIZE][docId & MASK];
                if (val >= minVal && val <= maxVal)
                {
                    break;
                }
                docId++;
            }
            return docId;
        }

        public override sealed int FindBits(int bits, int docId, int maxId)
        {
            while (docId <= maxId && (array[docId >> SHIFT_SIZE][docId & MASK] & bits) == 0)
            {
                docId++;
            }
            return docId;
        }

        public override sealed void Fill(int val)
        {
            short shortVal = (short)val;
            foreach (short[] block in array)
            {
                Arrays.Fill(block, shortVal);
            }
        }

        public override void EnsureCapacity(int size)
        {
            int newNumrows = (size >> SHIFT_SIZE) + 1;
            if (newNumrows > array.Length)
            {
                short[][] newArray = new short[newNumrows][]; // grow
                System.Array.Copy(array, 0, newArray, 0, array.Length);
                for (int i = array.Length; i < newNumrows; ++i)
                {
                    newArray[i] = new short[BLOCK_SIZE];
                }
                array = newArray;
            }
            numrows = newNumrows;
        }

        public override int MaxValue()
        {
            return short.MaxValue;
        }
    }
}
