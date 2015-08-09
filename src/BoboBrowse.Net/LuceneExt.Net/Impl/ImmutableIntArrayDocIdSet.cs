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

namespace LuceneExt.Impl
{
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
