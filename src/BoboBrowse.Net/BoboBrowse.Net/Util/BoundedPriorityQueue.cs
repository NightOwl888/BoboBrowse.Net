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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Util
{
    using Lucene.Net.Support;
    using System;
    using System.Collections.Generic;
    
    public class BoundedPriorityQueue<E> : PriorityQueue<E> where E: class
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private readonly int m_maxSize;
        public BoundedPriorityQueue(int maxSize)
            : base()
        {
            this.m_maxSize = maxSize;
        }

        public BoundedPriorityQueue(IComparer<E> comparer, int maxSize)
            : base(maxSize, comparer)
        {
            this.m_maxSize = maxSize;
        }


        public override bool Offer(E o)
        {
            int size = Count;
            if (size < m_maxSize)
            {
                return base.Offer(o);
            }
            else
            {
                E smallest = base.Peek();
                IComparer<E> comparer = base.Comparer;
                bool madeIt = false;
                if (comparer == null)
                {
                    if (((IComparable<E>)smallest).CompareTo(o) < 0)
                    {
                        madeIt = true;
                    }
                }
                else
                {
                    if (comparer.Compare(smallest, o) < 0)
                    {
                        madeIt = true;
                    }
                }

                if (madeIt)
                {
                    this.Poll();
                    return base.Offer(o);
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
