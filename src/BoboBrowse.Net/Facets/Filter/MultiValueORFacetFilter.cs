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

namespace BoboBrowse.Net.Facets.Filter
{
    using System;
    using Lucene.Net.Search;
    using Lucene.Net.Index;
    using Lucene.Net.Util;
    using BoboBrowse.Net.Util;
    
    public class MultiValueORFacetFilter : RandomAccessFilter
    {
        private readonly MultiValueFacetDataCache _dataCache;
        private readonly BigNestedIntArray _nestedArray;
        private readonly OpenBitSet _bitset;
        private readonly int[] _index;

        public MultiValueORFacetFilter(MultiValueFacetDataCache dataCache, int[] index)
        {
            _dataCache = dataCache;
            _nestedArray = dataCache._nestedArray;
            _index = index;
            _bitset = new OpenBitSet(_dataCache.valArray.Count);
            foreach (int i in _index)
            {
                _bitset.FastSet(i);
            }
        }

        private sealed class MultiValueFacetDocIdSetIterator : FacetOrFilter.FacetOrDocIdSetIterator
        {
            private readonly BigNestedIntArray _nestedArray;
            public MultiValueFacetDocIdSetIterator(MultiValueFacetDataCache dataCache, int[] index, OpenBitSet bs)
                : base(dataCache, index, bs)
            {
                _nestedArray = dataCache._nestedArray;
            }           

            public override int NextDoc()
            {
                while (_doc < _maxID) // not yet reached end
                {
                    if (_nestedArray.Contains(++_doc, _bitset))
                    {
                        return _doc;
                    }
                }
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override int Advance(int target)
            {
                if (_doc < target)
                {
                    _doc = target - 1;
                }

                while (_doc < _maxID) // not yet reached end
                {
                    if (_nestedArray.Contains(++_doc, _bitset))
                    {
                        return _doc;
                    }
                }
                return DocIdSetIterator.NO_MORE_DOCS;
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
            private MultiValueORFacetFilter parent;

            public MultiRandomAccessDocIdSet(MultiValueORFacetFilter parent)
            {
                this.parent = parent;
            }

            public override DocIdSetIterator Iterator()
            {
                return new MultiValueFacetDocIdSetIterator(parent._dataCache, parent._index, parent._bitset);
            }

            public override bool Get(int docId)
            {
                return parent._nestedArray.Contains(docId, parent._bitset);
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader)
        {
            if (_index.Length == 0)
            {
                return new EmptyRandomAccessDocIdSet();
            }
            else
            {
                return new MultiRandomAccessDocIdSet(this);
            }
        }
    }
}
