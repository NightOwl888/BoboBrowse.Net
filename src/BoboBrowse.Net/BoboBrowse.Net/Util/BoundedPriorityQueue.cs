// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Util
{
    using C5;
    using System;
    using System.Collections.Generic;
    
    public class BoundedPriorityQueue<E> : IntervalHeap<E>
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private readonly int _maxSize;
        public BoundedPriorityQueue(int maxSize)
            : base()
        {
            this._maxSize = maxSize;
        }

        public BoundedPriorityQueue(IComparer<E> comparator, int maxSize)
            : base(maxSize, comparator)
        {
            this._maxSize = maxSize;
        }


        public bool Offer(E o)
        {
            int size = Count;
            if (size < _maxSize)
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
                    this.Poll();
                    return base.Add(o);
                }
                else
                {
                    return false;
                }
            }
        }

        public E Poll()
        {
            if (this.Count == 0)
                return default(E);
            return base.DeleteMax();
        }
    }
}
