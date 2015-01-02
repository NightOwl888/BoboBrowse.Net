//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Written in Java.
//* 
//* Copyright (C) 2005-2006  John Wang
//*
//* This library is free software; you can redistribute it and/or
//* modify it under the terms of the GNU Lesser General Public
//* License as published by the Free Software Foundation; either
//* version 2.1 of the License, or (at your option) any later version.
//*
//* This library is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;
    using Lucene.Net.Util;

    public class FacetOrFilter : RandomAccessFilter
    {
        //private static long serialVersionUID = 1L; // NOT USED

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

        public override double GetFacetSelectivity(BoboIndexReader reader)
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

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboIndexReader reader)
        {
            if (_vals.Length == 0)
            {
                return EmptyDocIdSet.GetInstance();
            }
            else
            {
                return new FacetOrRandomAccessDocIdSet(_facetHandler, reader, _vals, _valueConverter, _takeCompliment);
            }
        }

        public class FacetOrRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private OpenBitSet _bitset;
	        private readonly BigSegmentedArray _orderArray;
	        private readonly FacetDataCache _dataCache;
            private readonly int[] _index;

            internal FacetOrRandomAccessDocIdSet(IFacetHandler facetHandler, BoboIndexReader reader, 
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

            public override DocIdSetIterator Iterator()
            {
                return new FacetOrDocIdSetIterator(_dataCache, _bitset);
            }
        }

        public class FacetOrDocIdSetIterator : DocIdSetIterator
        {
            protected internal int _doc;
            protected internal readonly FacetDataCache _dataCache;
            protected internal int _maxID;
            protected internal readonly OpenBitSet _bitset;
            protected internal readonly BigSegmentedArray _orderArray;

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
        }
    }
}
