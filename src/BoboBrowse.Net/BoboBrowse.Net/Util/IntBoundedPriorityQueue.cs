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
namespace BoboBrowse.Net.Util
{
    //using C5;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class IntBoundedPriorityQueue //: IntervalHeap<int>
    {
        //private static long serialVersionUID = 1L; // NOT USED
        private readonly int _capacity;
        private readonly int[] _items;
        private int _size = 0;
        private IComparer<int> _comp;
        private readonly int _forbiddenValue;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="comparator">a comparator that is used to order the items.</param>
        /// <param name="capacity">the maximum number of items the queue accepts</param>
        /// <param name="forbiddenValue"></param>
        public IntBoundedPriorityQueue(IComparer<int> comparator, int capacity, int forbiddenValue)
        {
            _capacity = capacity;
            _comp = comparator;
            _items = new int[capacity];// java.lang.reflect.Array.newInstance(, capacity);
            _forbiddenValue = forbiddenValue;
        }

        /// <summary>
        /// Retrieves, but does not remove, the head of this queue. This
        /// implementation returns the result of peek unless the queue is empty.
        /// </summary>
        /// <returns></returns>
        public virtual int Element()
        {
            if (_size == 0)
                throw new IndexOutOfRangeException("empty queue");
            return _items[0];
        }

        public int IntElement()
        {
            if (_size == 0)
                throw new IndexOutOfRangeException("empty queue");
            return _items[0];
        }

        /// <summary>
        /// Returns an iterator over the elements in this collection. There are no guarantees
        /// concerning the order in which the elements are returned (unless this collection is an
        /// instance of some class that provides a guarantee).
        /// </summary>
        /// <returns></returns>
        public IntIterator GetIterator()
        {
            return new IntIterator(this);
        }

        public class IntIterator : IEnumerator<int>
        {
            private int i = 0;
            private IntBoundedPriorityQueue parent;

            public IntIterator(IntBoundedPriorityQueue parent)
            {
                this.parent = parent;
            }

            public int Current
            {
                get { return parent._items[i]; }
            }

            public void Dispose()
            {
                this.parent = null;
            }

            object IEnumerator.Current
            {
                get { return parent._items[i]; }
            }

            public bool MoveNext()
            {
                i++;
                return  (i < parent._size);
            }

            public void Reset()
            {
            }
        }

        /// <summary>
        /// When the queue is full, the offered elements are added if they are bigger than the
        /// smallest one already in the queue.
        /// 
        /// Inserts the specified element into this queue, if possible. When using queues that
        /// may impose insertion restrictions (for example capacity bounds), method offer is
        /// generally preferable to method Collection.add, which can fail to insert an element
        /// only by throwing an exception.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Offer(int item)
        {
            if (_size < _capacity)
            {
                _items[_size] = item;
                PercolateUp(_size);
                _size++;
                //    System.out.println("adding  to queue " + item + "  \t  " +Thread.currentThread().getClass()+Thread.currentThread().getId() );
                return true;
            }
            else
            {
                if (_items[0] < item)
                {
                    _items[0] = item;
                    PercolateDown();
                    return true;
                }
                else
                    return false;
            }
        }

        /// <summary>
        /// Retrieves, but does not remove, the head of this queue, returning the <b>forbidden value</b>
        /// if the queue is empty.
        /// </summary>
        /// <returns></returns>
        public int Peek()
        {
            if (_size == 0)
                return _forbiddenValue;
            return _items[0];
        }

        /// <summary>
        /// Retrieves and removes the head of this queue, or the <b>forbidden value</b> if this queue is empty.
        /// </summary>
        /// <returns></returns>
        public int Poll()
        {
            if (_size == 0)
                return _forbiddenValue;
            int ret = _items[0];
            _size--;
            _items[0] = _items[_size];
            _items[_size] = 0;
            if (_size > 1)
                PercolateDown();
            return ret;
        }

        /// <summary>
        /// Returns the number of elements in this collection.
        /// </summary>
        public int Count
        {
            get{ return _size; }
        }

        private void PercolateDown()
        {
            int temp = _items[0];
            int index = 0;
            while (true)
            {
                int left = (index << 1) + 1;

                int right = left + 1;
                if (right < _size)
                {
                    left = _comp.Compare(_items[left], _items[right]) < 0 ? left : right;
                }
                else if (left >= _size)
                {
                    _items[index] = temp;
                    break;
                }
                if (_comp.Compare(_items[left], temp) < 0)
                {
                    _items[index] = _items[left];
                    index = left;
                }
                else
                {
                    _items[index] = temp;
                    break;
                }
            }
        }

        private void PercolateUp(int index)
        {
            int i;
            int temp = _items[index];
            while ((i = ((index - 1) >> 1)) >= 0 && _comp.Compare(temp, _items[i]) < 0)
            {
                _items[index] = _items[i];
                index = i;
            }
            _items[index] = temp;
        }

        public abstract class IntComparator : Comparer<int>
        {
            public override int Compare(int x, int y)
            {
                if (x < y) return -1;
                if (y > x) return 1;
                return 0;
            }
        }
    }
}
