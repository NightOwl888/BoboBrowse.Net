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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.DocIdSet
{
    using Lucene.Net.Search;

    public sealed class EmptyDocIdSet : RandomAccessDocIdSet
    {
        private readonly static EmptyDocIdSet SINGLETON = new EmptyDocIdSet();

        private class EmptyDocIdSetIterator : DocIdSetIterator
        {
            public override int DocID()
            {
                return -1;
            }

            public override int NextDoc()
            {
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override int Advance(int target)
            {
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override long Cost()
            {
                return 0;
            }
        }

        private static EmptyDocIdSetIterator SINGLETON_ITERATOR = new EmptyDocIdSetIterator();

        private EmptyDocIdSet()
        {
        }

        public static EmptyDocIdSet Instance
        {
            get { return SINGLETON; }
        }

        // TODO: Submit pull request to Lucene to fix this name
        public override DocIdSetIterator GetIterator()
        {
            return SINGLETON_ITERATOR;
        }

        //public override DocIdSetIterator Iterator()
        //{
        //    return SINGLETON_ITERATOR;
        //}

        public override bool Get(int docId)
        {
            return false;
        }
    }
}
