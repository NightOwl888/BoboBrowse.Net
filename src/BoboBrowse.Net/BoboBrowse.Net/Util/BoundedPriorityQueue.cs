//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
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
