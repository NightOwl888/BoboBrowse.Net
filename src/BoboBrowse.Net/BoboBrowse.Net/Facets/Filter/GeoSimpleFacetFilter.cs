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
            private int m_doc = -1;
		    private int m_minID = int.MaxValue;
		    private int m_maxID = -1;
		    private readonly int m_latStart;
		    private readonly int m_latEnd;
		    private readonly int m_longStart;
		    private readonly int m_longEnd;
		    private readonly BigSegmentedArray m_latOrderArray;

            internal GeoSimpleDocIdSetIterator(int latStart, int latEnd, int longStart, int longEnd, FacetDataCache latDataCache, FacetDataCache longDataCache)
            {
                m_latStart = latStart;
                m_longStart = longStart;
                m_latEnd = latEnd;
                m_longEnd = longEnd;
                for (int i = latStart; i <= latEnd; ++i)
                {
                    m_minID = Math.Min(m_minID, latDataCache.MinIDs[i]);
                    m_maxID = Math.Max(m_maxID, latDataCache.MaxIDs[i]);
                }
                for (int i = longStart; i <= longEnd; ++i)
                {
                    m_minID = Math.Min(m_minID, longDataCache.MinIDs[i]);
                    m_maxID = Math.Max(m_maxID, longDataCache.MaxIDs[i]);
                }
                m_doc = Math.Max(-1, m_minID - 1);
                m_latOrderArray = latDataCache.OrderArray;
            }

            public override int DocID
            {
                get { return m_doc; }
            }

            public override int NextDoc()
            {
                int latIndex;
                int longIndex;
                while (m_doc < m_maxID)
                {	//not yet reached end
                    latIndex = m_latOrderArray.Get(++m_doc);
                    longIndex = m_latOrderArray.Get(m_doc);
                    if ((latIndex >= m_latStart && latIndex <= m_latEnd) && (longIndex >= m_longStart && longIndex <= m_longEnd))
                        return m_doc;
                }
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override int Advance(int id)
            {
                if (m_doc < id)
                {
                    m_doc = id - 1;
                }
                int latIndex;
                int longIndex;
                while (m_doc < m_maxID)
                {	//not yet reached end
                    latIndex = m_latOrderArray.Get(++m_doc);
                    longIndex = m_latOrderArray.Get(m_doc);
                    if ((latIndex >= m_latStart && latIndex <= m_latEnd) && (longIndex >= m_longStart && longIndex <= m_longEnd))
                        return m_doc;
                }
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override long GetCost()
            {
                return 0;
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboSegmentReader reader)
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
            private readonly int m_latStart;
            private readonly int m_latEnd;
            private readonly int m_longStart;
            private readonly int m_longEnd;
            private readonly FacetDataCache m_latDataCache;
            private readonly FacetDataCache m_longDataCache;

            public GeoSimpleRandomAccessDocIdSet(int[] latRange, int[] longRange, FacetDataCache latDataCache, FacetDataCache longDataCache)
            {
                m_latStart = latRange[0];
                m_latEnd = latRange[1];
                m_longStart = longRange[0];
                m_longEnd = longRange[1];
                m_latDataCache = latDataCache;
                m_longDataCache = longDataCache;
            }

            public override bool Get(int docid)
            {
                int latIndex = m_latDataCache.OrderArray.Get(docid);
                int longIndex = m_longDataCache.OrderArray.Get(docid);
                return latIndex >= m_latStart && latIndex <= m_latEnd && longIndex >= m_longStart && longIndex <= m_longEnd;
            }

            public override DocIdSetIterator GetIterator()
            {
                return new GeoSimpleDocIdSetIterator(m_latStart, m_latEnd, m_longStart, m_longEnd, m_latDataCache, m_longDataCache);
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
