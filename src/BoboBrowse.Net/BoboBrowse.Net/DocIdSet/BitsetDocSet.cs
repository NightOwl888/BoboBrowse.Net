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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.DocIdSet
{
    using BoboBrowse.Net.Support;
    using Lucene.Net.Search;

    public class BitsetDocSet : DocIdSet
    {
        private readonly BitSet _bs;

        public BitsetDocSet()
        {
            _bs = new BitSet();
        }

        public BitsetDocSet(int nbits)
        {
            _bs = new BitSet(nbits);
        }

        public virtual void AddDoc(int docid)
        {
            _bs.Set(docid);
        }

        public virtual int Size
        {
            get { return _bs.Cardinality(); }
        }

        public override DocIdSetIterator Iterator()
        {
            return new BitsDocIdSetIterator(_bs);
        }

        public class BitsDocIdSetIterator : DocIdSetIterator
        {
            private readonly BitSet _bs;
            private int _current;

            public BitsDocIdSetIterator(BitSet bs)
            {
                _bs = bs;
                _current = -1;
            }

            public override int DocID()
            {
                return _current;
            }

            public override int NextDoc()
            {
                return _bs.NextSetBit(_current + 1);
            }

            public override int Advance(int target)
            {
                return _bs.NextSetBit(target);
            }
        }
    }
}
