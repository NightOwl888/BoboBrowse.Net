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

namespace LuceneExt
{
    using System;
    using Lucene.Net.Search;
    using LuceneExt.Util;

    public class ImmutableIntArrayDocIdSet : DocIdSet
    {
        private readonly int[] array;

        public ImmutableIntArrayDocIdSet(int[] array)
        {
            this.array = array;
        }

        public override DocIdSetIterator Iterator()
        {
            return new ImmutableIntArrayDocIdSetIterator(array);
        }

        public override bool IsCacheable
        {
            get
            {
                return true;
            }
        }

        public class ImmutableIntArrayDocIdSetIterator : DocIdSetIterator
        {
            private int doc;
            private int cursor;
            private readonly int[] array;

            public ImmutableIntArrayDocIdSetIterator(int[] array)
            {
                this.array = array;
                doc = -1;
                cursor = -1;
            }

            public override int DocID()
            {
                return doc;
            }

            public override int NextDoc()
            {
                if (++cursor < array.Length)
                {
                    doc = array[cursor];
                }
                else
                {
                    doc = DocIdSetIterator.NO_MORE_DOCS;
                }
                return doc;
            }

            public override int Advance(int target)
            {
                if (cursor >= array.Length || array.Length == -1)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }
                if (target <= doc)
                {
                    target = doc + 1;
                }

                int index = IntArray.BinarySearch(array, cursor, array.Length, target);

                if (index > 0)
                {
                    cursor = index;
                    doc = array[cursor];
                    return doc;
                }
                else
                {
                    cursor = -(index + 1);
                    if (cursor > array.Length)
                    {
                        doc = DocIdSetIterator.NO_MORE_DOCS;
                    }
                    else
                    {
                        doc = array[cursor];
                    }
                    return doc;
                }
            }
        }
    }
}
