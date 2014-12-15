// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;
    using System.Collections.Generic;

    ///<summary>@author ymatsuda
    ///</summary>
    public abstract class DynamicRangeFacetHandler : RuntimeFacetHandler<FacetDataNone>
    {
        protected internal readonly string _dataFacetName;
        protected internal RangeFacetHandler _dataFacetHandler;

        public DynamicRangeFacetHandler(string name, string dataFacetName)
            : base(name, new string[] { dataFacetName })
        {
            this._dataFacetName = dataFacetName;
        }

        protected internal abstract string BuildRangeString(string val);
        protected internal abstract IEnumerable<string> BuildAllRangeStrings();
        protected internal abstract string GetValueFromRangeString(object rangeString);

        public override RandomAccessFilter BuildRandomAccessFilter(string val, Properties props)
        {
            return _dataFacetHandler.BuildRandomAccessFilter(BuildRangeString(val), props);
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties prop)
        {
            List<string> valList = new List<string>(vals.Length);
            foreach (string val in vals)
            {
                valList.Add(BuildRangeString(val));
            }

            return _dataFacetHandler.BuildRandomAccessAndFilter(valList.ToArray(), prop);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot)
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
            return new DynamicRangeFacetCountCollector(this, name, _dataFacetHandler, docBase, fspec, list);
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

            public override IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase)
            {
                IFacetDataCache dataCache = this._dataFacetHandler.GetFacetData(reader);
                return new DynamicRangeFacetCountCollector(_parent, _name, dataCache, docBase, _fspec, _predefinedList);
            }

        }

        public override string[] GetFieldValues(BoboIndexReader reader, int docid)
        {
            return _dataFacetHandler.GetFieldValues(reader, docid);
        }

        public override object[] GetRawFieldValues(BoboIndexReader reader, int docid)
        {
            return _dataFacetHandler.GetRawFieldValues(reader, docid);
        }

        public override DocComparatorSource GetDocComparatorSource()
        {
            return _dataFacetHandler.GetDocComparatorSource();
        }

        public override FacetDataNone Load(BoboIndexReader reader)
        {
            _dataFacetHandler = (RangeFacetHandler)GetDependedFacetHandler(_dataFacetName);
            return FacetDataNone.instance;
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
                    return new BrowseFacet(value, facet.HitCount);
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
                    object val = facet.Value;
                    string rangeString = parent.GetValueFromRangeString(val);
                    if (rangeString != null)
                    {
                        BrowseFacet convertedFacet = new BrowseFacet(rangeString, facet.HitCount);
                        retList.Add(convertedFacet);
                    }
                }
                return retList;
            }
        }
    }
}