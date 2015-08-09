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
