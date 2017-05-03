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
namespace BoboBrowse.Net.Facets.Attribute
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class AttributesFacetCountCollector : DefaultFacetCountCollector
    {
        //public readonly BigNestedIntArray _array; // NOT USED
        private IEnumerable<BrowseFacet> cachedFacets;
        private readonly int numFacetsPerKey;
        private readonly char separator;
        private readonly MultiValueFacetDataCache dataCache;
        private string[] values;

        public AttributesFacetCountCollector(AttributesFacetHandler attributesFacetHandler, string name, 
            MultiValueFacetDataCache dataCache, int docBase, BrowseSelection browseSelection, 
            FacetSpec ospec, int numFacetsPerKey, char separator)
            : base(name, dataCache, docBase, browseSelection, ospec)
        {
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
            var keyOccurences = new Dictionary<string, AtomicInt32>();
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
                AtomicInt32 numOfKeys = keyOccurences.Get(key);
                if (numOfKeys == null)
                {
                    numOfKeys = new AtomicInt32(0);
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

        public override FacetIterator GetIterator()
        {
            return new AttributesFacetIterator(GetFacets());
        }
    }
}
