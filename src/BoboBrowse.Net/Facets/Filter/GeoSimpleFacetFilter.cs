// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// author nnarkhed
    /// </summary>
    public class GeoSimpleFacetFilter : RandomAccessFilter
    {
        private static long serialVersionUID = 1L;
	    private readonly FacetHandler<IFacetDataCache> _latFacetHandler;
	    private readonly FacetHandler<IFacetDataCache> _longFacetHandler;
	    private readonly string _latRangeString;
        private readonly string _longRangeString;

        public GeoSimpleFacetFilter(FacetHandler<IFacetDataCache> latHandler, FacetHandler<IFacetDataCache> longHandler, string latRangeString, string longRangeString)
        {
            _latFacetHandler = latHandler;
            _longFacetHandler = longHandler;
            _latRangeString = latRangeString;
            _longRangeString = longRangeString;
        }

        private sealed class GeoSimpleDocIdSetIterator : DocIdSetIterator
        {
            private int _doc = -1;
		    private int _totalFreq;
		    private int _minID = int.MaxValue;
		    private int _maxID = -1;
		    private readonly int _latStart;
		    private readonly int _latEnd;
		    private readonly int _longStart;
		    private readonly int _longEnd;
		    private readonly BigSegmentedArray _latOrderArray;
		    private readonly BigSegmentedArray _longOrderArray;

            internal GeoSimpleDocIdSetIterator(int latStart, int latEnd, int longStart, int longEnd, IFacetDataCache latDataCache, IFacetDataCache longDataCache)
            {
                _totalFreq = 0;
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
            IFacetDataCache latDataCache = _latFacetHandler.GetFacetData(reader);
		    IFacetDataCache longDataCache = _longFacetHandler.GetFacetData(reader);
		
		    int[] latRange = FacetRangeFilter.Parse(latDataCache, _latRangeString);
		    int[] longRange = FacetRangeFilter.Parse(longDataCache, _longRangeString);
		    if((latRange == null) || (longRange == null)) return null;


        }

        private class GeoSimpleRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly int _latStart;
            private readonly int _latEnd;
            private readonly int _longStart;
            private readonly int _longEnd;
            private readonly IFacetDataCache _latDataCache;
            private readonly IFacetDataCache _longDataCache;

            public GeoSimpleRandomAccessDocIdSet(int[] latRange, int[] longRange, IFacetDataCache latDataCache, IFacetDataCache longDataCache)
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
    }
}
