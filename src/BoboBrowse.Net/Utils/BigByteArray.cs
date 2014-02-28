//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

namespace BoboBrowse.Net.Utils
{
    using System;    
    using Lucene.Net.Util;

    [Serializable]
    public class BigByteArray : BigSegmentedArray
    {
        private sbyte[][] array;

        // Remember that 2^SHIFT_SIZE = BLOCK_SIZE 
        private const int BLOCK_SIZE = 4096;
        private const int SHIFT_SIZE = 12;
        private const int MASK = BLOCK_SIZE - 1;

        public BigByteArray(int size)
            : base(size, BLOCK_SIZE, SHIFT_SIZE)
        {
            array = new sbyte[numrows][];
            for (int i = 0; i < numrows; i++)
            {
                array[i] = new sbyte[BLOCK_SIZE];
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
                    break;
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
            sbyte byteVal = (sbyte)val;
            foreach (sbyte[] block in array)
            {
                Arrays.Fill(block, byteVal);
            }
        }

        public override void EnsureCapacity(int size)
        {
            int newNumrows = (size >> SHIFT_SIZE) + 1;
            if (newNumrows > array.Length)
            {
                sbyte[][] newArray = new sbyte[newNumrows][]; // grow
                System.Array.Copy(array, 0, newArray, 0, array.Length);
                for (int i = array.Length; i < newNumrows; ++i)
                {
                    newArray[i] = new sbyte[BLOCK_SIZE];
                }
                array = newArray;
            }
            numrows = newNumrows;
        }

        public override int MaxValue()
        {
            return sbyte.MaxValue;
        }
    }
}
