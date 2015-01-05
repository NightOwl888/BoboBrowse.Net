// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.Support;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// BigSegmentedArray that creates segments only when the corresponding index is
    /// being accessed.
    /// author jko
    /// </summary>
    [Serializable]
    public class LazyBigIntArray : BigSegmentedArray
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private int[][] _array;
        /* Remember that 2^SHIFT_SIZE = BLOCK_SIZE */
        private const int BLOCK_SIZE = 1024;
        private const int SHIFT_SIZE = 10;
        private const int MASK = BLOCK_SIZE - 1;

        private int _fillValue = 0;

        public LazyBigIntArray(int size)
            : base(size)
        {
            // initialize empty blocks
            _array = new int[_numrows][];
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#getBlockSize()
        /// </summary>
        /// <returns></returns>
        protected override int GetBlockSize()
        {
            return BLOCK_SIZE;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#getShiftSize()
        /// </summary>
        /// <returns></returns>
        protected override int GetShiftSize()
        {
            return SHIFT_SIZE;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#get(int)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override int Get(int id)
        {
            int i = id >> SHIFT_SIZE;
            if (_array[i] == null)
                return _fillValue; // return _fillValue to mimic int[] behavior
            else
                return _array[i][id & MASK];
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#add(int, int)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="val"></param>
        public override void Add(int id, int val)
        {
            int i = id >> SHIFT_SIZE;
            if (_array[i] == null)
            {
                _array[i] = new int[BLOCK_SIZE];
                if (_fillValue != 0)
                    Arrays.Fill(_array[i], _fillValue);
            }
            _array[i][id & MASK] = val;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#fill(int)
        /// </summary>
        /// <param name="val"></param>
        public override void Fill(int val)
        {
            foreach (int[] block in _array)
            {
                if (block == null) continue;
                Arrays.Fill(block, val);
            }

            _fillValue = val;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#ensureCapacity(int)
        /// </summary>
        /// <param name="size"></param>
        public override void EnsureCapacity(int size)
        {
            int newNumrows = (size >> SHIFT_SIZE) + 1;
            if (newNumrows > _array.Length)
            {
                int[][] newArray = new int[newNumrows][];           // grow
                System.Array.Copy(_array, 0, newArray, 0, _array.Length);
                // don't allocate new rows
                _array = newArray;
            }
            _numrows = newNumrows;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#maxValue()
        /// </summary>
        public override int MaxValue
        {
            get { return int.MaxValue; }
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#findValue(int, int, int)
        /// </summary>
        /// <param name="val"></param>
        /// <param name="id"></param>
        /// <param name="maxId"></param>
        /// <returns></returns>
        public override int FindValue(int val, int id, int maxId)
        {
            while (id <= maxId)
            {
                int i = id >> SHIFT_SIZE;
                if (_array[i] == null)
                {
                    if (val == _fillValue)
                        return id;
                    else
                        id = (i + 1) << SHIFT_SIZE; // jump to next segment
                }
                else
                {
                    if (_array[i][id & MASK] == val)
                        return id;
                    else
                        id++;
                }
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#findValues(org.apache.lucene.util.OpenBitSet, int, int)
        /// </summary>
        /// <param name="bitset"></param>
        /// <param name="id"></param>
        /// <param name="maxId"></param>
        /// <returns></returns>
        public override int FindValues(OpenBitSet bitset, int id, int maxId)
        {
            while (id <= maxId)
            {
                int i = id >> SHIFT_SIZE;
                if (_array[i] == null)
                {
                    if (bitset.FastGet(_fillValue))
                        return id;
                    else
                        id = (i + 1) << SHIFT_SIZE; // jump to next segment
                }
                else
                {
                    if (bitset.FastGet(_array[i][id & MASK]))
                        return id;
                    else
                        id++;
                }
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#findValues(org.apache.lucene.util.BitVector, int, int)
        /// </summary>
        /// <param name="bitset"></param>
        /// <param name="id"></param>
        /// <param name="maxId"></param>
        /// <returns></returns>
        public override int FindValues(BitVector bitset, int id, int maxId)
        {
            while (id <= maxId)
            {
                int i = id >> SHIFT_SIZE;
                if (_array[i] == null)
                {
                    if (bitset.Get(_fillValue))
                        return id;
                    else
                        id = (i + 1) << SHIFT_SIZE; // jump to next segment
                }
                else
                {
                    if (bitset.Get(_array[i][id & MASK]))
                        return id;
                    else
                        id++;
                }
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#findValueRange(int, int, int, int)
        /// </summary>
        /// <param name="minVal"></param>
        /// <param name="maxVal"></param>
        /// <param name="id"></param>
        /// <param name="maxId"></param>
        /// <returns></returns>
        public override int FindValueRange(int minVal, int maxVal, int id, int maxId)
        {
            while (id <= maxId)
            {
                int i = id >> SHIFT_SIZE;
                if (_array[i] == null)
                {
                    if (_fillValue >= minVal && _fillValue <= maxVal)
                        return id;
                    else
                        id = (i + 1) << SHIFT_SIZE; // jump to next segment
                }
                else
                {
                    int val = _array[i][id & MASK];
                    if (val >= minVal && val <= maxVal)
                        return id;
                    else
                        id++;
                }
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.util.BigSegmentedArray#findBits(int, int, int)
        /// </summary>
        /// <param name="bits"></param>
        /// <param name="id"></param>
        /// <param name="maxId"></param>
        /// <returns></returns>
        public override int FindBits(int bits, int id, int maxId)
        {
            while (id <= maxId)
            {
                int i = id >> SHIFT_SIZE;
                if (_array[i] == null)
                {
                    if ((_fillValue & bits) != 0)
                        return id;
                    else
                        id = (i + 1) << SHIFT_SIZE; // jump to next segment
                }
                else
                {
                    int val = _array[i][id & MASK];
                    if ((val & bits) != 0)
                        return id;
                    else
                        id++;
                }
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }
    }
}