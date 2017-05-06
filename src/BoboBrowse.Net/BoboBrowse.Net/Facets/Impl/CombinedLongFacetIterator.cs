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

    /// <summary>
    /// NOTE: This was CombinedLongFacetIterator in bobo-browse
    /// </summary>
    public class CombinedInt64FacetIterator : Int64FacetIterator
    {
        /// <summary>
        /// NOTE: This was LongIteratorNode in bobo-browse
        /// </summary>
        public class Int64IteratorNode
        {
            private readonly Int64FacetIterator m_iterator;
            protected long m_curFacet;
            protected int m_curFacetCount;

            public Int64IteratorNode(Int64FacetIterator iterator)
            {
                m_iterator = iterator;
                m_curFacet = TermInt64List.VALUE_MISSING;
                m_curFacetCount = 0;
            }

            /// <summary>
            /// Added in .NET version as an accessor to the _iterator field.
            /// </summary>
            /// <returns></returns>
            public virtual Int64FacetIterator GetIterator() // TODO: Implement IEnumerable<T> ?
            {
                return m_iterator;
            }

            /// <summary>
            /// Added in .NET version as an accessor to the _curFacet field.
            /// </summary>
            public virtual long CurFacet
            {
                get { return m_curFacet; }
            }

            /// <summary>
            /// Added in .NET version as an accessor to the _curFacetCount field.
            /// </summary>
            public virtual int CurFacetCount
            {
                get { return m_curFacetCount; }
            }

            public virtual bool Fetch(int minHits)
            {
                if (minHits > 0)
                    minHits = 1;
                if ((m_curFacet = m_iterator.NextLong(minHits)) != TermInt64List.VALUE_MISSING)
                {
                    m_curFacetCount = m_iterator.Count;
                    return true;
                }
                m_curFacet = TermInt64List.VALUE_MISSING;
                m_curFacetCount = 0;
                return false;
            }
        }

        private readonly Int64FacetPriorityQueue _queue;

        private IList<Int64FacetIterator> _iterators;

        private CombinedInt64FacetIterator(int length)
        {
            _queue = new Int64FacetPriorityQueue();
            _queue.Initialize(length);
        }

        public CombinedInt64FacetIterator(IList<Int64FacetIterator> iterators)
            : this(iterators.Count)
        {
            _iterators = iterators;
            foreach (Int64FacetIterator iterator in iterators)
            {
                Int64IteratorNode node = new Int64IteratorNode(iterator);
                if (node.Fetch(1))
                    _queue.Add(node);
            }
            m_facet = TermInt64List.VALUE_MISSING;
            m_count = 0;
        }

        public CombinedInt64FacetIterator(List<Int64FacetIterator> iterators, int minHits)
            : this(iterators.Count)
        {
            _iterators = iterators;
            foreach (Int64FacetIterator iterator in iterators)
            {
                Int64IteratorNode node = new Int64IteratorNode(iterator);
                if (node.Fetch(minHits))
                    _queue.Add(node);
            }
            m_facet = TermInt64List.VALUE_MISSING;
            m_count = 0;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#getFacet()
        /// </summary>
        /// <returns></returns>
        public virtual string GetFacet()
        {
            if (m_facet == TermInt64List.VALUE_MISSING) return null;
            return Format(m_facet);
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
            get { return m_count; }
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

            Int64IteratorNode node = _queue.Top;

            m_facet = node.CurFacet;
            long next = TermInt64List.VALUE_MISSING;
            m_count = 0;
            while (HasNext())
            {
                node = _queue.Top;
                next = node.CurFacet;
                if ((next != TermInt64List.VALUE_MISSING) && (next != m_facet))
                {
                    return Format(m_facet);
                }
                m_count += node.CurFacetCount;
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
            int qsize = _queue.Count;
            if (qsize == 0)
            {
                m_facet = TermInt64List.VALUE_MISSING;
                m_count = 0;
                return null;
            }

            Int64IteratorNode node = _queue.Top;
            m_facet = node.CurFacet;
            m_count = node.CurFacetCount;
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
                        node = _queue.Top;
                    }
                    else
                    {
                        // we reached the end. check if this _facet obeys the minHits
                        if (m_count < minHits)
                        {
                            m_facet = TermInt64List.VALUE_MISSING;
                            m_count = 0;
                            return null;
                        }
                        break;
                    }
                }
                long next = node.CurFacet;
                if (next != m_facet)
                {
                    // check if this _facet obeys the minHits
                    if (m_count >= minHits)
                        break;
                    // else, continue iterating to the next _facet
                    m_facet = next;
                    m_count = node.CurFacetCount;
                }
                else
                {
                    m_count += node.CurFacetCount;
                }
            }
            return Format(m_facet);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#hasNext()
        /// </summary>
        /// <returns></returns>
        public override bool HasNext()
        {
            return (_queue.Count > 0);
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
        /// <para/>
        /// NOTE: This was LongFacetPriorityQueue in bobo-browse
        /// </summary>
        public class Int64FacetPriorityQueue
        {
            private int m_size;
            private int m_maxSize;
            protected Int64IteratorNode[] m_heap;

            /// <summary>
            /// Subclass constructors must call this.
            /// </summary>
            /// <param name="maxSize"></param>
            public void Initialize(int maxSize)
            {
                m_size = 0;
                int heapSize;
                if (0 == maxSize)
                    // We allocate 1 extra to avoid if statement in top()
                    heapSize = 2;
                else
                    heapSize = maxSize + 1;
                m_heap = new Int64IteratorNode[heapSize];
                this.m_maxSize = maxSize;
            }

            public void Put(Int64IteratorNode element)
            {
                m_size++;
                m_heap[m_size] = element;
                UpHeap();
            }

            public Int64IteratorNode Add(Int64IteratorNode element)
            {
                m_size++;
                m_heap[m_size] = element;
                UpHeap();
                return m_heap[1];
            }

            public virtual bool Insert(Int64IteratorNode element)
            {
                return InsertWithOverflow(element) != element;
            }

            public virtual Int64IteratorNode InsertWithOverflow(Int64IteratorNode element)
            {
                if (m_size < m_maxSize)
                {
                    Put(element);
                    return null;
                }
                else if (m_size > 0 && !(element.CurFacet < m_heap[1].CurFacet))
                {
                    Int64IteratorNode ret = m_heap[1];
                    m_heap[1] = element;
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
            public Int64IteratorNode Top
            {
                get
                {
                    // We don't need to check size here: if maxSize is 0,
                    // then heap is length 2 array with both entries null.
                    // If size is 0 then heap[1] is already null.
                    return m_heap[1];
                }
            }

            /// <summary>
            /// Removes and returns the least element of the PriorityQueue in log(size)
            /// time.
            /// </summary>
            /// <returns></returns>
            public Int64IteratorNode Pop()
            {
                if (m_size > 0)
                {
                    Int64IteratorNode result = m_heap[1]; // save first value
                    m_heap[1] = m_heap[m_size]; // move last to first
                    m_heap[m_size] = null; // permit GC of objects
                    m_size--;
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

            public Int64IteratorNode UpdateTop()
            {
                DownHeap();
                return m_heap[1];
            }

            /// <summary>
            /// Returns the number of elements currently stored in the PriorityQueue.
            /// </summary>
            /// <returns></returns>
            // BoboBrowse.Net: we use Count instead of Size() in .NET
            public int Count
            {
                get { return m_size; }
            }

            /// <summary>
            /// Removes all entries from the PriorityQueue.
            /// </summary>
            public void Clear()
            {
                for (int i = 0; i <= m_size; i++)
                {
                    m_heap[i] = null;
                }
                m_size = 0;
            }

            private void UpHeap()
            {
                int i = m_size;
                Int64IteratorNode node = m_heap[i]; // save bottom node
                int j = (int)(((uint)i) >> 1);
                while (j > 0 && (node.CurFacet < m_heap[j].CurFacet))
                {
                    m_heap[i] = m_heap[j]; // shift parents down
                    i = j;
                    j = (int)(((uint)j) >> 1);
                }
                m_heap[i] = node; // install saved node
            }

            private void DownHeap()
            {
                int i = 1;
                Int64IteratorNode node = m_heap[i]; // save top node
                int j = i << 1; // find smaller child
                int k = j + 1;
                if (k <= m_size && (m_heap[k].CurFacet < m_heap[j].CurFacet))
                {
                    j = k;
                }
                while (j <= m_size && (m_heap[j].CurFacet < node.CurFacet))
                {
                    m_heap[i] = m_heap[j]; // shift up child
                    i = j;
                    j = i << 1;
                    k = j + 1;
                    if (k <= m_size && (m_heap[k].CurFacet < m_heap[j].CurFacet))
                    {
                        j = k;
                    }
                }
                m_heap[i] = node; // install saved node
            }
        }

        public override long NextLong()
        {
            if (!HasNext())
                throw new IndexOutOfRangeException("No more facets in this iteration");

            Int64IteratorNode node = _queue.Top;

            m_facet = node.CurFacet;
            long next = TermInt64List.VALUE_MISSING;
            m_count = 0;
            while (HasNext())
            {
                node = _queue.Top;
                next = node.CurFacet;
                if ((next != TermInt64List.VALUE_MISSING) && (next != m_facet))
                {
                    return m_facet;
                }
                m_count += node.CurFacetCount;
                if (node.Fetch(1))
                    _queue.UpdateTop();
                else
                    _queue.Pop();
            }
            return TermInt64List.VALUE_MISSING;
        }

        public override long NextLong(int minHits)
        {
            int qsize = _queue.Count;
            if (qsize == 0)
            {
                m_facet = TermInt64List.VALUE_MISSING;
                m_count = 0;
                return TermInt64List.VALUE_MISSING;
            }

            Int64IteratorNode node = _queue.Top;
            m_facet = node.CurFacet;
            m_count = node.CurFacetCount;
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
                        node = _queue.Top;
                    }
                    else
                    {
                        // we reached the end. check if this _facet obeys the minHits
                        if (m_count < minHits)
                        {
                            m_facet = TermInt64List.VALUE_MISSING;
                            m_count = 0;
                        }
                        break;
                    }
                }
                long next = node.CurFacet;
                if (next != m_facet)
                {
                    // check if this _facet obeys the minHits
                    if (m_count >= minHits)
                        break;
                    // else, continue iterating to the next _facet
                    m_facet = next;
                    m_count = node.CurFacetCount;
                }
                else
                {
                    m_count += node.CurFacetCount;
                }
            }
            return m_facet;
        }
    }
}

  