// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class BucketFacetCountCollector : IFacetCountCollector
    {
        private readonly String _name;
        private readonly DefaultFacetCountCollector _subCollector;
        private readonly FacetSpec _ospec;
        private readonly IDictionary<string, string[]> _predefinedBuckets;
        private int[] _collapsedCounts;
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

        private int[] GetCollapsedCounts()
        {
            if (_collapsedCounts == null)
            {
                _collapsedCounts = new int[_bucketValues.Count];
                IFacetDataCache dataCache = _subCollector._dataCache;
                ITermValueList subList = dataCache.ValArray;
                int[] subcounts = _subCollector._count;
                BitVector indexSet = new BitVector(subcounts.Length);
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
                                int subcount = subcounts[index];
                                count += subcount;
                                if (!indexSet.Get(index))
                                {
                                    indexSet.Set(index);
                                    c += dataCache.Freqs[index];
                                }
                            }
                        }
                        _collapsedCounts[i] = count;
                    }
                    i++;
                }
                _collapsedCounts[0] = (_numdocs - c);
            }
            return _collapsedCounts;
        }

        /// <summary>
        /// get the total count of all possible elements 
        /// </summary>
        /// <returns></returns>
        public virtual int[] GetCountDistribution()
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
        public BrowseFacet GetFacet(string bucketValue)
        {
            int index = _bucketValues.IndexOf(bucketValue);
            if (index < 0)
            {
                return new BrowseFacet(bucketValue, 0);
            }

            int[] counts = GetCollapsedCounts();

            return new BrowseFacet(bucketValue, counts[index]);
        }

        public int GetFacetHitsCount(object value)
        {
            int index = _bucketValues.IndexOf(value);
            if (index < 0)
            {
                return 0;
            }

            int[] counts = GetCollapsedCounts();

            return counts[index];
        }

        public sealed void Collect(int docid)
        {
            _subCollector.Collect(docid);
        }

        public sealed void CollectAll()
        {
            _subCollector.CollectAll();
        }

        public virtual IEnumerable<BrowseFacet> GetFacets()
        {
            int[] counts = GetCollapsedCounts();
            return DefaultFacetCountCollector.GetFacets(_ospec, counts, counts.Length, _bucketValues);
        }

        public void Close()
        {
            _subCollector.Close();
        }

        public FacetIterator Iterator()
        {
            int[] counts = GetCollapsedCounts();
            return new DefaultFacetIterator(_bucketValues, counts, counts.Length, true);
        }
    }
}
