//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
//*
//* Copyright (C) 2005-2015  John Wang
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//*   http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

﻿// Kamikaze version compatibility level: 3.0.6
namespace LuceneExt
{
    using Lucene.Net.Util;
    using LuceneExt.BitSet;
    using System;

    /// <summary> 
    /// Implementation of the p4delta algorithm for sorted integer arrays based on
    /// 
    /// <list type="number">
    ///     <item>
    ///         <description>Original Algorithm from http://homepages.cwi.nl/~heman/downloads/msthesis.pdf</description>
    ///     </item>
    ///     <item>
    ///         <description>Optimization and variation from http://www2008.org/papers/pdf/p387-zhangA.pdf</description>
    ///     </item>
    /// </list>
    /// 
    /// This class is a wrapper around a CompressedSegment based on Lucene OpenBitSet 
    /// </summary>
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
        private int _b = INVALID;

        private int _base = INVALID;

        private int _batchSize = INVALID;

        private int _exceptionCount = INVALID;

        private int _exceptionOffset = INVALID;

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
            this._base = @base;
            this._b = b;
            this._batchSize = batchSize;

            this._exceptionCount = exceptionCount;
            this._exceptionOffset = HEADER_MASK + _b * _batchSize;
        }

        public virtual void UpdateParams(OpenBitSet @set)
        {
            _b = GetBitSlice(@set, 0, BYTE_MASK);

            _exceptionOffset = HEADER_MASK + _b * _batchSize;
        }

        public virtual void UpdateParams(long[] @set)
        {
            _b = GetBitSlice(@set, 0, BYTE_MASK);
            _exceptionOffset = HEADER_MASK + _b * _batchSize;
        }

        ///<summary>Alternate implementation for compress
        ///   *  </summary>
        ///   * <param name="input"> </param>
        ///   * <returns> compressed bitset </returns>
        ///   * <exception cref="ArgumentException"> </exception>
        public virtual OpenBitSet Compress(int[] input)
        {
            if (_base == INVALID || _b == INVALID)
            {
                throw new ArgumentException(" Codec not initialized correctly ");
            }

            int BATCH_MAX = 1 << (_b - 1);
            // int validCount = (_batchSize - _exceptionCount)*_b +SIZE_MASK+BASE_MASK;

            // Compression mumbo jumbo
            // Set Size -b+base+compressedSet+exception*BASE_MASK
            MyOpenBitSet compressedSet = new MyOpenBitSet((_batchSize) * _b 
                + HEADER_MASK + _exceptionCount * (BASE_MASK));

            // System.out.println("Compressed Set Size : " + compressedSet.capacity());



            // Load the b
            CopyBits(compressedSet, _b, 0, BYTE_MASK);

            // copy the base value to BASE_MASK offset
            // copyBits(compressedSet, _base, BYTE_MASK, BASE_MASK);

            // Offset is the offset of the next location to place the value
            int offset = HEADER_MASK;
            int exceptionOffset = _exceptionOffset;
            int exceptionIndex = 0;

            // 1. Walk the list
            // TODO : Optimize this process.
            for (int i = 0; i < _batchSize; i++)
            {
                // else copy in the end
                if (input[i] < BATCH_MAX)
                {
                    CopyBits(compressedSet, input[i] << 1, offset, _b);
                }
                else
                {
                    // Copy the value to the exception location
                    // Add a bit marker to place
                    CopyBits(compressedSet, ((exceptionIndex << 1) | 0x1), offset, _b);
                    // System.out.println("Adding Exception
                    // Marker:"+(BATCH_MAX|(exceptionIndex-1)) + " at offset:"+offset);

                    // Copy the patch value to patch offset location
                    CopyBits(compressedSet, input[i], exceptionOffset, BASE_MASK);

                    // reset exceptionDelta
                    exceptionOffset += BASE_MASK;
                    exceptionIndex++;
                }

                offset += _b;
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
            if (_base == INVALID || _b == INVALID)
                throw new ArgumentException(" Codec not initialized correctly ");



            //    for(int i=0;i<_batchSize;i++)
            //      System.out.print(input[i]+":");
            //    System.out.println("\nB:"+_b)



            int BATCH_MAX = 1 << (_b - 1);
            // int validCount = (_batchSize - _exceptionCount)*_b +SIZE_MASK+BASE_MASK;


            // Compression mumbo jumbo // 劐溴怵嚯 觐祆屙?

            // Set Size _b+base+compressedSet+exception*BASE_MASK bits
            long[] compressedSet = new long[((((_batchSize) * _b + HEADER_MASK + _exceptionCount * (BASE_MASK))) >> 6) + 1];


            //new long[((_batchSize) * _b  + HEADER_MASK + _exceptionCount * (BASE_MASK))>>6 + 1];
            // System.out.println("Compressed Set Size : " + compressedSet.capacity());

            // Load the b
            CopyBits(compressedSet, _b, 0, BYTE_MASK);

            // copy the base value to BASE_MASK offset
            // copyBits(compressedSet, _base, BYTE_MASK, BASE_MASK);

            // Offset is the offset of the next location to place the value
            int offset = HEADER_MASK;
            int exceptionOffset = _exceptionOffset;
            int exceptionIndex = 0;

            // 1. Walk the list
            // TODO : Optimize this process.
            for (int i = 0; i < _batchSize; i++)
            {
                // else copy in the end
                if (input[i] < BATCH_MAX)
                {
                    CopyBits(compressedSet, input[i] << 1, offset, _b);
                }
                else
                {
                    // Copy the value to the exception location
                    // Add a bit marker to place
                    CopyBits(compressedSet, ((exceptionIndex << 1) | 0x1), offset, _b);
                    // System.out.println("Adding Exception
                    // Marker:"+(BATCH_MAX|(exceptionIndex-1)) + " at offset:"+offset);

                    // Copy the patch value to patch offset location
                    CopyBits(compressedSet, input[i], exceptionOffset, BASE_MASK);

                    // reset exceptionDelta
                    exceptionOffset += BASE_MASK;
                    exceptionIndex++;
                }

                offset += _b;
            }

            return compressedSet;
        }

        private static void CopyBits(MyOpenBitSet compressedSet, int val, int offset, int length)
        {
            long[] bits = compressedSet.Bits;
            int index = (int)((uint)offset) >> 6;
            int skip = offset & 0x3f;
            val &= (int)(((uint)0xffffffff) >> (32 - length));
            bits[index] |= (((long)val) << skip);
            if (64 - skip < length)
            {
                bits[index + 1] |= (long)(((ulong)val) >> (64 - skip));
            }
        }

        private static void CopyBits(long[] bits, int val, int offset, int length)
        {
            int index = (int)(((uint)offset) >> 6);
            int skip = offset & 0x3f;
            val &= (int)(((uint)0xffffffff) >> (32 - length));
            bits[index] |= (((long)val) << skip);
            if (64 - skip < length)
            {
                bits[index + 1] |= (long)(((ulong)val) >> (64 - skip));
            }

        }

        private static int GetBitSlice(OpenBitSet compressedSet, int offset, int length)
        {
            long[] bits = compressedSet.Bits;
            int index = (int)(((uint)offset) >> 6);
            int skip = offset & 0x3f;
            int val = (int)(((uint)bits[index]) >> skip);
            if (64 - skip < length)
            {
                val |= (int)bits[index + 1] << (64 - skip);
            }
            return val & (int)(((uint)0xffffffff) >> (32 - length));
        }

        private static int GetBitSlice(long[] bits, int offset, int length)
        {
            int index = (int)(((uint)offset) >> 6);
            int skip = offset & 0x3f;
            int val = (int)(((uint)bits[index]) >> skip);
            if (64 - skip < length)
            {
                val |= (int)bits[index + 1] << (64 - skip);
            }
            return val & (int)(((uint)0xffffffff) >> (32 - length));
        }

        // Method to allow iteration in decompressed form
        public int Get(long[] compressedSet, int index)
        {
            int retVal = GetBitSlice(compressedSet, (index * _b + HEADER_MASK), _b);

            // fake the function pointer logic
            return valueproc[retVal & 0x1].Process((int)(((uint)retVal) >> 1), _exceptionOffset, compressedSet);
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
            int[] op = new int[_batchSize];
            // reuse o/p
            op[0] = _base;

            // Offset of the exception list
            int exceptionOffset = HEADER_MASK + _b * _batchSize;

            // explode and patch
            for (int i = 1; i < _batchSize; i++)
            {
                int val = GetBitSlice(compressedSet, i * _b + HEADER_MASK, _b);

                if ((val & 0x1) != 0)
                {
                    // This is an exception
                    op[i] = GetBitSlice(compressedSet, exceptionOffset, BASE_MASK);
                    exceptionOffset += BASE_MASK;
                }
                else
                {
                    op[i] = (int)(((uint)val) >> 1);
                }
                op[i] += op[i - 1];
            }
            return op;
        }

        public virtual int[] Decompress(long[] compressedSet)
        {
            int[] op = new int[_batchSize];
            // reuse o/p
            op[0] = _base;

            // Offset of the exception list
            int exceptionOffset = HEADER_MASK + _b * _batchSize;

            // explode and patch
            for (int i = 1; i < _batchSize; i++)
            {
                int val = GetBitSlice(compressedSet, i * _b + HEADER_MASK, _b);

                if ((val & 0x1) != 0)
                {
                    // This is an exception
                    op[i] = GetBitSlice(compressedSet, exceptionOffset, BASE_MASK);
                    exceptionOffset += BASE_MASK;
                }
                else
                {
                    op[i] = (int)(((uint)val) >> 1);
                }
                op[i] += op[i - 1];
            }
            return op;
        }

        ///**
        //* Method not supported
        //* 
        //*/
        //public virtual int[] Decompress(BitSet compressedSet) 
        //{
        //    return null;
        //}

        public virtual string PrintParams()
        {
            return "b val:" + _b + " exceptionOffset:" + _exceptionOffset;
        }
    }
}
