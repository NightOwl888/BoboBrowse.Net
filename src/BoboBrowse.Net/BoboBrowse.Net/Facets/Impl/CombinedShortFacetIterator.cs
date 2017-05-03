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
    /// author "Xiaoyang Gu &lt;xgu@linkedin.com&gt;"
    /// </summary>
    public class CombinedShortFacetIterator : ShortFacetIterator
    {
        public class ShortIteratorNode
        {
            private readonly ShortFacetIterator m_iterator;
            protected short m_curFacet;
            protected int m_curFacetCount;

            public ShortIteratorNode(ShortFacetIterator iterator)
            {
                m_iterator = iterator;
                m_curFacet = TermShortList.VALUE_MISSING;
                m_curFacetCount = 0;
            }

            /// <summary>
            /// Added in .NET version as an accessor to the _iterator field.
            /// </summary>
            /// <returns></returns>
            public virtual ShortFacetIterator GetIterator()
            {
                return m_iterator;
            }

            /// <summary>
            /// Added in .NET version as an accessor to the _curFacet field.
            /// </summary>
            public virtual short CurFacet
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
                if ((m_curFacet = m_iterator.NextShort(minHits)) != TermShortList.VALUE_MISSING)
                {
                    m_curFacetCount = m_iterator.Count;
                    return true;
                }
                m_curFacet = TermShortList.VALUE_MISSING;
                m_curFacetCount = 0;
                return false;
            }
        }

        private readonly ShortFacetPriorityQueue _queue;

        private IList<ShortFacetIterator> _iterators;

        private CombinedShortFacetIterator(int length)
        {
            _queue = new ShortFacetPriorityQueue();
            _queue.Initialize(length);
        }

        public CombinedShortFacetIterator(IList<ShortFacetIterator> iterators)
            : this(iterators.Count)
        {
            _iterators = iterators;
            foreach (ShortFacetIterator iterator in iterators)
            {
                ShortIteratorNode node = new ShortIteratorNode(iterator);
                if (node.Fetch(1))
                    _queue.Add(node);
            }
            m_facet = TermShortList.VALUE_MISSING;
            m_count = 0;
        }

        public CombinedShortFacetIterator(List<ShortFacetIterator> iterators, int minHits)
            : this(iterators.Count)
        {
            _iterators = iterators;
            foreach (ShortFacetIterator iterator in iterators)
            {
                ShortIteratorNode node = new ShortIteratorNode(iterator);
                if (node.Fetch(minHits))
                    _queue.Add(node);
            }
            m_facet = TermShortList.VALUE_MISSING;
            m_count = 0;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#getFacet()
        /// </summary>
        /// <returns></returns>
        public virtual string GetFacet()
        {
            if (m_facet == -1) return null;
            return Format(m_facet);
        }
        public override string Format(short val)
        {
            return _iterators[0].Format(val);
        }
        public override string Format(object val)
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

            ShortIteratorNode node = _queue.Top;

            m_facet = node.CurFacet;
            int next = TermShortList.VALUE_MISSING;
            m_count = 0;
            while (HasNext())
            {
                node = _queue.Top;
                next = node.CurFacet;
                if ((next != TermShortList.VALUE_MISSING) && (next != m_facet))
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
        /// <returns>The next _facet that obeys the minHits</returns>
        public override string Next(int minHits)
        {
            int qsize = _queue.Count;
            if (qsize == 0)
            {
                m_facet = TermShortList.VALUE_MISSING;
                m_count = 0;
                return null;
            }

            ShortIteratorNode node = _queue.Top;
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
                            m_facet = TermShortList.VALUE_MISSING;
                            m_count = 0;
                            return null;
                        }
                        break;
                    }
                }
                short next = node.CurFacet;
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
        /// </summary>
        public class ShortFacetPriorityQueue
        {
            private int m_size;
            private int m_maxSize;
            protected ShortIteratorNode[] m_heap;

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
                m_heap = new ShortIteratorNode[heapSize];
                this.m_maxSize = maxSize;
            }

            public void Put(ShortIteratorNode element)
            {
                m_size++;
                m_heap[m_size] = element;
                UpHeap();
            }

            public ShortIteratorNode Add(ShortIteratorNode element)
            {
                m_size++;
                m_heap[m_size] = element;
                UpHeap();
                return m_heap[1];
            }

            public virtual bool Insert(ShortIteratorNode element)
            {
                return InsertWithOverflow(element) != element;
            }

            public virtual ShortIteratorNode InsertWithOverflow(ShortIteratorNode element)
            {
                if (m_size < m_maxSize)
                {
                    Put(element);
                    return null;
                }
                else if (m_size > 0 && !(element.CurFacet < m_heap[1].CurFacet))
                {
                    ShortIteratorNode ret = m_heap[1];
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
            public ShortIteratorNode Top
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
            public ShortIteratorNode Pop()
            {
                if (m_size > 0)
                {
                    ShortIteratorNode result = m_heap[1]; // save first value
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

            public ShortIteratorNode UpdateTop()
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
                ShortIteratorNode node = m_heap[i]; // save bottom node
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
                ShortIteratorNode node = m_heap[i]; // save top node
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

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.ShortFacetIterator#nextShort()
        /// </summary>
        /// <returns></returns>
        public override short NextShort()
        {
            if (!HasNext())
                throw new IndexOutOfRangeException("No more facets in this iteration");

            ShortIteratorNode node = _queue.Top;

            m_facet = node.CurFacet;
            int next = TermShortList.VALUE_MISSING;
            m_count = 0;
            while (HasNext())
            {
                node = _queue.Top;
                next = node.CurFacet;
                if ((next != -1) && (next != m_facet))
                {
                    return m_facet;
                }
                m_count += node.CurFacetCount;
                if (node.Fetch(1))
                    _queue.UpdateTop();
                else
                    _queue.Pop();
            }
            return TermShortList.VALUE_MISSING;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.ShortFacetIterator#nextShort(int)
        /// </summary>
        /// <param name="minHits"></param>
        /// <returns></returns>
        public override short NextShort(int minHits)
        {
            int qsize = _queue.Count;
            if (qsize == 0)
            {
                m_facet = TermShortList.VALUE_MISSING;
                m_count = 0;
                return TermShortList.VALUE_MISSING;
            }

            ShortIteratorNode node = _queue.Top;
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
                            m_facet = TermShortList.VALUE_MISSING;
                            m_count = 0;
                        }
                        break;
                    }
                }
                short next = node.CurFacet;
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
