//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A runtime facet handler that works on top of a number based facet handler, called the data facet handler. 
    /// T is a subtype of Number. T and the type of the term list of the data facet handler must match. 
    /// If the the data facet handler is an instance of RangeFacetHandler, it should not have the predefined ranges, 
    /// otherwise loading of HistogramFacetHandler will fail.
    /// 
    /// You must specify the name of the data facet handler, the range (start and end) of values you want to collect 
    /// a histogram and the unit (the width of a "bin") when you construct the HistogramFacetHandler object.
    /// 
    /// Supports BrowseSelection. It simply passes the selection to the data facet handler to build a filter. 
    /// ExpandSelection of FacetSpec must be set to true to collect counts of hits that don't match the selection.
    /// 
    /// The facet values returned by the count collector are the bin numbers starting from 0. They are not the 
    /// values from the data facet handlers. It is application's responsibility to map the bin numbers to 
    /// intervals of the actual values.
    /// </summary>
    /// <typeparam name="T">A numeric data type.</typeparam>
    public class HistogramFacetHandler<T> : RuntimeFacetHandler<FacetDataNone> 
        where T : struct
    {
        private readonly string _dataHandlerName;
        private readonly T _start;
        private readonly T _end;
        private readonly T _unit;

        private IFacetHandler _dataFacetHandler;

        /// <summary>
        /// Initializes a new instance of <see cref="T:HistogramFacetHandler"/>.
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="dataHandlerName">The facet handler this one depends upon.</param>
        /// <param name="start">The start of the range of values to collect a histogram.</param>
        /// <param name="end">The end of the range of values to collect a histogram.</param>
        /// <param name="unit">The unit (the width of a "bin").</param>
        public HistogramFacetHandler(string name, string dataHandlerName, T start, T end, T unit)
            : base(name, new string[] { dataHandlerName })
        {
            _dataHandlerName = dataHandlerName;
            _start = start;
            _end = end;
            _unit = unit;
        }

        public override FacetDataNone Load(BoboSegmentReader reader)
        {
            _dataFacetHandler = reader.GetFacetHandler(_dataHandlerName);
            if (_dataFacetHandler is RangeFacetHandler)
            {
                if (((RangeFacetHandler)_dataFacetHandler).HasPredefinedRanges)
                {
                    throw new NotSupportedException("underlying range facet handler should not have the predefined ranges");
                }
            }
            return FacetDataNone.Instance;
        }

        public override DocComparatorSource GetDocComparatorSource()
        {
            throw new NotSupportedException();
        }

        public override string[] GetFieldValues(BoboSegmentReader reader, int id)
        {
            return null;
        }

        public override object[] GetRawFieldValues(BoboSegmentReader reader, int id)
        {
            return null;
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> prop)
        {
            return _dataFacetHandler.BuildRandomAccessFilter(value, prop);
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, IDictionary<string, string> prop)
        {
            return _dataFacetHandler.BuildRandomAccessAndFilter(vals, prop);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, IDictionary<string, string> prop, bool isNot)
        {
            return _dataFacetHandler.BuildRandomAccessOrFilter(vals, prop, isNot);
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec ospec)
        {
            FacetCountCollectorSource baseCollectorSrc = _dataFacetHandler.GetFacetCountCollectorSource(sel, ospec);

            return new HistogramFacetCountCollectorSource(_dataHandlerName, baseCollectorSrc, _name, ospec, _start, _end, _unit);
        }

        public class HistogramFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly string _dataHandlerName;
            private readonly FacetCountCollectorSource _baseCollectorSrc;
            private readonly string _name;
            private readonly FacetSpec _ospec;
            private readonly T _start;
            private readonly T _end;
            private readonly T _unit;

            public HistogramFacetCountCollectorSource(
                string dataHandlerName,
                FacetCountCollectorSource baseCollectorSrc,
                string name,
                FacetSpec ospec,
                T start,
                T end,
                T unit)
            {
                _dataHandlerName = dataHandlerName;
                _baseCollectorSrc = baseCollectorSrc;
                _name = name;
                _ospec = ospec;
                _start = start;
                _end = end;
                _unit = unit;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                IFacetDataCache dataCache = (IFacetDataCache)reader.GetFacetData(_dataHandlerName);
                IFacetCountCollector baseCollector = _baseCollectorSrc.GetFacetCountCollector(reader, docBase);
                return new HistogramCollector(_name, baseCollector, dataCache, _ospec, _start, _end, _unit);
            }
        }

        public class HistogramCollector : IFacetCountCollector
        {
            private const string NUMBER_FORMAT = "0000000000";
            private readonly FacetSpec _ospec;
            private readonly T _start;
            private readonly T _end;
            private readonly T _unit;
            private readonly BigSegmentedArray _count;
            private readonly ITermValueList _valArray;
            private readonly IFacetCountCollector _baseCollector;
            private readonly string _facetName;

            private bool _isAggregated;

            public HistogramCollector(string facetName, IFacetCountCollector baseCollector, IFacetDataCache dataCache, FacetSpec ospec, T start, T end, T unit)
            {
                _facetName = facetName;
                _baseCollector = baseCollector;
                _valArray = dataCache.ValArray;
                _ospec = ospec;
                _isAggregated = false;
                _start = start;
                _end = end;
                _unit = unit;
                _count = new LazyBigIntArray(CountArraySize());
            }

            private int CountArraySize()
            {
                if (_start is long)
                {
                    long range = Convert.ToInt64(_end) - Convert.ToInt64(_start);
                    return (int)(range / Convert.ToInt64(_unit)) + 1;
                }
                else if (_start is int)
                {
                    int range = Convert.ToInt32(_end) - Convert.ToInt32(_start);
                    return (range / Convert.ToInt32(_unit)) + 1;
                }
                else
                {
                    double range = Convert.ToDouble(_end)- Convert.ToDouble(_start);
                    return (int)(range / Convert.ToDouble(_unit)) + 1;
                }
            }

            /// <summary>
            /// not supported
            /// </summary>
            /// <returns></returns>
            public virtual BigSegmentedArray GetCountDistribution()
            {
                if (!_isAggregated) Aggregate();
                return _count;
            }

            public virtual BrowseFacet GetFacet(string value)
            {
                if (!_isAggregated) Aggregate();

                int idx = int.Parse(value);
                if (idx >= 0 && idx < _count.Size())
                {
                    return new BrowseFacet(value, _count.Get(idx));
                }
                return null; 
            }

            public virtual int GetFacetHitsCount(object value)
            {
                if (!_isAggregated) Aggregate();

                int idx;
                if (value is string)
                    idx = int.Parse((string)value);
                else
                    idx = Convert.ToInt32(value);
                if (idx >= 0 && idx < _count.Size())
                {
                    return _count.Get(idx);
                }
                return 0;
            }

            public void Collect(int docid)
            {
                _baseCollector.Collect(docid);
            }

            public void CollectAll()
            {
                _baseCollector.CollectAll();
            }

            private void Aggregate()
            {
                if (_isAggregated) return;

                _isAggregated = true;

                int startIdx = _valArray.IndexOf(_start);
                if (startIdx < 0) startIdx = -(startIdx + 1);

                int endIdx = _valArray.IndexOf(_end);
                if (endIdx < 0) endIdx = -(endIdx + 1);

                BigSegmentedArray baseCounts = _baseCollector.GetCountDistribution();
                if (_start is long)
                {
                    long start = Convert.ToInt64(_start);
                    long unit = Convert.ToInt64(_unit);
                    TermLongList valArray = (TermLongList)_valArray;
                    for (int i = startIdx; i < endIdx; i++)
                    {
                        long val = valArray.GetPrimitiveValue(i);
                        int idx = (int)((val - start) / unit);
                        if (idx >= 0 && idx < _count.Size())
                        {
                            _count.Add(idx, _count.Get(idx) + baseCounts.Get(i));
                        }
                    }
                }
                else if (_start is int)
                {
                    int start = Convert.ToInt32(_start);
                    int unit = Convert.ToInt32(_unit);
                    TermIntList valArray = (TermIntList)_valArray;
                    for (int i = startIdx; i < endIdx; i++)
                    {
                        int val = valArray.GetPrimitiveValue(i);
                        int idx = ((val - start) / unit);
                        if (idx >= 0 && idx < _count.Size())
                        {
                            _count.Add(idx, _count.Get(idx) + baseCounts.Get(i));
                        }
                    }
                }
                else
                {
                    double start = Convert.ToDouble(_start);
                    double unit = Convert.ToDouble(_unit);
                    for (int i = startIdx; i < endIdx; i++)
                    {
                        double val = (double)_valArray.GetRawValue(i);
                        int idx = (int)((val - start) / unit);
                        if (idx >= 0 && idx < _count.Size())
                        {
                            _count.Add(idx, _count.Get(idx) + baseCounts.Get(i));
                        }
                    }
                }
            }

            public virtual IEnumerable<BrowseFacet> GetFacets()
            {
                if (_ospec != null)
                {
                    int minCount = _ospec.MinHitCount;
                    int max = _ospec.MaxCount;
                    if (max <= 0) max = _count.Size();

                    List<BrowseFacet> facetColl;
                    FacetSpec.FacetSortSpec sortspec = _ospec.OrderBy;
                    if (sortspec == FacetSpec.FacetSortSpec.OrderValueAsc)
                    {
                        facetColl = new List<BrowseFacet>(max);
                        for (int i = 0; i < _count.Size(); ++i)
                        {
                            int hits = _count.Get(i);
                            if (hits >= minCount)
                            {
                                BrowseFacet facet = new BrowseFacet(i.ToString(NUMBER_FORMAT), hits);
                                facetColl.Add(facet);
                            }
                            if (facetColl.Count >= max) break;
                        }
                        return facetColl;
                    }
                    else
                    {
                        return FacetCountCollector_Fields.EMPTY_FACET_LIST;
                    }
                }
                else
                {
                    return FacetCountCollector_Fields.EMPTY_FACET_LIST;
                }
            }

            public virtual FacetIterator Iterator()
            {
                if (!_isAggregated) Aggregate();
                return new HistogramFacetIterator(_count, NUMBER_FORMAT);
            }

            public virtual string Name
            {
                get { return _facetName; }
            }

            public virtual void Dispose()
            { }
        }

        public class HistogramFacetIterator : IntFacetIterator
        {
            private readonly string _format;
            private readonly BigSegmentedArray _count;
            private readonly int _maxMinusOne;
            private int _idx;

            public HistogramFacetIterator(BigSegmentedArray count, string format)
            {
                _idx = -1;
                _count = count;
                _maxMinusOne = count.Size() - 1;
                _format = format;
            }

            public override string Next()
            {
                if (HasNext())
                {
                    base.count = _count.Get(++_idx);
                    return (_facet = _idx).ToString();
                }
                return null;
            }

            public override string Next(int minHits)
            {
                while (_idx < _maxMinusOne)
                {
                    if (_count.Get(++_idx) >= minHits)
                    {
                        base.count = _count.Get(_idx);
                        return (_facet = _idx).ToString();
                    }
                }
                return null;
            }

            public override int NextInt()
            {
                if (HasNext())
                {
                    base.count = _count.Get(++_idx);
                    return (_facet = _idx);
                }
                return TermIntList.VALUE_MISSING;
            }

            public override int NextInt(int minHits)
            {
                while (_idx < _maxMinusOne)
                {
                    if (_count.Get(++_idx) >= minHits)
                    {
                        base.count = _count.Get(_idx);
                        return (_facet = _idx);
                    }
                }
                return TermIntList.VALUE_MISSING; 
            }

            public override bool HasNext()
            {
                return (_idx < _maxMinusOne);
            }

            public override void Remove()
            {
                throw new NotSupportedException();
            }

            public override string Format(int val)
            {
                return val.ToString(_format);
            }

            public override string Format(object val)
            {
                if (val == null) return string.Empty;
                return string.Format("{0:" + _format + "}", val);
            }
        }
    }
}
