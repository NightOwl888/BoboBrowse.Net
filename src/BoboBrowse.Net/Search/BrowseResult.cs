//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Written in Java.
//* 
//* Copyright (C) 2005-2006  John Wang
//*
//* This library is free software; you can redistribute it and/or
//* modify it under the terms of the GNU Lesser General Public
//* License as published by the Free Software Foundation; either
//* version 2.1 of the License, or (at your option) any later version.
//*
//* This library is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

namespace BoboBrowse.Net.Search
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using BoboBrowse.Net.Utils;

    [Serializable]
    public class BrowseResult
    {
        private Dictionary<string, IFacetAccessible> facetMap;
        private BrowseHit[] hits;
        private static BrowseHit[] NO_HITS = new BrowseHit[0];

        public BrowseResult()
        {
            facetMap = new Dictionary<string, IFacetAccessible>();
        }

        public int NumHits { get; set; }
        public int TotalDocs { get; set; }
        ///<summary>Search Time in milliseconds </summary>
        public long Time { get; set; }
        public string Error { get; set; }

        public BrowseHit[] Hits
        {
            get { return hits == null ? NO_HITS : hits; }
            set { hits = value; }
        }

        ///<summary>Gets all the facet collections </summary>
        public Dictionary<string, IFacetAccessible> FacetMap
        {
            get { return facetMap; }
        }

        ///<summary>Get the facets by name </summary>
        ///<param name="name"> </param>
        ///<returns> FacetAccessible instance corresponding to the name </returns>
        public virtual IFacetAccessible GetFacetAccessor(string name)
        {
            return facetMap[name];
        }

        ///<summary>Add a container full of choices </summary>
        ///<param name="facets"> container full of facets </param>
        public virtual void AddFacets(string name, IFacetAccessible facets)
        {
            facetMap.Add(name, facets);
        }

        ///<summary>Add all of the given FacetAccessible to this BrowseResult </summary>
        ///<param name="facets"> map of facets to add to the result set </param>
        public virtual void AddAll(Dictionary<string, IFacetAccessible> facets)
        {
            foreach (KeyValuePair<string, IFacetAccessible> pair in facets)
            {
                facetMap.Add(pair.Key, pair.Value);
            }
        }

        public static string ToString(Dictionary<string, IFacetAccessible> map)
        {
            StringBuilder buffer = new StringBuilder();

            buffer.Append("{");
            foreach (KeyValuePair<string, IFacetAccessible> entry in map)
            {
                string name = entry.Key;
                IFacetAccessible facetAccessor = entry.Value;
                buffer.Append("name=").Append(name).Append(",");
                buffer.Append("facets=").Append(facetAccessor.GetFacets()).Append(";");
            }
            buffer.Append("}").Append('\n');
            return buffer.ToString();
        }

        public override string ToString()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("hit count: ").Append(NumHits).Append('\n');
            buf.Append("total docs: ").Append(TotalDocs).Append('\n');
            buf.Append("facets: ").Append(ToString(facetMap));
            buf.Append("hits: ").Append(Arrays.ToString(hits));
            return buf.ToString();
        }
    }
}
