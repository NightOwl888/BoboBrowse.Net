// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class CombinedIntFacetIterator : IntFacetIterator
    {
        public class IntIteratorNode
        {
            public IntFacetIterator _iterator;
            public int _curFacet;
            public int _curFacetCount;

            public IntIteratorNode(IntFacetIterator iterator)
            {
                _iterator = iterator;
                _curFacet = TermIntList.VALUE_MISSING;
                _curFacetCount = 0;
            }

            public bool Fetch(int minHits)
            {
                if (minHits > 0)
                    minHits = 1;
                if ((_curFacet = _iterator.NextInt(minHits)) != TermIntList.VALUE_MISSING)
                {
                    _curFacetCount = _iterator.Count;
                    return true;
                }
                _curFacet = TermIntList.VALUE_MISSING;
                _curFacetCount = 0;
                return false;
            }

            public string Peek()//bad
            {
                throw new NotSupportedException();
                //      if(_iterator.hasNext()) 
                //      {
                //        return _iterator.getFacet();
                //      }
                //      return null;
            }
        }

        private readonly IntFacetPriorityQueue _queue;

        private IList<IntFacetIterator> _iterators;

        private CombinedIntFacetIterator(int length)
        {
            _queue = new IntFacetPriorityQueue();
            _queue.Initialize(length);
        }

        public CombinedIntFacetIterator(IList<IntFacetIterator> iterators)
            : this(iterators.Count)
        {
            _iterators = iterators;
            foreach (IntFacetIterator iterator in iterators)
            {
                IntIteratorNode node = new IntIteratorNode(iterator);
                if (node.Fetch(1))
                    _queue.Add(node);
            }
            _facet = TermIntList.VALUE_MISSING;
            _count = 0;
        }

        public CombinedIntFacetIterator(IList<IntFacetIterator> iterators, int minHits)
            : this(iterators.Count)
        {
            _iterators = iterators;
            foreach (IntFacetIterator iterator in iterators)
            {
                IntIteratorNode node = new IntIteratorNode(iterator);
                if (node.Fetch(minHits))
                    _queue.Add(node);
            }
            _facet = TermIntList.VALUE_MISSING;
            _count = 0;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#getFacet()
        /// </summary>
        /// <returns></returns>
        public virtual string GetFacet()
        {
            if (_facet == TermIntList.VALUE_MISSING) return null;
            return Format(_facet);
        }
        public override string Format(int val)
        {
            return _iterators[0].Format(val);
        }
        public override string Format(Object val)
        {
            return _iterators[0].Format(val);
        }
        /* (non-Javadoc)
         * @see com.browseengine.bobo.api.FacetIterator#getFacetCount()
         */
        public virtual int FacetCount
        {
            get { return _count; }
        }

        /* (non-Javadoc)
         * @see com.browseengine.bobo.api.FacetIterator#next()
         */
        public override string Next()
        {
            if (!HasNext())
                throw new IndexOutOfRangeException("No more facets in this iteration");

            IntIteratorNode node = _queue.Top();

            _facet = node._curFacet;
            int next = TermIntList.VALUE_MISSING;
            _count = 0;
            while (HasNext())
            {
                node = _queue.Top();
                next = node._curFacet;
                if ((next != TermIntList.VALUE_MISSING) && (next != _facet))
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
         * This version of the next() method applies the minHits from the facet spec before returning the facet and its hitcount
         * @param minHits the minHits from the facet spec for CombinedFacetAccessible
         * @return        The next facet that obeys the minHits 
         */
        public override string Next(int minHits)
        {
            int qsize = _queue.Size();
            if (qsize == 0)
            {
                _facet = TermIntList.VALUE_MISSING;
                _count = 0;
                return null;
            }

            IntIteratorNode node = _queue.Top();
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
                            _facet = TermIntList.VALUE_MISSING;
                            _count = 0;
                            return null;
                        }
                        break;
                    }
                }
                int next = node._curFacet;
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

        /* (non-Javadoc)
         * @see java.util.Iterator#hasNext()
         */
        public virtual bool HasNext()
        {
            return (_queue.Size() > 0);
        }

        /* (non-Javadoc)
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
        public class IntFacetPriorityQueue
        {
            private int size;
            private int maxSize;
            protected IntIteratorNode[] heap;

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
                heap = new IntIteratorNode[heapSize];
                this.maxSize = maxSize;
            }

            public void Put(IntIteratorNode element)
            {
                size++;
                heap[size] = element;
                UpHeap();
            }

            public IntIteratorNode Add(IntIteratorNode element)
            {
                size++;
                heap[size] = element;
                UpHeap();
                return heap[1];
            }

            public virtual bool Insert(IntIteratorNode element)
            {
                return InsertWithOverflow(element) != element;
            }

            public virtual IntIteratorNode InsertWithOverflow(IntIteratorNode element)
            {
                if (size < maxSize)
                {
                    Put(element);
                    return null;
                }
                else if (size > 0 && !(element._curFacet < heap[1]._curFacet))
                {
                    IntIteratorNode ret = heap[1];
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
            public IntIteratorNode Top()
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
            public IntIteratorNode Pop()
            {
                if (size > 0)
                {
                    IntIteratorNode result = heap[1]; // save first value
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

            public IntIteratorNode UpdateTop()
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
                IntIteratorNode node = heap[i]; // save bottom node
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
                IntIteratorNode node = heap[i]; // save top node
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

        /** (non-Javadoc)
         * @see com.browseengine.bobo.api.IntFacetIterator#nextInt()
         */
        public override int NextInt()
        {
            if (!HasNext())
                throw new IndexOutOfRangeException("No more facets in this iteration");

            IntIteratorNode node = _queue.Top();

            _facet = node._curFacet;
            int next = TermIntList.VALUE_MISSING;
            _count = 0;
            while (HasNext())
            {
                node = _queue.Top();
                next = node._curFacet;
                if ((next != TermIntList.VALUE_MISSING) && (next != _facet))
                {
                    return _facet;
                }
                _count += node._curFacetCount;
                if (node.Fetch(1))
                    _queue.UpdateTop();
                else
                    _queue.Pop();
            }
            return TermIntList.VALUE_MISSING;
        }


        public override int NextInt(int minHits)
        {
            int qsize = _queue.Size();
            if (qsize == 0)
            {
                _facet = TermIntList.VALUE_MISSING;
                _count = 0;
                return TermIntList.VALUE_MISSING;
            }

            IntIteratorNode node = _queue.Top();
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
                            _facet = TermIntList.VALUE_MISSING;
                            _count = 0;
                        }
                        break;
                    }
                }
                int next = node._curFacet;
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
