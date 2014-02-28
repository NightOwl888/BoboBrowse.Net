

namespace BoboBrowse.Net.Utils
{
    using System;
    using System.Collections.Generic;

    ///<summary>@author ymatsuda</summary>
    public class BigIntBuffer
    {
        private const int PAGESIZE = 1024;
        private const int MASK = 0x3FF;
        private const int SHIFT = 10;

        private readonly List<int[]> buffer;
        private int allocSize;
        private int mark;

        public BigIntBuffer()
        {
            buffer = new List<int[]>();
            allocSize = 0;
            mark = 0;
        }

        public virtual int Alloc(int size)
        {
            if (size > PAGESIZE)
                throw new System.ArgumentException("size too big");

            if ((mark + size) > allocSize)
            {
                int[] page = new int[PAGESIZE];
                buffer.Add(page);
                allocSize += PAGESIZE;
            }
            int ptr = mark;
            mark += size;

            return ptr;
        }

        public virtual void Reset()
        {
            mark = 0;
        }

        public virtual void Set(int ptr, int val)
        {
            int[] page = buffer[ptr >> SHIFT];
            page[ptr & MASK] = val;
        }

        public virtual int Get(int ptr)
        {
            int[] page = buffer[ptr >> SHIFT];
            return page[ptr & MASK];
        }
    }
}
