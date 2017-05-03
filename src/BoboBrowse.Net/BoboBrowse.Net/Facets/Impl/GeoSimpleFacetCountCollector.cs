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
    using BoboBrowse.Net.Util;
    using BoboBrowse.Net.Support.Logging;
    using System.Collections.Generic;
    using Lucene.Net.Support;

    /// <summary>
    /// author nnarkhed
    /// </summary>
    public class GeoSimpleFacetCountCollector : IFacetCountCollector
    {
        private static readonly ILog log = LogProvider.For<GeoSimpleFacetCountCollector>();
	    private readonly FacetSpec m_spec;
	    private readonly string m_name;
	    private int[] m_latCount;
	    private int[] m_longCount;
	    private readonly BigSegmentedArray m_latOrderArray;
	    private readonly FacetDataCache m_latDataCache;
	    private readonly TermStringList m_predefinedRanges;
	    private int[][] m_latPredefinedRangeIndexes;
        private readonly BigSegmentedArray m_longOrderArray;
	    private readonly FacetDataCache m_longDataCache;
	    private int[][] m_longPredefinedRangeIndexes;

        public GeoSimpleFacetCountCollector(string name, FacetDataCache latDataCache, FacetDataCache longDataCache, int docBase, FacetSpec spec, IList<string> predefinedRanges)
        {
            m_name = name;
            m_latDataCache = latDataCache;
            m_longDataCache = longDataCache;
            m_latCount = new int[m_latDataCache.Freqs.Length];
            m_longCount = new int[m_longDataCache.Freqs.Length];
            log.Info("latCount: " + m_latDataCache.Freqs.Length + " longCount: " + m_longDataCache.Freqs.Length);
            m_latOrderArray = m_latDataCache.OrderArray;
            m_longOrderArray = m_longDataCache.OrderArray;
            m_spec = spec;
            m_predefinedRanges = new TermStringList();
            predefinedRanges.Sort();
            m_predefinedRanges.AddAll(predefinedRanges);

            if (predefinedRanges != null)
            {
                m_latPredefinedRangeIndexes = new int[m_predefinedRanges.Count][];
                for (int j = 0; j < m_latPredefinedRangeIndexes.Length; j++)
                {
                    m_latPredefinedRangeIndexes[j] = new int[2];
                }
                m_longPredefinedRangeIndexes = new int[m_predefinedRanges.Count][];
                for (int j = 0; j < m_longPredefinedRangeIndexes.Length; j++)
                {
                    m_longPredefinedRangeIndexes[j] = new int[2];
                }
                int i = 0;
                foreach (string range in m_predefinedRanges)
                {
                    int[] ranges = GeoSimpleFacetFilter.Parse(m_latDataCache, m_longDataCache, range);
                    m_latPredefinedRangeIndexes[i][0] = ranges[0];   // latStart 
                    m_latPredefinedRangeIndexes[i][1] = ranges[1];   // latEnd
                    m_longPredefinedRangeIndexes[i][0] = ranges[2];  // longStart
                    m_longPredefinedRangeIndexes[i][1] = ranges[3];  // longEnd
                    i++;
                }
            }
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.facets.FacetCountCollector#collect(int)
        /// </summary>
        /// <param name="docid"></param>
        public virtual void Collect(int docid)
        {
            // increment the count only if both latitude and longitude ranges are true for a particular docid
            foreach (int[] range in m_latPredefinedRangeIndexes)
            {
                int latValue = m_latOrderArray.Get(docid);
                int longValue = m_longOrderArray.Get(docid);
                int latStart = range[0];
                int latEnd = range[1];
                if (latValue >= latStart && latValue <= latEnd)
                {
                    foreach (int[] longRange in m_longPredefinedRangeIndexes)
                    {
                        int longStart = longRange[0];
                        int longEnd = longRange[1];
                        if (longValue >= longStart && longValue <= longEnd)
                        {
                            m_latCount[m_latOrderArray.Get(docid)]++;
                            m_longCount[m_longOrderArray.Get(docid)]++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.facets.FacetCountCollector#collectAll()
        /// </summary>
        public virtual void CollectAll()
        {
            m_latCount = m_latDataCache.Freqs;
            m_longCount = m_longDataCache.Freqs;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.facets.FacetCountCollector#getCountDistribution()
        /// </summary>
        /// <returns></returns>
        public virtual BigSegmentedArray GetCountDistribution()
        {
            BigSegmentedArray dist = null;
            if (m_latPredefinedRangeIndexes != null)
            {
                dist = new LazyBigIntArray(m_latPredefinedRangeIndexes.Length);
                int n = 0;
                int start;
                int end;
                foreach (int[] range in m_latPredefinedRangeIndexes)
                {
                    start = range[0];
                    end = range[1];
                    int sum = 0;
                    for (int i = start; i < end; i++)
                    {
                        sum += m_latCount[i];
                    }
                    dist.Add(n++, sum);
                }
            }
            return dist;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.facets.FacetCountCollector#getName()
        /// </summary>
        public virtual string Name
        {
            get { return m_name; }
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetAccessible#getFacet(java.lang.String)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual BrowseFacet GetFacet(string value)
        {
            BrowseFacet facet = null;
            int[] range = FacetRangeFilter.Parse(m_latDataCache, value);

            if (range != null)
            {
                int sum = 0;
                for (int i = range[0]; i <= range[1]; ++i)
                {
                    sum += m_latCount[i];
                }
                facet = new BrowseFacet(value, sum);
            }
            return facet;
        }

        public virtual int GetFacetHitsCount(object value)
        {
            int[] range = FacetRangeFilter.Parse(m_latDataCache, (string)value);

            if (range != null)
            {
                int sum = 0;
                for (int i = range[0]; i <= range[1]; ++i)
                {
                    sum += m_latCount[i];
                }
                return sum;
            }
            return 0;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetAccessible#getFacets()
        /// </summary>
        /// <returns></returns>
        public virtual ICollection<BrowseFacet> GetFacets()
        {
            if (m_spec != null)
            {
                if (m_latPredefinedRangeIndexes != null)
                {
                    int minCount = m_spec.MinHitCount;
                    int[] rangeCounts = new int[m_latPredefinedRangeIndexes.Length];
                    for (int i = 0; i < m_latCount.Length; ++i)
                    {
                        if (m_latCount[i] > 0)
                        {
                            for (int k = 0; k < m_latPredefinedRangeIndexes.Length; ++k)
                            {
                                if (i >= m_latPredefinedRangeIndexes[k][0] && i <= m_latPredefinedRangeIndexes[k][1])
                                {
                                    rangeCounts[k] += m_latCount[i];
                                }
                            }
                        }
                    }
                    List<BrowseFacet> list = new List<BrowseFacet>(rangeCounts.Length);
                    for (int i = 0; i < rangeCounts.Length; ++i)
                    {
                        if (rangeCounts[i] >= minCount)
                        {
                            BrowseFacet choice = new BrowseFacet();
                            choice.FacetValueHitCount = rangeCounts[i];
                            choice.Value = m_predefinedRanges.Get(i);
                            list.Add(choice);
                        }
                    }
                    return list;
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

        public virtual void Dispose()
        { }

        public virtual FacetIterator GetIterator()
        {
            // each range is of the form <lat, lon, radius>
            LazyBigIntArray rangeCounts = new LazyBigIntArray(m_latPredefinedRangeIndexes.Length);
            for (int i = 0; i < m_latCount.Length; ++i)
            {
                if (m_latCount[i] > 0)
                {
                    for (int k = 0; k < m_latPredefinedRangeIndexes.Length; ++k)
                    {
                        if (i >= m_latPredefinedRangeIndexes[k][0] && i <= m_latPredefinedRangeIndexes[k][1])
                        {
                            rangeCounts.Add(k, rangeCounts.Get(k) + m_latCount[i]);
                        }
                    }
                }
            }
            return new DefaultFacetIterator(m_predefinedRanges, rangeCounts, rangeCounts.Length, true);
        }
    }
}
