//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
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
namespace BoboBrowse.Net.DocIdSet
{
    using System;
    using Lucene.Net.Search;

    [Serializable]
    public class IntArrayDocIdSet : DocSet
    {
        // private static long serialVersionUID = 1L; // NOT USED

        private IntArray array = null;

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

        public override bool Cacheable
        {
            get { return true; }
        }

        protected int BinarySearchForNearest(int val, int begin, int end)
        {

            int mid = (begin + end) / 2;
            int midval = array.Get(mid);

            if (mid == end) return midval >= val ? mid : -1;

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

        public class IntArrayDocIdSetIterator : StatefulDSIterator
        {
            Func<int> getPos;
            Func<IntArray> getArray;
            Func<int, int, int, int> binarySearchForNearest;
            int lastReturn = -1;
            int cursor = -1;

            public IntArrayDocIdSetIterator(Func<int> getPos, Func<IntArray> getArray, Func<int, int, int, int> binarySearchForNearest)
            {
                this.getPos = getPos;
                this.getArray = getArray;
                this.binarySearchForNearest = binarySearchForNearest;

                var pos = this.getPos();
                if (pos == -1) lastReturn = DocIdSetIterator.NO_MORE_DOCS;
            }

            public override int DocID()
            {
                return lastReturn;
            }

            public override int NextDoc()
            {
                var pos = this.getPos();
                var array = this.getArray();
                if (cursor < pos)
                {
                    return (lastReturn = array.Get(++cursor));
                }
                return (lastReturn = DocIdSetIterator.NO_MORE_DOCS);
            }

            public override int Advance(int target)
            {
                if (lastReturn == DocIdSetIterator.NO_MORE_DOCS) return DocIdSetIterator.NO_MORE_DOCS;
                if (target <= lastReturn) target = lastReturn + 1;
                var pos = getPos();

                int end = Math.Min(cursor + (target - lastReturn), pos);
                int index = binarySearchForNearest(target, cursor + 1, end);

                if (index == -1)
                {
                    cursor = pos;
                    return (lastReturn = DocIdSetIterator.NO_MORE_DOCS);
                }
                else
                {
                    cursor = index;
                    var array = getArray();
                    return (lastReturn = array.Get(cursor));
                }
            }

            public override int GetCursor()
            {
                return cursor;
            }

            public override long Cost()
            {
                return 0;
            }
        }

        public override DocIdSetIterator GetIterator()
        {
            return new IntArrayDocIdSetIterator(() => pos, () => array, (val, begin, end) => BinarySearchForNearest(val, begin, end));
        }

        public override int Size()
        {
            return pos + 1;
        }

        public override int FindWithIndex(int target)
        {
            IntArrayDocIdSetIterator dcit = new IntArrayDocIdSetIterator(() => pos, () => array, (val, begin, end) => BinarySearchForNearest(val, begin, end));
            try
            {
                int docid = dcit.Advance(target);
                if (docid == target) return dcit.GetCursor();
            }
            catch (Exception e)
            {
                //e.printStackTrace();
            }
            return -1;
        }

        public override long SizeInBytes()
        {
            // Object Overhead
            return array.Length() * 4 + 64;
        }

        public override void Optimize()
        {
            this.array.Seal();
        }
    }
}
