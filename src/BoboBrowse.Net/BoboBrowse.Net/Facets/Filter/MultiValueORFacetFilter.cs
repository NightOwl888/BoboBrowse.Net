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
    using Lucene.Net.Util;

    public class MultiValueORFacetFilter : RandomAccessFilter
    {
        //private static long serialVersionUID = 1L; // NOT USED
        private readonly IFacetHandler _facetHandler;
        private readonly string[] _vals;
        private readonly bool _takeCompliment;
        private readonly IFacetValueConverter _valueConverter;

        public MultiValueORFacetFilter(IFacetHandler facetHandler, string[] vals, bool takeCompliment)
            : this(facetHandler, vals, FacetValueConverter_Fields.DEFAULT, takeCompliment)
        {}
  
        public MultiValueORFacetFilter(IFacetHandler facetHandler, string[] vals, IFacetValueConverter valueConverter, bool takeCompliment)
        {
            _facetHandler = facetHandler;
            _vals = vals;
            _valueConverter = valueConverter;
            _takeCompliment = takeCompliment;
        }

        public override double GetFacetSelectivity(BoboIndexReader reader)
        {
            double selectivity = 0;
            MultiValueFacetDataCache dataCache = _facetHandler.GetFacetData<MultiValueFacetDataCache>(reader);
            int[] idxes = _valueConverter.Convert(dataCache, _vals);
            if (idxes == null)
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


        public sealed class MultiValueOrFacetDocIdSetIterator : FacetOrFilter.FacetOrDocIdSetIterator
        {
            private readonly BigNestedIntArray _nestedArray;
            public MultiValueOrFacetDocIdSetIterator(MultiValueFacetDataCache dataCache, OpenBitSet bs)
                : base(dataCache, bs)
            {
                _nestedArray = dataCache.NestedArray;
            }           

            public override int NextDoc()
            {
                _doc = (_doc < _maxID) ? _nestedArray.FindValues(_bitset, (_doc + 1), _maxID) : NO_MORE_DOCS;
                return _doc;
            }

            public override int Advance(int id)
            {
                if (_doc < id)
                {
                    _doc = (id <= _maxID) ? _nestedArray.FindValues(_bitset, id, _maxID) : NO_MORE_DOCS;
                    return _doc;
                }
                return NextDoc();
            }            
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboIndexReader reader)
        {
            MultiValueFacetDataCache dataCache = _facetHandler.GetFacetData<MultiValueFacetDataCache>(reader);
            int[] index = _valueConverter.Convert(dataCache, _vals);
            //BigNestedIntArray nestedArray = dataCache.NestedArray;
            OpenBitSet bitset = new OpenBitSet(dataCache.ValArray.Count);

            foreach (int i in index)
            {
                bitset.FastSet(i);
            }

            if (_takeCompliment)
            {
                // flip the bits
                int size = dataCache.ValArray.Count;
                for (int i = 0; i < size; ++i)
                {
                    bitset.FastFlip(i);
                }
            }

            long count = bitset.Cardinality();

            if (count == 0)
            {
                return new EmptyRandomAccessDocIdSet();
            }
            else
            {
                return new MultiRandomAccessDocIdSet(dataCache, bitset);
            }
        }

        private class EmptyRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private DocIdSet empty = EmptyDocIdSet.Instance;

            public override bool Get(int docId)
            {
                return false;
            }

            public override DocIdSetIterator Iterator()
            {
                return empty.Iterator();
            }
        }

        private class MultiRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly MultiValueFacetDataCache dataCache;
            private readonly OpenBitSet bitset;
            private readonly BigNestedIntArray nestedArray;

            public MultiRandomAccessDocIdSet(MultiValueFacetDataCache dataCache, OpenBitSet bitset)
            {
                this.dataCache = dataCache;
                this.bitset = bitset;
                this.nestedArray = dataCache.NestedArray;
            }

            public override DocIdSetIterator Iterator()
            {
                return new MultiValueOrFacetDocIdSetIterator(this.dataCache, this.bitset);
            }

            public override bool Get(int docId)
            {
                return this.nestedArray.Contains(docId, this.bitset);
            }
        }
    }
}
