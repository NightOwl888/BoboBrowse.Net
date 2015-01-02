//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Written in Java.
//* 
//* Copyright (C) 2005-2006  John Wang
//*
//* This library is free software; you can redistribute it and/or
//* modify it under the terms of the GNU Lesser General Public
//* License as published by the Free Software Foundation; either
//* version 2.1 of the License, or (at your option) any later version.
//*
//* This library is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com.  

namespace LuceneExt.Impl
{
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using LuceneExt.Util;
    using System;

    ///<summary>Doc id set wrapper around P4DSet  
    ///@author abhasin
    ///</summary>
    [Serializable]
    public class P4DDocIdSet : AbstractDocSet
    {

        private const int DEFAULT_B = 5;

        ///<summary>Utitlity Object compression. </summary>
        private readonly P4DSetNoBase compressedSet;

        ///<summary>List for the base integer values of the compressed batches. </summary>
        private readonly IntArray baseList;

        public P4DDocIdSet()
        {
            baseList = new IntArray();
            compressedSet = new P4DSetNoBase();
            compressedBits = 0;
        }

        public P4DDocIdSet(int batchSize)
            : this()
        {
            BATCH_SIZE = batchSize;
            BATCH_OVER = batchSize / 20;
        }

        public override bool IsCacheable
        {
            get
            {
                return true;
            }
        }

        protected internal override object Compress()
        {
            current[0] = 0;
            compressedSet.SetParam(current_base, current_b, BATCH_SIZE, current_ex_count);
            baseList.Add(current_base);
            return compressedSet.CompressAlt(current);
        }

        ///<summary>Method to decompress the entire batch
        ///   *  </summary>
        ///   * <param name="blob"> OpenBitSet </param>
        ///   * <returns> int array with decompressed segment of numbers </returns>
        protected internal virtual int[] Decompress(OpenBitSet blob)
        {
            return new P4DSetNoBase().Decompress(blob);
        }

        ///<summary>Binary search</summary>
        ///<param name="val"> </param>
        ///<param name="begin"> </param>
        ///<param name="end"> </param>
        ///<returns> index greater than or equal to the target. -1 if the target is out of range. </returns>
        protected internal virtual int BinarySearchForNearest(int val, int begin, int end)
        {
            int mid = (begin + end) / 2;

            if (mid == end || (baseList.Get(mid) <= val && baseList.Get(mid + 1) > val))
            {
                return mid;
            }
            else if (baseList.Get(mid) < val)
            {
                return BinarySearchForNearest(val, mid + 1, end);
            }
            else
            {
                return BinarySearchForNearest(val, begin, mid);
            }
        }

        protected internal virtual int BinarySearchForNearestAlt(int val, int begin, int end)
        {
            while (true)
            {
                int mid = (begin + end) / 2;

                if (mid == end || (baseList.Get(mid) <= val && baseList.Get(mid + 1) > val))
                {
                    return mid;
                }
                else if (baseList.Get(mid) < val)
                {
                    begin = mid + 1;
                }
                else
                {
                    end = mid;
                }
            }
        }

        [Serializable]
        internal class P4DDocIdSetIterator : StatefulDSIterator
        {
            private P4DDocIdSet parent;

            ///<summary>Address bits</summary>
            internal int ADDRESS_BITS;

            ///<summary>retaining Offset from the list of blobs from the iterator pov</summary>
            internal int cursor = -1;

            ///<summary>Current iterating batch index.</summary>
            internal int bi = -1;

            ///<summary>Current iterating offset.</summary>
            internal int offset = 0;

            ///<summary>doc() returned</summary>
            internal int lastReturn = -1;

            ///<summary>size of the set</summary>
            internal int size;

            ///<summary>Reference to the blob iterating</summary>
            internal long[] @ref = null;

            ///<summary>Reference to the blob iterating</summary>
            internal int blobSize;


            internal P4DSetNoBase localCompressedSet = new P4DSetNoBase();


            internal P4DDocIdSetIterator(P4DDocIdSet parent)
            {
                ADDRESS_BITS = (int)(Math.Log(parent.BATCH_SIZE) / Math.Log(2));
                size = parent.Size();
                blobSize = parent.blob.Size();

                localCompressedSet.SetParam(0, DEFAULT_B, parent.BATCH_SIZE, parent.BATCH_OVER);
            }

            public override int DocID()
            {
                return lastReturn;
            }

            ///<summary>Method to allow iteration in decompressed form</summary>
            /*public int get(OpenBitSet set, int index)
            {
                return compressedSet.get(set, index);
            }*/

            ///<summary>Method to allow iteration in decompressed form </summary>
            public virtual int @get(long[] @set, int index)
            {
                return localCompressedSet.Get(@set, index);
            }

            public override int NextDoc()
            {
                // increment the cursor and check if it falls in the range for the
                // number of batches, if not return false else, its within range
                if (++cursor < size)
                {

                    // We are already in the array
                    if (bi == blobSize)
                    {
                        if (offset == -1)
                        {
                            lastReturn = DocIdSetIterator.NO_MORE_DOCS;
                            return DocIdSetIterator.NO_MORE_DOCS;
                        }
                        else
                            lastReturn += parent.current[offset++];
                    }
                    // if we are not in the array but on the boundary of a batch
                    // update local blob and set params
                    else if (offset == 0)
                    {

                        bi = BatchIndex(cursor);

                        if (bi < blobSize)
                        {
                            lastReturn = parent.baseList.Get(bi);
                            @ref = parent.blob.Get(bi);
                            localCompressedSet.UpdateParams(@ref);
                            offset++; // cursor - (bi << ADDRESS_BITS);+1
                        }
                        else
                        {
                            // offset = 0;//cursor - (bi << ADDRESS_BITS);
                            lastReturn = parent.current[offset++];
                        }
                    }
                    else
                    {

                        lastReturn += localCompressedSet.Get(@ref, offset);
                        offset = (++offset) % parent.BATCH_SIZE;
                    }
                    return lastReturn;

                }
                lastReturn = DocIdSetIterator.NO_MORE_DOCS;
                return DocIdSetIterator.NO_MORE_DOCS;

            }

            ///     <summary> * Get the index of the batch this cursor position falls into
            ///     *  </summary>
            ///     * <param name="index">
            ///     * @return </param>
            private int BatchIndex(int index)
            {
                return index >> ADDRESS_BITS;
            }

            ///     <summary> * Next need be called after skipping.</summary>
            public override int Advance(int target)
            {

                if (target <= lastReturn)
                    target = lastReturn + 1;

                // NOTE : Update lastReturn.

                if (bi == blobSize || (bi + 1 < blobSize && target < parent.baseList.Get(bi + 1)))
                {
                    while (NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        if (lastReturn >= target)
                            return lastReturn;
                    }
                    lastReturn = DocIdSetIterator.NO_MORE_DOCS;
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

                // If the target is outside the compressed space
                if (blobSize == 0 || target >= parent.current[0])
                {

                    bi = blobSize;
                    @ref = null;

                    offset = FindAndUpdate(parent.current, target);

                    if (offset > 0)
                    {
                        cursor = blobSize * parent.BATCH_SIZE + offset - 1;
                        return lastReturn;
                    }
                    // We have gone over the batch boundary
                    else if (offset == 0)
                    {
                        cursor = (blobSize + 1) * parent.BATCH_SIZE;
                        return lastReturn;
                    }

                    lastReturn = DocIdSetIterator.NO_MORE_DOCS;
                    return DocIdSetIterator.NO_MORE_DOCS;
                }


                // This returns the blob where base value is less than the value looked
                // for.
                int index = parent.BinarySearchForNearest(target, bi, blobSize - 1);
                // Move both these further, as we are in this block, so that
                // doc() call works.
                bi = index;
                lastReturn = parent.baseList.Get(index);
                @ref = parent.blob.Get(index);
                localCompressedSet.UpdateParams(@ref);

                // find the nearest integer in the compressed space.
                offset = FindAndUpdate(@ref, target, lastReturn);

                if (offset < 0)
                {
                    // oops we fell into the gap. This case happens when we land
                    // in the gap between two batches. We can optimize this
                    // step.
                    if (++index < blobSize)
                    {
                        lastReturn = parent.baseList.Get(index);
                        @ref = parent.blob.Get(index);
                        localCompressedSet.UpdateParams(@ref);
                    }
                    else
                    {
                        lastReturn = parent.current[0];
                        @ref = null;
                    }
                    bi = index;
                    offset = 1;
                }

                cursor = bi * parent.BATCH_SIZE + offset - 1;

                return lastReturn;
            }

            //    private void printSet(OpenBitSet test, int base) {
            //      try {
            //        int localBase = base;
            //        for (int i = 1; i < BATCH_SIZE; i++) {
            //          localBase += compressedSet.get(test, i);
            //          System.out.print(localBase + ",");
            //        }
            //      } catch (Exception e) {
            //        e.printStackTrace();
            //        int localBase = base;
            //        int testint[] = compressedSet.decompress(test);
            //        for (int i = 1; i < BATCH_SIZE; i++) {
            //          localBase += testint[i];
            //          System.out.print(localBase + ",");
            //        }
            //      }
            //
            //    }

            private void PrintSet(long[] test, int @base)
            {
                try
                {
                    int localBase = @base;
                    for (int i = 1; i < parent.BATCH_SIZE; i++)
                    {
                        localBase += localCompressedSet.Get(test, i);
                        Console.Write(localBase + ",");
                    }
                }
                catch
                {                    
                    int localBase = @base;
                    int[] testint = localCompressedSet.Decompress(test);
                    for (int i = 1; i < parent.BATCH_SIZE; i++)
                    {
                        localBase += testint[i];
                        Console.Write(localBase + ",");
                    }
                }

            }

            ///     <summary> * Find the element in the compressed set
            ///     *  </summary>
            ///     * <param name="next"> </param>
            ///     * <param name="target"> </param>
            ///     * <param name="base">
            ///     * @return </param>
            private int FindAndUpdate(long[] next, int target, int @base)
            {
                lastReturn = @base;
                if (lastReturn >= target)
                    return 1;

                for (int i = 1; i < parent.BATCH_SIZE; i++)
                {
                    // System.out.println("Getting "+i);
                    // System.out.flush();

                    lastReturn += localCompressedSet.Get(next, i);
                    if (lastReturn >= target)
                    {
                        // if(i==127)
                        return (i + 1) % parent.BATCH_SIZE;
                    }
                }
                return -1;
            }

            ///    
            ///     <summary> * Find the element in the compressed set
            ///     *  </summary>
            ///     * <param name="next"> </param>
            ///     * <param name="target"> </param>
            ///     * <param name="base">
            ///     * @return
            ///     
            ///    private int findAndUpdate(OpenBitSet next, int target, int base) {
            ///      lastReturn = base;
            ///      if (lastReturn >= target)
            ///        return 1;
            ///
            ///      for (int i = 1; i < BATCH_SIZE; i++) {
            ///        // System.out.println("Getting "+i);
            ///        // System.out.flush();
            ///
            ///        lastReturn += compressedSet.get(next, i);
            ///        if (lastReturn >= target) {
            ///          // if(i==127)
            ///          return (i + 1) % BATCH_SIZE;
            ///        }
            ///      }
            ///      return -1; </param>
            ///    }

            ///     <summary> * Find the element in the set and update parameters.
            ///     *  </summary>
            private int FindAndUpdate(int[] array, int target)
            {

                if (array == null)
                    return -1;

                lastReturn = array[0];
                if (lastReturn >= target)
                    return 1;

                for (int i = 1; i < parent.current_size; i++)
                {
                    lastReturn += array[i];

                    if (lastReturn >= target)
                        return (i + 1) % parent.BATCH_SIZE;
                }
                return -1;

            }

            public override int GetCursor()
            {
                return cursor;
            }

        }

        public override DocIdSetIterator Iterator()
        {
            return new P4DDocIdSetIterator(this);
        }

        public override int FindWithIndex(int val)
        {

            P4DDocIdSetIterator dcit = new P4DDocIdSetIterator(this);

            int docid = dcit.Advance(val);
            if (docid == val)
                return dcit.GetCursor();
            return -1;
        }

        public override bool Find(int val)
        {
            long time = System.Environment.TickCount;
            int local = 0;

            if (Size() == 0)
            {
                return false;
            }


            if (val > lastAdded || val < baseList.Get(0)) //Short Circuit case where its not in the set at all
            {
                //System.out.println("Time to perform BinarySearch for:"+val+":"+(System.nanoTime() - time));
                return false;
            }
            else if (val >= current_base) // We are in the set
            {

                int i = 0;
                for (i = 0; i < current_size; i++)
                {
                    local += current[i];

                    if (local > val)
                    {
                        break;
                    }
                }

                if (i == current_size)
                {
                    return local == val;
                }
                else
                {
                    return (local - current[i] == val);
                }
            }
            else // We are in the compressed space
            {
                if (baseList.Size() == 0)
                {
                    return false;
                }

                int blobIndex = BinarySearchForNearest(val, 0, blob.Size() - 1);

                local = baseList.Get(blobIndex);
                long[] @ref = blob.Get(blobIndex);
                P4DSetNoBase localCompressedSet = new P4DSetNoBase();
                localCompressedSet.SetParam(0, DEFAULT_B, BATCH_SIZE, BATCH_OVER);
                localCompressedSet.UpdateParams(@ref);

                int i = 0;

                for (i = 0; i < BATCH_SIZE; i++)
                {
                    local += localCompressedSet.Get(@ref, i);

                    if (local > val)
                    {
                        break;
                    }

                }
                if (i == BATCH_SIZE)
                {
                    return local == val;
                }
                else
                {
                    return (local - localCompressedSet.Get(@ref, i)) == val;
                }
            }
        }

        private int FindIn(OpenBitSet OpenBitSet, int baseVal, int val)
        {
            return -1;
        }

        private int FindIn(int[] current, int baseVal, int val)
        {
            int local = baseVal;
            for (int i = 1; i < BATCH_SIZE; i++)
            {
                local += current[i];

                if (val > local)
                {
                    if (local == val)
                        return i;
                }
                else
                    return -1;

            }
            return -1;
        }

        public override void Optimize()
        {
            //Trim the baselist to size
            this.baseList.Seal();
            this.blob.Seal();
        }


        public override long SizeInBytes()
        {
            // 64 is the overhead for an int array
            // blobsize * numberofelements * 1.1 (Object Overhead)
            // batch_size * 4 + int array overhead
            // P4dDocIdSet Overhead 110
            Optimize();
            return (long)(baseList.Length() * 4 + 64 + blob.Length() * BATCH_SIZE * 1.1 + BATCH_SIZE * 4 + 24 + 110);

        }

        public virtual int TotalBlobSize()
        {
            int total = 0;
            for (int i = blob.Length() - 1; i >= 0; i--)
            {
                long[] segment = blob.Get(i);
                total += segment.Length;
            }
            return total;
        }
    }
}
