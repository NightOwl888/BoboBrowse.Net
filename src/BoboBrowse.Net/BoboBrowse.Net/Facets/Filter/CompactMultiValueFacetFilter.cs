// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;

    public class CompactMultiValueFacetFilter : RandomAccessFilter
    {
        //private static long serialVersionUID = 1L; // NOT USED
        private FacetHandler<FacetDataCache> _facetHandler;

        private readonly string[] _vals;

        public CompactMultiValueFacetFilter(FacetHandler<FacetDataCache> facetHandler, string val)
            : this(facetHandler, new string[] { val })
        {
        }

        public CompactMultiValueFacetFilter(FacetHandler<FacetDataCache> facetHandler, string[] vals)
        {
            _facetHandler = facetHandler;
            _vals = vals;
        }

        public double GetFacetSelectivity(BoboIndexReader reader)
        {
            double selectivity = 0;
            FacetDataCache dataCache = _facetHandler.GetFacetData<FacetDataCache>(reader);
            int[] idxes = FacetDataCache.Convert(dataCache, _vals);
            if(idxes == null)
            {
                return 0.0;
            }
            int accumFreq = 0;
            foreach (int idx in idxes)
            {
                accumFreq += dataCache.Freqs[idx];
            }
            int total = reader.MaxDoc;
            selectivity = (double)accumFreq / (double)total;
            if (selectivity > 0.999) 
            {
                selectivity = 1.0;
            }
            return selectivity;
        }

        private sealed class CompactMultiValueFacetDocIdSetIterator : DocIdSetIterator
        {
            private readonly int _bits;
            private int _doc;
            private readonly int _maxID;
            private readonly BigSegmentedArray _orderArray;

            public CompactMultiValueFacetDocIdSetIterator(FacetDataCache dataCache, int[] index, int bits)
            {
                _bits = bits;
                _doc = int.MaxValue;
                _maxID = -1;
                _orderArray = dataCache.OrderArray;
                foreach (int i in index)
                {
                    if (_doc > dataCache.MinIDs[i])
                    {
                        _doc = dataCache.MinIDs[i];
                    }
                    if (_maxID < dataCache.MaxIDs[i])
                    {
                        _maxID = dataCache.MaxIDs[i];
                    }
                }
                _doc--;
                if (_doc < 0)
                {
                    _doc = -1;
                }
            }

            public sealed override int DocID()
            {
                return _doc;
            }

            public sealed override int NextDoc()
            {
                _doc = (_doc < _maxID) ? _orderArray.FindBits(_bits, (_doc + 1), _maxID) : NO_MORE_DOCS;
                return _doc;
            }

            public override int Advance(int id)
            {
                if (_doc < id)
                {
                    _doc = (id <= _maxID) ? _orderArray.FindBits(_bits, id, _maxID) : NO_MORE_DOCS;
                    return _doc;
                }
                return NextDoc();
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboIndexReader reader)
        {
            FacetDataCache dataCache = _facetHandler.GetFacetData<FacetDataCache>(reader);
            int[] indexes = FacetDataCache.Convert(dataCache, _vals);

            int bits;

            bits = 0x0;
            foreach (int i in indexes)
            {
                bits |= 0x00000001 << (i - 1);
            }

            int finalBits = bits;

            BigSegmentedArray orderArray = dataCache.OrderArray;

            if (indexes.Length == 0)
            {
                return EmptyDocIdSet.GetInstance();
            }
            else
            {
                return new CompactMultiValueFacetFilterDocIdSet(dataCache, indexes, finalBits, orderArray);
            }
        }

        private class CompactMultiValueFacetFilterDocIdSet : RandomAccessDocIdSet
        {
            private readonly FacetDataCache dataCache;
            private readonly int[] indexes;
            private readonly int finalBits;
            private readonly BigSegmentedArray orderArray;

            public CompactMultiValueFacetFilterDocIdSet(FacetDataCache dataCache, int[] indexes, int finalBits, BigSegmentedArray orderArray)
            {
                this.dataCache = dataCache;
                this.indexes = indexes;
                this.finalBits = finalBits;
                this.orderArray = orderArray;
            }

            public override DocIdSetIterator Iterator()
            {
                return new CompactMultiValueFacetDocIdSetIterator(this.dataCache, this.indexes, this.finalBits);
            }

            public override bool Get(int docId)
            {
                return (orderArray.Get(docId) & this.finalBits) != 0x0;
            }
        } 
    }
}
