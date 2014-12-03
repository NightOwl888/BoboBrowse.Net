//*
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

namespace LuceneExt.Impl
{
    using System;
    using System.IO;
    using Lucene.Net.Search;
    using LuceneExt.Util;

    [Serializable]
    public class IntArrayDocIdSet : DocSet
    {      

        private readonly IntArray array;

        private int pos = -1;

        public IntArrayDocIdSet(int length)
        {
            array = new IntArray(length);
        }

        public IntArrayDocIdSet()
        {
            array = new IntArray();
        }

        public override void AddDoc(int docid)
        {
            ++pos;
            array.Add(docid);
        }

        public override bool IsCacheable
        {
            get
            {
                return true;
            }
        }

        protected internal virtual int BinarySearchForNearest(int val, int begin, int end)
        {
            int mid = (begin + end) / 2;
            int midval = array.Get(mid);

            if (mid == end)
            {
                return midval >= val ? mid : -1;
            }

            if (midval < val)
            {
                // Find number equal or greater than the target.
                if (array.Get(mid + 1) >= val)
                {
                    return mid + 1;
                }

                return BinarySearchForNearest(val, mid + 1, end);
            }
            else
            {
                // Find number equal or greater than the target.
                if (midval == val)
                {
                    return mid;
                }

                return BinarySearchForNearest(val, begin, mid);
            }
        }

        internal class IntArrayDocIdSetIterator : StatefulDSIterator
        {
            private int lastReturn = -1;
            private int cursor = -1;

            private readonly IntArrayDocIdSet parent;

            public IntArrayDocIdSetIterator(IntArrayDocIdSet parent)
            {
                this.parent = parent;
                if (parent.pos == -1)
                {
                    lastReturn = DocIdSetIterator.NO_MORE_DOCS;
                }
            }

            public override int DocID()
            {
                return lastReturn;
            }

            public override int NextDoc()
            {
                if (cursor < parent.pos)
                {
                    return (lastReturn = parent.array.Get(++cursor));
                }
                return (lastReturn = DocIdSetIterator.NO_MORE_DOCS);
            }

            public override int Advance(int target)
            {
                if (lastReturn == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

                if (target <= lastReturn)
                {
                    target = lastReturn + 1;
                }

                int end = Math.Min(cursor + (target - lastReturn), parent.pos);
                int index = parent.BinarySearchForNearest(target, cursor + 1, end);

                if (index == -1)
                {
                    cursor = parent.pos;
                    return (lastReturn = DocIdSetIterator.NO_MORE_DOCS);
                }
                else
                {
                    cursor = index;
                    return (lastReturn = parent.array.Get(cursor));
                }
            }

            public override int GetCursor()
            {
                return cursor;
            }
        }

        public override DocIdSetIterator Iterator()
        {
            return new IntArrayDocIdSetIterator(this);
        }

        public override int Size()
        {
            return pos + 1;
        }

        public override int FindWithIndex(int val)
        {
            IntArrayDocIdSetIterator dcit = new IntArrayDocIdSetIterator(this);
            try
            {
                int docid = dcit.Advance(val);
                if (docid == val)
                {
                    return dcit.GetCursor();
                }
            }
            catch (IOException e)
            {
                //
            }
            return -1;
        }

        public override long SizeInBytes()
        {
            //Object Overhead
            return array.Length() * 4 + 64;
        }

        public override void Optimize()
        {
            this.array.Seal();
        }
    }
}
