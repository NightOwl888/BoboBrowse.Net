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
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;

    public class MultiValueFacetFilter : RandomAccessFilter
    {
        private readonly MultiValueFacetDataCache _dataCache;
        private readonly BigNestedIntArray _nestedArray;
        private readonly int _index;

        public MultiValueFacetFilter(MultiValueFacetDataCache dataCache, int index)
        {
            _dataCache = dataCache;
            _nestedArray = dataCache._nestedArray;
            _index = index;
        }

        private sealed class MultiValueFacetDocIdSetIterator : FacetFilter.FacetDocIdSetIterator
        {
            private readonly BigNestedIntArray _nestedArray;

            public MultiValueFacetDocIdSetIterator(MultiValueFacetDataCache dataCache, int index)
                : base(dataCache, index)
            {
                _nestedArray = dataCache._nestedArray;
            }           

            public override int NextDoc()
            {
                while (doc < maxID) // not yet reached end
                {
                    if (_nestedArray.Contains(++doc, index))
                    {
                        return doc;
                    }
                }
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override int Advance(int target)
            {
                if (target > doc)
                {
                    doc = target - 1;
                    return NextDoc();
                }
                return NextDoc();
            }            
        }

        private class RandomRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private MultiValueFacetFilter parent;

            public RandomRandomAccessDocIdSet(MultiValueFacetFilter parent)
            {
                this.parent = parent;
            }

            public override DocIdSetIterator Iterator()
            {
                return new MultiValueFacetDocIdSetIterator(parent._dataCache, parent._index);
            }
            public override bool Get(int docId)
            {
                return parent._nestedArray.Contains(docId, parent._index);
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader)
        {
            if (_index < 0)
            {
                return EmptyDocIdSet.GetInstance();
            }
            else
            {
                return new RandomRandomAccessDocIdSet(this);
            }
        }
    }
}
