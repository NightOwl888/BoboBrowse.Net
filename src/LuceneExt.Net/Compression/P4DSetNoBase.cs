
namespace LuceneExt
{
    using System;
    using Lucene.Net.Util;

    ///
    /// <summary> * Implementation of the p4delta algorithm for sorted integer arrays based on
    /// * 
    /// * 1. Original Algorithm from
    /// * http://homepages.cwi.nl/~heman/downloads/msthesis.pdf 2. Optimization and
    /// * variation from http://www2008.org/papers/pdf/p387-zhangA.pdf
    /// * 
    /// * This class is a wrapper around a CompressedSegment based on Lucene OpenBitSet </summary>
    /// 
    [Serializable]
    public class P4DSetNoBase : ICompressedSortedIntegerSegment
    {
        private const int INVALID = -1;

        // Maximum bits that can be used = 32

        // Byte Mask
        private const int BYTE_MASK = 8;

        // 32 bits for retaining base value
        private const int BASE_MASK = 32;

        // Header size
        private const int HEADER_MASK = BYTE_MASK;

        // Parameters for the compressed set
        private int b = INVALID;

        private int @base = INVALID;

        private int batchSize = INVALID;

        private int exceptionCount = INVALID;

        private int exceptionOffset = INVALID;

        //private int[] op = null;

        internal interface Processor // FIMXE : Serializable
        {
            int Process(int retval, int exceptionOffset, long[] compressedSet);
        }

        private class P4DSetNoBaseProcessor1 : Processor
        {
            public int Process(int retVal, int exceptionOffset, long[] compressedSet)
            {
                return retVal;
            }
        }

        private class P4DSetNoBaseProcessor2 : Processor
        {
            public int Process(int retVal, int exceptionOffset, long[] compressedSet)
            {
                return GetBitSlice(compressedSet, exceptionOffset + retVal * BASE_MASK, BASE_MASK);
            }
        }

        private static readonly Processor[] valueproc = { new P4DSetNoBaseProcessor1(), new P4DSetNoBaseProcessor2() };

        // Get the actual value
        public virtual void SetParam(int @base, int b, int batchSize, int exceptionCount)
        {
            this.@base = @base;
            this.b = b;
            this.batchSize = batchSize;

            this.exceptionCount = exceptionCount;
            this.exceptionOffset = HEADER_MASK + this.b * this.batchSize;
        }

        public virtual void UpdateParams(OpenBitSet @set)
        {
            b = GetBitSlice(@set, 0, BYTE_MASK);

            exceptionOffset = HEADER_MASK + b * batchSize;
        }

        public virtual void UpdateParams(long[] @set)
        {
            b = GetBitSlice(@set, 0, BYTE_MASK);
            exceptionOffset = HEADER_MASK + b * batchSize;
        }

        ///<summary>Alternate implementation for compress
        ///   *  </summary>
        ///   * <param name="input"> </param>
        ///   * <returns> compressed bitset </returns>
        ///   * <exception cref="ArgumentException"> </exception>
        public virtual OpenBitSet Compress(int[] input)
        {
            if (@base == INVALID || b == INVALID)
            {
                throw new ArgumentException(" Codec not initialized correctly ");
            }

            int BATCH_MAX = 1 << (b - 1);
            // int validCount = (_batchSize - _exceptionCount)*_b +SIZE_MASK+BASE_MASK;

            // Compression mumbo jumbo
            // Set Size -b+base+compressedSet+exception*BASE_MASK
            OpenBitSet compressedSet = new OpenBitSet((batchSize) * b + HEADER_MASK + exceptionCount * (BASE_MASK));
            // System.out.println("Compressed Set Size : " + compressedSet.capacity());

            // Load the b
            CopyBits(compressedSet, b, 0, BYTE_MASK);

            // copy the base value to BASE_MASK offset
            // copyBits(compressedSet, _base, BYTE_MASK, BASE_MASK);

            // Offset is the offset of the next location to place the value
            int offset = HEADER_MASK;
            int exceptionOffset = this.exceptionOffset;
            int exceptionIndex = 0;

            // 1. Walk the list
            // TODO : Optimize this process.
            for (int i = 0; i < batchSize; i++)
            {
                // else copy in the end
                if (input[i] < BATCH_MAX)
                {
                    CopyBits(compressedSet, input[i] << 1, offset, b);
                }
                else
                {
                    // Copy the value to the exception location
                    // Add a bit marker to place
                    CopyBits(compressedSet, ((exceptionIndex << 1) | 0x1), offset, b);
                    // System.out.println("Adding Exception
                    // Marker:"+(BATCH_MAX|(exceptionIndex-1)) + " at offset:"+offset);

                    // Copy the patch value to patch offset location
                    CopyBits(compressedSet, input[i], exceptionOffset, BASE_MASK);

                    // reset exceptionDelta
                    exceptionOffset += BASE_MASK;
                    exceptionIndex++;
                }

                offset += b;
            }

            return compressedSet;
        }

        ///<summary>Alternate implementation for compress
        ///   *  </summary>
        ///   * <param name="input"> </param>
        ///   * <returns> comprssed set in long array form </returns>
        ///   * <exception cref="ArgumentException"> </exception>
        public virtual long[] CompressAlt(int[] input)
        {
            if (@base == INVALID || b == INVALID)
            {
                throw new ArgumentException(" Codec not initialized correctly ");
            }

            //    for(int i=0;i<_batchSize;i++)
            //      System.out.print(input[i]+":");
            //    System.out.println("\nB:"+_b)

            int BATCH_MAX = 1 << (b - 1);
            // int validCount = (_batchSize - _exceptionCount)*_b +SIZE_MASK+BASE_MASK;

            // Compression mumbo jumbo // 劐溴怵嚯 觐祆屙?

            // Set Size _b+base+compressedSet+exception*BASE_MASK bits
            long[] compressedSet = new long[((((batchSize) * b + HEADER_MASK + exceptionCount * (BASE_MASK))) >> 6) + 1];


            //new long[((_batchSize) * _b  + HEADER_MASK + _exceptionCount * (BASE_MASK))>>6 + 1];
            // System.out.println("Compressed Set Size : " + compressedSet.capacity());

            // Load the b
            CopyBits(compressedSet, b, 0, BYTE_MASK);

            // copy the base value to BASE_MASK offset
            // copyBits(compressedSet, _base, BYTE_MASK, BASE_MASK);

            // Offset is the offset of the next location to place the value
            int offset = HEADER_MASK;
            int exceptionOffset = this.exceptionOffset;
            int exceptionIndex = 0;

            // 1. Walk the list
            // TODO : Optimize this process.
            for (int i = 0; i < batchSize; i++)
            {
                // else copy in the end
                if (input[i] < BATCH_MAX)
                {
                    CopyBits(compressedSet, input[i] << 1, offset, b);
                }
                else
                {
                    // Copy the value to the exception location
                    // Add a bit marker to place
                    CopyBits(compressedSet, ((exceptionIndex << 1) | 0x1), offset, b);
                    // System.out.println("Adding Exception
                    // Marker:"+(BATCH_MAX|(exceptionIndex-1)) + " at offset:"+offset);

                    // Copy the patch value to patch offset location
                    CopyBits(compressedSet, input[i], exceptionOffset, BASE_MASK);

                    // reset exceptionDelta
                    exceptionOffset += BASE_MASK;
                    exceptionIndex++;
                }

                offset += b;
            }

            return compressedSet;
        }

        private static void CopyBits(OpenBitSet compressedSet, int val, int offset, int length)
        {
            long[] bits = compressedSet.Bits;
            uint index = (uint)offset >> 6;
            int skip = offset & 0x3f;
            val &= (int)(0xffffffff >> (32 - length));
            bits[index] |= (((long)val) << skip);
            if (64 - skip < length)
            {
                bits[index + 1] |= ((long)val >> (64 - skip));
            }
        }

        private static void CopyBits(long[] bits, int val, int offset, int length)
        {
            uint index = (uint)offset >> 6;
            int skip = offset & 0x3f;
            val &= (int)(0xffffffff >> (32 - length));
            bits[index] |= (((long)val) << skip);
            if (64 - skip < length)
            {
                bits[index + 1] |= ((long)val >> (64 - skip));
            }

        }

        private static int GetBitSlice(OpenBitSet compressedSet, int offset, int length)
        {
            long[] bits = compressedSet.Bits;
            int index = (int)(uint)offset >> 6;
            int skip = offset & 0x3f;
            int val = (int)(bits[index] >> skip);
            if (64 - skip < length)
            {
                val |= (int)bits[index + 1] << (64 - skip);
            }
            return val & (int)(0xffffffff >> (32 - length));
        }

        private static int GetBitSlice(long[] bits, int offset, int length)
        {
            uint index = (uint)offset >> 6;
            int skip = offset & 0x3f;
            int val = (int)(bits[index] >> skip);
            if (64 - skip < length)
            {
                val |= (int)bits[index + 1] << (64 - skip);
            }
            return val & (int)(0xffffffff >> (32 - length));
        }

        // Method to allow iteration in decompressed form
        public int Get(long[] compressedSet, int index)
        {
            int retVal = GetBitSlice(compressedSet, (index * b + HEADER_MASK), b);

            // fake the function pointer logic
            return valueproc[retVal & 0x1].Process((int)(uint)retVal >> 1, exceptionOffset, compressedSet);
        }

        //   Method to allow iteration in decompressed form
        //  public int get(OpenBitSet compressedSet, int index) {
        //    final int retVal = getBitSlice(compressedSet, (index * _b + HEADER_MASK), _b);
        //       
        //    // fake the function pointer logic
        //    return valueproc[retVal & 0x1].process(retVal >> 1, _exceptionOffset, compressedSet);
        //    
        //    
        //   /*This is an exception
        //   if (compressedSet.getBit((index + 1) * _b + HEADER_MASK - 1) == 1) {
        //
        //      int exOffset = _exceptionOffset + retVal * BASE_MASK;
        //      retVal = 0;
        //      // Get the actual value
        //      for (int j = 0; j < BASE_MASK; j++)
        //        retVal |= (compressedSet.getBit(exOffset + j) << j);
        //      return retVal;
        //    } 
        //    else
        //      return retVal;
        //  }

        public virtual int[] Decompress(OpenBitSet compressedSet)
        {
            int[] op = new int[batchSize];
            // reuse o/p
            op[0] = @base;

            // Offset of the exception list
            int exceptionOffset = HEADER_MASK + b * batchSize;

            // explode and patch
            for (int i = 1; i < batchSize; i++)
            {
                int val = GetBitSlice(compressedSet, i * b + HEADER_MASK, b);

                if ((val & 0x1) != 0)
                {
                    // This is an exception
                    op[i] = GetBitSlice(compressedSet, exceptionOffset, BASE_MASK);
                    exceptionOffset += BASE_MASK;
                }
                else
                {
                    op[i] = (int)((uint)val >> 1);
                }
                op[i] += op[i - 1];
            }
            return op;
        }

        public virtual int[] Decompress(long[] compressedSet)
        {
            int[] op = new int[batchSize];
            // reuse o/p
            op[0] = @base;

            // Offset of the exception list
            int exceptionOffset = HEADER_MASK + b * batchSize;

            // explode and patch
            for (int i = 1; i < batchSize; i++)
            {
                int val = GetBitSlice(compressedSet, i * b + HEADER_MASK, b);

                if ((val & 0x1) != 0)
                {
                    // This is an exception
                    op[i] = GetBitSlice(compressedSet, exceptionOffset, BASE_MASK);
                    exceptionOffset += BASE_MASK;
                }
                else
                {
                    op[i] = (int)(uint)val >> 1;
                }
                op[i] += op[i - 1];
            }
            return op;
        }

        public virtual string PrintParams()
        {
            return "b val:" + b + " exceptionOffset:" + exceptionOffset;
        }
    }
}
