// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Range;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class BitSetFilter : RandomAccessFilter
    {
        private static long serialVersionUID = 1L;
    
        protected readonly IFacetDataCacheBuilder facetDataCacheBuilder;
        protected readonly IBitSetBuilder bitSetBuilder;
        private volatile OpenBitSet bitSet;
        private volatile IFacetDataCache lastCache;

        public BitSetFilter(IBitSetBuilder bitSetBuilder, IFacetDataCacheBuilder facetDataCacheBuilder)
        {
            this.bitSetBuilder = bitSetBuilder;
            this.facetDataCacheBuilder = facetDataCacheBuilder;
        }

        public OpenBitSet GetBitSet(IFacetDataCache dataCache)
        {
            if (lastCache == dataCache)
            {
                return bitSet;
            }
            bitSet = bitSetBuilder.BitSet(dataCache);
            lastCache = dataCache;
            return bitSet;
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboIndexReader reader)
        {
            IFacetDataCache dataCache = facetDataCacheBuilder.Build(reader);
            OpenBitSet openBitSet = GetBitSet(dataCache);
            long count = openBitSet.Cardinality();
            if (count == 0)
            {
                return EmptyDocIdSet.GetInstance();
            }
            else
            {
                bool multi = dataCache is IMultiValueFacetDataCache;
                IMultiValueFacetDataCache multiCache = multi ? (IMultiValueFacetDataCache)dataCache : null;
                return new BitSetRandomAccessDocIdSet(multi, multiCache, openBitSet, dataCache);
            }
        }

        public class BitSetRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly bool _multi;
            private readonly IMultiValueFacetDataCache _multiCache;
            private readonly OpenBitSet _openBitSet;
            private readonly IFacetDataCache _dataCache;

            public BitSetRandomAccessDocIdSet(bool multi, IMultiValueFacetDataCache multiCache, OpenBitSet openBitSet, IFacetDataCache dataCache)
            {
                _multi = multi;
                _multiCache = multiCache;
                _openBitSet = openBitSet;
                _dataCache = dataCache;
            }

            public override DocIdSetIterator Iterator()
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

        public override double GetFacetSelectivity(BoboIndexReader reader)
        {
            IFacetDataCache dataCache = facetDataCacheBuilder.Build(reader);
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
