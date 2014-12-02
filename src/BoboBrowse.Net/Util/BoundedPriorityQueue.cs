// Copyright (c) COMPANY. All rights reserved. 

namespace BoboBrowse.Net.Util
{
    using System;
    using System.Collections.Generic;
    using C5;

    public class BoundedPriorityQueue<E> : IntervalHeap<E>
    {
        private readonly int maxSize;

        public BoundedPriorityQueue(int maxSize)
        {
            this.maxSize = maxSize;
        }

        public BoundedPriorityQueue(IComparer<E> comparator, int maxSize)
            : base(maxSize, comparator)
        {
            this.maxSize = maxSize;
        }

        public bool Offer(E o)
        {
            int size = Count;

            if (size < maxSize)
            {
                return base.Add(o);
            }
            else
            {
                E smallest = base.FindMax();

                IComparer<E> comparator = base.Comparer;
                bool madeIt = false;
                if (comparator == null)
                {
                    if (((IComparable<E>)smallest).CompareTo(o) < 0)
                    {
                        madeIt = true;
                    }
                }
                else
                {
                    if (comparator.Compare(smallest, o) < 0)
                    {
                        madeIt = true;
                    }
                }

                if (madeIt)
                {
                    base.DeleteMax();
                    return base.Add(o);
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
