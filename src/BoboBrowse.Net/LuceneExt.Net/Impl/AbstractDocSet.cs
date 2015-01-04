// * Bobo Browse Engine - High performance faceted/parametric search implementation 
// * that handles various types of semi-structured data.  Written in Java.
// * 
// * Copyright (C) 2005-2006  John Wang
// *
// * This library is free software; you can redistribute it and/or
// * modify it under the terms of the GNU Lesser General Public
// * License as published by the Free Software Foundation; either
// * version 2.1 of the License, or (at your option) any later version.
// *
// * This library is distributed in the hope that it will be useful,
// * but WITHOUT ANY WARRANTY; without even the implied warranty of
// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// * Lesser General Public License for more details.
// *
// * You should have received a copy of the GNU Lesser General Public
// * License along with this library; if not, write to the Free Software
// * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// * 
// * To contact the project administrators for the bobo-browse project, 
// * please go to https://sourceforge.net/projects/bobo-browse/, or 
// * send mail to owner@browseengine.com.  

namespace LuceneExt.Impl
{
    using Lucene.Net.Util;
    using LuceneExt.Util;
    using System;

    [Serializable]
    public abstract class AbstractDocSet : DocSet
    {
        private static double logBase2 = Math.Log(2);

        public const int DEFAULT_BATCH_SIZE = 256;

        ///<summary>Default batch size for compression blobs</summary>
        public int BATCH_SIZE = DEFAULT_BATCH_SIZE;

        ///<summary>Default batch size for compression blobs</summary>
        protected internal int BATCH_OVER = 12;

        ///<summary>Current base size</summary>
        protected internal int current_base;

        ///<summary>Last added value</summary>
        protected internal int lastAdded = 0;

        /////<summary>List of Data blobs</summary>
        //protected OpenBitSetArray blob = null;

        ///<summary>List of Data blobs</summary>
        protected internal LongSegmentArray blob = null;

        ///<summary>Pointer to the current data block.</summary>
        protected internal int[] current = null;

        ///<summary>Size of the current array</summary>
        protected internal int current_size = 0;

        ///<summary>Current Max bit count</summary>
        protected internal int current_ex_count = 0;

        ///<summary>Current Bit Size</summary>
        protected internal int current_b = 1;

        ///<summary>B Value accumulator</summary>
        protected internal int[] bVal = null;

        ///<summary>compressed bit size </summary>
        ///<summary>Compressed Bits </summary>
        protected internal long compressedBits;

        /// <summary>Internal compression Method  </summary>
        /// <returns> compressed object </returns>
        protected internal abstract object Compress();
        // protected abstract Object compressAlt();

        protected internal AbstractDocSet()
        {
            this.blob = new LongSegmentArray();

        }

        ///<summary>Internal Decompression Method</summary>
        private int[] decompress(OpenBitSet packedSet)
        {
            Console.Error.WriteLine("Method not implemented");
            return null;
        }

        /// <summary>Internal Decompression Method</summary>
        /// <returns> decompressed in the form of integer array </returns>
        protected internal virtual int[] decompress(DocIdBitSet packedSet)
        {
            Console.Error.WriteLine("Method not implemented");
            return null;
        }

        private void initSet()
        {
            this.current = new int[BATCH_SIZE];
            current_size = 0;
            current_b = 32;
            // blob = new ArrayList<OpenBitSet>();
            bVal = new int[33];
        }

        /// <summary>Number of compressed units plus the last block </summary>
        /// <returns> docset size </returns>
        public override int Size()
        {
            return blob.Size() * BATCH_SIZE + current_size;
        }



        ///<summary>Add document to this set</summary>
        public override void AddDoc(int docid)
        {
            if (Size() == 0)
            {
                initSet();
                current[current_size++] = docid;
                current_base = docid;
                lastAdded = current_base;
            }

            else if (current_size == BATCH_SIZE)
            {
                current_b = 32;
                current_ex_count = 0;

                int totalBitSize = current_b * BATCH_SIZE;
                int exceptionCount = 0;

                // formulate b value. Minimum bits used is minB.
                for (int b = 32; b > 0; b--)
                {
                    exceptionCount += bVal[b];

                    // break if exception count is too large for this b
                    if ((getNumBits(exceptionCount) + 1) >= b)
                        break;

                    if ((exceptionCount * 32 + b * BATCH_SIZE) < totalBitSize)
                    {
                        // this is the best parameter so far
                        current_b = b;
                        current_ex_count = exceptionCount;
                    }
                }

                long[] myop = (long[])Compress();
                compressedBits += myop.Length << 6;
                blob.Add(myop);

                // roll the batch
                current_size = 1;
                current_base = docid;
                lastAdded = current_base;
                current[0] = current_base;
                current_ex_count = 0;

                bVal = new int[33];

            } // end batch boundary

            else
            {
                try
                {
                    int delta = docid - lastAdded;
                    current[current_size] = delta;
                    lastAdded = docid;
                    if (delta != 0)
                        bVal[getNumBits(delta)]++;

                    current_size++;
                }
                catch
                {
                    Console.Error.WriteLine("Error inserting DOC:" + docid);

                }

            } // end append to end of array

        }

        ///<summary>Add document to this set
        ///   * 
        ///   
        ///  public void AddDoc(int docid) {
        ///    if (size() == 0) {
        ///      initSet();
        ///      current[current_size++] = docid;
        ///      current_base = docid;
        ///      lastAdded = current_base;
        ///    }
        ///
        ///    else if (current_size == BATCH_SIZE) {
        ///
        ///      int exceptionCount = 0;
        ///
        ///      // formulate b value. Minimum bits used is 5.
        ///      for (int k = 31; k > 3; k--) {
        ///        // System.out.print(bVal[k]+":");
        ///        exceptionCount += bVal[k];
        ///        if (exceptionCount >= BATCH_OVER) {
        ///          current_b = k;
        ///          exceptionCount -= bVal[k];
        ///          break;
        ///        }
        ///      }
        ///
        ///      // Compensate for extra bit
        ///      current_b += 1;
        ///
        ///      // set current_exception_count
        ///      current_ex_count = exceptionCount;
        ///
        ///      OpenBitSet myop = (OpenBitSet) compress();
        ///      compressedBits+=myop.capacity();
        ///      blob.add(myop);
        ///
        ///      // roll the batch
        ///      current_size = 1;
        ///      current_base = docid;
        ///      lastAdded = current_base;
        ///      current[0] = current_base;
        ///      current_ex_count = 0;
        ///
        ///      bVal = new int[33];
        ///
        ///    }// end batch boundary
        ///
        ///    else {
        ///      try {
        ///
        ///        current[current_size] = docid - lastAdded;
        ///        lastAdded = docid;
        ///        if (current[current_size] != 0)
        ///          bVal[(int) (Math.log(current[current_size]) / logBase2) + 1]++;
        ///
        ///        current_size++;
        ///      } catch (ArrayIndexOutOfBoundsException w) {
        ///        Console.Error.WriteLine("Error inserting DOC:" + docid);
        ///
        ///      }
        ///
        ///    } // end append to end of array
        /// </summary>
        ///  }

        private static readonly int[] NUMBITS = new int[256];

        static AbstractDocSet()
        {
            NUMBITS[0] = 1;
            for (int i = 1; i < 256; i++)
            {
                int j = 7;
                while (j > 0)
                {
                    if ((i & (1 << j)) != 0)
                        break;
                    j--;
                }
                NUMBITS[i] = j + 1;
            }
        }

        private static int getNumBits(int v)
        {
            int n;
            if ((n = (int)(uint)v >> 24) > 0)
                return (NUMBITS[n] + 24);
            if ((n = (int)(uint)v >> 16) > 0)
                return (NUMBITS[n] + 16);
            if ((n = (int)(uint)v >> 8) > 0)
                return (NUMBITS[n] + 8);
            return NUMBITS[v];
        }
    }
}
