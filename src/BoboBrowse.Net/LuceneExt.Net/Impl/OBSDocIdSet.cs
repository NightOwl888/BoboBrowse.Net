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
    using Lucene.Net.Util;
    using System;

    [Serializable]
    public class OBSDocIdSet : DocSet
    {
        private readonly OpenBitSet bitSet;

        internal int min = -1;

        internal int max = -1;

        public OBSDocIdSet(int length)
        {
            bitSet = new OpenBitSet(length);
        }

        public override void AddDoc(int docid)
        {
            if (min == -1)
            {
                min = docid;
            }
            max = docid;
            bitSet.Set(max - min);

        }

        public override bool IsCacheable
        {
            get
            {
                return true;
            }
        }

        internal class OBSDocIdSetIterator : StatefulDSIterator
        {
            private int lastReturn = -1;
            private int cursor = -1;
            private int marker = -1;
            private readonly OBSDocIdSet parent;

            public OBSDocIdSetIterator(OBSDocIdSet parent)
            {
                this.parent = parent;
            }

            public override int DocID()
            {
                return lastReturn + parent.min;
            }

            public override int NextDoc()
            {
                if (parent.bitSet.Size() - 1 > lastReturn)
                {
                    if (lastReturn == -1)
                    {

                        if (parent.bitSet.FastGet(0))
                        {
                            lastReturn = 0;
                            cursor++;
                            marker = lastReturn;

                            return lastReturn + parent.min;
                        }
                    }
                    else
                        lastReturn = parent.bitSet.NextSetBit(lastReturn + 1);

                    if (lastReturn != -1)
                    {
                        cursor++;
                        marker = lastReturn;
                        return lastReturn + parent.min;
                    }
                }
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override int Advance(int target)
            {
                if (target > parent.max)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

                target -= parent.min; // adjust target to the local offset
                if (target <= lastReturn)
                    target = lastReturn + 1;

                if (target <= 0)
                {
                    if (parent.bitSet.FastGet(0))
                    {
                        lastReturn = 0;
                        return parent.min;
                    }
                }
                else
                {
                    lastReturn = parent.bitSet.NextSetBit(target);
                    if (lastReturn != -1)
                        return lastReturn + parent.min;
                }
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override int GetCursor()
            {

                while (marker < lastReturn)
                {
                    if (parent.bitSet.FastGet(++marker))
                    {
                        cursor++;
                    }
                }

                return cursor;
            }
        }

        public override DocIdSetIterator Iterator()
        {
            return new OBSDocIdSetIterator(this);
        }

        public virtual int Range()
        {
            return max - min;
        }

        public override int Size()
        {
            return (int)bitSet.Cardinality();
        }

        public override int FindWithIndex(int val)
        {

            val -= min;
            if (val >= 0 && bitSet.Get(val))
            {
                int index = -1;
                int counter = -1;
                while (true)
                {
                    index = bitSet.NextSetBit(index + 1);
                    if (index <= val && index != -1)
                        counter++;
                    else
                        break;
                }
                return counter;

            }
            else
                return -1;

        }
        public override bool Find(int val)
        {
            val -= min;
            if (val >= 0 && bitSet.Get(val))
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public override long SizeInBytes()
        {
            return bitSet.Capacity() / 8;
        }

        public override void Optimize()
        {
            bitSet.TrimTrailingZeros();
        }
    }
}
