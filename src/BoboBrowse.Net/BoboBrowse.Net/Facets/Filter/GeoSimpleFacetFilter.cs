// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;
    using System;

    /// <summary>
    /// author nnarkhed
    /// </summary>
    public class GeoSimpleFacetFilter : RandomAccessFilter
    {
        //private static long serialVersionUID = 1L; // NOT USED
	    private readonly FacetHandler<FacetDataCache> _latFacetHandler;
	    private readonly FacetHandler<FacetDataCache> _longFacetHandler;
	    private readonly string _latRangeString;
        private readonly string _longRangeString;

        public GeoSimpleFacetFilter(FacetHandler<FacetDataCache> latHandler, FacetHandler<FacetDataCache> longHandler, string latRangeString, string longRangeString)
        {
            _latFacetHandler = latHandler;
            _longFacetHandler = longHandler;
            _latRangeString = latRangeString;
            _longRangeString = longRangeString;
        }

        private sealed class GeoSimpleDocIdSetIterator : DocIdSetIterator
        {
            private int _doc = -1;
            //private int _totalFreq; // NOT USED
		    private int _minID = int.MaxValue;
		    private int _maxID = -1;
		    private readonly int _latStart;
		    private readonly int _latEnd;
		    private readonly int _longStart;
		    private readonly int _longEnd;
		    private readonly BigSegmentedArray _latOrderArray;
		    private readonly BigSegmentedArray _longOrderArray;

            internal GeoSimpleDocIdSetIterator(int latStart, int latEnd, int longStart, int longEnd, FacetDataCache latDataCache, FacetDataCache longDataCache)
            {
                //_totalFreq = 0; // NOT USED
                _latStart = latStart;
                _longStart = longStart;
                _latEnd = latEnd;
                _longEnd = longEnd;
                for (int i = latStart; i <= latEnd; ++i)
                {
                    _minID = Math.Min(_minID, latDataCache.MinIDs[i]);
                    _maxID = Math.Max(_maxID, latDataCache.MaxIDs[i]);
                }
                for (int i = longStart; i <= longEnd; ++i)
                {
                    _minID = Math.Min(_minID, longDataCache.MinIDs[i]);
                    _maxID = Math.Max(_maxID, longDataCache.MaxIDs[i]);
                }
                _doc = Math.Max(-1, _minID - 1);
                _latOrderArray = latDataCache.OrderArray;
                _longOrderArray = longDataCache.OrderArray;
            }

            public override int DocID()
            {
                return _doc;
            }

            public override int NextDoc()
            {
                int latIndex;
                int longIndex;
                while (_doc < _maxID)
                {	//not yet reached end
                    latIndex = _latOrderArray.Get(++_doc);
                    longIndex = _latOrderArray.Get(_doc);
                    if ((latIndex >= _latStart && latIndex <= _latEnd) && (longIndex >= _longStart && longIndex <= _longEnd))
                        return _doc;
                }
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override int Advance(int id)
            {
                if (_doc < id)
                {
                    _doc = id - 1;
                }
                int latIndex;
                int longIndex;
                while (_doc < _maxID)
                {	//not yet reached end
                    latIndex = _latOrderArray.Get(++_doc);
                    longIndex = _latOrderArray.Get(_doc);
                    if ((latIndex >= _latStart && latIndex <= _latEnd) && (longIndex >= _longStart && longIndex <= _longEnd))
                        return _doc;
                }
                return DocIdSetIterator.NO_MORE_DOCS;
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboIndexReader reader)
        {
            FacetDataCache latDataCache = _latFacetHandler.GetFacetData<FacetDataCache>(reader);
		    FacetDataCache longDataCache = _longFacetHandler.GetFacetData<FacetDataCache>(reader);
		
		    int[] latRange = FacetRangeFilter.Parse(latDataCache, _latRangeString);
		    int[] longRange = FacetRangeFilter.Parse(longDataCache, _longRangeString);
		    if((latRange == null) || (longRange == null)) return null;

            return new GeoSimpleRandomAccessDocIdSet(latRange, longRange, latDataCache, longDataCache);
        }

        private class GeoSimpleRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly int _latStart;
            private readonly int _latEnd;
            private readonly int _longStart;
            private readonly int _longEnd;
            private readonly FacetDataCache _latDataCache;
            private readonly FacetDataCache _longDataCache;

            public GeoSimpleRandomAccessDocIdSet(int[] latRange, int[] longRange, FacetDataCache latDataCache, FacetDataCache longDataCache)
            {
                _latStart = latRange[0];
                _latEnd = latRange[1];
                _longStart = longRange[0];
                _longEnd = longRange[1];
                _latDataCache = latDataCache;
                _longDataCache = longDataCache;
            }

            public override bool Get(int docid)
            {
                int latIndex = _latDataCache.OrderArray.Get(docid);
                int longIndex = _longDataCache.OrderArray.Get(docid);
                return latIndex >= _latStart && latIndex <= _latEnd && longIndex >= _longStart && longIndex <= _longEnd;
            }

            public override DocIdSetIterator Iterator()
            {
                return new GeoSimpleDocIdSetIterator(_latStart, _latEnd, _longStart, _longEnd, _latDataCache, _longDataCache);
            }
        }

        public static int[] Parse(FacetDataCache latDataCache, FacetDataCache longDataCache, string rangeString)
        {
            GeoSimpleFacetHandler.GeoLatLonRange range = GeoSimpleFacetHandler.GeoLatLonRange.Parse(rangeString);
            // ranges[0] is latRangeStart, ranges[1] is latRangeEnd, ranges[2] is longRangeStart, ranges[3] is longRangeEnd
            string latLower = Convert.ToString(range.latStart);
            string latUpper = Convert.ToString(range.latEnd);
            string longLower = Convert.ToString(range.lonStart);
            string longUpper = Convert.ToString(range.lonEnd);

            int latStart, latEnd, longStart, longEnd;
            if (latLower == null)
                latStart = 1;
            else
            {
                latStart = latDataCache.ValArray.IndexOf(latLower);
                if (latStart < 0)
                {
                    latStart = -(latStart + 1);
                }
            }

            if (longLower == null)
                longStart = 1;
            else
            {
                longStart = longDataCache.ValArray.IndexOf(longLower);
                if (longStart < 0)
                {
                    longStart = -(longStart + 1);
                }
            }

            if (latUpper == null)
            {
                latEnd = latDataCache.ValArray.Count - 1;
            }
            else
            {
                latEnd = latDataCache.ValArray.IndexOf(latUpper);
                if (latEnd < 0)
                {
                    latEnd = -(latEnd + 1);
                    latEnd = Math.Max(0, latEnd - 1);
                }
            }

            if (longUpper == null)
            {
                longEnd = longDataCache.ValArray.Count - 1;
            }
            else
            {
                longEnd = longDataCache.ValArray.IndexOf(longUpper);
                if (longEnd < 0)
                {
                    longEnd = -(longEnd + 1);
                    longEnd = Math.Max(0, longEnd - 1);
                }
            }

            return new int[] { latStart, latEnd, longStart, longEnd };
        }
    }
}
