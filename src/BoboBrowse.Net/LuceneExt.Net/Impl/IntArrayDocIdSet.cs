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

﻿// Kamikaze version compatibility level: 3.0.6
namespace LuceneExt.Impl
{
    using Lucene.Net.Search;
    using LuceneExt.Util;
    using System;

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

        protected virtual int BinarySearchForNearest(int val, int begin, int end)
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
                if (array.Get(mid + 1) >= val) return mid + 1;

                return BinarySearchForNearest(val, mid + 1, end);
            }
            else
            {
                // Find number equal or greater than the target.
                if (midval == val) return mid;

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
                if (parent.pos == -1) lastReturn = DocIdSetIterator.NO_MORE_DOCS;
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
                if (lastReturn == DocIdSetIterator.NO_MORE_DOCS) return DocIdSetIterator.NO_MORE_DOCS;

                if (target <= lastReturn) target = lastReturn + 1;

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
                    return dcit.GetCursor();
            }
            catch
            {
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
