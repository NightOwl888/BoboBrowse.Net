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
    using System.IO;
    using Lucene.Net.Util;
    using Lucene.Net.Search;

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
