// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// author "Xiaoyang Gu<xgu@linkedin.com>"
    /// </summary>
    public class CombinedDoubleFacetIterator : DoubleFacetIterator
    {
        public class DoubleIteratorNode
        {
            public DoubleFacetIterator _iterator;
            public double _curFacet;
            public int _curFacetCount;

            public DoubleIteratorNode(DoubleFacetIterator iterator)
            {
                _iterator = iterator;
                _curFacet = TermDoubleList.VALUE_MISSING;
                _curFacetCount = 0;
            }

            public bool Fetch(int minHits)
            {
                if (minHits > 0)
                    minHits = 1;
                if ((_curFacet = _iterator.NextDouble(minHits)) != TermDoubleList.VALUE_MISSING)
                {
                    _curFacetCount = _iterator.Count;
                    return true;
                }
                _curFacet = TermDoubleList.VALUE_MISSING;
                _curFacetCount = 0;
                return false;
            }

            public string Peek()// bad
            {
                throw new NotSupportedException();
                // if(_iterator.hasNext())
                // {
                // return _iterator.getFacet();
                // }
                // return null;
            }
        }

        private readonly DoubleFacetPriorityQueue _queue;

        private IList<DoubleFacetIterator> _iterators;

        private CombinedDoubleFacetIterator(int length)
        {
            _queue = new DoubleFacetPriorityQueue();
            _queue.Initialize(length);
        }

        public CombinedDoubleFacetIterator(IList<DoubleFacetIterator> iterators)
            : this(iterators.Count)
        {
            _iterators = iterators;
            foreach (DoubleFacetIterator iterator in iterators)
            {
                DoubleIteratorNode node = new DoubleIteratorNode(iterator);
                if (node.Fetch(1))
                    _queue.Add(node);
            }
            _facet = TermDoubleList.VALUE_MISSING;
            _count = 0;
        }

        public CombinedDoubleFacetIterator(IList<DoubleFacetIterator> iterators, int minHits)
            : this(iterators.Count)
        {
            _iterators = iterators;
            foreach (DoubleFacetIterator iterator in iterators)
            {
                DoubleIteratorNode node = new DoubleIteratorNode(iterator);
                if (node.Fetch(minHits))
                    _queue.Add(node);
            }
            _facet = TermDoubleList.VALUE_MISSING;
            _count = 0;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#getFacet()
        /// </summary>
        /// <returns></returns>
        public virtual string GetFacet()
        {
            if (_facet == TermDoubleList.VALUE_MISSING) return null;
            return Format(_facet);
        }

        public override string Format(double val)
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
        /// <returns></returns>
        public virtual int FacetCount
        {
            get { return _count; }
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

            DoubleIteratorNode node = _queue.Top();

            _facet = node._curFacet;
            double next = TermDoubleList.VALUE_MISSING;
            _count = 0;
            while (HasNext())
            {
                node = _queue.Top();
                next = node._curFacet;
                if ((next != TermDoubleList.VALUE_MISSING) && (next != _facet))
                {
                    return Format(_facet);
                }
                _count += node._curFacetCount;
                if (node.Fetch(1))
                    _queue.UpdateTop();
                else
                    _queue.Pop();
            }
            return null;
        }

        /**
         * This version of the next() method applies the minHits from the facet spec
         * before returning the facet and its hitcount
         * 
         * @param minHits
         *          the minHits from the facet spec for CombinedFacetAccessible
         * @return The next facet that obeys the minHits
         */
        public override string Next(int minHits)
        {
            int qsize = _queue.Size();
            if (qsize == 0)
            {
                _facet = TermDoubleList.VALUE_MISSING;
                _count = 0;
                return null;
            }

            DoubleIteratorNode node = _queue.Top();
            _facet = node._curFacet;
            _count = node._curFacetCount;
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
                        node = _queue.Pop();
                    }
                    else
                    {
                        // we reached the end. check if this facet obeys the minHits
                        if (_count < minHits)
                        {
                            _facet = TermDoubleList.VALUE_MISSING;
                            _count = 0;
                            return null;
                        }
                        break;
                    }
                }
                double next = node._curFacet;
                if (next != _facet)
                {
                    // check if this facet obeys the minHits
                    if (_count >= minHits)
                        break;
                    // else, continue iterating to the next facet
                    _facet = next;
                    _count = node._curFacetCount;
                }
                else
                {
                    _count += node._curFacetCount;
                }
            }
            return Format(_facet);
        }

        /*
         * (non-Javadoc)
         * 
         * @see java.util.Iterator#hasNext()
         */
        public virtual bool HasNext()
        {
            return (_queue.Size() > 0);
        }

        /*
         * (non-Javadoc)
         * 
         * @see java.util.Iterator#remove()
         */
        public virtual void Remove()
        {
            throw new NotSupportedException("remove() method not supported for Facet Iterators");
        }

        /**
         * Lucene PriorityQueue
         * 
         */
        public class DoubleFacetPriorityQueue
        {
            private int size;
            private int maxSize;
            protected DoubleIteratorNode[] heap;

            /** Subclass constructors must call this. */
            public void Initialize(int maxSize)
            {
                size = 0;
                int heapSize;
                if (0 == maxSize)
                    // We allocate 1 extra to avoid if statement in top()
                    heapSize = 2;
                else
                    heapSize = maxSize + 1;
                heap = new DoubleIteratorNode[heapSize];
                this.maxSize = maxSize;
            }

            public void Put(DoubleIteratorNode element)
            {
                size++;
                heap[size] = element;
                UpHeap();
            }

            public DoubleIteratorNode Add(DoubleIteratorNode element)
            {
                size++;
                heap[size] = element;
                UpHeap();
                return heap[1];
            }

            public virtual bool Insert(DoubleIteratorNode element)
            {
                return InsertWithOverflow(element) != element;
            }

            public virtual DoubleIteratorNode InsertWithOverflow(DoubleIteratorNode element)
            {
                if (size < maxSize)
                {
                    Put(element);
                    return null;
                }
                else if (size > 0 && !(element._curFacet < heap[1]._curFacet))
                {
                    DoubleIteratorNode ret = heap[1];
                    heap[1] = element;
                    AdjustTop();
                    return ret;
                }
                else
                {
                    return element;
                }
            }

            /** Returns the least element of the PriorityQueue in constant time. */
            public DoubleIteratorNode Top()
            {
                // We don't need to check size here: if maxSize is 0,
                // then heap is length 2 array with both entries null.
                // If size is 0 then heap[1] is already null.
                return heap[1];
            }

            /**
             * Removes and returns the least element of the PriorityQueue in log(size)
             * time.
             */
            public DoubleIteratorNode Pop()
            {
                if (size > 0)
                {
                    DoubleIteratorNode result = heap[1]; // save first value
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

            public DoubleIteratorNode UpdateTop()
            {
                DownHeap();
                return heap[1];
            }

            /** Returns the number of elements currently stored in the PriorityQueue. */
            public int Size()
            {
                return size;
            }

            /** Removes all entries from the PriorityQueue. */
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
                DoubleIteratorNode node = heap[i]; // save bottom node
                int j = (int)(((uint)i) >> 1);
                while (j > 0 && (node._curFacet < heap[j]._curFacet))
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
                DoubleIteratorNode node = heap[i]; // save top node
                int j = i << 1; // find smaller child
                int k = j + 1;
                if (k <= size && (heap[k]._curFacet < heap[j]._curFacet))
                {
                    j = k;
                }
                while (j <= size && (heap[j]._curFacet < node._curFacet))
                {
                    heap[i] = heap[j]; // shift up child
                    i = j;
                    j = i << 1;
                    k = j + 1;
                    if (k <= size && (heap[k]._curFacet < heap[j]._curFacet))
                    {
                        j = k;
                    }
                }
                heap[i] = node; // install saved node
            }
        }

        public override double NextDouble()
        {
            if (!HasNext())
                throw new IndexOutOfRangeException("No more facets in this iteration");

            DoubleIteratorNode node = _queue.Top();

            _facet = node._curFacet;
            double next = TermDoubleList.VALUE_MISSING;
            _count = 0;
            while (HasNext())
            {
                node = _queue.Top();
                next = node._curFacet;
                if ((next != TermDoubleList.VALUE_MISSING) && (next != _facet))
                {
                    return _facet;
                }
                _count += node._curFacetCount;
                if (node.Fetch(1))
                    _queue.UpdateTop();
                else
                    _queue.Pop();
            }
            return TermDoubleList.VALUE_MISSING;
        }

        public override double NextDouble(int minHits)
        {
            int qsize = _queue.Size();
            if (qsize == 0)
            {
                _facet = TermDoubleList.VALUE_MISSING;
                _count = 0;
                return TermDoubleList.VALUE_MISSING;
            }

            DoubleIteratorNode node = _queue.Top();
            _facet = node._curFacet;
            _count = node._curFacetCount;
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
                        // we reached the end. check if this facet obeys the minHits
                        if (_count < minHits)
                        {
                            _facet = TermDoubleList.VALUE_MISSING;
                            _count = 0;
                        }
                        break;
                    }
                }
                double next = node._curFacet;
                if (next != _facet)
                {
                    // check if this facet obeys the minHits
                    if (_count >= minHits)
                        break;
                    // else, continue iterating to the next facet
                    _facet = next;
                    _count = node._curFacetCount;
                }
                else
                {
                    _count += node._curFacetCount;
                }
            }
            return _facet;
        }
    }
}
