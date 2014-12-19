// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
            _stringFacet = null;
            _count = 0;
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
            int j = (int)((uint)(i >> 1));
            while (j > 0 && val.CompareTo(heap[j].Facet) < 0)
            {
                heap[i] = heap[j];          // shift parents down
                i = j;
                j = (int)((uint)(j >> 1));
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
            if (k <= size && heap[k].Facet.CompareTo(heap[j].Facet) < 0)
            {
                j = k;
            }
            while (j <= size && heap[j].Facet.CompareTo(val) < 0)
            {
                heap[i] = heap[j];          // shift up child
                i = j;
                j = i << 1;
                k = j + 1;
                if (k <= size && heap[k].Facet.CompareTo(heap[j].Facet) < 0)
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

        public override string Next(int minHits)
        {
            if (size == 0)
            {
                _stringFacet = null;
                _count = 0;
                return null;
            }

            FacetIterator node = heap[1];
            _stringFacet = node.Facet;
            _count = node.Count;
            int min = (minHits > 0 ? 1 : 0);
            while (true)
            {
                if (node.Next(min) != null)
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
                        if (_count < minHits)
                        {
                            _stringFacet = null;
                            _count = 0;
                        }
                        break;
                    }
                }
                var next = node.Facet;
                if (next == null) throw new RuntimeException();
                if (!next.Equals(_stringFacet))
                {
                    // check if this facet obeys the minHits
                    if (_count >= minHits)
                        break;
                    // else, continue iterating to the next facet
                    _stringFacet = next;
                    _count = node.Count;
                }
                else
                {
                    _count += node.Count;
                }
            }
            return Format(_stringFacet);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#hasNext()
        /// </summary>
        /// <returns></returns>
        public virtual bool HasNext()
        {
            return (size > 0);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#remove()
        /// </summary>
        public virtual void Remove()
        {
            throw new NotSupportedException("remove() method not supported for Facet Iterators");
        }

        public override string Format(object val)
        {
            return _iterators[0].Format(val);
        }
    }
}
