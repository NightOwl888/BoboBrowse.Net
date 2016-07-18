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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class RangeFacetCountCollector : IFacetCountCollector
    {
        private readonly FacetSpec _ospec;
        protected BigSegmentedArray _count;
        private int _countLength;
        private readonly BigSegmentedArray _array;
        protected FacetDataCache _dataCache;
        private readonly string _name;
        private readonly TermStringList _predefinedRanges;
        private int[][] _predefinedRangeIndexes;

        public RangeFacetCountCollector(string name, FacetDataCache dataCache, int docBase, FacetSpec ospec, IEnumerable<string> predefinedRanges)
        {
            _name = name;
            _dataCache = dataCache;
            _countLength = _dataCache.Freqs.Length;
            _count = new LazyBigIntArray(_countLength);
            _array = _dataCache.OrderArray;
            _ospec = ospec;
            if (predefinedRanges != null)
            {
                _predefinedRanges = new TermStringList();
                var tempList = new List<string>(predefinedRanges);
                tempList.Sort();
                _predefinedRanges.AddAll(tempList);
            }
            else
            {
                _predefinedRanges = null;
            }

            if (_predefinedRanges != null)
            {
                _predefinedRangeIndexes = new int[_predefinedRanges.Count()][];
                int i = 0;
                foreach (string range in this._predefinedRanges)
                {
                    _predefinedRangeIndexes[i++] = FacetRangeFilter.Parse(this._dataCache, range);
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
            if (_predefinedRangeIndexes != null)
            {
                dist = new LazyBigIntArray(_predefinedRangeIndexes.Length);
                int n = 0;
                foreach (int[] range in _predefinedRangeIndexes)
                {
                    int start = range[0];
                    int end = range[1];

                    int sum = 0;
                    for (int i = start; i < end; ++i)
                    {
                        sum += _count.Get(i);
                    }
                    dist.Add(n++, sum);
                }
            }
            else
            {
                dist = _count;
            }

            return dist;
        }

        public virtual string Name
        {
            get { return _name; }
        }

        public virtual BrowseFacet GetFacet(string value)
        {
            BrowseFacet facet = null;
            int[] range = FacetRangeFilter.Parse(_dataCache, value);
            if (range != null)
            {
                int sum = 0;
                for (int i = range[0]; i <= range[1]; ++i)
                {
                    sum += _count.Get(i);
                }
                facet = new BrowseFacet(value, sum);
            }
            return facet;
        }

        public virtual int GetFacetHitsCount(object value)
        {
            int[] range = FacetRangeFilter.Parse(_dataCache, (string)value);
            int sum = 0;
            if (range != null)
            {
                for (int i = range[0]; i <= range[1]; ++i)
                {
                    sum += _count.Get(i);
                }
            }
            return sum;
        }

        public virtual void Collect(int docid)
        {
            int i = _array.Get(docid);
            _count.Add(i, _count.Get(i) + 1);
        }

        public void CollectAll()
        {
            _count = BigIntArray.FromArray(_dataCache.Freqs);
            _countLength = _dataCache.Freqs.Length;
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

        public virtual IEnumerable<BrowseFacet> GetFacets()
        {
            if (_ospec != null)
            {
                if (_predefinedRangeIndexes != null)
                {
                    int minCount = _ospec.MinHitCount;
                    //int maxNumOfFacets = _ospec.getMaxCount();
                    //if (maxNumOfFacets <= 0 || maxNumOfFacets > _predefinedRangeIndexes.length) 
                    //    maxNumOfFacets = _predefinedRangeIndexes.length;

                    int[] rangeCount = new int[_predefinedRangeIndexes.Length];
                    for (int k = 0; k < _predefinedRangeIndexes.Length; ++k)
                    {
                        int count = 0;
                        int idx = _predefinedRangeIndexes[k][0];
                        int end = _predefinedRangeIndexes[k][1];
                        while (idx < end)
                        {
                            count += _count.Get(idx++);
                        }
                        rangeCount[k] = count;
                    }

                    List<BrowseFacet> facetColl = new List<BrowseFacet>(_predefinedRanges.Count());
                    for (int k = 0; k < _predefinedRanges.Count(); ++k)
                    {
                        if (rangeCount[k] >= minCount)
                        {
                            BrowseFacet choice = new BrowseFacet(_predefinedRanges.ElementAt(k), rangeCount[k]);
                            facetColl.Add(choice);
                        }
                        //if(facetColl.size() >= maxNumOfFacets) break;
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

        public virtual List<BrowseFacet> GetFacetsNew()
        {
            if (_ospec != null)
            {
                if (_predefinedRangeIndexes != null)
                {
                    int minCount = _ospec.MinHitCount;
                    int maxNumOfFacets = _ospec.MaxCount;
                    if (maxNumOfFacets <= 0 || maxNumOfFacets > _predefinedRangeIndexes.Length) maxNumOfFacets = _predefinedRangeIndexes.Length;

                    BigSegmentedArray rangeCount = new LazyBigIntArray(_predefinedRangeIndexes.Length);

                    for (int k = 0; k < _predefinedRangeIndexes.Length; ++k)
                    {
                        int count = 0;
                        int idx = _predefinedRangeIndexes[k][0];
                        int end = _predefinedRangeIndexes[k][1];
                        while (idx <= end)
                        {
                            count += _count.Get(idx++);
                        }
                        rangeCount.Add(k, count);
                    }

                    List<BrowseFacet> facetColl;
                    FacetSpec.FacetSortSpec sortspec = _ospec.OrderBy;
                    if (sortspec == FacetSpec.FacetSortSpec.OrderValueAsc)
                    {
                        facetColl = new List<BrowseFacet>(maxNumOfFacets);
                        for (int k = 0; k < _predefinedRangeIndexes.Length; ++k)
                        {
                            if (rangeCount.Get(k) >= minCount)
                            {
                                BrowseFacet choice = new BrowseFacet(_predefinedRanges.Get(k), rangeCount.Get(k));
                                facetColl.Add(choice);
                            }
                            if (facetColl.Count >= maxNumOfFacets) break;
                        }
                    }
                    else //if (sortspec == FacetSortSpec.OrderHitsDesc)
                    {
                        IComparatorFactory comparatorFactory;
                        if (sortspec == FacetSpec.FacetSortSpec.OrderHitsDesc)
                        {
                            comparatorFactory = new FacetHitcountComparatorFactory();
                        }
                        else
                        {
                            comparatorFactory = _ospec.CustomComparatorFactory;
                        }

                        if (comparatorFactory == null)
                        {
                            throw new ArgumentException("facet comparator factory not specified");
                        }

                        IComparer<int> comparator = comparatorFactory.NewComparator(new RangeFacetCountCollectorFieldAccessor(_predefinedRanges), rangeCount);

                        int forbidden = -1;
                        IntBoundedPriorityQueue pq = new IntBoundedPriorityQueue(comparator, maxNumOfFacets, forbidden);
                        for (int i = 0; i < _predefinedRangeIndexes.Length; ++i)
                        {
                            if (rangeCount.Get(i) >= minCount) pq.Offer(i);
                        }

                        int val;
                        facetColl = new List<BrowseFacet>();
                        while ((val = pq.Poll()) != forbidden)
                        {
                            BrowseFacet facet = new BrowseFacet(_predefinedRanges.ElementAt(val), rangeCount.Get(val));
                            facetColl.Insert(0, facet);
                        }
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

        private class RangeFacetCountCollectorFieldAccessor : IFieldValueAccessor
        {
            private readonly TermStringList _predefinedRanges;

            public RangeFacetCountCollectorFieldAccessor(TermStringList predefinedRanges)
            {
                this._predefinedRanges = predefinedRanges;
            }

            public string GetFormatedValue(int index)
            {
                return _predefinedRanges.Get(index);
            }

            public object GetRawValue(int index)
            {
                return _predefinedRanges.GetRawValue(index);
            }
        }

        private class RangeFacet : BrowseFacet
        {
            //private static long serialVersionUID = 1L; // NOT USED

            private string _lower;
            private string _upper;

            public RangeFacet()
            { }

            public string Lower
            {
                get { return _lower; }
            }

            public string Upper
            {
                get { return _upper; }
            }

            public void SetValues(string lower, string upper)
            {
                _lower = lower;
                _upper = upper;
                this.Value = new StringBuilder("[").Append(_lower).Append(" TO ").Append(_upper).Append(']').ToString();
            }
        }

        public virtual void Dispose()
        { }

        public virtual FacetIterator GetIterator()
        {
            if (_predefinedRanges != null)
            {
                BigSegmentedArray rangeCounts = new LazyBigIntArray(_predefinedRangeIndexes.Length);
                for (int k = 0; k < _predefinedRangeIndexes.Length; ++k)
                {
                    int count = 0;
                    int idx = _predefinedRangeIndexes[k][0];
                    int end = _predefinedRangeIndexes[k][1];
                    while (idx <= end)
                    {
                        count += _count.Get(idx++);
                    }
                    rangeCounts.Add(k, rangeCounts.Get(k) + count);
                }
                return new DefaultFacetIterator(_predefinedRanges, rangeCounts, rangeCounts.Size(), true);
            }
            return null;
        }
    }
}
