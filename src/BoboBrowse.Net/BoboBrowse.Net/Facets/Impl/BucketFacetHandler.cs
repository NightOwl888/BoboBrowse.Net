// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using Common.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class BucketFacetHandler : FacetHandler<FacetDataNone>
    {
        private static ILog logger = LogManager.GetLogger(typeof(BucketFacetHandler));
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

        public override string[] GetFieldValues(BoboIndexReader reader, int id)
        {
            var dependOnFacetHandler = GetDependedFacetHandler(_dependsOnFacetName);
	        return dependOnFacetHandler.GetFieldValues(reader, id);
        }

        public override object[] GetRawFieldValues(BoboIndexReader reader, int id)
        {
            var dependOnFacetHandler = GetDependedFacetHandler(_dependsOnFacetName);
            return dependOnFacetHandler.GetRawFieldValues(reader, id);
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string bucketString, Properties prop)
        {
            var dependOnFacetHandler = GetDependedFacetHandler(_dependsOnFacetName);

            string[] elems = _predefinedBuckets.Get(bucketString);

            if (elems == null || elems.Length == 0) return EmptyFilter.Instance;
            if (elems.Length == 1) return dependOnFacetHandler.BuildRandomAccessFilter(elems[0], prop);
            return dependOnFacetHandler.BuildRandomAccessOrFilter(elems, prop, false);
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] bucketStrings, Properties prop)
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

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] bucketStrings, Properties prop, bool isNot)
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

        public override int GetNumItems(BoboIndexReader reader, int id)
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

            public override IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase)
            {
                IFacetCountCollector defaultCollector = _dependOnFacetHandler.GetFacetCountCollectorSource(_sel, _ospec).GetFacetCountCollector(reader, docBase);
                if (defaultCollector is DefaultFacetCountCollector)
                {
                    return new BucketFacetCountCollector(_name, (DefaultFacetCountCollector)defaultCollector, _ospec, _predefinedBuckets, reader.NumDocs());
                }
                else
                {
                    throw new InvalidOperationException("dependent facet handler must build DefaultFacetCountCollector");
                }
            }
        }

        public override FacetDataNone Load(BoboIndexReader reader)
        {
            return FacetDataNone.Instance;
        }
    }
}
