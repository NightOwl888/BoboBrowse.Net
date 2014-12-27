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
    using System.Runtime.CompilerServices;
    using System.Text;

    [Serializable]
    public abstract class PrimitiveArray<T>
    {        
        protected internal object Array { get; set; }

        protected internal int Count { get; set; }

        protected internal int Growth { get; set; }

        protected internal int Len { get; set; }

        private const int DEFAULT_SIZE = 1000;

        protected internal abstract object BuildArray(int len);

        protected internal PrimitiveArray(int len)
        {
            if (len <= 0)
            {
                throw new ArgumentException("len must be greater than 0: " + len);
            }
            Array = BuildArray(len);
            Count = 0;
            Growth = 10;

            Len = len;
        }

        protected internal PrimitiveArray()
            : this(DEFAULT_SIZE)
        {
        }

        public virtual void Clear()
        {
            Count = 0;
            Growth = 10;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected internal virtual void Expand()
        {
            Expand(Len + 100);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected internal virtual void Expand(int idx)
        {
            if (idx <= Len)
                return;
            int oldLen = Len;
            Len = idx + Growth;
            object newArray = BuildArray(Len);
            System.Array.Copy((Array)Array, 0, (Array)newArray, 0, oldLen); // FIXME : probabaly this will not work and we need another implementation based on generics here
            Growth += Len;
            Array = newArray;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual void EnsureCapacity(int idx)
        {
            Expand(idx);
        }

        public virtual int Size()
        {
            return Count;
        }

        ///<summary>called to shrink the array size to the current # of elements to save memory.</summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual void Seal()
        {
            if (Len > Count)
            {
                object newArray = BuildArray(Count);
                System.Array.Copy((Array)Array, 0, (Array)newArray, 0, Count);
                Array = newArray;
                Len = Count;
            }
            Growth = 10;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual T[] ToArray(T[] array)
        {
            System.Array.Copy((Array)this.Array, 0, (Array)array, 0, Count);
            return array;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual object ToArray()
        {
            object array = BuildArray(Count);
            System.Array.Copy((Array)this.Array, 0, (Array)array, 0, Count);
            return array;
        }

        public PrimitiveArray<T> Clone()
        {
            PrimitiveArray<T> obj;
            try
            {
                obj = (PrimitiveArray<T>)this.GetType().GetConstructors()[0].Invoke(new object[] { }); // FIXME i still think that we need a better way do to this

                obj.Count = Count;
                obj.Growth = Growth;
                obj.Len = Len;

                object newArray = BuildArray(Len);
                System.Array.Copy((Array)Array, 0, (Array)newArray, 0, Count);
                obj.Array = newArray;
                return obj;
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message);
            }
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder("[");
            for (int i = 0; i < Count; ++i)
            {
                if (i != 0)
                {
                    buffer.Append(", ");
                }
                buffer.Append(((Array)Array).GetValue(i));
            }
            buffer.Append(']');

            return buffer.ToString();
        }

        public virtual int Length()
        {
            return Len;
        }
    }
}
