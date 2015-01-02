// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using System;
    using System.Collections.Generic;

    public class HistogramFacetHandler<T> : RuntimeFacetHandler<FacetDataNone> 
        where T : struct
    {
        private readonly string _dataHandlerName;
        private readonly T _start;
        private readonly T _end;
        private readonly T _unit;

        private IFacetHandler _dataFacetHandler;

        public HistogramFacetHandler(string name, string dataHandlerName, T start, T end, T unit)
            : base(name, new string[] { dataHandlerName })
        {
            _dataHandlerName = dataHandlerName;
            _start = start;
            _end = end;
            _unit = unit;
        }

        public override FacetDataNone Load(BoboIndexReader reader)
        {
            _dataFacetHandler = reader.GetFacetHandler(_dataHandlerName);
            if (_dataFacetHandler is RangeFacetHandler)
            {
                if (((RangeFacetHandler)_dataFacetHandler).HasPredefinedRanges)
                {
                    throw new NotSupportedException("underlying range facet handler should not have the predefined ranges");
                }
            }
            return FacetDataNone.instance;
        }

        public override DocComparatorSource GetDocComparatorSource()
        {
            throw new NotSupportedException();
        }

        public override string[] GetFieldValues(BoboIndexReader reader, int id)
        {
            return null;
        }

        public override object[] GetRawFieldValues(BoboIndexReader reader, int id)
        {
            return null;
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, Properties prop)
        {
            return _dataFacetHandler.BuildRandomAccessFilter(value, prop);
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties prop)
        {
            return _dataFacetHandler.BuildRandomAccessAndFilter(vals, prop);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot)
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

            public override IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase)
            {
                FacetDataCache dataCache = (FacetDataCache)reader.GetFacetData(_dataHandlerName);
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
            private readonly int[] _count;
            private readonly ITermValueList _valArray;
            private readonly IFacetCountCollector _baseCollector;
            private readonly string _facetName;

            private bool _isAggregated;

            public HistogramCollector(string facetName, IFacetCountCollector baseCollector, FacetDataCache dataCache, FacetSpec ospec, T start, T end, T unit)
            {
                _facetName = facetName;
                _baseCollector = baseCollector;
                _valArray = dataCache.ValArray;
                _ospec = ospec;
                _isAggregated = false;
                _start = start;
                _end = end;
                _unit = unit;
                _count = new int[CountArraySize()];
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
            public virtual int[] GetCountDistribution()
            {
                if (!_isAggregated) Aggregate();
                return _count;
            }

            public virtual BrowseFacet GetFacet(string value)
            {
                if (!_isAggregated) Aggregate();

                int idx = int.Parse(value);
                if (idx >= 0 && idx < _count.Length)
                {
                    return new BrowseFacet(value, _count[idx]);
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
                if (idx >= 0 && idx < _count.Length)
                {
                    return _count[idx];
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

                int[] baseCounts = _baseCollector.GetCountDistribution();
                if (_start is long)
                {
                    long start = Convert.ToInt64(_start);
                    long unit = Convert.ToInt64(_unit);
                    TermLongList valArray = (TermLongList)_valArray;
                    for (int i = startIdx; i < endIdx; i++)
                    {
                        long val = valArray.GetPrimitiveValue(i);
                        int idx = (int)((val - start) / unit);
                        if (idx >= 0 && idx < _count.Length)
                        {
                            _count[idx] += baseCounts[i];
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
                        if (idx >= 0 && idx < _count.Length)
                        {
                            _count[idx] += baseCounts[i];
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
                        if (idx >= 0 && idx < _count.Length)
                        {
                            _count[idx] += baseCounts[i];
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
                    if (max <= 0) max = _count.Length;

                    List<BrowseFacet> facetColl;
                    FacetSpec.FacetSortSpec sortspec = _ospec.OrderBy;
                    if (sortspec == FacetSpec.FacetSortSpec.OrderValueAsc)
                    {
                        facetColl = new List<BrowseFacet>(max);
                        for (int i = 0; i < _count.Length; ++i)
                        {
                            int hits = _count[i];
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
            private readonly int[] _count;
            private readonly int _maxMinusOne;
            private int _idx;

            public HistogramFacetIterator(int[] count, string format)
            {
                _idx = -1;
                _count = count;
                _maxMinusOne = count.Length - 1;
                _format = format;
            }

            public override string Next()
            {
                if (HasNext())
                {
                    base.count = _count[++_idx];
                    return (_facet = _idx).ToString();
                }
                return null;
            }

            public override string Next(int minHits)
            {
                while (_idx < _maxMinusOne)
                {
                    if (_count[++_idx] >= minHits)
                    {
                        base.count = _count[_idx];
                        return (_facet = _idx).ToString();
                    }
                }
                return null;
            }

            public override int NextInt()
            {
                if (HasNext())
                {
                    base.count = _count[++_idx];
                    return (_facet = _idx);
                }
                return TermIntList.VALUE_MISSING;
            }

            public override int NextInt(int minHits)
            {
                while (_idx < _maxMinusOne)
                {
                    if (_count[++_idx] >= minHits)
                    {
                        base.count = _count[_idx];
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
