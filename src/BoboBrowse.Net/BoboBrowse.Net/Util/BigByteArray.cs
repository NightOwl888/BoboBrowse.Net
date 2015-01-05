//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.Support;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System;

    [Serializable]
    public class BigByteArray : BigSegmentedArray
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private sbyte[][] _array;

        // Remember that 2^SHIFT_SIZE = BLOCK_SIZE 
        private const int BLOCK_SIZE = 4096;
        private const int SHIFT_SIZE = 12;
        private const int MASK = BLOCK_SIZE - 1;

        public BigByteArray(int size)
            : base(size)
        {
            _array = new sbyte[_numrows][];
            for (int i = 0; i < _numrows; i++)
            {
                _array[i] = new sbyte[BLOCK_SIZE];
            }
        }

        public override sealed void Add(int docId, int val)
        {
            _array[docId >> SHIFT_SIZE][docId & MASK] = (sbyte)val;
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
                if (docId++ > maxId) break;
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
	        while(true)
	        {
	            if(bitset.Get(_array[docId >> SHIFT_SIZE][docId & MASK])) return docId;
	            if(docId++ >= maxId) break;
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
            sbyte byteVal = (sbyte)val;
            foreach (sbyte[] block in _array)
            {
                Arrays.Fill(block, byteVal);
            }
        }

        public override void EnsureCapacity(int size)
        {
            int newNumrows = (size >> SHIFT_SIZE) + 1;
            if (newNumrows > _array.Length)
            {
                sbyte[][] newArray = new sbyte[newNumrows][]; // grow
                System.Array.Copy(_array, 0, newArray, 0, _array.Length);
                for (int i = _array.Length; i < newNumrows; ++i)
                {
                    newArray[i] = new sbyte[BLOCK_SIZE];
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
            get { return sbyte.MaxValue; }
        }
    }
}
