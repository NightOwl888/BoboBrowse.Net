//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
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

﻿// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Sort
{
    using Lucene.Net.Search;
    using System;

    public class DocIDPriorityQueue
    {
        public int size;
        protected readonly ScoreDoc[] heap;
        public readonly int @base;

        private readonly DocComparer comparer;

        public DocIDPriorityQueue(DocComparer comparer, int maxSize, int @base)
        {
            this.comparer = comparer;
            size = 0;
            this.@base = @base;
            int heapSize;
            if (0 == maxSize)
                // We allocate 1 extra to avoid if statement in top()
                heapSize = 2;
            else
                heapSize = maxSize + 1;
            this.heap = new ScoreDoc[heapSize];
        }

        /// <summary>
        /// Adds an Object to a PriorityQueue in log(size) time. If one tries to add
        /// more objects than maxSize from initialize an
        /// {@link ArrayIndexOutOfBoundsException} is thrown.
        /// </summary>
        /// <param name="element"></param>
        /// <returns>the new 'bottom' element in the queue.</returns>
        public ScoreDoc Add(ScoreDoc element)
        {
            size++;
            heap[size] = element;
            UpHeap(size);
            return heap[1];
        }

        public virtual IComparable SortValue(ScoreDoc doc)
        {
            return this.comparer.Value(doc);
        }

        private int Compare(ScoreDoc doc1, ScoreDoc doc2)
        {
            int cmp = comparer.Compare(doc1, doc2);
            if (cmp != 0)
            {
                return -cmp;
            }
            else
            {
                return doc2.Doc - doc1.Doc;
            }
        }

        public virtual ScoreDoc Replace(ScoreDoc element)
        {
            heap[1] = element;
            DownHeap(1);
            return heap[1];
        }

        /// <summary>
        /// Takes O(size) time.
        /// </summary>
        /// <param name="newEle"></param>
        /// <param name="oldEle"></param>
        /// <returns>the 'bottom' element in the queue.</returns>
        public virtual ScoreDoc Replace(ScoreDoc newEle, ScoreDoc oldEle)
        {
            for (int i = 1; i <= size; ++i)
            {
                if (heap[i] == oldEle)
                {
                    heap[i] = newEle;
                    UpHeap(i);
                    DownHeap(i);
                    break;
                }
            }
            return heap[1];
        }

        /// <summary>
        /// Gets the least element of the PriorityQueue in constant time. 
        /// </summary>
        /// <returns></returns>
        public ScoreDoc Top
        {
            get
            {
                // We don't need to check size here: if maxSize is 0,
                // then heap is length 2 array with both entries null.
                // If size is 0 then heap[1] is already null.
                return heap[1];
            }
        }

        /// <summary>
        /// Removes and returns the least element of the PriorityQueue 
        /// in log(size) time.
        /// </summary>
        /// <returns></returns>
        public ScoreDoc Pop()
        {
            if (size > 0)
            {
                ScoreDoc result = heap[1];			  // save first value
                heap[1] = heap[size];			  // move last to first
                heap[size] = null;			  // permit GC of objects
                size--;
                DownHeap(1);				  // adjust heap
                return result;
            }
            else
                return null;
        }

        /// <summary>
        /// Should be called when the Object at top changes values. Still log(n) worst
        /// case, but it's at least twice as fast to
        /// 
        /// <pre>
        /// pq.Top.Change();
        /// pq.UpdateTop();
        /// </pre>
        /// 
        /// instead of
        /// 
        /// <pre>
        /// o = pq.Pop();
        /// o.Change();
        /// pq.Push(o);
        /// </pre>
        /// </summary>
        /// <returns>the new 'top' element.</returns>
        public ScoreDoc UpdateTop()
        {
            DownHeap(1);
            return heap[1];
        }

        /// <summary>
        /// Gets the number of elements currently stored in the PriorityQueue.
        /// </summary>
        // BoboBrowse.Net: we use Count instead of Size() in .NET
        public int Count
        {
            get { return size; }
        }

        /// <summary>
        /// Removes all entries from the PriorityQueue.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i <= size; i++)
            {
                heap[i] = null;
            }
            size = 0;
        }

        private void UpHeap(int i)
        {
            ScoreDoc node = heap[i];    // save bottom node
            int j = (int)(((uint)i) >> 1);
            while (j > 0 && Compare(node, heap[j]) < 0)
            {
                heap[i] = heap[j];      // shift parents down
                i = j;
                j = (int)(((uint)j) >> 1);
            }
            heap[i] = node;             // install saved node
        }

        private void DownHeap(int i)
        {
            ScoreDoc node = heap[i];    // save top node
            int j = i << 1;             // find smaller child
            int k = j + 1;
            if (k <= size && Compare(heap[k], heap[j]) < 0)
            {
                j = k;
            }
            while (j <= size && Compare(heap[j], node) < 0)
            {
                heap[i] = heap[j];      // shift up child
                i = j;
                j = i << 1;
                k = j + 1;
                if (k <= size && Compare(heap[k], heap[j]) < 0)
                {
                    j = k;
                }
            }
            heap[i] = node;             // install saved node
        }
    }
}
