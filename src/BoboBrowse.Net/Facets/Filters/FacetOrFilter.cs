//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Written in Java.
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
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using BoboBrowse.Net.Util;

    public class FacetOrFilter : RandomAccessFilter
    {
        protected internal readonly FacetDataCache dataCache;
        protected internal readonly BigSegmentedArray orderArray;
        protected internal readonly int[] index;
        private OpenBitSet bitset;

        public FacetOrFilter(FacetDataCache dataCache, int[] index)
            : this(dataCache, index, false)
        {
        }

        public FacetOrFilter(FacetDataCache dataCache, int[] index, bool takeCompliment)
        {
            this.dataCache = dataCache;
            orderArray = dataCache.orderArray;
            this.index = index;
            bitset = new OpenBitSet(this.dataCache.valArray.Count);
            foreach (int i in this.index)
            {
                bitset.FastSet(i);
            }
            if (takeCompliment)
            {
                bitset.Flip(0, this.dataCache.valArray.Count);
            }
        }

        private class EmptyDocIdSetContainer : RandomAccessDocIdSet
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

        private class FacetDocIdSetContainer : RandomAccessDocIdSet
        {
            private FacetOrFilter parent;

            public FacetDocIdSetContainer(FacetOrFilter parent)
            {
                this.parent = parent;
            }

            public override DocIdSetIterator Iterator()
            {
                return new FacetOrDocIdSetIterator(parent.dataCache, parent.index, parent.bitset);
            }

            public override bool Get(int docId)
            {
                return parent.bitset.FastGet(parent.orderArray.Get(docId));
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader)
        {
            if (index.Length == 0)
            {
                return new EmptyDocIdSetContainer();
            }
            else
            {
                return new FacetDocIdSetContainer(this);
            }
        }

        public class FacetOrDocIdSetIterator : DocIdSetIterator
        {
            protected internal int _doc;
            protected internal readonly FacetDataCache _dataCache;
            protected internal readonly int[] _index;
            protected internal int _maxID;
            protected internal readonly OpenBitSet _bitset;
            protected internal readonly BigSegmentedArray _orderArray;

            public FacetOrDocIdSetIterator(FacetDataCache dataCache, int[] index, OpenBitSet bitset)
            {
                _dataCache = dataCache;
                _index = index;
                _orderArray = dataCache.orderArray;
                _bitset = bitset;

                _doc = int.MaxValue;
                _maxID = -1;
                foreach (int i in _index)
                {
                    if (_doc > _dataCache.minIDs[i])
                    {
                        _doc = _dataCache.minIDs[i];
                    }
                    if (_maxID < _dataCache.maxIDs[i])
                    {
                        _maxID = _dataCache.maxIDs[i];
                    }
                }
                _doc--;
                if (_doc < 0)
                    _doc = -1;
            }        

            public override int Advance(int target)
            {
                if (target < _doc)
                    target = _doc + 1;
                _doc = _orderArray.FindValues(_bitset, target, _maxID);
                return _doc;
            }

            public override int DocID()
            {
                return _doc;
            }

            public override int NextDoc()
            {
                _doc = _orderArray.FindValues(_bitset, _doc + 1, _maxID);
                return _doc;
            }
        }
    }
}
