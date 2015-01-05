// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Common.Logging;
    using System;
    using System.Collections.Generic;

    public class GeoFacetHandler : FacetHandler<GeoFacetHandler.GeoFacetData>
    {
        private static ILog logger = LogManager.GetLogger<GeoFacetHandler>();
	    private string _latFieldName;
	    private string _lonFieldName;
	    // variable to specify if the geo distance calculations are in miles. Default is miles
	    private bool _miles;

        /// <summary>
        /// Initializes a new instance of <see cref="T:GeoFacetHandler"/>.
        /// </summary>
        /// <param name="name">Name of the geo facet.</param>
        /// <param name="latFieldName">Name of the Lucene.Net index field that stores the latitude value.</param>
        /// <param name="lonFieldName">Name of the Lucene.Net index field that stores the longitude value.</param>
        public GeoFacetHandler(string name, string latFieldName, string lonFieldName)
            : base(name, new List<string>(new string[] { latFieldName, lonFieldName }))
        {
            _latFieldName = latFieldName;
            _lonFieldName = lonFieldName;
            _miles = true;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:GeoFacetHandler"/>.
        /// </summary>
        /// <param name="name">Name of the geo facet.</param>
        /// <param name="latFieldName">Name of the Lucene.Net index field that stores the latitude value.</param>
        /// <param name="lonFieldName">Name of the Lucene.Net index field that stores the longitude value.</param>
        /// <param name="miles">variable to specify if the geo distance calculations are in miles. False indicates distance calculation is in kilometers</param>
        public GeoFacetHandler(string name, string latFieldName, string lonFieldName, bool miles)
            : this(name, latFieldName, lonFieldName)
        {
            _miles = miles;
        }

        public class GeoFacetData
        {
            private BigFloatArray _xValArray;
            private BigFloatArray _yValArray;
            private BigFloatArray _zValArray;

            public GeoFacetData()
            {
                _xValArray = null;
                _yValArray = null;
                _zValArray = null;
            }

            public GeoFacetData(BigFloatArray xvals, BigFloatArray yvals, BigFloatArray zvals)
            {
                _xValArray = xvals;
                _yValArray = yvals;
                _zValArray = zvals;
            }

            public static BigFloatArray NewInstance(int maxDoc)
            {
                BigFloatArray array = new BigFloatArray(maxDoc);
                array.EnsureCapacity(maxDoc);
                return array;
            }

            /// <summary>
            /// Gets or sets the _xValArray
            /// </summary>
            public virtual BigFloatArray xValArray
            {
                get { return _xValArray; }
                set { _xValArray = value; }
            }

            /// <summary>
            /// Gets or sets the _yValArray
            /// </summary>
            public virtual BigFloatArray yValArray
            {
                get { return _yValArray; }
                set { _yValArray = value; }
            }

            /// <summary>
            /// Gets or sets the _zValArray
            /// </summary>
            public virtual BigFloatArray zValArray
            {
                get { return _zValArray; }
                set { _zValArray = value; }
            }

            public virtual void Load(string latFieldName, string lonFieldName, BoboIndexReader reader)
            {
                if (reader == null) throw new ArgumentNullException("reader object is null");

                FacetDataCache latCache = (FacetDataCache)reader.GetFacetData(latFieldName);
                FacetDataCache lonCache = (FacetDataCache)reader.GetFacetData(lonFieldName);

                int maxDoc = reader.MaxDoc;

                BigFloatArray xVals = this._xValArray;
                BigFloatArray yVals = this._yValArray;
                BigFloatArray zVals = this._zValArray;

                if (xVals == null)
                    xVals = NewInstance(maxDoc);
                else
                    xVals.EnsureCapacity(maxDoc);
                if (yVals == null)
                    yVals = NewInstance(maxDoc);
                else
                    yVals.EnsureCapacity(maxDoc);
                if (zVals == null)
                    zVals = NewInstance(maxDoc);
                else
                    zVals.EnsureCapacity(maxDoc);

                this._xValArray = xVals;
                this._yValArray = yVals;
                this._zValArray = zVals;

                BigSegmentedArray latOrderArray = latCache.OrderArray;
                ITermValueList latValList = latCache.ValArray;

                BigSegmentedArray lonOrderArray = lonCache.OrderArray;
                ITermValueList lonValList = lonCache.ValArray;

                for (int i = 0; i < maxDoc; ++i)
                {
                    string docLatString = latValList.Get(latOrderArray.Get(i)).Trim();
                    string docLonString = lonValList.Get(lonOrderArray.Get(i)).Trim();

                    float docLat = 0;
                    if (docLatString.Length > 0)
                    {
                        docLat = float.Parse(docLatString);
                    }

                    float docLon = 0;
                    if (docLonString.Length > 0)
                    {
                        docLon = float.Parse(docLonString);
                    }

                    float[] coords = GeoMatchUtil.GeoMatchCoordsFromDegrees(docLat, docLon);
                    _xValArray.Add(i, coords[0]);
                    _yValArray.Add(i, coords[1]);
                    _zValArray.Add(i, coords[2]);
                }
            }
        }

        /// <summary>
        /// Builds a random access filter.
        /// </summary>
        /// <param name="value">Should be of the form: lat, lon: rad</param>
        /// <param name="selectionProperty"></param>
        /// <returns></returns>
        public override RandomAccessFilter BuildRandomAccessFilter(string value, Properties selectionProperty)
        {
            GeoFacetCountCollector.GeoRange range = GeoFacetCountCollector.Parse(value);
            return new GeoFacetFilter(this, range.Lat, range.Lon, range.Rad, _miles);
        }

        public override DocComparatorSource GetDocComparatorSource()
        {
            throw new NotSupportedException("Doc comparator not yet supported for Geo Facets");
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec)
        {
            IEnumerable<string> ranges = sel.Values;
            return new GeoFacetHandlerFacetCountCollectorSource(this, ranges, fspec);
        }

        private class GeoFacetHandlerFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly GeoFacetHandler _parent;
            private readonly IEnumerable<string> _ranges;
            private readonly FacetSpec _fspec;

            public GeoFacetHandlerFacetCountCollectorSource(GeoFacetHandler parent, IEnumerable<string> ranges, FacetSpec fspec)
            {
                _parent = parent;
                _ranges = ranges;
                _fspec = fspec;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase)
            {
                GeoFacetData dataCache = _parent.GetFacetData<GeoFacetData>(reader);
                return new GeoFacetCountCollector(_parent._name, dataCache, docBase, _fspec, _ranges, _parent._miles);
            }
        }

        public override string[] GetFieldValues(BoboIndexReader reader, int id)
        {
            GeoFacetData dataCache = GetFacetData<GeoFacetData>(reader);
            BigFloatArray xvals = dataCache.xValArray;
            BigFloatArray yvals = dataCache.yValArray;
            BigFloatArray zvals = dataCache.zValArray;

            float xvalue = xvals.Get(id);
            float yvalue = yvals.Get(id);
            float zvalue = zvals.Get(id);
            float lat = GeoMatchUtil.GetMatchLatDegreesFromXYZCoords(xvalue, yvalue, zvalue);
            float lon = GeoMatchUtil.GetMatchLonDegreesFromXYZCoords(xvalue, yvalue, zvalue);

            string[] fieldValues = new string[2];
            fieldValues[0] = Convert.ToString(lat);
            fieldValues[1] = Convert.ToString(lon);
            return fieldValues;
        }

        public override GeoFacetData Load(BoboIndexReader reader)
        {
            GeoFacetData dataCache = new GeoFacetData();
            dataCache.Load(_latFieldName, _lonFieldName, reader);
            return dataCache;
        }
    }
}
