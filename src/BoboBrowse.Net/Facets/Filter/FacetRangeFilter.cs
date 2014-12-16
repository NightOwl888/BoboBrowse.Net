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

// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;

    public sealed class FacetRangeFilter<T> : RandomAccessFilter
    {
        private static long serialVersionUID = 1L;

        private readonly FacetHandler<FacetDataCache<T>> _facetHandler;
        private readonly string _rangeString;

        public FacetRangeFilter(FacetHandler<FacetDataCache<T>> facetHandler, string rangeString)
        {
            _facetHandler = facetHandler;
            _rangeString = rangeString;
        }

        public override double GetFacetSelectivity(BoboIndexReader reader)
        {
            double selectivity = 0;
            FacetDataCache<T> dataCache = _facetHandler.GetFacetData(reader);
            int[] range = Parse(dataCache, _rangeString);
            if (range != null)
            {
                int accumFreq = 0;
                for (int idx = range[0]; idx <= range[1]; ++idx)
                {
                    accumFreq += dataCache.freqs[idx];
                }
                int total = reader.MaxDoc;
                selectivity = (double)accumFreq / (double)total;
            }
            if (selectivity > 0.999)
            {
                selectivity = 1.0;
            }
            return selectivity;
        }


        private sealed class FacetRangeDocIdSetIterator : DocIdSetIterator
        {
            private int _doc = -1;

            private int _minID = int.MaxValue; // FIXME : ??? max value 
            private int _maxID = -1;
            private readonly int _start;
            private readonly int _end;
            private readonly BigSegmentedArray _orderArray;

            public FacetRangeDocIdSetIterator(int start, int end, FacetDataCache<T> dataCache)
            {
                _start = start;
                _end = end;
                for (int i = start; i <= end; ++i)
                {
                    _minID = Math.Min(_minID, dataCache.minIDs[i]);
                    _maxID = Math.Max(_maxID, dataCache.maxIDs[i]);
                }
                _doc = Math.Max(-1, _minID - 1);
                _orderArray = dataCache.orderArray;
            }

            public override int DocID()
            {
                return _doc;
            }

            public override int NextDoc()
            {
                _doc = (_doc < _maxID) ? _orderArray.FindValueRange(_start, _end, (_doc + 1), _maxID) : NO_MORE_DOCS;
                return _doc;
            }

            public sealed override int Advance(int id)
            {
                if (_doc < id)
                {
                    _doc = (id <= _maxID) ? _orderArray.FindValueRange(_start, _end, id, _maxID) : NO_MORE_DOCS;
                    return _doc;
                }
                return NextDoc();
            }
        }

        private sealed class MultiFacetRangeDocIdSetIterator : DocIdSetIterator
        {
            private int _doc = -1;
  
            private int _minID = int.MaxValue;
            private int _maxID = -1;
            private readonly int _start;
            private readonly int _end;
            private readonly BigNestedIntArray nestedArray;

            public MultiFacetRangeDocIdSetIterator(int start, int end, MultiValueFacetDataCache dataCache)
            {
                _start = start;
                _end = end;
                for (int i = start; i <= end; ++i)
                {
                    _minID = Math.Min(_minID, dataCache.minIDs[i]);
                    _maxID = Math.Max(_maxID, dataCache.maxIDs[i]);
                }
                _doc = Math.Max(-1, _minID - 1);
                nestedArray = dataCache._nestedArray;
            }

            public sealed override int DocID()
            {
                return _doc;
            }

            public override int NextDoc()
            {
                _doc = (_doc < _maxID) ? nestedArray.FindValuesInRange(_start, _end, (_doc + 1), _maxID) : NO_MORE_DOCS;
                return _doc;
            }

            public override int Advance(int id)
            {
                if (_doc < id)
                {
                    _doc = (_doc <= _maxID) ? nestedArray.FindValuesInRange(_start, _end, id, _maxID) : NO_MORE_DOCS;
                    return _doc;
                }
                return NextDoc();
            }
        }

        public class FacetRangeValueConverter : FacetValueConverter
        {
            public static FacetRangeValueConverter instance = new FacetRangeValueConverter();
            private FacetRangeValueConverter()
            { }

            public int[] Convert(FacetDataCache<T> dataCache, T[] vals)
            {
                return ConvertIndexes(dataCache, vals);
            }
        }

        public static int[] ConvertIndexes(FacetDataCache<T> dataCache, T[] vals)
        {
            List<int> list = new List<int>();
            foreach (T val in vals)
            {
                int[] range = Parse(dataCache, Convert.ToString(val));
                if (range != null)
                {
                    for (int i = range[0]; i <= range[1]; ++i)
                    {
                        list.Add(i);
                    }
                }
            }
            return list.ToArray();
        }

        public static int[] Parse(FacetDataCache<T> dataCache, string rangeString)
        {
            string[] ranges = GetRangeStrings(rangeString);
            string lower = ranges[0];
            string upper = ranges[1];
            string includeLower = ranges[2];
            string includeUpper = ranges[3];

            bool incLower = true, incUpper = true;

            if ("false".Equals(includeLower))
                incLower = false;
            if ("false".Equals(includeUpper))
                incUpper = false;

            if ("*".Equals(lower))
            {
                lower = null;
            }

            if ("*".Equals(upper))
            {
                upper = null;
            }

            int start, end;
            if (lower == null)
            {
                start = 1;
            }
            else
            {
                start = dataCache.valArray.IndexOf(lower);
                if (start < 0)
                {
                    start = -(start + 1);
                }
                else
                {
                    //when the lower value is in the list, we need to consider if we want this lower value included or not;
                    if (incLower == false)
                    {
                        start++;
                    }

                }
            }

            if (upper == null)
            {
                end = dataCache.valArray.Size - 1;
            }
            else
            {
                end = dataCache.valArray.IndexOf(upper);
                if (end < 0)
                {
                    end = -(end + 1);
                    end = Math.Max(0, end - 1);
                }
                else
                {
                    //when the lower value is in the list, we need to consider if we want this lower value included or not;
                    if (incUpper == false)
                    {
                        end--;
                    }
                }
            }

            return new int[] { start, end };
        }

        public static string[] GetRangeStrings(string rangeString)
        {
            int index2 = rangeString.IndexOf(" TO ");
            bool incLower = true, incUpper = true;

            if (rangeString.Trim().StartsWith("("))
                incLower = false;

            if (rangeString.Trim().EndsWith(")"))
                incUpper = false;

            int index = -1, index3 = -1;

            if (incLower == true)
                index = rangeString.IndexOf('[');
            else if (incLower == false)
                index = rangeString.IndexOf('(');

            if (incUpper == true)
                index3 = rangeString.IndexOf(']');
            else if (incUpper == false)
                index3 = rangeString.IndexOf(')');

            string lower, upper;
            try
            {
                lower = rangeString.Substring(index + 1, index2).Trim();
                upper = rangeString.Substring(index2 + 4, index3).Trim();

                return new String[] { lower, upper, Convert.ToString(incLower).ToLower(), Convert.ToString(incUpper).ToLower() };
            }
            catch (RuntimeException re)
            {
                throw re;
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboIndexReader reader)
        {
            FacetDataCache<T> dataCache = _facetHandler.GetFacetData(reader);

            bool multi = dataCache is MultiValueFacetDataCache<T>;    
            BigNestedIntArray nestedArray = multi ? ((MultiValueFacetDataCache<T>)dataCache)._nestedArray : null;
            int[] range = Parse(dataCache, _rangeString);
    
            if (range == null) return null;
    
            if (range[0] > range[1])
            {
                return EmptyDocIdSet.GetInstance();
            }
    
            if (range[0] == range[1] && range[0] < 0)
            {
	            return EmptyDocIdSet.GetInstance();
            }


        }

        private class RangeRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly int _start;
            private readonly int _end;
            private readonly FacetDataCache<T> _dataCache;
            private readonly BigNestedIntArray _nestedArray;
            private readonly bool _multi;

            public RangeRandomAccessDocIdSet(int start, int end, FacetDataCache<T> dataCache, BigNestedIntArray nestedArray, bool multi)
            {
                _start = start;
                _end = end;
                _dataCache = dataCache;
                _nestedArray = nestedArray;
                _multi = multi;
            }

            public override bool Get(int docId)
            {
                if (_multi)
                {
                    _nestedArray.ContainsValueInRange(docId, _start, _end);
                }
                int index = _dataCache.orderArray.Get(docId);
                return index >= _start && index <= _end;
            }

            public override DocIdSetIterator Iterator()
            {
                if (_multi)
                {
                    return new MultiFacetRangeDocIdSetIterator(_start, _end, (MultiValueFacetDataCache<T>)dataCache);
                }
                else
                {
                    return new FacetRangeDocIdSetIterator(_start, _end, _dataCache);
                }
            }

        }
    }
}
