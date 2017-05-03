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
	    private readonly FacetHandler<GeoFacetHandler.GeoFacetData> m_handler;
	    private readonly float m_lat;
	    private readonly float m_lon;
        private readonly float m_rad;
        // variable to specify if the geo distance calculations are in miles. Default is miles
        private readonly bool m_miles;

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
            m_handler = facetHandler;
            m_lat = lat;
            m_lon = lon;
            m_rad = radius;
            m_miles = miles;
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboSegmentReader reader)
        {
            int maxDoc = reader.MaxDoc;

            GeoFacetHandler.GeoFacetData dataCache = m_handler.GetFacetData<GeoFacetHandler.GeoFacetData>(reader);
		    return new GeoDocIdSet(dataCache.xValArray, dataCache.yValArray, dataCache.zValArray,
				m_lat, m_lon, m_rad, maxDoc, m_miles);
        }

        private sealed class GeoDocIdSet : RandomAccessDocIdSet
        {
            private readonly BigFloatArray m_xvals;
		    private readonly BigFloatArray m_yvals;
		    private readonly BigFloatArray m_zvals;
		    private readonly float m_radius;
		    private readonly float m_targetX;
		    private readonly float m_targetY;
		    private readonly float m_targetZ;
            private readonly float m_delta;
		    private readonly int m_maxDoc;
	        // variable to specify if the geo distance calculations are in miles. Default is miles
	        private readonly bool m_miles;

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
                m_xvals = xvals;
                m_yvals = yvals;
                m_zvals = zvals;
                m_miles = miles;
                if (m_miles)
                    m_radius = GeoMatchUtil.GetMilesRadiusCosine(radius);
                else
                    m_radius = GeoMatchUtil.GetKMRadiusCosine(radius);
                float[] coords = GeoMatchUtil.GeoMatchCoordsFromDegrees(lat, lon);
                m_targetX = coords[0];
                m_targetY = coords[1];
                m_targetZ = coords[2];
                if (m_miles)
                    m_delta = (float)(radius / GeoMatchUtil.EARTH_RADIUS_MILES);
                else
                    m_delta = (float)(radius / GeoMatchUtil.EARTH_RADIUS_KM);
                m_maxDoc = maxdoc;
            }

            public override bool Get(int docId)
            {
                float docX = m_xvals.Get(docId);
                float docY = m_yvals.Get(docId);
                float docZ = m_zvals.Get(docId);

                return InCircle(docX, docY, docZ, m_targetX, m_targetY, m_targetZ, m_radius);
            }

            public override DocIdSetIterator GetIterator()
            {
                return new GeoDocIdSetIterator(m_xvals, m_yvals, m_zvals, m_targetX, m_targetY, m_targetZ, m_delta, m_radius, m_maxDoc);
            }
        }

        private class GeoDocIdSetIterator : DocIdSetIterator
        {
            private readonly BigFloatArray m_xvals;
            private readonly BigFloatArray m_yvals;
            private readonly BigFloatArray m_zvals;
            private readonly float m_radius;
            private readonly float m_targetX;
            private readonly float m_targetY;
            private readonly float m_targetZ;
            private readonly float m_delta;
            private readonly int m_maxDoc;
            private int m_doc;

            internal GeoDocIdSetIterator(BigFloatArray xvals, BigFloatArray yvals, BigFloatArray zvals, float targetX, float targetY, float targetZ,
                float delta, float radiusCosine, int maxdoc)
            {
                m_xvals = xvals;
                m_yvals = yvals;
                m_zvals = zvals;
                m_targetX = targetX;
                m_targetY = targetY;
                m_targetZ = targetZ;
                m_delta = delta;
                m_radius = radiusCosine;
                m_maxDoc = maxdoc;
                m_doc = -1;
            }

            public sealed override int DocID
            {
                get { return m_doc; }
            }

            public sealed override int NextDoc()
            {
                float x = m_targetX;
                float xu = x + m_delta;
                float xl = x - m_delta;
                float y = m_targetY;
                float yu = y + m_delta;
                float yl = y - m_delta;
                float z = m_targetZ;
                float zu = z + m_delta;
                float zl = z - m_delta;

                int docid = m_doc;
                while (++docid < m_maxDoc)
                {
                    float docX = m_xvals.Get(docid);
                    if (docX > xu || docX < xl) continue;

                    float docY = m_yvals.Get(docid);
                    if (docY > yu || docY < yl) continue;

                    float docZ = m_zvals.Get(docid);
                    if (docZ > zu || docZ < zl) continue;

                    if (GeoFacetFilter.InCircle(docX, docY, docZ, m_targetX, m_targetY, m_targetZ, m_radius))
                    {
                        m_doc = docid;
                        return m_doc;
                    }
                }
                m_doc = DocIdSetIterator.NO_MORE_DOCS;
                return m_doc;
            }

            public sealed override int Advance(int targetId)
            {
                if (m_doc < targetId)
                {
                    m_doc = targetId - 1;
                }

                float x = m_targetX;
                float xu = x + m_delta;
                float xl = x - m_delta;
                float y = m_targetY;
                float yu = y + m_delta;
                float yl = y - m_delta;
                float z = m_targetZ;
                float zu = z + m_delta;
                float zl = z - m_delta;

                int docid = m_doc;
                while (++docid < m_maxDoc)
                {
                    float docX = m_xvals.Get(docid);
                    if (docX > xu || docX < xl) continue;

                    float docY = m_yvals.Get(docid);
                    if (docY > yu || docY < yl) continue;

                    float docZ = m_zvals.Get(docid);
                    if (docZ > zu || docZ < zl) continue;

                    if (GeoFacetFilter.InCircle(docX, docY, docZ, m_targetX, m_targetY, m_targetZ, m_radius))
                    {
                        m_doc = docid;
                        return m_doc;
                    }
                }
                m_doc = DocIdSetIterator.NO_MORE_DOCS;
                return m_doc;
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
