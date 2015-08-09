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

// Version compatibility level: 3.2.0
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
        private FacetIterator[] heap;
        private int size;
        internal IList<FacetIterator> _iterators;

        public CombinedFacetIterator(IList<FacetIterator> iterators)
        {
            _iterators = iterators;
            heap = new FacetIterator[iterators.Count + 1];
            size = 0;
            foreach (FacetIterator iterator in iterators)
            {
                if (iterator.Next(0) != null)
                    Add(iterator);
            }
            facet = null;
            count = 0;
        }

        private void Add(FacetIterator element)
        {
            size++;
            heap[size] = element;
            UpHeap();
        }

        private void UpHeap()
        {
            int i = size;
            FacetIterator node = heap[i];   // save bottom node
            var val = node.Facet;
            int j = (int)(((uint)i) >> 1);
            //while (j > 0 && val.CompareTo(heap[j].Facet) < 0)
            while (j > 0 && string.CompareOrdinal(val, heap[j].Facet) < 0)
            {
                heap[i] = heap[j];          // shift parents down
                i = j;
                j = (int)(((uint)j) >> 1);
            }
            heap[i] = node;                 // install saved node
        }

        private void DownHeap()
        {
            int i = 1;
            FacetIterator node = heap[i];   // save top node
            var val = node.Facet;
            int j = i << 1;                 // find smaller child
            int k = j + 1;
            if (k <= size && string.CompareOrdinal(heap[k].Facet, heap[j].Facet) < 0)
            {
                j = k;
            }
            while (j <= size && string.CompareOrdinal(heap[j].Facet, val) < 0)
            {
                heap[i] = heap[j];          // shift up child
                i = j;
                j = i << 1;
                k = j + 1;
                if (k <= size && string.CompareOrdinal(heap[k].Facet, heap[j].Facet) < 0)
                {
                    j = k;
                }
            }
            heap[i] = node;                 // install saved node
        }

        private void Pop()
        {
            if (size > 0)
            {
                heap[1] = heap[size];       // move last to first
                heap[size] = null;          // permit GC of objects
                if (--size > 0) DownHeap(); // adjust heap
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
            if (size == 0)
            {
                facet = null;
                count = 0;
                return null;
            }

            FacetIterator node = heap[1];
            facet = node.Facet;
            count = node.Count;
            int min = (minHits > 0 ? 1 : 0);
            while (true)
            {
                // NOTE: In the original version, we were just comparing against
                // a null string, but the Format method could return an empty string.
                if (!string.IsNullOrEmpty(node.Next(min)))
                {
                    DownHeap();
                    node = heap[1];
                }
                else
                {
                    Pop();
                    if (size > 0)
                    {
                        node = heap[1];
                    }
                    else
                    {
                        // we reached the end. check if this facet obeys the minHits
                        if (count < minHits)
                        {
                            facet = null;
                            count = 0;
                        }
                        break;
                    }
                }
                var next = node.Facet;
                if (next == null) throw new RuntimeException();
                if (!next.Equals(facet))
                {
                    // check if this facet obeys the minHits
                    if (count >= minHits)
                        break;
                    // else, continue iterating to the next facet
                    facet = next;
                    count = node.Count;
                }
                else
                {
                    count += node.Count;
                }
            }
            return Format(facet);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#hasNext()
        /// </summary>
        /// <returns></returns>
        public override bool HasNext()
        {
            return (size > 0);
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
            return _iterators[0].Format(val);
        }
    }
}
