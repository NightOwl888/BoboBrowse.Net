namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Util;
    using C5;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class RangeFacetCountCollector : IFacetCountCollector
    {
        private readonly FacetSpec ospec;
        private int[] count;
        private readonly BigSegmentedArray orderArray;
        private readonly FacetDataCache dataCache;
        private readonly string name;
        private readonly bool autoRange;
        private readonly IEnumerable<string> predefinedRanges;
        private readonly int[][] predefinedRangeIndexes;

        public RangeFacetCountCollector(string name, RangeFacetHandler rangeFacetHandler, FacetSpec ospec, IEnumerable<string> predefinedRanges, bool autoRange)
            : this(name, rangeFacetHandler.GetDataCache(), ospec, predefinedRanges, autoRange)
        {
        }

        protected internal RangeFacetCountCollector(string name, FacetDataCache dataCache, FacetSpec ospec, IEnumerable<string> predefinedRanges, bool autoRange)
        {
            this.name = name;
            this.dataCache = dataCache;
            this.ospec = ospec;
            count = new int[this.dataCache.freqs.Length];
            orderArray = this.dataCache.orderArray;
            this.predefinedRanges = predefinedRanges;
            this.autoRange = autoRange;

            if (this.predefinedRanges != null)
            {
                predefinedRangeIndexes = new int[this.predefinedRanges.Count()][];
                int i = 0;
                foreach (string range in this.predefinedRanges)
                {
                    predefinedRangeIndexes[i++] = RangeFacetHandler.Parse(this.dataCache, range);
                }
            }
        }

        ///   <summary> * gets distribution of the value arrays. This is only valid when predefined ranges are available. </summary>
        public virtual int[] GetCountDistribution()
        {

            int[] dist = null;
            if (predefinedRangeIndexes != null)
            {
                dist = new int[predefinedRangeIndexes.Length];
                int n = 0;
                foreach (int[] range in predefinedRangeIndexes)
                {
                    int start = range[0];
                    int end = range[1];

                    int sum = 0;
                    for (int i = start; i < end; ++i)
                    {
                        sum += count[i];
                    }
                    dist[n++] = sum;
                }
            }

            return dist;
        }

        public virtual string Name
        {
            get
            {
                return name;
            }
        }

        public virtual BrowseFacet GetFacet(string @value)
        {
            BrowseFacet facet = null;
            int[] range = RangeFacetHandler.Parse(dataCache, @value);
            if (range != null)
            {
                int sum = 0;
                for (int i = range[0]; i <= range[1]; ++i)
                {
                    sum += count[i];
                }
                facet = new BrowseFacet(@value, sum);
            }
            return facet;
        }

        public void Collect(int docid)
        {
            count[orderArray.Get(docid)]++;
        }

        public void CollectAll()
        {
            count = dataCache.freqs;
        }

        internal virtual void ConvertFacets(BrowseFacet[] facets)
        {
            int i = 0;
            foreach (BrowseFacet facet in facets)
            {
                int hit = facet.HitCount;
                object val = facet.Value;
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
                        object val1 = rChoices[i].Lower;
                        object val2 = rChoices[i + 1].Upper;
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

        private class RangeComparator : MultiBoboBrowser.BrowseFacetValueComparator
        {
        }

        private IEnumerable<BrowseFacet> BuildDynamicRanges()
        {
            TreeSet<BrowseFacet> facetSet = new TreeSet<BrowseFacet>(new RangeComparator());

            int minCount = ospec.MinHitCount;
            // we would skip first element at index 0 (which means no value)
            for (int i = 1; i < count.Length; ++i)
            {
                if (count[i] >= minCount)
                {
                    object val = dataCache.valArray.GetRawValue(i);
                    facetSet.Add(new BrowseFacet(val, count[i]));
                }
            }

            if (ospec.MaxCount <= 0)
            {
                ospec.MaxCount = 5;
            }
            int maxCount = ospec.MaxCount;

            BrowseFacet[] facets = facetSet.ToArray();

            if (facetSet.Count < maxCount)
            {
                ConvertFacets(facets);
            }
            else
            {
                facets = FoldChoices(facets, maxCount);
            }

            return facets;
        }

        public virtual IEnumerable<BrowseFacet> GetFacets()
        {
            if (ospec != null)
            {
                if (autoRange)
                {
                    return BuildDynamicRanges();
                }
                else
                {
                    if (predefinedRangeIndexes != null)
                    {
                        int minCount = ospec.MinHitCount;
                        int[] rangeCounts = new int[predefinedRangeIndexes.Length];
                        for (int i = 0; i < count.Length; ++i)
                        {
                            if (count[i] > 0)
                            {
                                for (int k = 0; k < predefinedRangeIndexes.Length; ++k)
                                {
                                    if (i >= predefinedRangeIndexes[k][0] && i <= predefinedRangeIndexes[k][1])
                                    {
                                        rangeCounts[k] += count[i];
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
                                choice.HitCount = rangeCounts[i];
                                choice.Value = predefinedRanges.ElementAt(i);
                                list.Add(choice);
                            }
                        }
                        return list;
                    }
                    else
                    {
                        return IFacetCountCollector_Fields.EMPTY_FACET_LIST;
                    }
                }
            }
            else
            {
                return IFacetCountCollector_Fields.EMPTY_FACET_LIST;
            }
        }
    }
}
