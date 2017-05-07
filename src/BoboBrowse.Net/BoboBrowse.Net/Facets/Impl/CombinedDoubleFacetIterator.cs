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
    public class CombinedDoubleFacetIterator : DoubleFacetIterator
    {
        public class DoubleIteratorNode
        {
            private readonly DoubleFacetIterator m_iterator;
            protected double m_curFacet;
            protected int m_curFacetCount;

            public DoubleIteratorNode(DoubleFacetIterator iterator)
            {
                m_iterator = iterator;
                m_curFacet = TermDoubleList.VALUE_MISSING;
                m_curFacetCount = 0;
            }

            /// <summary>
            /// Added in .NET version as an accessor to the _iterator field.
            /// </summary>
            /// <returns></returns>
            public virtual DoubleFacetIterator GetIterator()
            {
                return m_iterator;
            }

            /// <summary>
            /// Added in .NET version as an accessor to the _curFacet field.
            /// </summary>
            public virtual double CurFacet
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
                if ((m_curFacet = m_iterator.NextDouble(minHits)) != TermDoubleList.VALUE_MISSING)
                {
                    m_curFacetCount = m_iterator.Count;
                    return true;
                }
                m_curFacet = TermDoubleList.VALUE_MISSING;
                m_curFacetCount = 0;
                return false;
            }
        }

        private readonly DoubleFacetPriorityQueue m_queue;

        private IList<DoubleFacetIterator> m_iterators;

        private CombinedDoubleFacetIterator(int length)
        {
            m_queue = new DoubleFacetPriorityQueue();
            m_queue.Initialize(length);
        }

        public CombinedDoubleFacetIterator(IList<DoubleFacetIterator> iterators)
            : this(iterators.Count)
        {
            m_iterators = iterators;
            foreach (DoubleFacetIterator iterator in iterators)
            {
                DoubleIteratorNode node = new DoubleIteratorNode(iterator);
                if (node.Fetch(1))
                    m_queue.Add(node);
            }
            m_facet = TermDoubleList.VALUE_MISSING;
            m_count = 0;
        }

        public CombinedDoubleFacetIterator(IList<DoubleFacetIterator> iterators, int minHits)
            : this(iterators.Count)
        {
            m_iterators = iterators;
            foreach (DoubleFacetIterator iterator in iterators)
            {
                DoubleIteratorNode node = new DoubleIteratorNode(iterator);
                if (node.Fetch(minHits))
                    m_queue.Add(node);
            }
            m_facet = TermDoubleList.VALUE_MISSING;
            m_count = 0;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#getFacet()
        /// </summary>
        /// <returns></returns>
        public virtual string GetFacet()
        {
            if (m_facet == TermDoubleList.VALUE_MISSING) return null;
            return Format(m_facet);
        }

        public override string Format(double val)
        {
            return m_iterators[0].Format(val);
        }

        public override string Format(Object val)
        {
            return m_iterators[0].Format(val);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#getFacetCount()
        /// </summary>
        /// <returns></returns>
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

            DoubleIteratorNode node = m_queue.Top;

            m_facet = node.CurFacet;
            double next = TermDoubleList.VALUE_MISSING;
            m_count = 0;
            while (HasNext())
            {
                node = m_queue.Top;
                next = node.CurFacet;
                if ((next != TermDoubleList.VALUE_MISSING) && (next != m_facet))
                {
                    return Format(m_facet);
                }
                m_count += node.CurFacetCount;
                if (node.Fetch(1))
                    m_queue.UpdateTop();
                else
                    m_queue.Pop();
            }
            return null;
        }

        /// <summary>
        /// This version of the next() method applies the minHits from the facet spec
        /// before returning the facet and its hitcount
        /// </summary>
        /// <param name="minHits">the minHits from the facet spec for CombinedFacetAccessible</param>
        /// <returns>The next facet that obeys the minHits</returns>
        public override string Next(int minHits)
        {
            int qsize = m_queue.Count;
            if (qsize == 0)
            {
                m_facet = TermDoubleList.VALUE_MISSING;
                m_count = 0;
                return null;
            }

            DoubleIteratorNode node = m_queue.Top;
            m_facet = node.CurFacet;
            m_count = node.CurFacetCount;
            while (true)
            {
                if (node.Fetch(minHits))
                {
                    node = m_queue.UpdateTop();
                }
                else
                {
                    m_queue.Pop();
                    if (--qsize > 0)
                    {
                        node = m_queue.Pop();
                    }
                    else
                    {
                        // we reached the end. check if this facet obeys the minHits
                        if (m_count < minHits)
                        {
                            m_facet = TermDoubleList.VALUE_MISSING;
                            m_count = 0;
                            return null;
                        }
                        break;
                    }
                }
                double next = node.CurFacet;
                if (next != m_facet)
                {
                    // check if this facet obeys the minHits
                    if (m_count >= minHits)
                        break;
                    // else, continue iterating to the next facet
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
            return (m_queue.Count > 0);
        }

        // BoboBrowse.Net: Not supported in .NET anyway
        ///// <summary>
        ///// (non-Javadoc)
        ///// see java.util.Iterator#remove()
        ///// </summary>
        //public override void Remove()
        //{
        //    throw new NotSupportedException("remove() method not supported for Facet Iterators");
        //}

        /// <summary>
        /// Lucene PriorityQueue
        /// </summary>
        public class DoubleFacetPriorityQueue
        {
            private int m_size;
            private int m_maxSize;
            protected DoubleIteratorNode[] m_heap;

            /** Subclass constructors must call this. */
            public void Initialize(int maxSize)
            {
                m_size = 0;
                int heapSize;
                if (0 == maxSize)
                    // We allocate 1 extra to avoid if statement in top()
                    heapSize = 2;
                else
                    heapSize = maxSize + 1;
                m_heap = new DoubleIteratorNode[heapSize];
                this.m_maxSize = maxSize;
            }

            public void Put(DoubleIteratorNode element)
            {
                m_size++;
                m_heap[m_size] = element;
                UpHeap();
            }

            public DoubleIteratorNode Add(DoubleIteratorNode element)
            {
                m_size++;
                m_heap[m_size] = element;
                UpHeap();
                return m_heap[1];
            }

            public virtual bool Insert(DoubleIteratorNode element)
            {
                return InsertWithOverflow(element) != element;
            }

            public virtual DoubleIteratorNode InsertWithOverflow(DoubleIteratorNode element)
            {
                if (m_size < m_maxSize)
                {
                    Put(element);
                    return null;
                }
                else if (m_size > 0 && !(element.CurFacet < m_heap[1].CurFacet))
                {
                    DoubleIteratorNode ret = m_heap[1];
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
            public DoubleIteratorNode Top
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
            public DoubleIteratorNode Pop()
            {
                if (m_size > 0)
                {
                    DoubleIteratorNode result = m_heap[1]; // save first value
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

            public DoubleIteratorNode UpdateTop()
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
                DoubleIteratorNode node = m_heap[i]; // save bottom node
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
                DoubleIteratorNode node = m_heap[i]; // save top node
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

        public override double NextDouble()
        {
            if (!HasNext())
                throw new IndexOutOfRangeException("No more facets in this iteration");

            DoubleIteratorNode node = m_queue.Top;

            m_facet = node.CurFacet;
            double next = TermDoubleList.VALUE_MISSING;
            m_count = 0;
            while (HasNext())
            {
                node = m_queue.Top;
                next = node.CurFacet;
                if ((next != TermDoubleList.VALUE_MISSING) && (next != m_facet))
                {
                    return m_facet;
                }
                m_count += node.CurFacetCount;
                if (node.Fetch(1))
                    m_queue.UpdateTop();
                else
                    m_queue.Pop();
            }
            return TermDoubleList.VALUE_MISSING;
        }

        public override double NextDouble(int minHits)
        {
            int qsize = m_queue.Count;
            if (qsize == 0)
            {
                m_facet = TermDoubleList.VALUE_MISSING;
                m_count = 0;
                return TermDoubleList.VALUE_MISSING;
            }

            DoubleIteratorNode node = m_queue.Top;
            m_facet = node.CurFacet;
            m_count = node.CurFacetCount;
            while (true)
            {
                if (node.Fetch(minHits))
                {
                    node = m_queue.UpdateTop();
                }
                else
                {
                    m_queue.Pop();
                    if (--qsize > 0)
                    {
                        node = m_queue.Top;
                    }
                    else
                    {
                        // we reached the end. check if this facet obeys the minHits
                        if (m_count < minHits)
                        {
                            m_facet = TermDoubleList.VALUE_MISSING;
                            m_count = 0;
                        }
                        break;
                    }
                }
                double next = node.CurFacet;
                if (next != m_facet)
                {
                    // check if this facet obeys the minHits
                    if (m_count >= minHits)
                        break;
                    // else, continue iterating to the next facet
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
