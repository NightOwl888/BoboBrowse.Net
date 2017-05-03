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
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets.Data;
    using Lucene.Net.Search;
    using Lucene.Net.Util;

    public class BitSetFilter : RandomAccessFilter
    {
        protected readonly IFacetDataCacheBuilder m_facetDataCacheBuilder;
        protected readonly IBitSetBuilder m_bitSetBuilder;
        private volatile OpenBitSet m_bitSet;
        private volatile FacetDataCache m_lastCache;

        public BitSetFilter(IBitSetBuilder bitSetBuilder, IFacetDataCacheBuilder facetDataCacheBuilder)
        {
            this.m_bitSetBuilder = bitSetBuilder;
            this.m_facetDataCacheBuilder = facetDataCacheBuilder;
        }

        public virtual OpenBitSet GetBitSet(FacetDataCache dataCache)
        {
            if (m_lastCache == dataCache)
            {
                return m_bitSet;
            }
            m_bitSet = m_bitSetBuilder.BitSet(dataCache);
            m_lastCache = dataCache;
            return m_bitSet;
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboSegmentReader reader)
        {
            FacetDataCache dataCache = m_facetDataCacheBuilder.Build(reader);
            OpenBitSet openBitSet = GetBitSet(dataCache);
            long count = openBitSet.Cardinality();
            if (count == 0)
            {
                return EmptyDocIdSet.Instance;
            }
            else
            {
                bool multi = dataCache is MultiValueFacetDataCache;
                MultiValueFacetDataCache multiCache = multi ? (MultiValueFacetDataCache)dataCache : null;
                return new BitSetRandomAccessDocIdSet(multi, multiCache, openBitSet, dataCache);
            }
        }

        private class BitSetRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly bool m_multi;
            private readonly MultiValueFacetDataCache m_multiCache;
            private readonly OpenBitSet m_openBitSet;
            private readonly FacetDataCache m_dataCache;

            public BitSetRandomAccessDocIdSet(bool multi, MultiValueFacetDataCache multiCache, OpenBitSet openBitSet, FacetDataCache dataCache)
            {
                m_multi = multi;
                m_multiCache = multiCache;
                m_openBitSet = openBitSet;
                m_dataCache = dataCache;
            }

            public override DocIdSetIterator GetIterator()
            {
                if (m_multi)
                {
                    return new MultiValueORFacetFilter.MultiValueOrFacetDocIdSetIterator(m_multiCache, m_openBitSet);
                }
                else
                {
                    return new FacetOrFilter.FacetOrDocIdSetIterator(m_dataCache, m_openBitSet);
                }
            }

            public override bool Get(int docId)
            {
                if (m_multi)
                {
                    return m_multiCache.NestedArray.Contains(docId, m_openBitSet);
                }
                else
                {
                    return m_openBitSet.FastGet(m_dataCache.OrderArray.Get(docId));
                }
            }
        }

        public override double GetFacetSelectivity(BoboSegmentReader reader)
        {
            FacetDataCache dataCache = m_facetDataCacheBuilder.Build(reader);
            OpenBitSet openBitSet = GetBitSet(dataCache);
            int[] frequencies = dataCache.Freqs;
            double selectivity = 0;
            int accumFreq = 0;
            int index = openBitSet.NextSetBit(0);
            while (index >= 0)
            {
                accumFreq += frequencies[index];
                index = openBitSet.NextSetBit(index + 1);
            }
            int total = reader.MaxDoc;
            selectivity = (double)accumFreq / (double)total;
            if (selectivity > 0.999)
            {
                selectivity = 1.0;
            }
            return selectivity;
        }
    }
}
