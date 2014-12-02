//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Written in Java.
//* 
//* Copyright (C) 2005-2006  John Wang
//*
//* This library is free software; you can redistribute it and/or
//* modify it under the terms of the GNU Lesser General Public
//* License as published by the Free Software Foundation; either
//* version 2.1 of the License, or (at your option) any later version.
//*
//* This library is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

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
