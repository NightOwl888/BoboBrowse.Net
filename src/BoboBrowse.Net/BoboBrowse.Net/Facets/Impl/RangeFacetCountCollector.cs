//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
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
    using BoboBrowse.Net.Util;
    using Lucene.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class RangeFacetCountCollector : IFacetCountCollector
    {
        private readonly FacetSpec m_ospec;
        protected BigSegmentedArray m_count;
        private int m_countLength;
        private readonly BigSegmentedArray m_array;
        protected FacetDataCache m_dataCache;
        private readonly string m_name;
        private readonly TermStringList m_predefinedRanges;
        private int[][] m_predefinedRangeIndexes;

        public RangeFacetCountCollector(string name, FacetDataCache dataCache, int docBase, FacetSpec ospec, IList<string> predefinedRanges)
        {
            m_name = name;
            m_dataCache = dataCache;
            m_countLength = m_dataCache.Freqs.Length;
            m_count = new LazyBigIntArray(m_countLength);
            m_array = m_dataCache.OrderArray;
            m_ospec = ospec;
            if (predefinedRanges != null)
            {
                m_predefinedRanges = new TermStringList();
                predefinedRanges.Sort();
                m_predefinedRanges.AddAll(predefinedRanges);
            }
            else
            {
                m_predefinedRanges = null;
            }

            if (m_predefinedRanges != null)
            {
                m_predefinedRangeIndexes = new int[m_predefinedRanges.Count][];
                int i = 0;
                foreach (string range in this.m_predefinedRanges)
                {
                    m_predefinedRangeIndexes[i++] = FacetRangeFilter.Parse(this.m_dataCache, range);
                }
            }
        }

        /// <summary>
        /// gets distribution of the value arrays. When predefined ranges are available, this returns distribution by predefined ranges.
        /// </summary>
        /// <returns></returns>
        public virtual BigSegmentedArray GetCountDistribution()
        {
            BigSegmentedArray dist = null;
            if (m_predefinedRangeIndexes != null)
            {
                dist = new LazyBigIntArray(m_predefinedRangeIndexes.Length);
                int n = 0;
                foreach (int[] range in m_predefinedRangeIndexes)
                {
                    int start = range[0];
                    int end = range[1];

                    int sum = 0;
                    for (int i = start; i < end; ++i)
                    {
                        sum += m_count.Get(i);
                    }
                    dist.Add(n++, sum);
                }
            }
            else
            {
                dist = m_count;
            }

            return dist;
        }

        public virtual string Name
        {
            get { return m_name; }
        }

        public virtual BrowseFacet GetFacet(string value)
        {
            BrowseFacet facet = null;
            int[] range = FacetRangeFilter.Parse(m_dataCache, value);
            if (range != null)
            {
                int sum = 0;
                for (int i = range[0]; i <= range[1]; ++i)
                {
                    sum += m_count.Get(i);
                }
                facet = new BrowseFacet(value, sum);
            }
            return facet;
        }

        public virtual int GetFacetHitsCount(object value)
        {
            int[] range = FacetRangeFilter.Parse(m_dataCache, (string)value);
            int sum = 0;
            if (range != null)
            {
                for (int i = range[0]; i <= range[1]; ++i)
                {
                    sum += m_count.Get(i);
                }
            }
            return sum;
        }

        public virtual void Collect(int docid)
        {
            int i = m_array.Get(docid);
            m_count.Add(i, m_count.Get(i) + 1);
        }

        public void CollectAll()
        {
            m_count = BigIntArray.FromArray(m_dataCache.Freqs);
            m_countLength = m_dataCache.Freqs.Length;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// This method was internal in the original design, but made it
        /// protected to make it easy to bring back the auto ranges feature if so desired.
        /// </remarks>
        /// <param name="facets"></param>
        protected virtual void ConvertFacets(BrowseFacet[] facets)
        {
            int i = 0;
            foreach (BrowseFacet facet in facets)
            {
                int hit = facet.FacetValueHitCount;
                string val = facet.Value;
                RangeFacet rangeFacet = new RangeFacet();
                rangeFacet.SetValues(val, val);
                rangeFacet.FacetValueHitCount = hit;
                facets[i++] = rangeFacet;
            }
        }

        public virtual ICollection<BrowseFacet> GetFacets()
        {
            if (m_ospec != null)
            {
                if (m_predefinedRangeIndexes != null)
                {
                    int minCount = m_ospec.MinHitCount;
                    //int maxNumOfFacets = _ospec.getMaxCount();
                    //if (maxNumOfFacets <= 0 || maxNumOfFacets > _predefinedRangeIndexes.length) 
                    //    maxNumOfFacets = _predefinedRangeIndexes.length;

                    int[] rangeCount = new int[m_predefinedRangeIndexes.Length];
                    for (int k = 0; k < m_predefinedRangeIndexes.Length; ++k)
                    {
                        int count = 0;
                        int idx = m_predefinedRangeIndexes[k][0];
                        int end = m_predefinedRangeIndexes[k][1];
                        while (idx < end)
                        {
                            count += m_count.Get(idx++);
                        }
                        rangeCount[k] = count;
                    }

                    List<BrowseFacet> facetColl = new List<BrowseFacet>(m_predefinedRanges.Count);
                    for (int k = 0; k < m_predefinedRanges.Count; ++k)
                    {
                        if (rangeCount[k] >= minCount)
                        {
                            BrowseFacet choice = new BrowseFacet(m_predefinedRanges[k], rangeCount[k]);
                            facetColl.Add(choice);
                        }
                        //if(facetColl.size() >= maxNumOfFacets) break;
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

        public virtual List<BrowseFacet> GetFacetsNew()
        {
            if (m_ospec != null)
            {
                if (m_predefinedRangeIndexes != null)
                {
                    int minCount = m_ospec.MinHitCount;
                    int maxNumOfFacets = m_ospec.MaxCount;
                    if (maxNumOfFacets <= 0 || maxNumOfFacets > m_predefinedRangeIndexes.Length) maxNumOfFacets = m_predefinedRangeIndexes.Length;

                    BigSegmentedArray rangeCount = new LazyBigIntArray(m_predefinedRangeIndexes.Length);

                    for (int k = 0; k < m_predefinedRangeIndexes.Length; ++k)
                    {
                        int count = 0;
                        int idx = m_predefinedRangeIndexes[k][0];
                        int end = m_predefinedRangeIndexes[k][1];
                        while (idx <= end)
                        {
                            count += m_count.Get(idx++);
                        }
                        rangeCount.Add(k, count);
                    }

                    List<BrowseFacet> facetColl;
                    FacetSpec.FacetSortSpec sortspec = m_ospec.OrderBy;
                    if (sortspec == FacetSpec.FacetSortSpec.OrderValueAsc)
                    {
                        facetColl = new List<BrowseFacet>(maxNumOfFacets);
                        for (int k = 0; k < m_predefinedRangeIndexes.Length; ++k)
                        {
                            if (rangeCount.Get(k) >= minCount)
                            {
                                BrowseFacet choice = new BrowseFacet(m_predefinedRanges.Get(k), rangeCount.Get(k));
                                facetColl.Add(choice);
                            }
                            if (facetColl.Count >= maxNumOfFacets) break;
                        }
                    }
                    else //if (sortspec == FacetSortSpec.OrderHitsDesc)
                    {
                        IComparerFactory comparerFactory;
                        if (sortspec == FacetSpec.FacetSortSpec.OrderHitsDesc)
                        {
                            comparerFactory = new FacetHitcountComparerFactory();
                        }
                        else
                        {
                            comparerFactory = m_ospec.CustomComparerFactory;
                        }

                        if (comparerFactory == null)
                        {
                            throw new ArgumentException("facet comparer factory not specified");
                        }

                        IComparer<int> comparer = comparerFactory.NewComparer(new RangeFacetCountCollectorFieldAccessor(m_predefinedRanges), rangeCount);

                        int forbidden = -1;
                        IntBoundedPriorityQueue pq = new IntBoundedPriorityQueue(comparer, maxNumOfFacets, forbidden);
                        for (int i = 0; i < m_predefinedRangeIndexes.Length; ++i)
                        {
                            if (rangeCount.Get(i) >= minCount) pq.Offer(i);
                        }

                        int val;
                        facetColl = new List<BrowseFacet>();
                        while ((val = pq.Poll()) != forbidden)
                        {
                            BrowseFacet facet = new BrowseFacet(m_predefinedRanges.ElementAt(val), rangeCount.Get(val));
                            facetColl.Insert(0, facet);
                        }
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

        private class RangeFacetCountCollectorFieldAccessor : IFieldValueAccessor
        {
            private readonly TermStringList m_predefinedRanges;

            public RangeFacetCountCollectorFieldAccessor(TermStringList predefinedRanges)
            {
                this.m_predefinedRanges = predefinedRanges;
            }

            public string GetFormatedValue(int index)
            {
                return m_predefinedRanges.Get(index);
            }

            public object GetRawValue(int index)
            {
                return m_predefinedRanges.GetRawValue(index);
            }
        }

        private class RangeFacet : BrowseFacet
        {
            //private static long serialVersionUID = 1L; // NOT USED

            private string m_lower;
            private string m_upper;

            public RangeFacet()
            { }

            public string Lower
            {
                get { return m_lower; }
            }

            public string Upper
            {
                get { return m_upper; }
            }

            public void SetValues(string lower, string upper)
            {
                m_lower = lower;
                m_upper = upper;
                this.Value = new StringBuilder("[").Append(m_lower).Append(" TO ").Append(m_upper).Append(']').ToString();
            }
        }

        public virtual void Dispose()
        { }

        public virtual FacetIterator GetIterator()
        {
            if (m_predefinedRanges != null)
            {
                BigSegmentedArray rangeCounts = new LazyBigIntArray(m_predefinedRangeIndexes.Length);
                for (int k = 0; k < m_predefinedRangeIndexes.Length; ++k)
                {
                    int count = 0;
                    int idx = m_predefinedRangeIndexes[k][0];
                    int end = m_predefinedRangeIndexes[k][1];
                    while (idx <= end)
                    {
                        count += m_count.Get(idx++);
                    }
                    rangeCounts.Add(k, rangeCounts.Get(k) + count);
                }
                return new DefaultFacetIterator(m_predefinedRanges, rangeCounts, rangeCounts.Length, true);
            }
            return null;
        }
    }
}
