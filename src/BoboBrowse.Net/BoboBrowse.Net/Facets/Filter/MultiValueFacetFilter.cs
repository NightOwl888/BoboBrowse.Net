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
    using BoboBrowse.Net.Facets.Range;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;

    public class MultiValueFacetFilter : RandomAccessFilter
    {
        private readonly string m_val;
        private readonly MultiDataCacheBuilder m_multiDataCacheBuilder;

        public MultiValueFacetFilter(MultiDataCacheBuilder multiDataCacheBuilder, string val)
        {
            this.m_multiDataCacheBuilder = multiDataCacheBuilder;
            m_val = val;
        }

        public override double GetFacetSelectivity(BoboSegmentReader reader)
        {
            double selectivity = 0;
            FacetDataCache dataCache = m_multiDataCacheBuilder.Build(reader);
            int idx = dataCache.ValArray.IndexOf(m_val);
            if (idx < 0)
            {
                return 0.0;
            }
            int freq = dataCache.Freqs[idx];
            int total = reader.MaxDoc;
            selectivity = (double)freq / (double)total;
            return selectivity;
        }

        public sealed class MultiValueFacetDocIdSetIterator : FacetFilter.FacetDocIdSetIterator
        {
            private readonly BigNestedInt32Array m_nestedArray;

            public MultiValueFacetDocIdSetIterator(MultiValueFacetDataCache dataCache, int index)
                : base(dataCache, index)
            {
                m_nestedArray = dataCache.NestedArray;
            }           

            public override int NextDoc()
            {
                m_doc = (m_doc < m_maxID ? m_nestedArray.FindValue(m_index, (m_doc + 1), m_maxID) : NO_MORE_DOCS);
                return m_doc;
            }

            public override int Advance(int id)
            {
                if (m_doc < id)
                {
                    m_doc = (id <= m_maxID ? m_nestedArray.FindValue(m_index, id, m_maxID) : NO_MORE_DOCS);
                    return m_doc;
                }
                return NextDoc();
            }            
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboSegmentReader reader)
        {
            MultiValueFacetDataCache dataCache = (MultiValueFacetDataCache)m_multiDataCacheBuilder.Build(reader);
            int index = dataCache.ValArray.IndexOf(m_val);
            if (index < 0)
            {
                return EmptyDocIdSet.Instance;
            }
            else
            {
                return new MultiValueRandomAccessDocIdSet(dataCache, index);
            }
        }

        private class MultiValueRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly MultiValueFacetDataCache m_dataCache;
            private readonly int m_index;
            private readonly BigNestedInt32Array m_nestedArray;

            public MultiValueRandomAccessDocIdSet(MultiValueFacetDataCache dataCache, int index)
            {
                m_dataCache = dataCache;
                m_index = index;
                m_nestedArray = dataCache.NestedArray;
            }

            public override DocIdSetIterator GetIterator()
            {
                return new MultiValueFacetDocIdSetIterator(m_dataCache, m_index);
            }

            public override bool Get(int docId)
            {
                return m_nestedArray.Contains(docId, m_index);
            }
        }
    }
}
