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
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;
    using System;

    public class FacetFilter : RandomAccessFilter
    {
        protected readonly IFacetHandler m_facetHandler;
        protected readonly string m_value;


        public FacetFilter(IFacetHandler facetHandler, string value)
        {
            m_facetHandler = facetHandler;
            m_value = value;
        }

        public override double GetFacetSelectivity(BoboSegmentReader reader)
        {
            double selectivity = 0;
            FacetDataCache dataCache = m_facetHandler.GetFacetData<FacetDataCache>(reader);
            int idx = dataCache.ValArray.IndexOf(m_value);
            if (idx < 0)
            {
                return 0.0;
            }
            int freq = dataCache.Freqs[idx];
            int total = reader.MaxDoc;
            selectivity = (double)freq / (double)total;
            return selectivity;
        }

        public class FacetDocIdSetIterator : DocIdSetIterator
        {
            protected int m_doc;
            protected readonly int m_index;
            protected readonly int m_maxID;
            protected readonly BigSegmentedArray m_orderArray;

            public FacetDocIdSetIterator(FacetDataCache dataCache, int index)
            {
                m_index = index;
                m_doc = Math.Max(-1, dataCache.MinIDs[m_index] - 1);
                m_maxID = dataCache.MaxIDs[m_index];
                m_orderArray = dataCache.OrderArray;
            }

            public override int DocID
            {
                get { return m_doc; }
            }

            public override int NextDoc()
            {
                m_doc = (m_doc < m_maxID) ? m_orderArray.FindValue(m_index, m_doc + 1, m_maxID) : NO_MORE_DOCS;
                return m_doc;
            }

            public override int Advance(int id)
            {
                if (m_doc < id)
                {
                    m_doc = (id <= m_maxID) ? m_orderArray.FindValue(m_index, id, m_maxID) : NO_MORE_DOCS;
                    return m_doc;
                }
                return NextDoc();
            }

            public override long GetCost()
            {
                return 0;
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboSegmentReader reader)
        {
            FacetDataCache dataCache = m_facetHandler.GetFacetData<FacetDataCache>(reader);
            int index = dataCache.ValArray.IndexOf(m_value);
            if (index < 0)
            {
                return EmptyDocIdSet.Instance;
            }
            else
            {
                return new FacetDataRandomAccessDocIdSet(dataCache, index);
            }
        }

        public class FacetDataRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly FacetDataCache m_dataCache;
	        private readonly BigSegmentedArray m_orderArray;
	        private readonly int m_index;

            internal FacetDataRandomAccessDocIdSet(FacetDataCache dataCache, int index)
            {
                m_dataCache = dataCache;
                m_orderArray = dataCache.OrderArray;
                m_index = index;
            }

            public override bool Get(int docId)
            {
                return m_orderArray.Get(docId) == m_index;
            }

            public override DocIdSetIterator GetIterator()
            {
                return new FacetDocIdSetIterator(m_dataCache, m_index);
            }
        }
    }
}
