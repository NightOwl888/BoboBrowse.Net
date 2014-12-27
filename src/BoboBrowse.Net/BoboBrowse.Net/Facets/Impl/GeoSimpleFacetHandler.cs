// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// author nnarkhed
    /// </summary>
    public class GeoSimpleFacetHandler : RuntimeFacetHandler<FacetDataNone>
    {
        private static ILog logger = LogManager.GetLogger<GeoSimpleFacetHandler>();
	    protected readonly string _latFacetName;
        protected readonly string _longFacetName;
	    protected RangeFacetHandler _latFacetHandler;
	    protected RangeFacetHandler _longFacetHandler;

        public class GeoLatLonRange
        {
            public readonly string latRange;
            public readonly string lonRange;
            public readonly float latStart;
            public readonly float latEnd;
            public readonly float lonStart;
            public readonly float lonEnd;
            public readonly float radius;

            private GeoLatLonRange(string latRange, string lonRange, float latStart, float latEnd, float lonStart, float lonEnd, float radius)
            {
                this.latRange = latRange;
                this.lonRange = lonRange;
                this.latStart = latStart;
                this.latEnd = latEnd;
                this.lonStart = lonStart;
                this.lonEnd = lonEnd;
                this.radius = radius;
            }

            public static GeoLatLonRange Parse(string val)
            {
                GeoFacetCountCollector.GeoRange range = GeoFacetCountCollector.Parse(val);
                float latStart = range.Lat - range.Rad;
                float latEnd = range.Lat + range.Rad;
                float lonStart = range.Lon - range.Rad;
                float lonEnd = range.Lon + range.Rad;

                StringBuilder buf = new StringBuilder();
                buf.Append("[").Append(latStart).Append(" TO ").Append(latEnd).Append("]");
                string latRange = buf.ToString();

                buf = new StringBuilder();
                buf.Append("[").Append(lonStart).Append(" TO ").Append(lonEnd).Append("]");
                string lonRange = buf.ToString();

                return new GeoLatLonRange(latRange, lonRange, latStart, latEnd, lonStart, lonEnd, range.Rad);
            }
        }

        public GeoSimpleFacetHandler(string name, string latFacetName, string longFacetName)
            : base(name, new string[] { latFacetName, longFacetName })
        {
            _latFacetName = latFacetName;
            _longFacetName = longFacetName;
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string val, Properties props)
        {
            GeoLatLonRange range = GeoLatLonRange.Parse(val);

            RandomAccessFilter latFilter = _latFacetHandler.BuildRandomAccessFilter(range.latRange, props);
            RandomAccessFilter longFilter = _longFacetHandler.BuildRandomAccessFilter(range.lonRange, props);
            return new RandomAccessAndFilter(new RandomAccessFilter[] { latFilter, longFilter });
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties props)
        {
            List<string> latValList = new List<string>(vals.Length);
            List<string> longValList = new List<string>(vals.Length);
            foreach (string val in vals)
            {
                GeoLatLonRange range = GeoLatLonRange.Parse(val);
                latValList.Add(range.latRange);
                longValList.Add(range.lonRange);
            }
            RandomAccessFilter latFilter = _latFacetHandler.BuildRandomAccessAndFilter(latValList.ToArray(), props);
            RandomAccessFilter longFilter = _longFacetHandler.BuildRandomAccessAndFilter(longValList.ToArray(), props);
            return new RandomAccessAndFilter(new RandomAccessFilter[] { latFilter, longFilter });
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties props, bool isNot)
        {
            List<string> latValList = new List<string>(vals.Length);
            List<string> longValList = new List<string>(vals.Length);
            foreach (string val in vals)
            {
                GeoLatLonRange range = GeoLatLonRange.Parse(val);
                latValList.Add(range.latRange);
                longValList.Add(range.lonRange);
            }
            RandomAccessFilter latFilter = _latFacetHandler.BuildRandomAccessOrFilter(latValList.ToArray(), props, isNot);
            RandomAccessFilter longFilter = _longFacetHandler.BuildRandomAccessOrFilter(longValList.ToArray(), props, isNot);
            return new RandomAccessAndFilter(new RandomAccessFilter[] { latFilter, longFilter });
        }

        private static IEnumerable<string> BuildAllRangeStrings(string[] values)
        {
            if (values == null) return new List<string>();
            List<string> ranges = new List<string>(values.Length);
            //string[] range = null;
            foreach (string value in values)
            {
                ranges.Add(value);
            }
            return ranges;
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec)
        {
            IEnumerable<string> list = BuildAllRangeStrings(sel.Values);
            // every string in the above list is of the form <latitude, longitude, radius>, which can be interpreted by GeoSimpleFacetCountCollector
            return new GeoSimpleFacetHandlerFacetCountCollectorSource(_latFacetHandler, _longFacetHandler, _name, fspec, list);
        }

        private class GeoSimpleFacetHandlerFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly RangeFacetHandler _latFacetHandler;
            private readonly RangeFacetHandler _longFacetHandler;
            private readonly string _name;
            private readonly FacetSpec _fspec;
            private readonly IEnumerable<string> _list;

            public GeoSimpleFacetHandlerFacetCountCollectorSource(RangeFacetHandler latFacetHandler, RangeFacetHandler longFacetHandler,
                string name, FacetSpec fspec, IEnumerable<string> list)
            {
                _latFacetHandler = latFacetHandler;
                _longFacetHandler = longFacetHandler;
                _name = name;
                _fspec = fspec;
                _list = list;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase)
            {
                FacetDataCache latDataCache = _latFacetHandler.GetFacetData<FacetDataCache>(reader);
                FacetDataCache longDataCache = _longFacetHandler.GetFacetData<FacetDataCache>(reader);
                return new GeoSimpleFacetCountCollector(_name, latDataCache, longDataCache, docBase, _fspec, _list);
            }
        }

        public override string[] GetFieldValues(BoboIndexReader reader, int docid)
        {
            string[] latValues = _latFacetHandler.GetFieldValues(reader, docid);
            string[] longValues = _longFacetHandler.GetFieldValues(reader, docid);
            string[] allValues = new string[latValues.Length + longValues.Length];
            int index = 0;
            foreach (string value in latValues)
            {
                allValues[index++] = value;
            }
            foreach (string value in longValues)
            {
                allValues[index++] = value;
            }
            return allValues;
        }

        public override object[] GetRawFieldValues(BoboIndexReader reader, int docid)
        {
            object[] latValues = _latFacetHandler.GetRawFieldValues(reader, docid);
            object[] longValues = _longFacetHandler.GetRawFieldValues(reader, docid);
            object[] allValues = new object[latValues.Length + longValues.Length];
            int index = 0;
            foreach (object value in latValues)
            {
                allValues[index++] = value;
            }
            foreach (object value in longValues)
            {
                allValues[index++] = value;
            }
            return allValues;
        }

        public override FacetDataNone Load(BoboIndexReader reader)
        {
            _latFacetHandler = (RangeFacetHandler)GetDependedFacetHandler(_latFacetName);
            _longFacetHandler = (RangeFacetHandler)GetDependedFacetHandler(_longFacetName);
            return FacetDataNone.instance;
        }

        public override DocComparatorSource GetDocComparatorSource()
        {
            return new GeoFacetDocComparatorSource(this);
        }

        public class GeoFacetDocComparatorSource : DocComparatorSource
        {
            private FacetHandler<FacetDataNone> _facetHandler;

            public GeoFacetDocComparatorSource(GeoSimpleFacetHandler geoSimpleFacetHandler)
            {
                _facetHandler = geoSimpleFacetHandler;
            }

            public override DocComparator GetComparator(IndexReader reader, int docbase)
            {
                if (!(reader is BoboIndexReader)) throw new ArgumentException("reader not instance of " + typeof(BoboIndexReader));
                //BoboIndexReader boboReader = (BoboIndexReader)reader;  // NOT USED
                //FacetDataNone dataCache = _facetHandler.GetFacetData<FacetDataNone>((BoboIndexReader)reader);  // NOT USED
                return new GeoSimpleFacetHandlerDocComparator();
            }

            public class GeoSimpleFacetHandlerDocComparator : DocComparator
            {
                public override IComparable Value(ScoreDoc doc)
                {
                    return 1;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    return 0;
                }
            }
        }
    }
}
