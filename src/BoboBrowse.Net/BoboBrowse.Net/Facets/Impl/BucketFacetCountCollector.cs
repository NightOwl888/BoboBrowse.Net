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

// Version compatibility level: 3.2.0
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
        private readonly string _name;
        private readonly DefaultFacetCountCollector _subCollector;
        private readonly FacetSpec _ospec;
        private readonly IDictionary<string, string[]> _predefinedBuckets;
        private BigSegmentedArray _collapsedCounts;
        private TermStringList _bucketValues;
        private readonly int _numdocs;

        public BucketFacetCountCollector(string name, DefaultFacetCountCollector subCollector, FacetSpec ospec, IDictionary<string, string[]> predefinedBuckets, int numdocs)
        {
            _name = name;
            _subCollector = subCollector;
            _ospec = ospec;
            _numdocs = numdocs;

            _predefinedBuckets = predefinedBuckets;
            _collapsedCounts = null;

            _bucketValues = new TermStringList();
            _bucketValues.Add("");

            List<string> bucketArray = _predefinedBuckets.Keys.ToList();
            bucketArray.Sort();
            foreach (string bucket in bucketArray)
            {
                _bucketValues.Add(bucket);
            }
            _bucketValues.Seal();
        }

        private BigSegmentedArray GetCollapsedCounts()
        {
            if (_collapsedCounts == null)
            {
                _collapsedCounts = new LazyBigIntArray(_bucketValues.Count);
                FacetDataCache dataCache = _subCollector.DataCache;
                ITermValueList subList = dataCache.ValArray;
                BigSegmentedArray subcounts = _subCollector.Count;
                BitVector indexSet = new BitVector(subcounts.Size());
                int c = 0;
                int i = 0;
                foreach (string val in _bucketValues)
                {
                    if (val.Length > 0)
                    {
                        string[] subVals = _predefinedBuckets.Get(val);
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
                        _collapsedCounts.Add(i, count);
                    }
                    i++;
                }
                _collapsedCounts.Add(0, (_numdocs - c));
            }
            return _collapsedCounts;
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
            get { return _name; }
        }

        /// <summary>
        /// get the facet of one particular bucket
        /// </summary>
        /// <param name="bucketValue"></param>
        /// <returns></returns>
        public virtual BrowseFacet GetFacet(string bucketValue)
        {
            int index = _bucketValues.IndexOf(bucketValue);
            if (index < 0)
            {
                return new BrowseFacet(bucketValue, 0);
            }

            BigSegmentedArray counts = GetCollapsedCounts();

            return new BrowseFacet(bucketValue, counts.Get(index));
        }

        public virtual int GetFacetHitsCount(object value)
        {
            int index = _bucketValues.IndexOf(value);
            if (index < 0)
            {
                return 0;
            }

            BigSegmentedArray counts = GetCollapsedCounts();

            return counts.Get(index);
        }

        public void Collect(int docid)
        {
            _subCollector.Collect(docid);
        }

        public void CollectAll()
        {
            _subCollector.CollectAll();
        }

        public virtual IEnumerable<BrowseFacet> GetFacets()
        {
            BigSegmentedArray counts = GetCollapsedCounts();
            return DefaultFacetCountCollector.GetFacets(_ospec, counts, counts.Size(), _bucketValues);
        }

        public virtual void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _subCollector.Dispose();
            }
        }

        public virtual FacetIterator Iterator()
        {
            BigSegmentedArray counts = GetCollapsedCounts();
            return new DefaultFacetIterator(_bucketValues, counts, counts.Size(), true);
        }
    }
}
