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

namespace LuceneExt.Util
{
    using System;

    [Serializable]
    public class IntArray : PrimitiveArray<int>
    {
        public IntArray(int len)
            : base(len)
        {
        }

        public IntArray()
        {
        }

        public virtual void Add(int val)
        {
            EnsureCapacity(Count + 1);
            int[] array = (int[])base.Array;
            array[Count] = val;
            Count++;
        }


        public virtual void Set(int index, int val)
        {
            EnsureCapacity(index);
            int[] array = (int[])base.Array;
            array[index] = val;
            Count = Math.Max(Count, index + 1);
        }

        public virtual int Get(int index)
        {
            int[] array = (int[])base.Array;
            return array[index];
        }

        public virtual bool Contains(int elem)
        {
            int size = Size();
            for (int i = 0; i < size; ++i)
            {
                if (Get(i) == elem)
                {
                    return true;
                }
            }
            return false;
        }

        protected internal override object BuildArray(int len)
        {
            return new int[len];
        }

        public static int BinarySearch(int[] a, int fromIndex, int toIndex, int key)
        {
            int low = fromIndex;
            int high = toIndex - 1;

            while (low <= high)
            {
                int mid = (low + high) >> 1;
                int midVal = a[mid];

                if (midVal < key)
                {
                    low = mid + 1;
                }
                else if (midVal > key)
                {
                    high = mid - 1;
                }
                else
                {
                    return mid; // key found
                }
            }
            return -(low + 1); // key not found.
        }
    }
}
