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
        protected readonly IFacetDataCacheBuilder facetDataCacheBuilder;
        protected readonly IBitSetBuilder bitSetBuilder;
        private volatile OpenBitSet bitSet;
        private volatile FacetDataCache lastCache;

        public BitSetFilter(IBitSetBuilder bitSetBuilder, IFacetDataCacheBuilder facetDataCacheBuilder)
        {
            this.bitSetBuilder = bitSetBuilder;
            this.facetDataCacheBuilder = facetDataCacheBuilder;
        }

        public virtual OpenBitSet GetBitSet(FacetDataCache dataCache)
        {
            if (lastCache == dataCache)
            {
                return bitSet;
            }
            bitSet = bitSetBuilder.BitSet(dataCache);
            lastCache = dataCache;
            return bitSet;
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboSegmentReader reader)
        {
            FacetDataCache dataCache = facetDataCacheBuilder.Build(reader);
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
            private readonly bool _multi;
            private readonly MultiValueFacetDataCache _multiCache;
            private readonly OpenBitSet _openBitSet;
            private readonly FacetDataCache _dataCache;

            public BitSetRandomAccessDocIdSet(bool multi, MultiValueFacetDataCache multiCache, OpenBitSet openBitSet, FacetDataCache dataCache)
            {
                _multi = multi;
                _multiCache = multiCache;
                _openBitSet = openBitSet;
                _dataCache = dataCache;
            }

            public override DocIdSetIterator GetIterator()
            {
                if (_multi)
                {
                    return new MultiValueORFacetFilter.MultiValueOrFacetDocIdSetIterator(_multiCache, _openBitSet);
                }
                else
                {
                    return new FacetOrFilter.FacetOrDocIdSetIterator(_dataCache, _openBitSet);
                }
            }

            public override bool Get(int docId)
            {
                if (_multi)
                {
                    return _multiCache.NestedArray.Contains(docId, _openBitSet);
                }
                else
                {
                    return _openBitSet.FastGet(_dataCache.OrderArray.Get(docId));
                }
            }
        }

        public override double GetFacetSelectivity(BoboSegmentReader reader)
        {
            FacetDataCache dataCache = facetDataCacheBuilder.Build(reader);
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
