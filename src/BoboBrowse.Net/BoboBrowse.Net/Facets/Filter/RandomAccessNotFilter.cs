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
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using Lucene.Net.Search;
    using LuceneExt.Impl;

    public class RandomAccessNotFilter : RandomAccessFilter
    {
        //private static long serialVersionUID = 1L; // NOT USED

        protected readonly RandomAccessFilter _innerFilter;

        public RandomAccessNotFilter(RandomAccessFilter innerFilter)
        {
            _innerFilter = innerFilter;
        }

        public override double GetFacetSelectivity(BoboIndexReader reader)
        {
            double selectivity = _innerFilter.GetFacetSelectivity(reader);
            selectivity = selectivity > 0.999 ? 0.0 : (1 - selectivity);
            return selectivity;
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboIndexReader reader)
        {
            RandomAccessDocIdSet innerDocIdSet = _innerFilter.GetRandomAccessDocIdSet(reader);
            DocIdSet notInnerDocIdSet = new NotDocIdSet(innerDocIdSet, reader.MaxDoc);
            return new NotRandomAccessDocIdSet(innerDocIdSet, notInnerDocIdSet);
        }

        private class NotRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly RandomAccessDocIdSet innerDocIdSet;
            private readonly DocIdSet notInnerDocIdSet;

            public NotRandomAccessDocIdSet(RandomAccessDocIdSet innerDocIdSet, DocIdSet notInnerDocIdSet)
            {
                this.innerDocIdSet = innerDocIdSet;
                this.notInnerDocIdSet = notInnerDocIdSet;
            }

            public override bool Get(int docId)
            {
                return !innerDocIdSet.Get(docId);
            }
            public override DocIdSetIterator Iterator()
            {
                return notInnerDocIdSet.Iterator();
            }
        }
    }
}
