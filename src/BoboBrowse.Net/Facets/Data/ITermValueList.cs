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

namespace BoboBrowse.Net.Facets.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public interface ITermValueList
    {
        int Count { get; }
        string Get(int index);
        object GetRawValue(int index);
        string Format(object o);
        int IndexOf(object o);
        void Add(string o);
        List<string> GetInnerList();
        void Seal();
    }

    /// <summary>This class behaves as List<String> with a few extensions:
    /// <ul>
    /// <li> Semi-immutable, e.g. once added, cannot be removed. </li>
    /// <li> Assumes sequence of values added are in sorted order </li>
    /// <li> <seealso cref="#indexOf(Object)"/> return value conforms to the contract of <seealso cref="Arrays#binarySearch(Object[], Object)"/></li>
    /// <li> <seealso cref="#seal()"/> is introduce to trim the List size, similar to <seealso cref="ArrayList#TrimToSize()"/>, once it is called, no add should be performed.</li>
    /// </u> </summary>
    public abstract class TermValueList<T>
        : List<T>
        , ITermValueList
    {
        public abstract string Format(object o);
        public abstract void Add(string o);
        public abstract int IndexOf(object o);

        protected internal TermValueList()
        {
        }

        protected internal TermValueList(int capacity)
            : base(capacity)
        {
        }

        public virtual List<string> GetInnerList()
        {
            return new List<string>(this.Select(x => Format(x)));
        }

        public virtual bool Contains(object o)
        {
            return base.IndexOf((T)o) >= 0;
        }

        public virtual string Get(int index)
        {
            return Format(this[index]);
        }

        public virtual object GetRawValue(int index)
        {
            return this[index];
        }

        public virtual bool IsEmpty()
        {
            return Count == 0;
        }

        public virtual int LastIndexOf(object o)
        {
            return base.IndexOf((T)o); // FIXME
        }

        public virtual int Size()
        {
            return Count;
        }

        public virtual List<string> SubList(int fromIndex, int toIndex)
        {
            throw new InvalidOperationException("not supported");
        }

        public virtual void Seal()
        {
            TrimExcess();
        }
    }
}
