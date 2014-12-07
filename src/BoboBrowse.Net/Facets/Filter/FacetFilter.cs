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
    using Lucene.Net.Index;
    using System;

    public class FacetFilter<T> : RandomAccessFilter
    {
        private static long serialVersionUID = 1L;

        protected readonly FacetHandler<FacetDataCache<T>> _facetHandler;
        protected readonly string _value;


        public FacetFilter(FacetHandler<FacetDataCache<T>> facetHandler, string value)
        {
            _facetHandler = facetHandler;
            _value = value;
        }

        public override double GetFacetSelectivity(BoboIndexReader reader)
        {
            double selectivity = 0;
            FacetDataCache<T> dataCache = _facetHandler.GetFacetData(reader);
            int idx = dataCache.valArray.IndexOf(_value);
            if (idx < 0)
            {
                return 0.0;
            }
            int freq = dataCache.freqs[idx];
            int total = reader.MaxDoc;
            selectivity = (double)freq / (double)total;
            return selectivity;
        }

        public class FacetDocIdSetIterator : DocIdSetIterator
        {
            protected internal int _doc;
            protected internal readonly int _index;
            protected internal readonly int _maxID;
            protected internal readonly BigSegmentedArray _orderArray;

            public FacetDocIdSetIterator(FacetDataCache<T> dataCache, int index)
            {
                _index = index;
                _doc = Math.Max(-1, dataCache.minIDs[_index] - 1);
                _maxID = dataCache.maxIDs[_index];
                _orderArray = dataCache.orderArray;
            }

            public override int DocID()
            {
                return _doc;
            }

            public override int NextDoc()
            {
                _doc = (_doc < _maxID) ? _orderArray.FindValue(_index, _doc + 1, _maxID) : NO_MORE_DOCS;
                return _doc;
            }

            public override int Advance(int id)
            {
                if (_doc < id)
                {
                    _doc = (id <= _maxID) ? _orderArray.FindValue(_index, id, _maxID) : NO_MORE_DOCS;
                    return _doc;
                }
                return NextDoc();
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboIndexReader reader)
        {
            FacetDataCache<T> dataCache = _facetHandler.GetFacetData(reader);
            int index = dataCache.valArray.IndexOf(_value);
            if (index < 0)
            {
                return EmptyDocIdSet.GetInstance();
            }
            else
            {
                return new FacetDataRandomAccessDocIdSet(dataCache, index);
            }
        }

        private class FacetDataRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly FacetDataCache<T> _dataCache;
	        private readonly BigSegmentedArray _orderArray;
	        private readonly int _index;

            public FacetDataRandomAccessDocIdSet(FacetDataCache<T> dataCache, int index)
            {
                _dataCache = dataCache;
                _orderArray = dataCache.orderArray;
                _index = index;
            }

            public override bool Get(int docId)
            {
                return _orderArray.Get(docId) == _index;
            }

            public override DocIdSetIterator Iterator()
            {
                return new FacetDocIdSetIterator(_dataCache, _index);
            }
        }
    }
}
