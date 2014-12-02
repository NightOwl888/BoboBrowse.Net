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

namespace BoboBrowse.Net.Facets.Filters
{
    using System;
    using Lucene.Net.Search;
    using Lucene.Net.Index;
    using BoboBrowse.Net.Util;

    public class FacetFilter : RandomAccessFilter
    {
        protected internal readonly FacetDataCache dataCache;
        protected internal readonly BigSegmentedArray orderArray;
        protected internal readonly int index;

        public FacetFilter(FacetDataCache dataCache, int index)
        {
            this.dataCache = dataCache;
            orderArray = dataCache.orderArray;
            this.index = index;
        }

        public class FacetDocIdSetIterator : DocIdSetIterator
        {
            protected internal int doc;
            protected internal readonly int index;
            protected internal readonly int maxID;
            protected internal readonly BigSegmentedArray orderArray;

            public FacetDocIdSetIterator(FacetDataCache dataCache, int index)
            {
                this.index = index;
                doc = Math.Max(-1, dataCache.minIDs[this.index] - 1);
                maxID = dataCache.maxIDs[this.index];
                orderArray = dataCache.orderArray;
            }

            public override int Advance(int target)
            {
                if (target < doc)
                {
                    target = doc + 1;
                }
                doc = orderArray.FindValue(index, target, maxID);
                return doc > maxID ? DocIdSetIterator.NO_MORE_DOCS : doc;
            }

            public override int DocID()
            {
                return doc;
            }

            public override int NextDoc()
            {
                doc = orderArray.FindValue(index, doc + 1, maxID);
                return doc > maxID ? DocIdSetIterator.NO_MORE_DOCS : doc;
            }
        }

        private class EmptyFacetFilterDocIdSet : RandomAccessDocIdSet
        {
            private readonly DocIdSet empty = EmptyDocIdSet.GetInstance();

            public override bool Get(int docId)
            {
                return false;
            }

            public override DocIdSetIterator Iterator()
            {
                return empty.Iterator();
            }
        }

        private class CacheFacetFilterDocIdSet : RandomAccessDocIdSet
        {
            private readonly FacetFilter parent;

            public CacheFacetFilterDocIdSet(FacetFilter parent)
            {
                this.parent = parent;
            }

            public override DocIdSetIterator Iterator()
            {
                return new FacetDocIdSetIterator(parent.dataCache, parent.index);
            }

            public override bool Get(int docId)
            {
                return parent.orderArray.Get(docId) == parent.index;
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader)
        {
            if (index < 0)
            {
                return new EmptyFacetFilterDocIdSet();
            }
            else
            {
                return new CacheFacetFilterDocIdSet(this);
            }
        }
    }
}
