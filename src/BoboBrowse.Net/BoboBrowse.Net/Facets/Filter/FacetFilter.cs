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

// Version compatibility level: 3.2.0
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
            protected int _doc;
            protected readonly int _index;
            protected readonly int _maxID;
            protected readonly BigSegmentedArray _orderArray;

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
                return EmptyDocIdSet.Instance;
            }
            else
            {
                return new FacetDataRandomAccessDocIdSet(dataCache, index);
            }
        }

        public class FacetDataRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly FacetDataCache _dataCache;
	        private readonly BigSegmentedArray _orderArray;
	        private readonly int _index;

            internal FacetDataRandomAccessDocIdSet(FacetDataCache dataCache, int index)
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
