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
        //public readonly BigNestedIntArray m_array; // NOT USED
        private ICollection<BrowseFacet> m_cachedFacets;
        private readonly int m_numFacetsPerKey;
        private readonly char m_separator;
        new private readonly MultiValueFacetDataCache m_dataCache;
        private string[] m_values;

        public AttributesFacetCountCollector(AttributesFacetHandler attributesFacetHandler, string name, 
            MultiValueFacetDataCache dataCache, int docBase, BrowseSelection browseSelection, 
            FacetSpec ospec, int numFacetsPerKey, char separator)
            : base(name, dataCache, docBase, browseSelection, ospec)
        {
            this.m_dataCache = dataCache;
            this.m_numFacetsPerKey = numFacetsPerKey;
            this.m_separator = separator;
            //_array = dataCache.NestedArray; // NOT USED
            if (browseSelection != null)
            {
                m_values = browseSelection.Values;
            }
        }

        public override void Collect(int docid)
        {
            m_dataCache.NestedArray.CountNoReturn(docid, m_count);
        }

        public override void CollectAll()
        {
            m_count = BigIntArray.FromArray(base.m_dataCache.Freqs);
        }

        public override ICollection<BrowseFacet> GetFacets()
        {
            if (m_cachedFacets == null)
            {
                int max = m_ospec.MaxCount;
                m_ospec.MaxCount = max * 10;
                ICollection<BrowseFacet> facets = base.GetFacets();
                m_ospec.MaxCount = max;
                FilterByKeys(facets, m_separator, m_numFacetsPerKey, m_values);
                m_cachedFacets = facets;
            }
            return m_cachedFacets;
        }

        private void FilterByKeys(ICollection<BrowseFacet> facets, char separator, int numFacetsPerKey, string[] values) {
            var keyOccurences = new Dictionary<string, AtomicInt32>();
            var toDelete = new List<BrowseFacet>();
            string separatorString = separator.ToString();
            foreach (var facet in facets)
            {
                string value = facet.Value;
                if (!value.Contains(separatorString))
                {
                    toDelete.Add(facet);
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
                        toDelete.Add(facet);
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
                    toDelete.Add(facet);
                }
            }
            facets.RemoveAll(toDelete);
        }

        public override FacetIterator GetIterator()
        {
            return new AttributesFacetIterator(GetFacets());
        }
    }
}
