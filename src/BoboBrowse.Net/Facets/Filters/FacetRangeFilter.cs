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
    using BoboBrowse.Net.Util;

    public sealed class FacetRangeFilter : RandomAccessFilter
    {
        private readonly FacetDataCache _dataCache;
        private readonly int _start;
        private readonly int _end;

        public FacetRangeFilter(FacetDataCache dataCache, int start, int end)
        {
            _dataCache = dataCache;
            _start = start;
            _end = end;
        }

        private sealed class FacetRangeDocIdSetIterator : DocIdSetIterator
        {
            private int _doc = -1;
            private int _totalFreq;
            private int _minID = int.MaxValue; // FIXME : ??? max value 
            private int _maxID = -1;
            private readonly int _start;
            private readonly int _end;
            private readonly BigSegmentedArray _orderArray;

            internal FacetRangeDocIdSetIterator(int start, int end, FacetDataCache dataCache)
            {
                _totalFreq = 0;
                _start = start;
                _end = end;
                for (int i = start; i <= end; ++i)
                {
                    _totalFreq += dataCache.freqs[i];
                    _minID = Math.Min(_minID, dataCache.minIDs[i]);
                    _maxID = Math.Max(_maxID, dataCache.maxIDs[i]);
                }
                _doc = Math.Max(-1, _minID - 1);
                _orderArray = dataCache.orderArray;
            }

            public override int Advance(int target)
            {
                if (target < _doc)
                    target = _doc + 1;
                _doc = _orderArray.FindValueRange(_start, _end, target, _maxID);
                return _doc;
            }

            public override int DocID()
            {
                return _doc;
            }

            public override int NextDoc()
            {
                _doc = _orderArray.FindValueRange(_start, _end, _doc + 1, _maxID); ;
                return _doc;
            }
        }

        private class RangeRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly FacetRangeFilter parent;

            public RangeRandomAccessDocIdSet(FacetRangeFilter parent)
            {
                this.parent = parent;
            }

            public override bool Get(int docId)
            {
                int index = parent._dataCache.orderArray.Get(docId);
                return index >= parent._start && index <= parent._end;
            }

            public override DocIdSetIterator Iterator()
            {
                return new FacetRangeDocIdSetIterator(parent._start, parent._end, parent._dataCache);
            }

        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader)
        {
            return new RangeRandomAccessDocIdSet(this);
        }
    }
}
