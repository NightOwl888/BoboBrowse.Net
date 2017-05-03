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
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class BucketFacetHandler : FacetHandler<FacetDataNone>
    {
        private readonly IDictionary<string, string[]> m_predefinedBuckets;
        private readonly string m_dependsOnFacetName;

        public BucketFacetHandler(string name, IDictionary<string, string[]> predefinedBuckets, string dependsOnFacetName)
            : base(name, new string[] { dependsOnFacetName })
        {
            m_predefinedBuckets = predefinedBuckets;
            m_dependsOnFacetName = dependsOnFacetName;
        }

        public override DocComparerSource GetDocComparerSource()
        {
            var dependOnFacetHandler = GetDependedFacetHandler(m_dependsOnFacetName);
            return dependOnFacetHandler.GetDocComparerSource();
        }

        public override string[] GetFieldValues(BoboSegmentReader reader, int id)
        {
            var dependOnFacetHandler = GetDependedFacetHandler(m_dependsOnFacetName);
	        return dependOnFacetHandler.GetFieldValues(reader, id);
        }

        public override object[] GetRawFieldValues(BoboSegmentReader reader, int id)
        {
            var dependOnFacetHandler = GetDependedFacetHandler(m_dependsOnFacetName);
            return dependOnFacetHandler.GetRawFieldValues(reader, id);
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string bucketString, IDictionary<string, string> prop)
        {
            var dependOnFacetHandler = GetDependedFacetHandler(m_dependsOnFacetName);

            string[] elems = m_predefinedBuckets.Get(bucketString);

            if (elems == null || elems.Length == 0) return EmptyFilter.Instance;
            if (elems.Length == 1) return dependOnFacetHandler.BuildRandomAccessFilter(elems[0], prop);
            return dependOnFacetHandler.BuildRandomAccessOrFilter(elems, prop, false);
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] bucketStrings, IDictionary<string, string> prop)
        {
            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>();
            var dependOnFacetHandler = GetDependedFacetHandler(m_dependsOnFacetName);

            foreach (string bucketString in bucketStrings)
            {
                string[] vals = m_predefinedBuckets.Get(bucketString);
                RandomAccessFilter filter = dependOnFacetHandler.BuildRandomAccessOrFilter(vals, prop, false);
                if (filter == EmptyFilter.Instance) return EmptyFilter.Instance;
                filterList.Add(filter);
            }
            if (filterList.Count == 0) return EmptyFilter.Instance;
            if (filterList.Count == 1) return filterList[0];
            return new RandomAccessAndFilter(filterList);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] bucketStrings, IDictionary<string, string> prop, bool isNot)
        {
            if (isNot)
            {
                RandomAccessFilter excludeFilter = BuildRandomAccessAndFilter(bucketStrings, prop);
                return new RandomAccessNotFilter(excludeFilter);
            }
            else
            {
                var dependOnFacetHandler = GetDependedFacetHandler(m_dependsOnFacetName);

                HashSet<string> selections = new HashSet<string>();
                foreach (string bucket in bucketStrings)
                {
                    string[] vals = m_predefinedBuckets.Get(bucket);
                    if (vals != null)
                    {
                        foreach (string val in vals)
                        {
                            selections.Add(val);
                        }
                    }
                }
                if (selections != null && selections.Count > 0)
                {
                    string[] sels = selections.ToArray();
                    if (selections.Count == 1)
                    {
                        return dependOnFacetHandler.BuildRandomAccessFilter(sels[0], prop);
                    }
                    else
                    {
                        return dependOnFacetHandler.BuildRandomAccessOrFilter(sels, prop, false);
                    }
                }
                else
                {
                    return EmptyFilter.Instance;
                }
            }
        }

        public override int GetNumItems(BoboSegmentReader reader, int id)
        {
            var dependOnFacetHandler = GetDependedFacetHandler(m_dependsOnFacetName);
            FacetDataCache data = dependOnFacetHandler.GetFacetData<FacetDataCache>(reader);
            return data.GetNumItems(id);
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec)
        {
            var dependOnFacetHandler = GetDependedFacetHandler(m_dependsOnFacetName);
            return new BucketFacetCountCollectorSource(m_name, sel, fspec, m_predefinedBuckets, dependOnFacetHandler);
        }

        private class BucketFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly string m_name;
            private readonly BrowseSelection m_sel;
            private readonly FacetSpec m_ospec;
            private readonly IDictionary<string, string[]> m_predefinedBuckets;
            private readonly IFacetHandler m_dependOnFacetHandler;

            public BucketFacetCountCollectorSource(string name, BrowseSelection sel, FacetSpec ospec, IDictionary<string, string[]> predefinedBuckets, IFacetHandler dependOnFacetHandler)
            {
                m_name = name;
                m_sel = sel;
                m_ospec = ospec;
                m_predefinedBuckets = predefinedBuckets;
                m_dependOnFacetHandler = dependOnFacetHandler;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                IFacetCountCollector defaultCollector = m_dependOnFacetHandler.GetFacetCountCollectorSource(m_sel, m_ospec).GetFacetCountCollector(reader, docBase);
                if (defaultCollector is DefaultFacetCountCollector)
                {
                    return new BucketFacetCountCollector(m_name, (DefaultFacetCountCollector)defaultCollector, m_ospec, m_predefinedBuckets, reader.NumDocs);
                }
                else
                {
                    throw new InvalidOperationException("dependent facet handler must build DefaultFacetCountCollector");
                }
            }
        }

        public override FacetDataNone Load(BoboSegmentReader reader)
        {
            return FacetDataNone.Instance;
        }
    }
}
