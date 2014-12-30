// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.Support;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System;
    
    public class BigShortArray : BigSegmentedArray
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private short[][] _array;

        // Remember that 2^SHIFT_SIZE = BLOCK_SIZE 
        private const int BLOCK_SIZE = 2048;
        private const int SHIFT_SIZE = 11;
        private const int MASK = BLOCK_SIZE - 1;

        public BigShortArray(int size)
            : base(size)
        {
            _array = new short[_numrows][];
            for (int i = 0; i < _numrows; i++)
            {
                _array[i] = new short[BLOCK_SIZE];
            }
        }

        public override sealed void Add(int docId, int val)
        {
            _array[docId >> SHIFT_SIZE][docId & MASK] = (short)val;
        }

        public override sealed int Get(int docId)
        {
            return _array[docId >> SHIFT_SIZE][docId & MASK];
        }

        public override sealed int FindValue(int val, int docId, int maxId)
        {
            while (true)
            {
                if (_array[docId >> SHIFT_SIZE][docId & MASK] == val) return docId;
                if (docId++ >= maxId) break;
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        public override sealed int FindValues(OpenBitSet bitset, int docId, int maxId)
        {
            while (true)
            {
                if (bitset.FastGet(_array[docId >> SHIFT_SIZE][docId & MASK])) return docId;
                if (docId++ >= maxId) break;
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        public override sealed int FindValues(BitVector bitset, int docId, int maxId)
        {
            while (true)
            {
                if (bitset.Get(_array[docId >> SHIFT_SIZE][docId & MASK])) return docId;
                if (docId++ >= maxId) break;
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        public override sealed int FindValueRange(int minVal, int maxVal, int docId, int maxId)
        {
            while (true)
            {
                int val = _array[docId >> SHIFT_SIZE][docId & MASK];
                if (val >= minVal && val <= maxVal) return docId;
                if (docId++ >= maxId) break;
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        public override sealed int FindBits(int bits, int docId, int maxId)
        {
            while (true)
            {
                if ((_array[docId >> SHIFT_SIZE][docId & MASK] & bits) != 0) return docId;
                if (docId++ >= maxId) break;
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        public override sealed void Fill(int val)
        {
            short shortVal = (short)val;
            foreach (short[] block in _array)
            {
                Arrays.Fill(block, shortVal);
            }
        }

        public override void EnsureCapacity(int size)
        {
            int newNumrows = (size >> SHIFT_SIZE) + 1;
            if (newNumrows > _array.Length)
            {
                short[][] newArray = new short[newNumrows][]; // grow
                System.Array.Copy(_array, 0, newArray, 0, _array.Length);
                for (int i = _array.Length; i < newNumrows; ++i)
                {
                    newArray[i] = new short[BLOCK_SIZE];
                }
                _array = newArray;
            }
            _numrows = newNumrows;
        }

        protected override sealed int GetBlockSize()
        {
            return BLOCK_SIZE;
        }

        protected override sealed int GetShiftSize()
        {
            return SHIFT_SIZE;
        }

        public override int MaxValue
        {
            get { return short.MaxValue; }
        }
    }
}
