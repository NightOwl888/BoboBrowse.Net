// Version compatibility level: 3.1.0
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
        protected int[] _count;
        private int _countLength;
        private readonly BigSegmentedArray _array;
        private readonly FacetDataCache _dataCache;
        private readonly string _name;
        private readonly TermStringList _predefinedRanges;
        private readonly int[][] _predefinedRangeIndexes;
        private int _docBase;

        public RangeFacetCountCollector(string name, FacetDataCache dataCache, int docBase, FacetSpec ospec, IEnumerable<string> predefinedRanges)
        {
            _name = name;
            _dataCache = dataCache;
            _countLength = _dataCache.Freqs.Length;
            _count = new int[_countLength];
            _array = _dataCache.OrderArray;
            _docBase = docBase;
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
        /// gets distribution of the value arrays. This is only valid when predefined ranges are available.
        /// </summary>
        /// <returns></returns>
        public virtual int[] GetCountDistribution()
        {
            int[] dist = null;
            if (_predefinedRangeIndexes != null)
            {
                dist = new int[_predefinedRangeIndexes.Length];
                int n = 0;
                foreach (int[] range in _predefinedRangeIndexes)
                {
                    int start = range[0];
                    int end = range[1];

                    int sum = 0;
                    for (int i = start; i < end; ++i)
                    {
                        sum += _count[i];
                    }
                    dist[n++] = sum;
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
                    sum += _count[i];
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
                    sum += _count[i];
                }
            }
            return sum;
        }

        public virtual void Collect(int docid)
        {
            _count[_array.Get(docid)]++;
        }

        public void CollectAll()
        {
            _count = _dataCache.Freqs;
            _countLength = _dataCache.Freqs.Length;
        }

        internal virtual void ConvertFacets(BrowseFacet[] facets)
        {
            int i = 0;
            foreach (BrowseFacet facet in facets)
            {
                int hit = facet.HitCount;
                string val = facet.Value;
                RangeFacet rangeFacet = new RangeFacet();
                rangeFacet.SetValues(val, val);
                rangeFacet.HitCount = hit;
                facets[i++] = rangeFacet;
            }
        }

        // this is really crappy, need to fix it
        private BrowseFacet[] FoldChoices(BrowseFacet[] choices, int max)
        {
            if (max == 0 || choices.Length <= max)
                return choices;
            List<RangeFacet> list = new List<RangeFacet>();

            for (int i = 0; i < choices.Length; i += 2)
            {
                RangeFacet rangeChoice = new RangeFacet();
                if ((i + 1) < choices.Length)
                {
                    if (choices is RangeFacet[])
                    {
                        RangeFacet[] rChoices = (RangeFacet[])choices;
                        string val1 = rChoices[i].Lower;
                        string val2 = rChoices[i + 1].Upper;
                        rangeChoice.SetValues(val1, val2);
                        rangeChoice.HitCount = choices[i].HitCount + choices[i + 1].HitCount;
                    }
                    else
                    {
                        rangeChoice.SetValues(choices[i].Value, choices[i + 1].Value);
                        rangeChoice.HitCount = choices[i].HitCount + choices[i + 1].HitCount;
                    }

                }
                else
                {
                    if (choices is RangeFacet[])
                    {
                        RangeFacet[] rChoices = (RangeFacet[])choices;
                        rangeChoice.SetValues(rChoices[i].Lower, rChoices[i].Upper);
                    }
                    else
                    {
                        rangeChoice.SetValues(choices[i].Value, choices[i].Value);
                    }
                    rangeChoice.HitCount = choices[i].HitCount;
                }
                list.Add(rangeChoice);
            }

            RangeFacet[] result = list.ToArray();
            return FoldChoices(result, max);
        }

        public virtual IEnumerable<BrowseFacet> GetFacets()
        {
            if (_ospec != null)
            {
                if (_predefinedRangeIndexes != null)
                {
                    int minCount = _ospec.MinHitCount;
                    //int maxNumOfFacets = _ospec.getMaxCount();
                    //if (maxNumOfFacets <= 0 || maxNumOfFacets > _predefinedRangeIndexes.length) maxNumOfFacets = _predefinedRangeIndexes.length;

                    int[] rangeCount = new int[_predefinedRangeIndexes.Length];
                    for (int k = 0; k < _predefinedRangeIndexes.Length; ++k)
                    {
                        int count = 0;
                        int idx = _predefinedRangeIndexes[k][0];
                        int end = _predefinedRangeIndexes[k][1];
                        while (idx < end)
                        {
                            count += _count[idx++];
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
                    return IFacetCountCollector_Fields.EMPTY_FACET_LIST;
                }
            }
            else
            {
                return IFacetCountCollector_Fields.EMPTY_FACET_LIST;
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

                    int[] rangeCount = new int[_predefinedRangeIndexes.Length];

                    for (int k = 0; k < _predefinedRangeIndexes.Length; ++k)
                    {
                        int count = 0;
                        int idx = _predefinedRangeIndexes[k][0];
                        int end = _predefinedRangeIndexes[k][1];
                        while (idx <= end)
                        {
                            count += _count[idx++];
                        }
                        rangeCount[k] = count;
                    }

                    List<BrowseFacet> facetColl;
                    FacetSpec.FacetSortSpec sortspec = _ospec.OrderBy;
                    if (sortspec == FacetSpec.FacetSortSpec.OrderValueAsc)
                    {
                        facetColl = new List<BrowseFacet>(maxNumOfFacets);
                        for (int k = 0; k < _predefinedRangeIndexes.Length; ++k)
                        {
                            if (rangeCount[k] >= minCount)
                            {
                                BrowseFacet choice = new BrowseFacet(_predefinedRanges.ElementAt(k), rangeCount[k]);
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
                            if (rangeCount[i] >= minCount) pq.Offer(i);
                        }

                        int val;
                        facetColl = new List<BrowseFacet>();
                        while ((val = pq.Poll()) != forbidden)
                        {
                            BrowseFacet facet = new BrowseFacet(_predefinedRanges.ElementAt(val), rangeCount[val]);
                            facetColl.Insert(0, facet);
                        }
                    }
                    return facetColl;
                }
                else
                {
                    return IFacetCountCollector_Fields.EMPTY_FACET_LIST;
                }
            }
            else
            {
                return IFacetCountCollector_Fields.EMPTY_FACET_LIST;
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
            private static long serialVersionUID = 1L;

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

        public virtual FacetIterator Iterator()
        {
            if (_predefinedRanges != null)
            {
                int[] rangeCounts = new int[_predefinedRangeIndexes.Length];
                for (int k = 0; k < _predefinedRangeIndexes.Length; ++k)
                {
                    int count = 0;
                    int idx = _predefinedRangeIndexes[k][0];
                    int end = _predefinedRangeIndexes[k][1];
                    while (idx <= end)
                    {
                        count += _count[idx++];
                    }
                    rangeCounts[k] += count;
                }
                return new DefaultFacetIterator(_predefinedRanges, rangeCounts, rangeCounts.Length, true);
            }
            return null;
        }
    }
}
