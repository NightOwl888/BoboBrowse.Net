// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Attribute
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class AttributesFacetCountCollector : DefaultFacetCountCollector
    {
        private readonly AttributesFacetHandler attributesFacetHandler;
        //public readonly BigNestedIntArray _array; // NOT USED
        //private int[] buffer;    // NOT USED
        private IEnumerable<BrowseFacet> cachedFacets;
        private readonly int numFacetsPerKey;
        private readonly char separator;
        //private OpenBitSet excludes; // NOT USED
        //private OpenBitSet includes; // NOT USED
        private readonly MultiValueFacetDataCache dataCache;
        private string[] values;

        public AttributesFacetCountCollector(AttributesFacetHandler attributesFacetHandler, string name, MultiValueFacetDataCache dataCache, int docBase, BrowseSelection browseSelection, FacetSpec ospec, int numFacetsPerKey, char separator)
            : base(name, dataCache, docBase, browseSelection, ospec)
        {
            this.attributesFacetHandler = attributesFacetHandler;
            this.dataCache = dataCache;
            this.numFacetsPerKey = numFacetsPerKey;
            this.separator = separator;
            //_array = dataCache.NestedArray; // NOT USED
            if (browseSelection != null)
            {
                values = browseSelection.Values;
            }
        }

        public override void Collect(int docid)
        {
            dataCache.NestedArray.CountNoReturn(docid, _count);
        }

        public override void CollectAll()
        {
            _count = BigIntArray.FromArray(_dataCache.Freqs);
        }

        public override IEnumerable<BrowseFacet> GetFacets()
        {
            if (cachedFacets == null)
            {
                int max = _ospec.MaxCount;
                _ospec.MaxCount = max * 10;
                IEnumerable<BrowseFacet> facets = base.GetFacets();
                _ospec.MaxCount = max;
                cachedFacets = FilterByKeys(facets, separator, numFacetsPerKey, values);
            }
            return cachedFacets;
        }

        private IEnumerable<BrowseFacet> FilterByKeys(IEnumerable<BrowseFacet> facets, char separator, int numFacetsPerKey, string[] values) {
            var keyOccurences = new Dictionary<string, AtomicInteger>();
            var editable = facets.ToList();
            string separatorString = Convert.ToString(separator);
            for (int i = 0; i < facets.Count(); i++)
            {
                BrowseFacet facet = facets.ElementAt(i);
                string value = facet.Value;
                if (!value.Contains(separatorString)) {
                    editable.Remove(facet);
                    continue;
                }

                if (values != null && values.Length > 0)
                {
                    bool belongsToKeys = false;
                    foreach (var val in values)
                    {
                        if (value.StartsWith(val))
                        {
                            belongsToKeys = true;
                            break;
                        }
                    }
                    if (!belongsToKeys)
                    {
                        editable.Remove(facet);
                        continue;
                    }
                }
                string key = value.Substring(0, value.IndexOf(separatorString));
                AtomicInteger numOfKeys = keyOccurences.Get(key);
                if (numOfKeys == null)
                {
                    numOfKeys = new AtomicInteger(0);
                    keyOccurences.Put(key, numOfKeys);
                }
                int count = numOfKeys.IncrementAndGet();
                if (count > numFacetsPerKey)
                {
                    editable.Remove(facet);
                }
            }
            return editable;
        }

        public override FacetIterator Iterator()
        {
            return new AttributesFacetIterator(GetFacets());
        }
    }
}
