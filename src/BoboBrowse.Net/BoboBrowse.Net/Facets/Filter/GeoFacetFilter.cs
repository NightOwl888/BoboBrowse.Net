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
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;

    /// <summary>
    /// author nnarkhed
    /// </summary>
    public class GeoFacetFilter : RandomAccessFilter
    {
	    private readonly FacetHandler<GeoFacetHandler.GeoFacetData> _handler;
	    private readonly float _lat;
	    private readonly float _lon;
        private readonly float _rad;
        // variable to specify if the geo distance calculations are in miles. Default is miles
        private readonly bool _miles;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="facetHandler">The Geo Facet Handler for this instance</param>
        /// <param name="lat">latitude value of the user's point of interest</param>
        /// <param name="lon">longitude value of the user's point of interest</param>
        /// <param name="radius">Radius from the point of interest</param>
        /// <param name="miles">variable to specify if the geo distance calculations are in miles. False indicates distance calculation is in kilometers</param>
        public GeoFacetFilter(FacetHandler<GeoFacetHandler.GeoFacetData> facetHandler, float lat, float lon, float radius, bool miles)
        {
            _handler = facetHandler;
            _lat = lat;
            _lon = lon;
            _rad = radius;
            _miles = miles;
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboSegmentReader reader)
        {
            int maxDoc = reader.MaxDoc;

            GeoFacetHandler.GeoFacetData dataCache = _handler.GetFacetData<GeoFacetHandler.GeoFacetData>(reader);
		    return new GeoDocIdSet(dataCache.xValArray, dataCache.yValArray, dataCache.zValArray,
				_lat, _lon, _rad, maxDoc, _miles);
        }

        private sealed class GeoDocIdSet : RandomAccessDocIdSet
        {
            private readonly BigFloatArray _xvals;
		    private readonly BigFloatArray _yvals;
		    private readonly BigFloatArray _zvals;
		    private readonly float _radius;
		    private readonly float _targetX;
		    private readonly float _targetY;
		    private readonly float _targetZ;
            private readonly float _delta;
		    private readonly int _maxDoc;
	        // variable to specify if the geo distance calculations are in miles. Default is miles
	        private readonly bool _miles;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="xvals">array of x coordinate values for docid</param>
            /// <param name="yvals">array of y coordinate values for docid</param>
            /// <param name="zvals">array of z coordinate values for docid</param>
            /// <param name="lat">target latitude</param>
            /// <param name="lon">target longitude</param>
            /// <param name="radius">target radius</param>
            /// <param name="maxdoc">max doc in the docid set</param>
            /// <param name="miles">variable to specify if the geo distance calculations are in miles. 
            /// False indicates distance calculation is in kilometers</param>
            internal GeoDocIdSet(BigFloatArray xvals, BigFloatArray yvals, BigFloatArray zvals, float lat, float lon,
                float radius, int maxdoc, bool miles)
            {
                _xvals = xvals;
                _yvals = yvals;
                _zvals = zvals;
                _miles = miles;
                if (_miles)
                    _radius = GeoMatchUtil.GetMilesRadiusCosine(radius);
                else
                    _radius = GeoMatchUtil.GetKMRadiusCosine(radius);
                float[] coords = GeoMatchUtil.GeoMatchCoordsFromDegrees(lat, lon);
                _targetX = coords[0];
                _targetY = coords[1];
                _targetZ = coords[2];
                if (_miles)
                    _delta = (float)(radius / GeoMatchUtil.EARTH_RADIUS_MILES);
                else
                    _delta = (float)(radius / GeoMatchUtil.EARTH_RADIUS_KM);
                _maxDoc = maxdoc;
            }

            public override bool Get(int docId)
            {
                float docX = _xvals.Get(docId);
                float docY = _yvals.Get(docId);
                float docZ = _zvals.Get(docId);

                return InCircle(docX, docY, docZ, _targetX, _targetY, _targetZ, _radius);
            }

            public override DocIdSetIterator GetIterator()
            {
                return new GeoDocIdSetIterator(_xvals, _yvals, _zvals, _targetX, _targetY, _targetZ, _delta, _radius, _maxDoc);
            }
        }

        private class GeoDocIdSetIterator : DocIdSetIterator
        {
            private readonly BigFloatArray _xvals;
            private readonly BigFloatArray _yvals;
            private readonly BigFloatArray _zvals;
            private readonly float _radius;
            private readonly float _targetX;
            private readonly float _targetY;
            private readonly float _targetZ;
            private readonly float _delta;
            private readonly int _maxDoc;
            private int _doc;

            internal GeoDocIdSetIterator(BigFloatArray xvals, BigFloatArray yvals, BigFloatArray zvals, float targetX, float targetY, float targetZ,
                float delta, float radiusCosine, int maxdoc)
            {
                _xvals = xvals;
                _yvals = yvals;
                _zvals = zvals;
                _targetX = targetX;
                _targetY = targetY;
                _targetZ = targetZ;
                _delta = delta;
                _radius = radiusCosine;
                _maxDoc = maxdoc;
                _doc = -1;
            }

            public sealed override int DocID
            {
                get { return _doc; }
            }

            public sealed override int NextDoc()
            {
                float x = _targetX;
                float xu = x + _delta;
                float xl = x - _delta;
                float y = _targetY;
                float yu = y + _delta;
                float yl = y - _delta;
                float z = _targetZ;
                float zu = z + _delta;
                float zl = z - _delta;

                int docid = _doc;
                while (++docid < _maxDoc)
                {
                    float docX = _xvals.Get(docid);
                    if (docX > xu || docX < xl) continue;

                    float docY = _yvals.Get(docid);
                    if (docY > yu || docY < yl) continue;

                    float docZ = _zvals.Get(docid);
                    if (docZ > zu || docZ < zl) continue;

                    if (GeoFacetFilter.InCircle(docX, docY, docZ, _targetX, _targetY, _targetZ, _radius))
                    {
                        _doc = docid;
                        return _doc;
                    }
                }
                _doc = DocIdSetIterator.NO_MORE_DOCS;
                return _doc;
            }

            public sealed override int Advance(int targetId)
            {
                if (_doc < targetId)
                {
                    _doc = targetId - 1;
                }

                float x = _targetX;
                float xu = x + _delta;
                float xl = x - _delta;
                float y = _targetY;
                float yu = y + _delta;
                float yl = y - _delta;
                float z = _targetZ;
                float zu = z + _delta;
                float zl = z - _delta;

                int docid = _doc;
                while (++docid < _maxDoc)
                {
                    float docX = _xvals.Get(docid);
                    if (docX > xu || docX < xl) continue;

                    float docY = _yvals.Get(docid);
                    if (docY > yu || docY < yl) continue;

                    float docZ = _zvals.Get(docid);
                    if (docZ > zu || docZ < zl) continue;

                    if (GeoFacetFilter.InCircle(docX, docY, docZ, _targetX, _targetY, _targetZ, _radius))
                    {
                        _doc = docid;
                        return _doc;
                    }
                }
                _doc = DocIdSetIterator.NO_MORE_DOCS;
                return _doc;
            }

            public override long GetCost()
            {
                return 0;
            }
        }

        public static bool InCircle(float docX, float docY, float docZ, float targetX, float targetY, float targetZ, float radCosine)
        {
            if (docX == -1.0f && docY == -1.0f && docZ == -1.0f)
                return false;
            float dotProductCosine = (docX * targetX) + (docY * targetY) + (docZ * targetZ);
            return (radCosine <= dotProductCosine);
        }
    }
}
