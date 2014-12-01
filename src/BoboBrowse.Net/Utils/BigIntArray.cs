
namespace BoboBrowse.Net.Utils
{
    using System;
    using Lucene.Net.Util;
    using Lucene.Net.Search;

    /// <summary> * 
    /// * @author femekci
    /// * This class is written for a special purpose. No check is done in insertion and getting a value
    /// * for performance reasons. Be careful if you are going to use this class  </summary>
    [Serializable]
    public sealed class BigIntArray : BigSegmentedArray
    {
        private int[][] array;

        // Remember that 2^SHIFT_SIZE = BLOCK_SIZE 
        private const int BLOCK_SIZE = 1024;
        private const int SHIFT_SIZE = 10;
        private const int MASK = BLOCK_SIZE - 1;

        public BigIntArray(int size)
            : base(size, BLOCK_SIZE, SHIFT_SIZE)
        {
            array = new int[numrows][];
            for (int i = 0; i < numrows; i++)
            {
                array[i] = new int[BLOCK_SIZE];
            }
        }

        public override void Add(int docId, int val)
        {
            array[docId >> SHIFT_SIZE][docId & MASK] = val;
        }

        public override int Get(int docId)
        {
            return array[docId >> SHIFT_SIZE][docId & MASK];
        }

        public override int FindValue(int val, int docId, int maxId)
        {
            while (docId <= maxId && array[docId >> SHIFT_SIZE][docId & MASK] != val)
            {
                docId++;
            }

            return docId > maxId ? DocIdSetIterator.NO_MORE_DOCS : docId;
        }

        public override int FindValues(OpenBitSet bitset, int docId, int maxId)
        {
            while (docId <= maxId && !bitset.FastGet(array[docId >> SHIFT_SIZE][docId & MASK]))
            {
                docId++;
            }
            return docId > maxId ? DocIdSetIterator.NO_MORE_DOCS : docId;
        }

        public override int FindValueRange(int minVal, int maxVal, int docId, int maxId)
        {
            while (docId <= maxId)
            {
                int val = array[docId >> SHIFT_SIZE][docId & MASK];
                if (val >= minVal && val <= maxVal)
                    break;
                docId++;
            }
            return docId > maxId ? DocIdSetIterator.NO_MORE_DOCS : docId;
        }

        public override int FindBits(int bits, int docId, int maxId)
        {
            while (docId <= maxId && (array[docId >> SHIFT_SIZE][docId & MASK] & bits) == 0)
            {
                docId++;
            }
            return docId > maxId ? DocIdSetIterator.NO_MORE_DOCS : docId;
        }

        public override void Fill(int val)
        {
            foreach (int[] block in array)
            {
                Arrays.Fill(block, val);
            }
        }

        public override void EnsureCapacity(int size)
        {
            int newNumrows = (size >> SHIFT_SIZE) + 1;
            if (newNumrows > array.Length)
            {
                int[][] newArray = new int[newNumrows][]; // grow
                System.Array.Copy(array, 0, newArray, 0, array.Length);
                for (int i = array.Length; i < newNumrows; ++i)
                {
                    newArray[i] = new int[BLOCK_SIZE];
                }
                array = newArray;
            }
            numrows = newNumrows;
        }

        public override int MaxValue()
        {
            return int.MaxValue;
        }
    }
}
