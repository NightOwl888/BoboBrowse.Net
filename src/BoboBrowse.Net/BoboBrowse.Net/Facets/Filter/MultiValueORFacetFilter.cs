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
            private DocIdSet empty = EmptyDocIdSet.GetInstance();

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
