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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Range;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;

    public class MultiValueFacetFilter : RandomAccessFilter
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private readonly string _val;
        private readonly MultiDataCacheBuilder multiDataCacheBuilder;

        public MultiValueFacetFilter(MultiDataCacheBuilder multiDataCacheBuilder, string val)
        {
            this.multiDataCacheBuilder = multiDataCacheBuilder;
            _val = val;
        }

        public override double GetFacetSelectivity(BoboIndexReader reader)
        {
            double selectivity = 0;
            FacetDataCache dataCache = multiDataCacheBuilder.Build(reader);
            int idx = dataCache.ValArray.IndexOf(_val);
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
            private readonly BigNestedIntArray _nestedArray;

            public MultiValueFacetDocIdSetIterator(MultiValueFacetDataCache dataCache, int index)
                : base(dataCache, index)
            {
                _nestedArray = dataCache.NestedArray;
            }           

            public override int NextDoc()
            {
                _doc = (_doc < _maxID ? _nestedArray.FindValue(_index, (_doc + 1), _maxID) : NO_MORE_DOCS);
                return _doc;
            }

            public override int Advance(int id)
            {
                if (_doc < id)
                {
                    _doc = (id <= _maxID ? _nestedArray.FindValue(_index, id, _maxID) : NO_MORE_DOCS);
                    return _doc;
                }
                return NextDoc();
            }            
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboIndexReader reader)
        {
            MultiValueFacetDataCache dataCache = (MultiValueFacetDataCache)multiDataCacheBuilder.Build(reader);
            int index = dataCache.ValArray.IndexOf(_val);
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
            private readonly MultiValueFacetDataCache _dataCache;
            private readonly int _index;
            private readonly BigNestedIntArray _nestedArray;

            public MultiValueRandomAccessDocIdSet(MultiValueFacetDataCache dataCache, int index)
            {
                _dataCache = dataCache;
                _index = index;
                _nestedArray = dataCache.NestedArray;
            }

            public override DocIdSetIterator Iterator()
            {
                return new MultiValueFacetDocIdSetIterator(_dataCache, _index);
            }
            public override bool Get(int docId)
            {
                return _nestedArray.Contains(docId, _index);
            }
        }
    }
}
