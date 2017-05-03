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
    using BoboBrowse.Net.Support;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// author nnarkhed
    /// </summary>
    public class CombinedFacetIterator : FacetIterator
    {
        private readonly FacetIterator[] m_heap;
        private int m_size;
        internal IList<FacetIterator> m_iterators;

        public CombinedFacetIterator(IList<FacetIterator> iterators)
        {
            m_iterators = iterators;
            m_heap = new FacetIterator[iterators.Count + 1];
            m_size = 0;
            foreach (FacetIterator iterator in iterators)
            {
                if (iterator.Next(0) != null)
                    Add(iterator);
            }
            m_facet = null;
            m_count = 0;
        }

        private void Add(FacetIterator element)
        {
            m_size++;
            m_heap[m_size] = element;
            UpHeap();
        }

        private void UpHeap()
        {
            int i = m_size;
            FacetIterator node = m_heap[i];   // save bottom node
            var val = node.Facet;
            int j = (int)(((uint)i) >> 1);
            //while (j > 0 && val.CompareTo(heap[j].Facet) < 0)
            while (j > 0 && string.CompareOrdinal(val, m_heap[j].Facet) < 0)
            {
                m_heap[i] = m_heap[j];          // shift parents down
                i = j;
                j = (int)(((uint)j) >> 1);
            }
            m_heap[i] = node;                 // install saved node
        }

        private void DownHeap()
        {
            int i = 1;
            FacetIterator node = m_heap[i];   // save top node
            var val = node.Facet;
            int j = i << 1;                 // find smaller child
            int k = j + 1;
            if (k <= m_size && string.CompareOrdinal(m_heap[k].Facet, m_heap[j].Facet) < 0)
            {
                j = k;
            }
            while (j <= m_size && string.CompareOrdinal(m_heap[j].Facet, val) < 0)
            {
                m_heap[i] = m_heap[j];          // shift up child
                i = j;
                j = i << 1;
                k = j + 1;
                if (k <= m_size && string.CompareOrdinal(m_heap[k].Facet, m_heap[j].Facet) < 0)
                {
                    j = k;
                }
            }
            m_heap[i] = node;                 // install saved node
        }

        private void Pop()
        {
            if (m_size > 0)
            {
                m_heap[1] = m_heap[m_size];       // move last to first
                m_heap[m_size] = null;          // permit GC of objects
                if (--m_size > 0) DownHeap(); // adjust heap
            }
        }

        /// <summary>
        /// (non-Javadoc)
        /// @see FacetIterator#next()
        /// </summary>
        /// <returns></returns>
        public override string Next()
        {
            if (!HasNext())
                throw new IndexOutOfRangeException("No more facets in this iteration");

            return Next(1);
        }

        /// <summary>
        /// This version of the next() method applies the minHits from the facet spec before returning the facet and its hitcount
        /// </summary>
        /// <param name="minHits">the minHits from the facet spec for CombinedFacetAccessible</param>
        /// <returns>The next facet that obeys the minHits</returns>
        public override string Next(int minHits)
        {
            if (m_size == 0)
            {
                m_facet = null;
                m_count = 0;
                return null;
            }

            FacetIterator node = m_heap[1];
            m_facet = node.Facet;
            m_count = node.Count;
            int min = (minHits > 0 ? 1 : 0);
            while (true)
            {
                // NOTE: In the original version, we were just comparing against
                // a null string, but the Format method could return an empty string.
                if (!string.IsNullOrEmpty(node.Next(min)))
                {
                    DownHeap();
                    node = m_heap[1];
                }
                else
                {
                    Pop();
                    if (m_size > 0)
                    {
                        node = m_heap[1];
                    }
                    else
                    {
                        // we reached the end. check if this facet obeys the minHits
                        if (m_count < minHits)
                        {
                            m_facet = null;
                            m_count = 0;
                        }
                        break;
                    }
                }
                var next = node.Facet;
                if (next == null) throw new RuntimeException();
                if (!next.Equals(m_facet))
                {
                    // check if this facet obeys the minHits
                    if (m_count >= minHits)
                        break;
                    // else, continue iterating to the next facet
                    m_facet = next;
                    m_count = node.Count;
                }
                else
                {
                    m_count += node.Count;
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
            return (m_size > 0);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#remove()
        /// </summary>
        public override void Remove()
        {
            throw new NotSupportedException("remove() method not supported for Facet Iterators");
        }

        public override string Format(object val)
        {
            return m_iterators[0].Format(val);
        }
    }
}
