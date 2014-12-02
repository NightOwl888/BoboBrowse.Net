

namespace LuceneExt.Util
{
    using System;
    using Lucene.Net.Search;

    /// <summary> * Licensed to the Apache Software Foundation (ASF) under one or more
    /// * contributor license agreements.  See the NOTICE file distributed with
    /// * this work for additional information regarding copyright ownership.
    /// * The ASF licenses this file to You under the Apache License, Version 2.0
    /// * (the "License"); you may not use this file except in compliance with
    /// * the License.  You may obtain a copy of the License at
    /// *
    /// *     http://www.apache.org/licenses/LICENSE-2.0
    /// *
    /// * Unless required by applicable law or agreed to in writing, software
    /// * distributed under the License is distributed on an "AS IS" BASIS,
    /// * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    /// * See the License for the specific language governing permissions and
    /// * limitations under the License. </summary>
    /// 

    // Derived from org.apache.lucene.util.ScorerDocQueue of July 2008 


    /// <summary> A DisiDocQueue maintains a partial ordering of its DocIdSetIterators such that the
    /// *  least DocIdSetIterator (disi) can always be found in constant time.
    /// *  Put()'s and pop()'s require log(size) time.
    /// *  The ordering is by DocIdSetIterator.Doc(). </summary>
    public class DisiDocQueue
    {
        private readonly HeapedDisiDoc[] heap;
        private readonly int maxSize;
        private int size;

        private sealed class HeapedDisiDoc
        {
            internal readonly DocIdSetIterator Disi;
            internal int Doc;

            internal HeapedDisiDoc(DocIdSetIterator disi)
                : this(disi, disi.DocID())
            {
            }

            internal HeapedDisiDoc(DocIdSetIterator disi, int doc)
            {
                Disi = disi;
                Doc = doc;
            }

            internal void Adjust()
            {
                Doc = Disi.DocID();
            }
        }

        private HeapedDisiDoc topHDD; // same as heap[1], only for speed

        /// <summary> Create a DisiDocQueue with a maximum size.  </summary>
        public DisiDocQueue(int maxSize)
        {
            // assert maxSize >= 0;
            size = 0;
            int heapSize = maxSize + 1;
            heap = new HeapedDisiDoc[heapSize];
            this.maxSize = maxSize;
            topHDD = heap[1]; // initially null
        }

        ///<summary>Adds a Scorer to a ScorerDocQueue in log(size) time.
        ///   * If one tries to add more Scorers than maxSize
        ///   * a RuntimeException (ArrayIndexOutOfBound) is thrown. </summary>
        public void Put(DocIdSetIterator disi)
        {
            size++;
            heap[size] = new HeapedDisiDoc(disi);
            UpHeap();
        }

        ///<summary>Adds a DocIdSetIterator to the DisiDocQueue in log(size) time if either
        ///   * the DisiDocQueue is not full, or not lessThan(disi, top()). </summary>
        ///   * <param name="disi"> </param>
        ///   * <returns> true if DocIdSetIterator is added, false otherwise. </returns>
        public bool Insert(DocIdSetIterator disi)
        {
            if (size < maxSize)
            {
                Put(disi);
                return true;
            }
            else
            {
                int docNr = disi.DocID();
                if ((size > 0) && (!(docNr < topHDD.Doc))) // heap[1] is top()
                {
                    heap[1] = new HeapedDisiDoc(disi, docNr);
                    DownHeap();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        ///   <summary> Returns the least DocIdSetIterator of the DisiDocQueue in constant time.
        ///   * Should not be used when the queue is empty. </summary>
        public DocIdSetIterator Top()
        {
            return topHDD.Disi;
        }

        ///   <summary> Returns document number of the least Scorer of the ScorerDocQueue
        ///   * in constant time.
        ///   * Should not be used when the queue is empty. </summary>
        public int TopDoc()
        {
            return topHDD.Doc;
        }

        public bool TopNextAndAdjustElsePop()
        {
            return CheckAdjustElsePop(topHDD.Disi.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
        }

        public bool TopSkipToAndAdjustElsePop(int target)
        {
            return CheckAdjustElsePop(topHDD.Disi.Advance(target) != DocIdSetIterator.NO_MORE_DOCS);
        }

        private bool CheckAdjustElsePop(bool cond)
        {
            if (cond) // see also adjustTop
            {
                topHDD.Doc = topHDD.Disi.DocID();
            } // see also popNoResult
            else
            {
                heap[1] = heap[size]; // move last to first
                heap[size] = null;
                size--;
            }
            DownHeap();
            return cond;
        }

        ///   <summary> Removes and returns the least disi of the DisiDocQueue in log(size)
        ///   * time.
        ///   * Should not be used when the queue is empty. </summary>
        public DocIdSetIterator Pop()
        {
            DocIdSetIterator result = topHDD.Disi;
            PopNoResult();
            return result;
        }

        ///   <summary> Removes the least disi of the DisiDocQueue in log(size) time.
        ///   * Should not be used when the queue is empty. </summary>
        private void PopNoResult()
        {
            heap[1] = heap[size]; // move last to first
            heap[size] = null;
            size--;
            DownHeap(); // adjust heap
        }

        ///   <summary> Should be called when the disi at top changes doc() value.
        ///   * Still log(n) worst case, but it's at least twice as fast to <pre>
        ///   *  { pq.top().change(); pq.adjustTop(); }
        ///   * </pre> instead of <pre>
        ///   *  { o = pq.pop(); o.change(); pq.push(o); }
        ///   * </pre> </summary>
        public void AdjustTop()
        {
            topHDD.Adjust();
            DownHeap();
        }

        /// <summary> Returns the number of disis currently stored in the DisiDocQueue.  </summary>
        public int Size()
        {
            return size;
        }

        /// <summary> Removes all entries from the DisiDocQueue.  </summary>
        public void Clear()
        {
            for (int i = 0; i <= size; i++)
            {
                heap[i] = null;
            }
            size = 0;
        }

        private void UpHeap()
        {
            int i = size;
            HeapedDisiDoc node = heap[i]; // save bottom node
            int j = (int)(uint)i >> 1;
            while ((j > 0) && (node.Doc < heap[j].Doc))
            {
                heap[i] = heap[j]; // shift parents down
                i = j;
                j = (int)(uint)j >> 1;
            }
            heap[i] = node; // install saved node
            topHDD = heap[1];
        }

        private void DownHeap()
        {
            int i = 1;
            HeapedDisiDoc node = heap[i]; // save top node
            int j = i << 1; // find smaller child
            int k = j + 1;
            if ((k <= size) && (heap[k].Doc < heap[j].Doc))
            {
                j = k;
            }
            while ((j <= size) && (heap[j].Doc < node.Doc))
            {
                heap[i] = heap[j]; // shift up child
                i = j;
                j = i << 1;
                k = j + 1;
                if (k <= size && (heap[k].Doc < heap[j].Doc))
                {
                    j = k;
                }
            }
            heap[i] = node; // install saved node
            topHDD = heap[1];
        }
    }
}
