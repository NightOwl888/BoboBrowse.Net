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
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Util;
    using System.Collections.Generic;
    using System.Linq;

    public class BucketFacetCountCollector : IFacetCountCollector
    {
        private readonly string m_name;
        private readonly DefaultFacetCountCollector m_subCollector;
        private readonly FacetSpec m_ospec;
        private readonly IDictionary<string, string[]> m_predefinedBuckets;
        private BigSegmentedArray m_collapsedCounts;
        private readonly TermStringList m_bucketValues;
        private readonly int m_numdocs;

        public BucketFacetCountCollector(string name, DefaultFacetCountCollector subCollector, FacetSpec ospec, IDictionary<string, string[]> predefinedBuckets, int numdocs)
        {
            m_name = name;
            m_subCollector = subCollector;
            m_ospec = ospec;
            m_numdocs = numdocs;

            m_predefinedBuckets = predefinedBuckets;
            m_collapsedCounts = null;

            m_bucketValues = new TermStringList();
            m_bucketValues.Add("");

            List<string> bucketArray = m_predefinedBuckets.Keys.ToList();
            bucketArray.Sort();
            foreach (string bucket in bucketArray)
            {
                m_bucketValues.Add(bucket);
            }
            m_bucketValues.Seal();
        }

        private BigSegmentedArray GetCollapsedCounts()
        {
            if (m_collapsedCounts == null)
            {
                m_collapsedCounts = new LazyBigInt32Array(m_bucketValues.Count);
                FacetDataCache dataCache = m_subCollector.DataCache;
                ITermValueList subList = dataCache.ValArray;
                BigSegmentedArray subcounts = m_subCollector.Count;
                FixedBitSet indexSet = new FixedBitSet(subcounts.Length);
                int c = 0;
                int i = 0;
                foreach (string val in m_bucketValues)
                {
                    if (val.Length > 0)
                    {
                        string[] subVals = m_predefinedBuckets.Get(val);
                        int count = 0;
                        foreach (string subVal in subVals)
                        {
                            int index = subList.IndexOf(subVal);
                            if (index > 0)
                            {
                                int subcount = subcounts.Get(index);
                                count += subcount;
                                if (!indexSet.Get(index))
                                {
                                    indexSet.Set(index);
                                    c += dataCache.Freqs[index];
                                }
                            }
                        }
                        m_collapsedCounts.Add(i, count);
                    }
                    i++;
                }
                m_collapsedCounts.Add(0, (m_numdocs - c));
            }
            return m_collapsedCounts;
        }

        /// <summary>
        /// get the total count of all possible elements 
        /// </summary>
        /// <returns></returns>
        public virtual BigSegmentedArray GetCountDistribution()
        {
            return GetCollapsedCounts();
        }

        public virtual string Name
        {
            get { return m_name; }
        }

        /// <summary>
        /// get the facet of one particular bucket
        /// </summary>
        /// <param name="bucketValue"></param>
        /// <returns></returns>
        public virtual BrowseFacet GetFacet(string bucketValue)
        {
            int index = m_bucketValues.IndexOf(bucketValue);
            if (index < 0)
            {
                return new BrowseFacet(bucketValue, 0);
            }

            BigSegmentedArray counts = GetCollapsedCounts();

            return new BrowseFacet(bucketValue, counts.Get(index));
        }

        public virtual int GetFacetHitsCount(object value)
        {
            int index = m_bucketValues.IndexOf(value);
            if (index < 0)
            {
                return 0;
            }

            BigSegmentedArray counts = GetCollapsedCounts();

            return counts.Get(index);
        }

        public void Collect(int docid)
        {
            m_subCollector.Collect(docid);
        }

        public void CollectAll()
        {
            m_subCollector.CollectAll();
        }

        public virtual ICollection<BrowseFacet> GetFacets()
        {
            BigSegmentedArray counts = GetCollapsedCounts();
            return DefaultFacetCountCollector.GetFacets(m_ospec, counts, counts.Length, m_bucketValues);
        }

        public virtual void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_subCollector.Dispose();
            }
        }

        public virtual FacetIterator GetIterator()
        {
            BigSegmentedArray counts = GetCollapsedCounts();
            return new DefaultFacetIterator(m_bucketValues, counts, counts.Length, true);
        }
    }
}
