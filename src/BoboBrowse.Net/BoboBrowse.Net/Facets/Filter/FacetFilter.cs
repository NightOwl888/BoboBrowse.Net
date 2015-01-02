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
    using System;

    public class FacetFilter : RandomAccessFilter
    {
        //private static long serialVersionUID = 1L; // NOT USED

        protected readonly IFacetHandler _facetHandler;
        protected readonly string _value;


        public FacetFilter(IFacetHandler facetHandler, string value)
        {
            _facetHandler = facetHandler;
            _value = value;
        }

        public override double GetFacetSelectivity(BoboIndexReader reader)
        {
            double selectivity = 0;
            FacetDataCache dataCache = _facetHandler.GetFacetData<FacetDataCache>(reader);
            int idx = dataCache.ValArray.IndexOf(_value);
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
            protected internal int _doc;
            protected internal readonly int _index;
            protected internal readonly int _maxID;
            protected internal readonly BigSegmentedArray _orderArray;

            public FacetDocIdSetIterator(FacetDataCache dataCache, int index)
            {
                _index = index;
                _doc = Math.Max(-1, dataCache.MinIDs[_index] - 1);
                _maxID = dataCache.MaxIDs[_index];
                _orderArray = dataCache.OrderArray;
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
            FacetDataCache dataCache = _facetHandler.GetFacetData<FacetDataCache>(reader);
            int index = dataCache.ValArray.IndexOf(_value);
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
            private readonly FacetDataCache _dataCache;
	        private readonly BigSegmentedArray _orderArray;
	        private readonly int _index;

            public FacetDataRandomAccessDocIdSet(FacetDataCache dataCache, int index)
            {
                _dataCache = dataCache;
                _orderArray = dataCache.OrderArray;
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
