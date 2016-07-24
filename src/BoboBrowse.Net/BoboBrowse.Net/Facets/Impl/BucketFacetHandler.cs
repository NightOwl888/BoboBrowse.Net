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
        private readonly IDictionary<string, string[]> _predefinedBuckets;
        private readonly string _dependsOnFacetName;

        public BucketFacetHandler(string name, IDictionary<string, string[]> predefinedBuckets, string dependsOnFacetName)
            : base(name, new string[] { dependsOnFacetName })
        {
            _predefinedBuckets = predefinedBuckets;
            _dependsOnFacetName = dependsOnFacetName;
        }

        public override DocComparatorSource GetDocComparatorSource()
        {
            var dependOnFacetHandler = GetDependedFacetHandler(_dependsOnFacetName);
            return dependOnFacetHandler.GetDocComparatorSource();
        }

        public override string[] GetFieldValues(BoboSegmentReader reader, int id)
        {
            var dependOnFacetHandler = GetDependedFacetHandler(_dependsOnFacetName);
	        return dependOnFacetHandler.GetFieldValues(reader, id);
        }

        public override object[] GetRawFieldValues(BoboSegmentReader reader, int id)
        {
            var dependOnFacetHandler = GetDependedFacetHandler(_dependsOnFacetName);
            return dependOnFacetHandler.GetRawFieldValues(reader, id);
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string bucketString, IDictionary<string, string> prop)
        {
            var dependOnFacetHandler = GetDependedFacetHandler(_dependsOnFacetName);

            string[] elems = _predefinedBuckets.Get(bucketString);

            if (elems == null || elems.Length == 0) return EmptyFilter.Instance;
            if (elems.Length == 1) return dependOnFacetHandler.BuildRandomAccessFilter(elems[0], prop);
            return dependOnFacetHandler.BuildRandomAccessOrFilter(elems, prop, false);
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] bucketStrings, IDictionary<string, string> prop)
        {
            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>();
            var dependOnFacetHandler = GetDependedFacetHandler(_dependsOnFacetName);

            foreach (string bucketString in bucketStrings)
            {
                string[] vals = _predefinedBuckets.Get(bucketString);
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
                var dependOnFacetHandler = GetDependedFacetHandler(_dependsOnFacetName);

                HashSet<string> selections = new HashSet<string>();
                foreach (string bucket in bucketStrings)
                {
                    string[] vals = _predefinedBuckets.Get(bucket);
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
            var dependOnFacetHandler = GetDependedFacetHandler(_dependsOnFacetName);
            FacetDataCache data = dependOnFacetHandler.GetFacetData<FacetDataCache>(reader);
            return data.GetNumItems(id);
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec)
        {
            var dependOnFacetHandler = GetDependedFacetHandler(_dependsOnFacetName);
            return new BucketFacetCountCollectorSource(_name, sel, fspec, _predefinedBuckets, dependOnFacetHandler);
        }

        private class BucketFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly string _name;
            private readonly BrowseSelection _sel;
            private readonly FacetSpec _ospec;
            private readonly IDictionary<string, string[]> _predefinedBuckets;
            private readonly IFacetHandler _dependOnFacetHandler;

            public BucketFacetCountCollectorSource(string name, BrowseSelection sel, FacetSpec ospec, IDictionary<string, string[]> predefinedBuckets, IFacetHandler dependOnFacetHandler)
            {
                _name = name;
                _sel = sel;
                _ospec = ospec;
                _predefinedBuckets = predefinedBuckets;
                _dependOnFacetHandler = dependOnFacetHandler;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                IFacetCountCollector defaultCollector = _dependOnFacetHandler.GetFacetCountCollectorSource(_sel, _ospec).GetFacetCountCollector(reader, docBase);
                if (defaultCollector is DefaultFacetCountCollector)
                {
                    return new BucketFacetCountCollector(_name, (DefaultFacetCountCollector)defaultCollector, _ospec, _predefinedBuckets, reader.NumDocs);
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
