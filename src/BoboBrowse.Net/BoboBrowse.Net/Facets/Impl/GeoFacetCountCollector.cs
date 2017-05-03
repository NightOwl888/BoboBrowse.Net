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
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Support;
    using System;
    using System.Collections.Generic;

    public class GeoFacetCountCollector : IFacetCountCollector
    {
        private readonly string m_name;
	    private readonly FacetSpec m_spec;
	    private readonly BigSegmentedArray m_count;
	    private readonly int m_countlength;
        private readonly TermStringList m_predefinedRanges;
	    private readonly GeoRange[] m_ranges;
	    private readonly BigFloatArray m_xvals;
	    private readonly BigFloatArray m_yvals;
	    private readonly BigFloatArray m_zvals;
        // variable to specify if the geo distance calculations are in miles. Default is miles
        private readonly bool m_miles;

        public class GeoRange
        {
            private readonly float m_lat;
		    private readonly float m_lon;
            private readonly float m_rad;

            public GeoRange(float lat, float lon, float radius)
            {
                m_lat = lat;
                m_lon = lon;
                m_rad = radius;
            }

            /// <summary>
            /// Gets the latitude value
            /// </summary>
            public virtual float Lat
            {
                get { return m_lat; }
            }

            /// <summary>
            /// Gets the longitude value
            /// </summary>
            public virtual float Lon
            {
                get { return m_lon; }
            }

            /// <summary>
            /// Gets the radius
            /// </summary>
            public virtual float Rad
            {
                get { return m_rad; }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">name of the Geo Facet</param>
        /// <param name="dataCache">The data cache for the Geo Facet</param>
        /// <param name="docBase">the base doc id</param>
        /// <param name="fspec">the facet spec for this facet</param>
        /// <param name="predefinedRanges">List of ranges, where each range looks like &lt;lat, lon: rad&gt;</param>
        /// <param name="miles">variable to specify if the geo distance calculations are in miles. False indicates distance calculation is in kilometers</param>
        public GeoFacetCountCollector(string name, GeoFacetHandler.GeoFacetData dataCache, int docBase, 
            FacetSpec fspec, IList<string> predefinedRanges, bool miles)
        {
            m_name = name;
            m_xvals = dataCache.xValArray;
            m_yvals = dataCache.yValArray;
            m_zvals = dataCache.zValArray;
            m_spec = fspec;
            m_predefinedRanges = new TermStringList();
            predefinedRanges.Sort();
            m_predefinedRanges.AddAll(predefinedRanges);
            m_countlength = predefinedRanges.Count;
            m_count = new LazyBigIntArray(m_countlength);
            m_ranges = new GeoRange[predefinedRanges.Count];
            int index = 0;
            foreach (string range in predefinedRanges)
            {
                m_ranges[index++] = Parse(range);
            }
            m_miles = miles;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="docid">The docid for which the facet counts are to be calculated</param>
        public virtual void Collect(int docid)
        {
            float docX = m_xvals.Get(docid);
            float docY = m_yvals.Get(docid);
            float docZ = m_zvals.Get(docid);

            float radius, targetX, targetY, targetZ, delta;
            float xu, xl, yu, yl, zu, zl;
            int countIndex = -1;
            foreach (GeoRange range in m_ranges)
            {
                // the countIndex for the count array should increment with the range index of the _ranges array
                countIndex++;
                if (m_miles)
                    radius = GeoMatchUtil.GetMilesRadiusCosine(range.Rad);
                else
                    radius = GeoMatchUtil.GetKMRadiusCosine(range.Rad);

                float[] coords = GeoMatchUtil.GeoMatchCoordsFromDegrees(range.Lat, range.Lon);
                targetX = coords[0];
                targetY = coords[1];
                targetZ = coords[2];

                if (m_miles)
                    delta = (float)(range.Rad / GeoMatchUtil.EARTH_RADIUS_MILES);
                else
                    delta = (float)(range.Rad / GeoMatchUtil.EARTH_RADIUS_KM);

                xu = targetX + delta;
                xl = targetX - delta;

                // try to see if the range checks can short circuit the actual inCircle check
                if (docX > xu || docX < xl) continue;

                yu = targetY + delta;
                yl = targetY - delta;

                if (docY > yu || docY < yl) continue;

                zu = targetZ + delta;
                zl = targetZ - delta;

                if (docZ > zu || docZ < zl) continue;

                if (GeoFacetFilter.InCircle(docX, docY, docZ, targetX, targetY, targetZ, radius))
                {
                    // if the lat, lon values of this docid match the current user-specified range, then increment the 
                    // appropriate count[] value
                    m_count.Add(countIndex, m_count.Get(countIndex) + 1);
                    // do not break here, since one document could lie in multiple user-specified ranges
                }
            }
        }

        public virtual void CollectAll()
        {
            throw new NotSupportedException("collectAll is not supported for Geo Facets yet");
        }

        public virtual BigSegmentedArray GetCountDistribution()
        {
            BigSegmentedArray dist = null;
            if (m_predefinedRanges != null)
            {
                dist = new LazyBigIntArray(m_predefinedRanges.Count);
                int distIdx = 0;
                for (int i = 0; i < m_count.Length; i++)
                {
                    int count = m_count.Get(i);
                    dist.Add(distIdx++, count);
                }
            }
            return dist;
        }

        public virtual string Name
        {
            get { return m_name; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value">This value should be one of the user-specified ranges for this Facet Count Collector. Else an
        /// ArgumentException will be raised</param>
        /// <returns>The BrowseFacet corresponding to the range value</returns>
        public virtual BrowseFacet GetFacet(string value)
        {
            if (m_predefinedRanges != null)
            {
                int index = 0;
                if ((index = m_predefinedRanges.IndexOf(value)) != -1)
                {
                    BrowseFacet choice = new BrowseFacet();
                    choice.FacetValueHitCount = m_count.Get(index);
                    choice.Value = value;
                    return choice;
                }
                else
                {
                    // user specified an unknown range value. the overhead to calculate the count for an unknown range value is high,
                    // in the sense it requires to go through each docid in the index. Till we get a better solution, this operation is
                    // unsupported
                    throw new ArgumentException("The value argument is not one of the user-specified ranges");
                }
            }
            else
            {
                throw new ArgumentException("There are no user-specified ranges for this Facet Count Collector object");
            }
        }

        public virtual int GetFacetHitsCount(object value)
        {
            if (m_predefinedRanges != null)
            {
                int index = 0;
                if ((index = m_predefinedRanges.IndexOf(value)) != -1)
                {
                    return m_count.Get(index);
                }
                else
                {
                    throw new ArgumentException("The value argument is not one of the user-specified ranges");
                }
            }
            else
            {
                throw new ArgumentException("There are no user-specified ranges for this Facet Count Collector object");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>A list containing BrowseFacet objects for each of the user-specified ranges</returns>
        public virtual ICollection<BrowseFacet> GetFacets()
        {
            if (m_spec != null)
            {
                int minHitCount = m_spec.MinHitCount;
                if (m_ranges != null)
                {
                    List<BrowseFacet> facets = new List<BrowseFacet>();
                    int countIndex = -1;
                    foreach (string value in m_predefinedRanges)
                    {
                        countIndex++;
                        if (m_count.Get(countIndex) >= minHitCount)
                        {
                            BrowseFacet choice = new BrowseFacet();
                            choice.FacetValueHitCount = m_count.Get(countIndex);
                            choice.Value = value;
                            facets.Add(choice);
                        }
                    }
                    return facets;
                }
                else
                {
                    return FacetCountCollector.EMPTY_FACET_LIST;
                }
            }
            else
            {
                return FacetCountCollector.EMPTY_FACET_LIST;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="range">Value should be of the format - lat , lon : radius</param>
        /// <returns>GeoRange object containing the lat, lon and radius value</returns>
        public static GeoRange Parse(string range)
        {
            string[] parts = range.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if ((parts == null) || (parts.Length != 2))
                throw new ArgumentException("Range value not in the expected format(lat, lon : radius)");
            string coord_part = parts[0];
            float rad = float.Parse(parts[1].Trim());

            string[] coords = coord_part.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if ((coords == null) || (coords.Length != 2))
                throw new ArgumentException("Range value not in the expected format(lat, lon : radius)");
            float lat = float.Parse(coords[0].Trim());
            float lon = float.Parse(coords[1].Trim());

            return new GeoRange(lat, lon, rad);
        }

        public virtual void Dispose()
        { }

        public virtual FacetIterator GetIterator()
        {
            return new DefaultFacetIterator(m_predefinedRanges, m_count, m_countlength, true);
        }
    }
}
