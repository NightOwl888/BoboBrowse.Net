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
        protected readonly string _dataFacetName;
        protected RangeFacetHandler _dataFacetHandler;

        public DynamicRangeFacetHandler(string name, string dataFacetName)
            : base(name, new string[] { dataFacetName })
        {
            this._dataFacetName = dataFacetName;
        }

        protected abstract string BuildRangeString(string val);
        protected abstract IEnumerable<string> BuildAllRangeStrings();
        protected abstract string GetValueFromRangeString(string rangeString);

        public override RandomAccessFilter BuildRandomAccessFilter(string val, IDictionary<string, string> props)
        {
            return _dataFacetHandler.BuildRandomAccessFilter(BuildRangeString(val), props);
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, IDictionary<string, string> prop)
        {
            List<string> valList = new List<string>(vals.Length);
            foreach (string val in vals)
            {
                valList.Add(BuildRangeString(val));
            }

            return _dataFacetHandler.BuildRandomAccessAndFilter(valList.ToArray(), prop);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, IDictionary<string, string> prop, bool isNot)
        {
            List<string> valList = new List<string>(vals.Length);
            foreach (string val in vals)
            {
                valList.Add(BuildRangeString(val));
            }
            return _dataFacetHandler.BuildRandomAccessOrFilter(valList.ToArray(), prop, isNot);
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec)
        {
            var list = BuildAllRangeStrings();
            return new DynamicRangeFacetCountCollectorSource(this, _dataFacetHandler, Name, fspec, list);
        }

        private class DynamicRangeFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly DynamicRangeFacetHandler _parent;
            private readonly RangeFacetHandler _dataFacetHandler;
            private readonly string _name;
            private readonly FacetSpec _fspec;
            private readonly IEnumerable<string> _predefinedList;

            public DynamicRangeFacetCountCollectorSource(DynamicRangeFacetHandler parent, RangeFacetHandler dataFacetHandler, string name, FacetSpec fspec, IEnumerable<string> predefinedList)
            {
                this._parent = parent;
                this._dataFacetHandler = dataFacetHandler;
                this._name = name;
                this._fspec = fspec;
                this._predefinedList = predefinedList;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                IFacetDataCache dataCache = this._dataFacetHandler.GetFacetData<IFacetDataCache>(reader);
                return new DynamicRangeFacetCountCollector(_parent, _name, dataCache, docBase, _fspec, _predefinedList);
            }

        }

        public override string[] GetFieldValues(BoboSegmentReader reader, int docid)
        {
            return _dataFacetHandler.GetFieldValues(reader, docid);
        }

        public override object[] GetRawFieldValues(BoboSegmentReader reader, int docid)
        {
            return _dataFacetHandler.GetRawFieldValues(reader, docid);
        }

        public override DocComparatorSource GetDocComparatorSource()
        {
            return _dataFacetHandler.GetDocComparatorSource();
        }

        public override FacetDataNone Load(BoboSegmentReader reader)
        {
            _dataFacetHandler = (RangeFacetHandler)GetDependedFacetHandler(_dataFacetName);
            return FacetDataNone.Instance;
        }

        private class DynamicRangeFacetCountCollector : RangeFacetCountCollector
        {
            private readonly DynamicRangeFacetHandler parent;

            internal DynamicRangeFacetCountCollector(DynamicRangeFacetHandler parent, string name, IFacetDataCache dataCache, int docBase, FacetSpec fspec, IEnumerable<string> predefinedList)
                : base(name, dataCache, docBase, fspec, predefinedList)
            {
                this.parent = parent;
            }

            public override BrowseFacet GetFacet(string value)
            {
                string rangeString = parent.BuildRangeString(value);
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
                string rangeString = parent.BuildRangeString((string)value);
                return base.GetFacetHitsCount(rangeString);
            }

            public override IEnumerable<BrowseFacet> GetFacets()
            {
                IEnumerable<BrowseFacet> list = base.GetFacets();
                List<BrowseFacet> retList = new List<BrowseFacet>();
                IEnumerator<BrowseFacet> iter = list.GetEnumerator();
                while (iter.MoveNext())
                {
                    BrowseFacet facet = iter.Current;
                    string val = facet.Value;
                    string rangeString = parent.GetValueFromRangeString(val);
                    if (rangeString != null)
                    {
                        BrowseFacet convertedFacet = new BrowseFacet(rangeString, facet.FacetValueHitCount);
                        retList.Add(convertedFacet);
                    }
                }
                return retList;
            }

            public override FacetIterator Iterator()
            {
                FacetIterator iter = base.Iterator();

                List<BrowseFacet> facets = new List<BrowseFacet>();
                while (iter.HasNext())
                {
                    string facet = iter.Next();
                    int count = iter.Count;
                    facets.Add(new BrowseFacet(parent.GetValueFromRangeString(facet), count));
                }
                facets.Sort(ListMerger.FACET_VAL_COMPARATOR);
                return new PathFacetIterator(facets);
            }
        }
    }
}