// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Common.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// author nnarkhed
    /// </summary>
    public class GeoSimpleFacetCountCollector : IFacetCountCollector
    {
        private static ILog log = LogManager.GetLogger<GeoSimpleFacetCountCollector>();
	    private readonly FacetSpec _spec;
	    private readonly string _name;
	    private int[] _latCount;
	    private int[] _longCount;
	    private readonly BigSegmentedArray _latOrderArray;
	    private FacetDataCache _latDataCache;
	    private readonly TermStringList _predefinedRanges;
	    private int[][] _latPredefinedRangeIndexes;
        private readonly BigSegmentedArray _longOrderArray;
	    private FacetDataCache _longDataCache;
	    private int[][] _longPredefinedRangeIndexes;
	    private int _docBase;

        public GeoSimpleFacetCountCollector(string name, FacetDataCache latDataCache, FacetDataCache longDataCache, int docBase, FacetSpec spec, IEnumerable<string> predefinedRanges)
        {
            _name = name;
            _latDataCache = latDataCache;
            _longDataCache = longDataCache;
            _latCount = new int[_latDataCache.Freqs.Length];
            _longCount = new int[_longDataCache.Freqs.Length];
            log.Info("latCount: " + _latDataCache.Freqs.Length + " longCount: " + _longDataCache.Freqs.Length);
            _latOrderArray = _latDataCache.OrderArray;
            _longOrderArray = _longDataCache.OrderArray;
            _docBase = docBase;
            _spec = spec;
            _predefinedRanges = new TermStringList();
            var predefinedRangesTemp = new List<string>(predefinedRanges);
            predefinedRangesTemp.Sort();
            _predefinedRanges.AddAll(predefinedRangesTemp);

            if (predefinedRanges != null)
            {
                _latPredefinedRangeIndexes = new int[_predefinedRanges.Count][];
                for (int j = 0; j < _latPredefinedRangeIndexes.Length; j++)
                {
                    _latPredefinedRangeIndexes[j] = new int[2];
                }
                _longPredefinedRangeIndexes = new int[_predefinedRanges.Count][];
                for (int j = 0; j < _longPredefinedRangeIndexes.Length; j++)
                {
                    _longPredefinedRangeIndexes[j] = new int[2];
                }
                int i = 0;
                foreach (string range in _predefinedRanges)
                {
                    int[] ranges = GeoSimpleFacetFilter.Parse(_latDataCache, _longDataCache, range);
                    _latPredefinedRangeIndexes[i][0] = ranges[0];   // latStart 
                    _latPredefinedRangeIndexes[i][1] = ranges[1];   // latEnd
                    _longPredefinedRangeIndexes[i][0] = ranges[2];  // longStart
                    _longPredefinedRangeIndexes[i][1] = ranges[3];  // longEnd
                    i++;
                }
            }
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.facets.FacetCountCollector#collect(int)
        /// </summary>
        /// <param name="docid"></param>
        public virtual void Collect(int docid)
        {
            // increment the count only if both latitude and longitude ranges are true for a particular docid
            foreach (int[] range in _latPredefinedRangeIndexes)
            {
                int latValue = _latOrderArray.Get(docid);
                int longValue = _longOrderArray.Get(docid);
                int latStart = range[0];
                int latEnd = range[1];
                if (latValue >= latStart && latValue <= latEnd)
                {
                    foreach (int[] longRange in _longPredefinedRangeIndexes)
                    {
                        int longStart = longRange[0];
                        int longEnd = longRange[1];
                        if (longValue >= longStart && longValue <= longEnd)
                        {
                            _latCount[_latOrderArray.Get(docid)]++;
                            _longCount[_longOrderArray.Get(docid)]++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.facets.FacetCountCollector#collectAll()
        /// </summary>
        public virtual void CollectAll()
        {
            _latCount = _latDataCache.Freqs;
            _longCount = _longDataCache.Freqs;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.facets.FacetCountCollector#getCountDistribution()
        /// </summary>
        /// <returns></returns>
        public virtual int[] GetCountDistribution()
        {
            int[] dist = null;
            if (_latPredefinedRangeIndexes != null)
            {
                dist = new int[_latPredefinedRangeIndexes.Length];
                int n = 0;
                int start;
                int end;
                foreach (int[] range in _latPredefinedRangeIndexes)
                {
                    start = range[0];
                    end = range[1];
                    int sum = 0;
                    for (int i = start; i < end; i++)
                    {
                        sum += _latCount[i];
                    }
                    dist[n++] = sum;
                }
            }
            return dist;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.facets.FacetCountCollector#getName()
        /// </summary>
        public virtual string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetAccessible#getFacet(java.lang.String)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual BrowseFacet GetFacet(string value)
        {
            BrowseFacet facet = null;
            int[] range = FacetRangeFilter.Parse(_latDataCache, value);

            if (range != null)
            {
                int sum = 0;
                for (int i = range[0]; i <= range[1]; ++i)
                {
                    sum += _latCount[i];
                }
                facet = new BrowseFacet(value, sum);
            }
            return facet;
        }

        public int GetFacetHitsCount(object value)
        {
            int[] range = FacetRangeFilter.Parse(_latDataCache, (string)value);

            if (range != null)
            {
                int sum = 0;
                for (int i = range[0]; i <= range[1]; ++i)
                {
                    sum += _latCount[i];
                }
                return sum;
            }
            return 0;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetAccessible#getFacets()
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<BrowseFacet> GetFacets()
        {
            if (_spec != null)
            {
                if (_latPredefinedRangeIndexes != null)
                {
                    int minCount = _spec.MinHitCount;
                    int[] rangeCounts = new int[_latPredefinedRangeIndexes.Length];
                    for (int i = 0; i < _latCount.Length; ++i)
                    {
                        if (_latCount[i] > 0)
                        {
                            for (int k = 0; k < _latPredefinedRangeIndexes.Length; ++k)
                            {
                                if (i >= _latPredefinedRangeIndexes[k][0] && i <= _latPredefinedRangeIndexes[k][1])
                                {
                                    rangeCounts[k] += _latCount[i];
                                }
                            }
                        }
                    }
                    List<BrowseFacet> list = new List<BrowseFacet>(rangeCounts.Length);
                    for (int i = 0; i < rangeCounts.Length; ++i)
                    {
                        if (rangeCounts[i] >= minCount)
                        {
                            BrowseFacet choice = new BrowseFacet();
                            choice.HitCount = rangeCounts[i];
                            choice.Value = _predefinedRanges.Get(i);
                            list.Add(choice);
                        }
                    }
                    return list;
                }
                else
                {
                    return IFacetCountCollector_Fields.EMPTY_FACET_LIST;
                }
            }
            else
            {
                return IFacetCountCollector_Fields.EMPTY_FACET_LIST;
            }
        }

        public virtual void Close()
        { 
        }

        public virtual FacetIterator Iterator()
        {
            // each range is of the form <lat, lon, radius>
            int[] rangeCounts = new int[_latPredefinedRangeIndexes.Length];
            for (int i = 0; i < _latCount.Length; ++i)
            {
                if (_latCount[i] > 0)
                {
                    for (int k = 0; k < _latPredefinedRangeIndexes.Length; ++k)
                    {
                        if (i >= _latPredefinedRangeIndexes[k][0] && i <= _latPredefinedRangeIndexes[k][1])
                        {
                            rangeCounts[k] += _latCount[i];
                        }
                    }
                }
            }
            return new DefaultFacetIterator(_predefinedRanges, rangeCounts, rangeCounts.Length, true);
        }
    }
}
