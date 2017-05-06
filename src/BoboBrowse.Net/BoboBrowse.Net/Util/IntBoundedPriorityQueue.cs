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
namespace BoboBrowse.Net.Util
{
    //using C5;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// NOTE: This was IntBoundedPriorityQueue in bobo-browse
    /// </summary>
    public class Int32BoundedPriorityQueue //: IntervalHeap<int>
    {
        //private static long serialVersionUID = 1L; // NOT USED
        private readonly int m_capacity;
        private readonly int[] m_items;
        private int m_size = 0;
        private readonly IComparer<int> m_comp;
        private readonly int m_forbiddenValue;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="comparer">a comparer that is used to order the items.</param>
        /// <param name="capacity">the maximum number of items the queue accepts</param>
        /// <param name="forbiddenValue"></param>
        public Int32BoundedPriorityQueue(IComparer<int> comparer, int capacity, int forbiddenValue)
        {
            m_capacity = capacity;
            m_comp = comparer;
            m_items = new int[capacity];// java.lang.reflect.Array.newInstance(, capacity);
            m_forbiddenValue = forbiddenValue;
        }

        /// <summary>
        /// Retrieves, but does not remove, the head of this queue. This
        /// implementation returns the result of peek unless the queue is empty.
        /// </summary>
        /// <returns></returns>
        public virtual int Element()
        {
            if (m_size == 0)
                throw new IndexOutOfRangeException("empty queue");
            return m_items[0];
        }

        /// <summary>
        /// NOTE: This was IntElement() in bobo-browse
        /// </summary>
        public int Int32Element()
        {
            if (m_size == 0)
                throw new IndexOutOfRangeException("empty queue");
            return m_items[0];
        }

        /// <summary>
        /// Returns an iterator over the elements in this collection. There are no guarantees
        /// concerning the order in which the elements are returned (unless this collection is an
        /// instance of some class that provides a guarantee).
        /// </summary>
        /// <returns></returns>
        public Int32Iterator GetIterator() // TODO: Implement IEnumerable<T> and change this to GetEnumerator()
        {
            return new Int32Iterator(this);
        }

        /// <summary>
        /// NOTE: This was IntIterator in bobo-browse
        /// </summary>
        public class Int32Iterator : IEnumerator<int>
        {
            private int i = 0;
            private Int32BoundedPriorityQueue parent;

            public Int32Iterator(Int32BoundedPriorityQueue parent)
            {
                this.parent = parent;
            }

            public int Current
            {
                get { return parent.m_items[i]; }
            }

            public void Dispose()
            {
                this.parent = null;
            }

            object IEnumerator.Current
            {
                get { return parent.m_items[i]; }
            }

            public bool MoveNext()
            {
                i++;
                return  (i < parent.m_size);
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
            if (m_size < m_capacity)
            {
                m_items[m_size] = item;
                PercolateUp(m_size);
                m_size++;
                //    System.out.println("adding  to queue " + item + "  \t  " +Thread.currentThread().getClass()+Thread.currentThread().getId() );
                return true;
            }
            else
            {
                if (m_items[0] < item)
                {
                    m_items[0] = item;
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
            if (m_size == 0)
                return m_forbiddenValue;
            return m_items[0];
        }

        /// <summary>
        /// Retrieves and removes the head of this queue, or the <b>forbidden value</b> if this queue is empty.
        /// </summary>
        /// <returns></returns>
        public int Poll()
        {
            if (m_size == 0)
                return m_forbiddenValue;
            int ret = m_items[0];
            m_size--;
            m_items[0] = m_items[m_size];
            m_items[m_size] = 0;
            if (m_size > 1)
                PercolateDown();
            return ret;
        }

        /// <summary>
        /// Returns the number of elements in this collection.
        /// </summary>
        public int Count
        {
            get{ return m_size; }
        }

        private void PercolateDown()
        {
            int temp = m_items[0];
            int index = 0;
            while (true)
            {
                int left = (index << 1) + 1;

                int right = left + 1;
                if (right < m_size)
                {
                    left = m_comp.Compare(m_items[left], m_items[right]) < 0 ? left : right;
                }
                else if (left >= m_size)
                {
                    m_items[index] = temp;
                    break;
                }
                if (m_comp.Compare(m_items[left], temp) < 0)
                {
                    m_items[index] = m_items[left];
                    index = left;
                }
                else
                {
                    m_items[index] = temp;
                    break;
                }
            }
        }

        private void PercolateUp(int index)
        {
            int i;
            int temp = m_items[index];
            while ((i = ((index - 1) >> 1)) >= 0 && m_comp.Compare(temp, m_items[i]) < 0)
            {
                m_items[index] = m_items[i];
                index = i;
            }
            m_items[index] = temp;
        }

        /// <summary>
        /// NOTE: This was IntComparator in bobo-browse
        /// </summary>
        public abstract class Int32Comparer : Comparer<int>
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
