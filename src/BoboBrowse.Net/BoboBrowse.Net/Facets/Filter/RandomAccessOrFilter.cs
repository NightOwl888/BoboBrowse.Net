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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class RandomAccessOrFilter : RandomAccessFilter
    {
        //private static long serialVersionUID = 1L; // NOT USED

        protected readonly IEnumerable<RandomAccessFilter> _filters;

        public RandomAccessOrFilter(IEnumerable<RandomAccessFilter> filters)
        {
            if (filters == null)
            {
                throw new ArgumentNullException("filters");
            }
            _filters = filters;
        }

        public override double GetFacetSelectivity(BoboSegmentReader reader)
        {
            double selectivity = 0;
            foreach (RandomAccessFilter filter in _filters)
            {
                selectivity += filter.GetFacetSelectivity(reader);
            }
    
            if(selectivity > 0.999)
            {
                selectivity = 1.0;
            }
            return selectivity;
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboSegmentReader reader)
        {
            var count = _filters.Count();
            if (count == 1)
            {
                return _filters.ElementAt(0).GetRandomAccessDocIdSet(reader);
            }
            else
            {
                List<DocIdSet> list = new List<DocIdSet>(count);
                List<RandomAccessDocIdSet> randomAccessList = new List<RandomAccessDocIdSet>(count);
                foreach (RandomAccessFilter f in _filters)
                {
                    RandomAccessDocIdSet s = f.GetRandomAccessDocIdSet(reader);
                    list.Add(s);
                    randomAccessList.Add(s);
                }
                RandomAccessDocIdSet[] randomAccessDocIdSets = randomAccessList.ToArray();
                DocIdSet orDocIdSet = new OrDocIdSet(list);
                return new RandomOrFilterDocIdSet(randomAccessDocIdSets, orDocIdSet);
            }
        }

        private class RandomOrFilterDocIdSet : RandomAccessDocIdSet
        {
            private RandomAccessDocIdSet[] randomAccessDocIdSets;
            private DocIdSet orDocIdSet;

            public RandomOrFilterDocIdSet(RandomAccessDocIdSet[] randomAccessDocIdSets, DocIdSet orDocIdSet)
            {
                this.orDocIdSet = orDocIdSet;
                this.randomAccessDocIdSets = randomAccessDocIdSets;
            }
            public override bool Get(int docId)
            {
                foreach (RandomAccessDocIdSet s in randomAccessDocIdSets)
                {
                    if (s.Get(docId))
                        return true;
                }
                return false;
            }

            public override DocIdSetIterator Iterator()
            {
                return orDocIdSet.Iterator();
            }
        }  
    }
}
