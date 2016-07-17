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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using System;
    using System.Collections.Generic;

    public class CombinedLongFacetIterator : LongFacetIterator
    {
        public class LongIteratorNode
        {
            private readonly LongFacetIterator _iterator;
            protected long _curFacet;
            protected int _curFacetCount;

            public LongIteratorNode(LongFacetIterator iterator)
            {
                _iterator = iterator;
                _curFacet = TermLongList.VALUE_MISSING;
                _curFacetCount = 0;
            }

            /// <summary>
            /// Added in .NET version as an accessor to the _iterator field.
            /// </summary>
            /// <returns></returns>
            public virtual LongFacetIterator Iterator()
            {
                return _iterator;
            }

            /// <summary>
            /// Added in .NET version as an accessor to the _curFacet field.
            /// </summary>
            public virtual long CurFacet
            {
                get { return _curFacet; }
            }

            /// <summary>
            /// Added in .NET version as an accessor to the _curFacetCount field.
            /// </summary>
            public virtual int CurFacetCount
            {
                get { return _curFacetCount; }
            }

            public virtual bool Fetch(int minHits)
            {
                if (minHits > 0)
                    minHits = 1;
                if ((_curFacet = _iterator.NextLong(minHits)) != TermLongList.VALUE_MISSING)
                {
                    _curFacetCount = _iterator.Count;
                    return true;
                }
                _curFacet = TermLongList.VALUE_MISSING;
                _curFacetCount = 0;
                return false;
            }
        }

        private readonly LongFacetPriorityQueue _queue;

        private IList<LongFacetIterator> _iterators;

        private CombinedLongFacetIterator(int length)
        {
            _queue = new LongFacetPriorityQueue();
            _queue.Initialize(length);
        }

        public CombinedLongFacetIterator(IList<LongFacetIterator> iterators)
            : this(iterators.Count)
        {
            _iterators = iterators;
            foreach (LongFacetIterator iterator in iterators)
            {
                LongIteratorNode node = new LongIteratorNode(iterator);
                if (node.Fetch(1))
                    _queue.Add(node);
            }
            _facet = TermLongList.VALUE_MISSING;
            count = 0;
        }

        public CombinedLongFacetIterator(List<LongFacetIterator> iterators, int minHits)
            : this(iterators.Count)
        {
            _iterators = iterators;
            foreach (LongFacetIterator iterator in iterators)
            {
                LongIteratorNode node = new LongIteratorNode(iterator);
                if (node.Fetch(minHits))
                    _queue.Add(node);
            }
            _facet = TermLongList.VALUE_MISSING;
            count = 0;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#getFacet()
        /// </summary>
        /// <returns></returns>
        public virtual string GetFacet()
        {
            if (_facet == TermLongList.VALUE_MISSING) return null;
            return Format(_facet);
        }
        public override string Format(long val)
        {
            return _iterators[0].Format(val);
        }
        public override string Format(Object val)
        {
            return _iterators[0].Format(val);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#getFacetCount()
        /// </summary>
        public virtual int FacetCount
        {
            get { return count; }
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#next()
        /// </summary>
        /// <returns></returns>
        public override string Next()
        {
            if (!HasNext())
                throw new IndexOutOfRangeException("No more facets in this iteration");

            LongIteratorNode node = _queue.Top();

            _facet = node.CurFacet;
            long next = TermLongList.VALUE_MISSING;
            count = 0;
            while (HasNext())
            {
                node = _queue.Top();
                next = node.CurFacet;
                if ((next != TermLongList.VALUE_MISSING) && (next != _facet))
                {
                    return Format(_facet);
                }
                count += node.CurFacetCount;
                if (node.Fetch(1))
                    _queue.UpdateTop();
                else
                    _queue.Pop();
            }
            return null;
        }

        /// <summary>
        /// This version of the next() method applies the minHits from the _facet spec before returning the _facet and its hitcount
        /// </summary>
        /// <param name="minHits">the minHits from the _facet spec for CombinedFacetAccessible</param>
        /// <returns>The next _facet that obeys the minHits </returns>
        public override string Next(int minHits)
        {
            int qsize = _queue.Size();
            if (qsize == 0)
            {
                _facet = TermLongList.VALUE_MISSING;
                count = 0;
                return null;
            }

            LongIteratorNode node = _queue.Top();
            _facet = node.CurFacet;
            count = node.CurFacetCount;
            while (true)
            {
                if (node.Fetch(minHits))
                {
                    node = _queue.UpdateTop();
                }
                else
                {
                    _queue.Pop();
                    if (--qsize > 0)
                    {
                        node = _queue.Top();
                    }
                    else
                    {
                        // we reached the end. check if this _facet obeys the minHits
                        if (count < minHits)
                        {
                            _facet = TermLongList.VALUE_MISSING;
                            count = 0;
                            return null;
                        }
                        break;
                    }
                }
                long next = node.CurFacet;
                if (next != _facet)
                {
                    // check if this _facet obeys the minHits
                    if (count >= minHits)
                        break;
                    // else, continue iterating to the next _facet
                    _facet = next;
                    count = node.CurFacetCount;
                }
                else
                {
                    count += node.CurFacetCount;
                }
            }
            return Format(_facet);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#hasNext()
        /// </summary>
        /// <returns></returns>
        public override bool HasNext()
        {
            return (_queue.Size() > 0);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#remove()
        /// </summary>
        public override void Remove()
        {
            throw new NotSupportedException("remove() method not supported for Facet Iterators");
        }

        /// <summary>
        /// Lucene PriorityQueue
        /// </summary>
        public class LongFacetPriorityQueue
        {
            private int size;
            private int maxSize;
            protected LongIteratorNode[] heap;

            /// <summary>
            /// Subclass constructors must call this.
            /// </summary>
            /// <param name="maxSize"></param>
            public void Initialize(int maxSize)
            {
                size = 0;
                int heapSize;
                if (0 == maxSize)
                    // We allocate 1 extra to avoid if statement in top()
                    heapSize = 2;
                else
                    heapSize = maxSize + 1;
                heap = new LongIteratorNode[heapSize];
                this.maxSize = maxSize;
            }

            public void Put(LongIteratorNode element)
            {
                size++;
                heap[size] = element;
                UpHeap();
            }

            public LongIteratorNode Add(LongIteratorNode element)
            {
                size++;
                heap[size] = element;
                UpHeap();
                return heap[1];
            }

            public virtual bool Insert(LongIteratorNode element)
            {
                return InsertWithOverflow(element) != element;
            }

            public virtual LongIteratorNode InsertWithOverflow(LongIteratorNode element)
            {
                if (size < maxSize)
                {
                    Put(element);
                    return null;
                }
                else if (size > 0 && !(element.CurFacet < heap[1].CurFacet))
                {
                    LongIteratorNode ret = heap[1];
                    heap[1] = element;
                    AdjustTop();
                    return ret;
                }
                else
                {
                    return element;
                }
            }

            /// <summary>
            /// Returns the least element of the PriorityQueue in constant time.
            /// </summary>
            /// <returns></returns>
            public LongIteratorNode Top()
            {
                // We don't need to check size here: if maxSize is 0,
                // then heap is length 2 array with both entries null.
                // If size is 0 then heap[1] is already null.
                return heap[1];
            }

            /// <summary>
            /// Removes and returns the least element of the PriorityQueue in log(size)
            /// time.
            /// </summary>
            /// <returns></returns>
            public LongIteratorNode Pop()
            {
                if (size > 0)
                {
                    LongIteratorNode result = heap[1]; // save first value
                    heap[1] = heap[size]; // move last to first
                    heap[size] = null; // permit GC of objects
                    size--;
                    DownHeap(); // adjust heap
                    return result;
                }
                else
                    return null;
            }

            public void AdjustTop()
            {
                DownHeap();
            }

            public LongIteratorNode UpdateTop()
            {
                DownHeap();
                return heap[1];
            }

            /// <summary>
            /// Returns the number of elements currently stored in the PriorityQueue.
            /// </summary>
            /// <returns></returns>
            public int Size()
            {
                return size;
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

            private void UpHeap()
            {
                int i = size;
                LongIteratorNode node = heap[i]; // save bottom node
                int j = (int)(((uint)i) >> 1);
                while (j > 0 && (node.CurFacet < heap[j].CurFacet))
                {
                    heap[i] = heap[j]; // shift parents down
                    i = j;
                    j = (int)(((uint)j) >> 1);
                }
                heap[i] = node; // install saved node
            }

            private void DownHeap()
            {
                int i = 1;
                LongIteratorNode node = heap[i]; // save top node
                int j = i << 1; // find smaller child
                int k = j + 1;
                if (k <= size && (heap[k].CurFacet < heap[j].CurFacet))
                {
                    j = k;
                }
                while (j <= size && (heap[j].CurFacet < node.CurFacet))
                {
                    heap[i] = heap[j]; // shift up child
                    i = j;
                    j = i << 1;
                    k = j + 1;
                    if (k <= size && (heap[k].CurFacet < heap[j].CurFacet))
                    {
                        j = k;
                    }
                }
                heap[i] = node; // install saved node
            }
        }

        public override long NextLong()
        {
            if (!HasNext())
                throw new IndexOutOfRangeException("No more facets in this iteration");

            LongIteratorNode node = _queue.Top();

            _facet = node.CurFacet;
            long next = TermLongList.VALUE_MISSING;
            count = 0;
            while (HasNext())
            {
                node = _queue.Top();
                next = node.CurFacet;
                if ((next != TermLongList.VALUE_MISSING) && (next != _facet))
                {
                    return _facet;
                }
                count += node.CurFacetCount;
                if (node.Fetch(1))
                    _queue.UpdateTop();
                else
                    _queue.Pop();
            }
            return TermLongList.VALUE_MISSING;
        }

        public override long NextLong(int minHits)
        {
            int qsize = _queue.Size();
            if (qsize == 0)
            {
                _facet = TermLongList.VALUE_MISSING;
                count = 0;
                return TermLongList.VALUE_MISSING;
            }

            LongIteratorNode node = _queue.Top();
            _facet = node.CurFacet;
            count = node.CurFacetCount;
            while (true)
            {
                if (node.Fetch(minHits))
                {
                    node = _queue.UpdateTop();
                }
                else
                {
                    _queue.Pop();
                    if (--qsize > 0)
                    {
                        node = _queue.Top();
                    }
                    else
                    {
                        // we reached the end. check if this _facet obeys the minHits
                        if (count < minHits)
                        {
                            _facet = TermLongList.VALUE_MISSING;
                            count = 0;
                        }
                        break;
                    }
                }
                long next = node.CurFacet;
                if (next != _facet)
                {
                    // check if this _facet obeys the minHits
                    if (count >= minHits)
                        break;
                    // else, continue iterating to the next _facet
                    _facet = next;
                    count = node.CurFacetCount;
                }
                else
                {
                    count += node.CurFacetCount;
                }
            }
            return _facet;
        }
    }
}

  