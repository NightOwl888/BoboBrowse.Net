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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections.Generic;

    public class GeoFacetCountCollector : IFacetCountCollector
    {
        private readonly string _name;
	    private readonly FacetSpec _spec;
	    private BigSegmentedArray _count;
	    private int _countlength;
	    private GeoFacetHandler.GeoFacetData _dataCache;
        private readonly TermStringList _predefinedRanges;
	    private int _docBase;
	    private GeoRange[] _ranges;
	    private BigFloatArray _xvals;
	    private BigFloatArray _yvals;
	    private BigFloatArray _zvals;
        // variable to specify if the geo distance calculations are in miles. Default is miles
        private bool _miles;

        public class GeoRange
        {
            private readonly float _lat;
		    private readonly float _lon;
            private readonly float _rad;

            public GeoRange(float lat, float lon, float radius)
            {
                _lat = lat;
                _lon = lon;
                _rad = radius;
            }

            /// <summary>
            /// Gets the latitude value
            /// </summary>
            public virtual float Lat
            {
                get { return _lat; }
            }

            /// <summary>
            /// Gets the longitude value
            /// </summary>
            public virtual float Lon
            {
                get { return _lon; }
            }

            /// <summary>
            /// Gets the radius
            /// </summary>
            public virtual float Rad
            {
                get { return _rad; }
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
        public GeoFacetCountCollector(string name, GeoFacetHandler.GeoFacetData dataCache,
            int docBase, FacetSpec fspec, IEnumerable<string> predefinedRanges, bool miles)
        {
            _name = name;
            _dataCache = dataCache;
            _xvals = dataCache.xValArray;
            _yvals = dataCache.yValArray;
            _zvals = dataCache.zValArray;
            _spec = fspec;
            _predefinedRanges = new TermStringList();
            var predefinedTemp = new List<string>(predefinedRanges);
            predefinedTemp.Sort();
            _predefinedRanges.AddAll(predefinedTemp);
            _docBase = docBase;
            _countlength = predefinedTemp.Count;
            _count = new LazyBigIntArray(_countlength);
            _ranges = new GeoRange[predefinedTemp.Count];
            int index = 0;
            foreach (string range in predefinedTemp)
            {
                _ranges[index++] = Parse(range);
            }
            _miles = miles;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="docid">The docid for which the facet counts are to be calculated</param>
        public virtual void Collect(int docid)
        {
            float docX = _xvals.Get(docid);
            float docY = _yvals.Get(docid);
            float docZ = _zvals.Get(docid);

            float radius, targetX, targetY, targetZ, delta;
            float xu, xl, yu, yl, zu, zl;
            int countIndex = -1;
            foreach (GeoRange range in _ranges)
            {
                // the countIndex for the count array should increment with the range index of the _ranges array
                countIndex++;
                if (_miles)
                    radius = GeoMatchUtil.GetMilesRadiusCosine(range.Rad);
                else
                    radius = GeoMatchUtil.GetKMRadiusCosine(range.Rad);

                float[] coords = GeoMatchUtil.GeoMatchCoordsFromDegrees(range.Lat, range.Lon);
                targetX = coords[0];
                targetY = coords[1];
                targetZ = coords[2];

                if (_miles)
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
                    _count.Add(countIndex, _count.Get(countIndex) + 1);
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
            if (_predefinedRanges != null)
            {
                dist = new LazyBigIntArray(_predefinedRanges.Count);
                int distIdx = 0;
                for (int i = 0; i < _count.Size(); i++)
                {
                    int count = _count.Get(i);
                    dist.Add(distIdx++, count);
                }
            }
            return dist;
        }

        public virtual string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value">This value should be one of the user-specified ranges for this Facet Count Collector. Else an
        /// ArgumentException will be raised</param>
        /// <returns>The BrowseFacet corresponding to the range value</returns>
        public virtual BrowseFacet GetFacet(string value)
        {
            if (_predefinedRanges != null)
            {
                int index = 0;
                if ((index = _predefinedRanges.IndexOf(value)) != -1)
                {
                    BrowseFacet choice = new BrowseFacet();
                    choice.FacetValueHitCount = _count.Get(index);
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
            if (_predefinedRanges != null)
            {
                int index = 0;
                if ((index = _predefinedRanges.IndexOf(value)) != -1)
                {
                    return _count.Get(index);
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
        public virtual IEnumerable<BrowseFacet> GetFacets()
        {
            if (_spec != null)
            {
                int minHitCount = _spec.MinHitCount;
                if (_ranges != null)
                {
                    List<BrowseFacet> facets = new List<BrowseFacet>();
                    int countIndex = -1;
                    foreach (string value in _predefinedRanges)
                    {
                        countIndex++;
                        if (_count.Get(countIndex) >= minHitCount)
                        {
                            BrowseFacet choice = new BrowseFacet();
                            choice.FacetValueHitCount = _count.Get(countIndex);
                            choice.Value = value;
                            facets.Add(choice);
                        }
                    }
                    return facets;
                }
                else
                {
                    return FacetCountCollector_Fields.EMPTY_FACET_LIST;
                }
            }
            else
            {
                return FacetCountCollector_Fields.EMPTY_FACET_LIST;
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

        public virtual FacetIterator Iterator()
        {
            return new DefaultFacetIterator(_predefinedRanges, _count, _countlength, true);
        }
    }
}
