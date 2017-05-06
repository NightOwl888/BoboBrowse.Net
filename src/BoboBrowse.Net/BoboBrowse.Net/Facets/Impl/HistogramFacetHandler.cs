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
        private readonly string m_dataHandlerName;
        private readonly T m_start;
        private readonly T m_end;
        private readonly T m_unit;

        private IFacetHandler m_dataFacetHandler;

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
            m_dataHandlerName = dataHandlerName;
            m_start = start;
            m_end = end;
            m_unit = unit;
        }

        public override FacetDataNone Load(BoboSegmentReader reader)
        {
            m_dataFacetHandler = reader.GetFacetHandler(m_dataHandlerName);
            if (m_dataFacetHandler is RangeFacetHandler)
            {
                if (((RangeFacetHandler)m_dataFacetHandler).HasPredefinedRanges)
                {
                    throw new NotSupportedException("underlying range facet handler should not have the predefined ranges");
                }
            }
            return FacetDataNone.Instance;
        }

        public override DocComparerSource GetDocComparerSource()
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
            return m_dataFacetHandler.BuildRandomAccessFilter(value, prop);
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, IDictionary<string, string> prop)
        {
            return m_dataFacetHandler.BuildRandomAccessAndFilter(vals, prop);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, IDictionary<string, string> prop, bool isNot)
        {
            return m_dataFacetHandler.BuildRandomAccessOrFilter(vals, prop, isNot);
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec ospec)
        {
            FacetCountCollectorSource baseCollectorSrc = m_dataFacetHandler.GetFacetCountCollectorSource(sel, ospec);

            return new HistogramFacetCountCollectorSource(m_dataHandlerName, baseCollectorSrc, m_name, ospec, m_start, m_end, m_unit);
        }

        public class HistogramFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly string m_dataHandlerName;
            private readonly FacetCountCollectorSource m_baseCollectorSrc;
            private readonly string m_name;
            private readonly FacetSpec m_ospec;
            private readonly T m_start;
            private readonly T m_end;
            private readonly T m_unit;

            public HistogramFacetCountCollectorSource(
                string dataHandlerName,
                FacetCountCollectorSource baseCollectorSrc,
                string name,
                FacetSpec ospec,
                T start,
                T end,
                T unit)
            {
                m_dataHandlerName = dataHandlerName;
                m_baseCollectorSrc = baseCollectorSrc;
                m_name = name;
                m_ospec = ospec;
                m_start = start;
                m_end = end;
                m_unit = unit;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                FacetDataCache dataCache = (FacetDataCache)reader.GetFacetData(m_dataHandlerName);
                IFacetCountCollector baseCollector = m_baseCollectorSrc.GetFacetCountCollector(reader, docBase);
                return new HistogramCollector(m_name, baseCollector, dataCache, m_ospec, m_start, m_end, m_unit);
            }
        }

        public class HistogramCollector : IFacetCountCollector
        {
            private const string NUMBER_FORMAT = "0000000000";
            private readonly FacetSpec m_ospec;
            private readonly T m_start;
            private readonly T m_end;
            private readonly T m_unit;
            private readonly BigSegmentedArray m_count;
            private readonly ITermValueList m_valArray;
            private readonly IFacetCountCollector m_baseCollector;
            private readonly string m_facetName;

            private bool m_isAggregated;

            public HistogramCollector(string facetName, IFacetCountCollector baseCollector, FacetDataCache dataCache, FacetSpec ospec, T start, T end, T unit)
            {
                m_facetName = facetName;
                m_baseCollector = baseCollector;
                m_valArray = dataCache.ValArray;
                m_ospec = ospec;
                m_isAggregated = false;
                m_start = start;
                m_end = end;
                m_unit = unit;
                m_count = new LazyBigInt32Array(CountArraySize());
            }

            private int CountArraySize()
            {
                if (m_start is long)
                {
                    long range = Convert.ToInt64(m_end) - Convert.ToInt64(m_start);
                    return (int)(range / Convert.ToInt64(m_unit)) + 1;
                }
                else if (m_start is int)
                {
                    int range = Convert.ToInt32(m_end) - Convert.ToInt32(m_start);
                    return (range / Convert.ToInt32(m_unit)) + 1;
                }
                else
                {
                    double range = Convert.ToDouble(m_end)- Convert.ToDouble(m_start);
                    return (int)(range / Convert.ToDouble(m_unit)) + 1;
                }
            }

            /// <summary>
            /// not supported
            /// </summary>
            /// <returns></returns>
            public virtual BigSegmentedArray GetCountDistribution()
            {
                if (!m_isAggregated) Aggregate();
                return m_count;
            }

            public virtual BrowseFacet GetFacet(string value)
            {
                if (!m_isAggregated) Aggregate();

                int idx = int.Parse(value);
                if (idx >= 0 && idx < m_count.Length)
                {
                    return new BrowseFacet(value, m_count.Get(idx));
                }
                return null; 
            }

            public virtual int GetFacetHitsCount(object value)
            {
                if (!m_isAggregated) Aggregate();

                int idx;
                if (value is string)
                    idx = int.Parse((string)value);
                else
                    idx = Convert.ToInt32(value);
                if (idx >= 0 && idx < m_count.Length)
                {
                    return m_count.Get(idx);
                }
                return 0;
            }

            public void Collect(int docid)
            {
                m_baseCollector.Collect(docid);
            }

            public void CollectAll()
            {
                m_baseCollector.CollectAll();
            }

            private void Aggregate()
            {
                if (m_isAggregated) return;

                m_isAggregated = true;

                int startIdx = m_valArray.IndexOf(m_start);
                if (startIdx < 0) startIdx = -(startIdx + 1);

                int endIdx = m_valArray.IndexOf(m_end);
                if (endIdx < 0) endIdx = -(endIdx + 1);

                BigSegmentedArray baseCounts = m_baseCollector.GetCountDistribution();
                if (m_start is long)
                {
                    long start = Convert.ToInt64(m_start);
                    long unit = Convert.ToInt64(m_unit);
                    TermInt64List valArray = (TermInt64List)m_valArray;
                    for (int i = startIdx; i < endIdx; i++)
                    {
                        long val = valArray.GetPrimitiveValue(i);
                        int idx = (int)((val - start) / unit);
                        if (idx >= 0 && idx < m_count.Length)
                        {
                            m_count.Add(idx, m_count.Get(idx) + baseCounts.Get(i));
                        }
                    }
                }
                else if (m_start is int)
                {
                    int start = Convert.ToInt32(m_start);
                    int unit = Convert.ToInt32(m_unit);
                    TermInt32List valArray = (TermInt32List)m_valArray;
                    for (int i = startIdx; i < endIdx; i++)
                    {
                        int val = valArray.GetPrimitiveValue(i);
                        int idx = ((val - start) / unit);
                        if (idx >= 0 && idx < m_count.Length)
                        {
                            m_count.Add(idx, m_count.Get(idx) + baseCounts.Get(i));
                        }
                    }
                }
                else
                {
                    double start = Convert.ToDouble(m_start);
                    double unit = Convert.ToDouble(m_unit);
                    for (int i = startIdx; i < endIdx; i++)
                    {
                        double val = (double)m_valArray.GetRawValue(i);
                        int idx = (int)((val - start) / unit);
                        if (idx >= 0 && idx < m_count.Length)
                        {
                            m_count.Add(idx, m_count.Get(idx) + baseCounts.Get(i));
                        }
                    }
                }
            }

            public virtual ICollection<BrowseFacet> GetFacets()
            {
                if (m_ospec != null)
                {
                    int minCount = m_ospec.MinHitCount;
                    int max = m_ospec.MaxCount;
                    if (max <= 0) max = m_count.Length;

                    List<BrowseFacet> facetColl;
                    FacetSpec.FacetSortSpec sortspec = m_ospec.OrderBy;
                    if (sortspec == FacetSpec.FacetSortSpec.OrderValueAsc)
                    {
                        facetColl = new List<BrowseFacet>(max);
                        for (int i = 0; i < m_count.Length; ++i)
                        {
                            int hits = m_count.Get(i);
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
                        return FacetCountCollector.EMPTY_FACET_LIST;
                    }
                }
                else
                {
                    return FacetCountCollector.EMPTY_FACET_LIST;
                }
            }

            public virtual FacetIterator GetIterator()
            {
                if (!m_isAggregated) Aggregate();
                return new HistogramFacetIterator(m_count, NUMBER_FORMAT);
            }

            public virtual string Name
            {
                get { return m_facetName; }
            }

            public virtual void Dispose()
            { }
        }

        public class HistogramFacetIterator : Int32FacetIterator
        {
            private readonly string m_format;
            new private readonly BigSegmentedArray m_count;
            private readonly int m_maxMinusOne;
            private int m_idx;

            public HistogramFacetIterator(BigSegmentedArray count, string format)
            {
                m_idx = -1;
                m_count = count;
                m_maxMinusOne = count.Length - 1;
                m_format = format;
            }

            public override string Next()
            {
                if (HasNext())
                {
                    base.m_count = m_count.Get(++m_idx);
                    return (m_facet = m_idx).ToString();
                }
                return null;
            }

            public override string Next(int minHits)
            {
                while (m_idx < m_maxMinusOne)
                {
                    if (m_count.Get(++m_idx) >= minHits)
                    {
                        base.m_count = m_count.Get(m_idx);
                        return (m_facet = m_idx).ToString();
                    }
                }
                return null;
            }

            public override int NextInt()
            {
                if (HasNext())
                {
                    base.m_count = m_count.Get(++m_idx);
                    return (m_facet = m_idx);
                }
                return TermInt32List.VALUE_MISSING;
            }

            public override int NextInt(int minHits)
            {
                while (m_idx < m_maxMinusOne)
                {
                    if (m_count.Get(++m_idx) >= minHits)
                    {
                        base.m_count = m_count.Get(m_idx);
                        return (m_facet = m_idx);
                    }
                }
                return TermInt32List.VALUE_MISSING; 
            }

            public override bool HasNext()
            {
                return (m_idx < m_maxMinusOne);
            }

            public override void Remove()
            {
                throw new NotSupportedException();
            }

            public override string Format(int val)
            {
                return val.ToString(m_format);
            }

            public override string Format(object val)
            {
                if (val == null) return string.Empty;
                return string.Format("{0:" + m_format + "}", val);
            }
        }
    }
}
