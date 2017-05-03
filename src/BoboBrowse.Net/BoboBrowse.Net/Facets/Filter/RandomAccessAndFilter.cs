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
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using Lucene.Net.Search;
    using System.Collections.Generic;
    using System.Linq;

    public class RandomAccessAndFilter : RandomAccessFilter
    {
        protected IList<RandomAccessFilter> m_filters;

        public RandomAccessAndFilter(IList<RandomAccessFilter> filters)
        {
            m_filters = filters;
        }

        public override double GetFacetSelectivity(BoboSegmentReader reader)
        {
            double selectivity = double.MaxValue;
            foreach (RandomAccessFilter filter in m_filters)
            {
                double curSelectivity = filter.GetFacetSelectivity(reader);
                if(selectivity > curSelectivity)
                {
                    selectivity = curSelectivity;
                }
            }
            if (selectivity > 0.999)
            {
                selectivity = 1.0;
            }
            return selectivity;
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboSegmentReader reader)
        {
            var count = m_filters.Count;
            if (count == 1)
            {
                return m_filters[0].GetRandomAccessDocIdSet(reader);
            }
            else
            {
                List<DocIdSet> list = new List<DocIdSet>(count);
                List<RandomAccessDocIdSet> randomAccessList = new List<RandomAccessDocIdSet>(count);
                foreach (RandomAccessFilter f in m_filters)
                {
                    RandomAccessDocIdSet s = f.GetRandomAccessDocIdSet(reader);
                    list.Add(s);
                    randomAccessList.Add(s);
                }
                RandomAccessDocIdSet[] randomAccessDocIdSets = randomAccessList.ToArray();
                DocIdSet andDocIdSet = new AndDocIdSet(list);
                return new RandomAccessAndFilterSet(randomAccessDocIdSets, andDocIdSet);
            }
        }

        private class RandomAccessAndFilterSet : RandomAccessDocIdSet
        {
            private RandomAccessDocIdSet[] m_randomAccessDocIdSets;
            private DocIdSet m_andDocIdSet;

            public RandomAccessAndFilterSet(RandomAccessDocIdSet[] randomAccessDocIdSets, DocIdSet andDocIdSet)
            {
                this.m_randomAccessDocIdSets = randomAccessDocIdSets;
                this.m_andDocIdSet = andDocIdSet;
            }

            public override bool Get(int docId)
            {
                foreach (RandomAccessDocIdSet s in m_randomAccessDocIdSets)
                {
                    if (!s.Get(docId))
                        return false;
                }
                return true;
            }

            public override DocIdSetIterator GetIterator()
            {
                return m_andDocIdSet.GetIterator();
            }
        }
    }
}
