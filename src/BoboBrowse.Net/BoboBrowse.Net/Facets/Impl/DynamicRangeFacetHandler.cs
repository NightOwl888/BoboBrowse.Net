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
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using System.Collections.Generic;

    ///<summary>@author ymatsuda
    ///</summary>
    public abstract class DynamicRangeFacetHandler : RuntimeFacetHandler<FacetDataNone>
    {
        protected readonly string m_dataFacetName;
        protected RangeFacetHandler m_dataFacetHandler;

        public DynamicRangeFacetHandler(string name, string dataFacetName)
            : base(name, new string[] { dataFacetName })
        {
            this.m_dataFacetName = dataFacetName;
        }

        protected abstract string BuildRangeString(string val);
        protected abstract IList<string> BuildAllRangeStrings();
        protected abstract string GetValueFromRangeString(string rangeString);

        public override RandomAccessFilter BuildRandomAccessFilter(string val, IDictionary<string, string> props)
        {
            return m_dataFacetHandler.BuildRandomAccessFilter(BuildRangeString(val), props);
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, IDictionary<string, string> prop)
        {
            List<string> valList = new List<string>(vals.Length);
            foreach (string val in vals)
            {
                valList.Add(BuildRangeString(val));
            }

            return m_dataFacetHandler.BuildRandomAccessAndFilter(valList.ToArray(), prop);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, IDictionary<string, string> prop, bool isNot)
        {
            List<string> valList = new List<string>(vals.Length);
            foreach (string val in vals)
            {
                valList.Add(BuildRangeString(val));
            }
            return m_dataFacetHandler.BuildRandomAccessOrFilter(valList.ToArray(), prop, isNot);
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec)
        {
            var list = BuildAllRangeStrings();
            return new DynamicRangeFacetCountCollectorSource(this, m_dataFacetHandler, Name, fspec, list);
        }

        private class DynamicRangeFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly DynamicRangeFacetHandler m_parent;
            private readonly RangeFacetHandler m_dataFacetHandler;
            private readonly string m_name;
            private readonly FacetSpec m_fspec;
            private readonly IList<string> m_predefinedList;

            public DynamicRangeFacetCountCollectorSource(DynamicRangeFacetHandler parent, RangeFacetHandler dataFacetHandler, string name, FacetSpec fspec, IList<string> predefinedList)
            {
                this.m_parent = parent;
                this.m_dataFacetHandler = dataFacetHandler;
                this.m_name = name;
                this.m_fspec = fspec;
                this.m_predefinedList = predefinedList;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                FacetDataCache dataCache = this.m_dataFacetHandler.GetFacetData<FacetDataCache>(reader);
                return new DynamicRangeFacetCountCollector(m_parent, m_name, dataCache, docBase, m_fspec, m_predefinedList);
            }

        }

        public override string[] GetFieldValues(BoboSegmentReader reader, int docid)
        {
            return m_dataFacetHandler.GetFieldValues(reader, docid);
        }

        public override object[] GetRawFieldValues(BoboSegmentReader reader, int docid)
        {
            return m_dataFacetHandler.GetRawFieldValues(reader, docid);
        }

        public override DocComparerSource GetDocComparerSource()
        {
            return m_dataFacetHandler.GetDocComparerSource();
        }

        public override FacetDataNone Load(BoboSegmentReader reader)
        {
            m_dataFacetHandler = (RangeFacetHandler)GetDependedFacetHandler(m_dataFacetName);
            return FacetDataNone.Instance;
        }

        private class DynamicRangeFacetCountCollector : RangeFacetCountCollector
        {
            private readonly DynamicRangeFacetHandler m_parent;

            internal DynamicRangeFacetCountCollector(DynamicRangeFacetHandler parent, string name, FacetDataCache dataCache, int docBase, FacetSpec fspec, IList<string> predefinedList)
                : base(name, dataCache, docBase, fspec, predefinedList)
            {
                this.m_parent = parent;
            }

            public override BrowseFacet GetFacet(string value)
            {
                string rangeString = m_parent.BuildRangeString(value);
                BrowseFacet facet = base.GetFacet(rangeString);
                if (facet != null)
                {
                    return new BrowseFacet(value, facet.FacetValueHitCount);
                }
                else
                {
                    return null;
                }
            }

            public override int GetFacetHitsCount(object value)
            {
                string rangeString = m_parent.BuildRangeString((string)value);
                return base.GetFacetHitsCount(rangeString);
            }

            public override ICollection<BrowseFacet> GetFacets()
            {
                IEnumerable<BrowseFacet> list = base.GetFacets();
                List<BrowseFacet> retList = new List<BrowseFacet>();
                IEnumerator<BrowseFacet> iter = list.GetEnumerator();
                while (iter.MoveNext())
                {
                    BrowseFacet facet = iter.Current;
                    string val = facet.Value;
                    string rangeString = m_parent.GetValueFromRangeString(val);
                    if (rangeString != null)
                    {
                        BrowseFacet convertedFacet = new BrowseFacet(rangeString, facet.FacetValueHitCount);
                        retList.Add(convertedFacet);
                    }
                }
                return retList;
            }

            public override FacetIterator GetIterator()
            {
                FacetIterator iter = base.GetIterator();

                List<BrowseFacet> facets = new List<BrowseFacet>();
                while (iter.HasNext())
                {
                    string facet = iter.Next();
                    int count = iter.Count;
                    facets.Add(new BrowseFacet(m_parent.GetValueFromRangeString(facet), count));
                }
                facets.Sort(ListMerger.FACET_VAL_COMPARER);
                return new PathFacetIterator(facets);
            }
        }
    }
}