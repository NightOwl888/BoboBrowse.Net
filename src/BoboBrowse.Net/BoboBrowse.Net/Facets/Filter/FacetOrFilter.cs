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
    using Lucene.Net.Util;

    public class FacetOrFilter : RandomAccessFilter
    {
        protected readonly IFacetHandler _facetHandler;
        protected readonly string[] _vals;
        private readonly bool _takeCompliment;
        private readonly IFacetValueConverter _valueConverter;

        public FacetOrFilter(IFacetHandler facetHandler, string[] vals, bool takeCompliment)
            : this(facetHandler, vals, takeCompliment, FacetValueConverter_Fields.DEFAULT)
        {
        }

        public FacetOrFilter(IFacetHandler facetHandler, string[] vals, bool takeCompliment, IFacetValueConverter valueConverter)
        {
            _facetHandler = facetHandler;
            _vals = vals;
            _takeCompliment = takeCompliment;
            _valueConverter = valueConverter;
        }

        public override double GetFacetSelectivity(BoboSegmentReader reader)
        {
            double selectivity = 0;
            FacetDataCache dataCache = _facetHandler.GetFacetData<FacetDataCache>(reader);
            int accumFreq = 0;
            foreach (string val in _vals)
            {
                int idx = dataCache.ValArray.IndexOf(val);
                if (idx < 0)
                {
                    continue;
                }
                accumFreq += dataCache.Freqs[idx];
            }
            int total = reader.MaxDoc;
            selectivity = (double)accumFreq / (double)total;
            if (selectivity > 0.999)
            {
                selectivity = 1.0;
            }
            if (_takeCompliment)
            {
                selectivity = 1.0 - selectivity;
            }
            return selectivity;
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboSegmentReader reader)
        {
            if (_vals.Length == 0)
            {
                return EmptyDocIdSet.Instance;
            }
            else
            {
                return new FacetOrRandomAccessDocIdSet(_facetHandler, reader, _vals, _valueConverter, _takeCompliment);
            }
        }

        public class FacetOrRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly OpenBitSet _bitset;
	        private readonly BigSegmentedArray _orderArray;
	        private readonly FacetDataCache _dataCache;
            private readonly int[] _index;

            internal FacetOrRandomAccessDocIdSet(IFacetHandler facetHandler, BoboSegmentReader reader, 
                string[] vals, IFacetValueConverter valConverter, bool takeCompliment)
            {
		        _dataCache = facetHandler.GetFacetData<FacetDataCache>(reader);
		        _orderArray = _dataCache.OrderArray;
	            _index = valConverter.Convert(_dataCache, vals);
	    
	            _bitset = new OpenBitSet(_dataCache.ValArray.Count);
	            foreach (int i in _index)
	            {
	              _bitset.FastSet(i);
	            }
      
                if (takeCompliment)
                {
                    // flip the bits
                    for (int i = 0; i < _dataCache.ValArray.Count; ++i)
                    {
                        _bitset.FastFlip(i);
                    }
                }
	        }

            public override bool Get(int docId)
            {
                return _bitset.FastGet(_orderArray.Get(docId));
            }

            public override DocIdSetIterator GetIterator()
            {
                return new FacetOrDocIdSetIterator(_dataCache, _bitset);
            }
        }

        public class FacetOrDocIdSetIterator : DocIdSetIterator
        {
            protected int _doc;
            protected readonly FacetDataCache _dataCache;
            protected int _maxID;
            protected readonly OpenBitSet _bitset;
            protected readonly BigSegmentedArray _orderArray;

            public FacetOrDocIdSetIterator(FacetDataCache dataCache, OpenBitSet bitset)
            {
                _dataCache = dataCache;
                _orderArray = dataCache.OrderArray;
                _bitset = bitset;

                _doc = int.MaxValue;
                _maxID = -1;
                int size = _dataCache.ValArray.Count;
                for (int i = 0; i < size; ++i)
                {
                    if (!bitset.FastGet(i))
                    {
                        continue;
                    }
                    if (_doc > _dataCache.MinIDs[i])
                    {
                        _doc = _dataCache.MinIDs[i];
                    }
                    if (_maxID < _dataCache.MaxIDs[i])
                    {
                        _maxID = _dataCache.MaxIDs[i];
                    }
                }
                _doc--;
                if (_doc < 0)
                    _doc = -1;
            }

            public override int DocID()
            {
                return _doc;
            }

            public override int NextDoc()
            {
                _doc = (_doc < _maxID) ? _orderArray.FindValues(_bitset, _doc + 1, _maxID) : NO_MORE_DOCS;
                return _doc;
            }

            public override int Advance(int id)
            {
                if (_doc < id)
                {
                    _doc = (id <= _maxID) ? _orderArray.FindValues(_bitset, id, _maxID) : NO_MORE_DOCS;
                    return _doc;
                }
                return NextDoc();
            }

            public override long Cost()
            {
                return 0;
            }
        }
    }
}
