
namespace BoboBrowse.Net.Util
{
    using System;

    public class BigFloatArray
    {
        private float[][] aarray;
        private int numrows;

        // Remember that 2^SHIFT_SIZE = BLOCK_SIZE 
        private const int BLOCK_SIZE = 1024;
        private const int SHIFT_SIZE = 10;
        private const int MASK = BLOCK_SIZE - 1;

        public BigFloatArray(int size)
        {
            numrows = size >> SHIFT_SIZE;
            aarray = new float[numrows + 1][];
            for (int i = 0; i <= numrows; i++)
            {
                aarray[i] = new float[BLOCK_SIZE];
            }
        }

        public virtual void Add(int docId, float val)
        {
            aarray[docId >> SHIFT_SIZE][docId & MASK] = val;
        }

        public virtual float Get(int docId)
        {
            return aarray[docId >> SHIFT_SIZE][docId & MASK];
        }

        public virtual int Capacity()
        {
            return numrows * BLOCK_SIZE;
        }

        public virtual void EnsureCapacity(int size)
        {
            int newNumrows = (size >> SHIFT_SIZE) + 1;
            if (newNumrows > aarray.Length)
            {
                float[][] newArray = new float[newNumrows][]; // grow
                System.Array.Copy(aarray, 0, newArray, 0, aarray.Length);
                for (int i = aarray.Length; i < newNumrows; ++i)
                {
                    newArray[i] = new float[BLOCK_SIZE];
                }
                aarray = newArray;
            }
            numrows = newNumrows;
        }
    }
}
