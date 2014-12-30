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

﻿// Version compatibility level: 3.1.0
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.MapRed;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Text;
    
    [Serializable]
    public class BrowseResult : IDisposable
    {
        //private static long serialVersionUID = -8620935391852879446L; // NOT USED

        /// <summary>
        /// The transaction ID
        /// </summary>
        private long tid = -1;

        /// <summary>
        /// Get or sets the transaction ID.
        /// </summary>
        public long Tid
        {
            get { return tid; }
            set { tid = value; }
        }

        [NonSerialized]
        private SortCollector _sortCollector;
        //private int totalGroups;
	    private IDictionary<string, IFacetAccessible> _facetMap;
	    private BrowseHit[] hits;
        private IList<string> errors = new List<string>();
	    private static BrowseHit[] NO_HITS = new BrowseHit[0];

        /// <summary>
        /// Constructor
        /// </summary>
        public BrowseResult()
        {
            _facetMap = new Dictionary<string, IFacetAccessible>();
        }

        /// <summary>
        /// Gets or sets the group accessible.
        /// </summary>
        public virtual IFacetAccessible[] GroupAccessibles { get; set; }

        /// <summary>
        /// Get or sets the sort collector.
        /// </summary>
        public virtual SortCollector SortCollector 
        {
            get { return _sortCollector; }
            set { _sortCollector = value; }
        }

        /// <summary>
        /// Get the facets by name
        /// </summary>
        /// <param name="name">name</param>
        /// <returns>IFacetAccessible instance corresponding to the name</returns>
        public virtual IFacetAccessible GetFacetAccessor(string name)
        {
            return _facetMap.Get(name);
        }

        /// <summary>
        /// Gets or sets the hit count
        /// </summary>
        public virtual int NumHits { get; set; }

        /// <summary>
        /// Gets or sets the group count
        /// </summary>
        public virtual int NumGroups { get; set; }

        /// <summary>
        /// Gets or sets the total number of docs in the index
        /// </summary>
        public virtual int TotalDocs { get; set; }

        ///<summary>Add a container full of choices </summary>
        ///<param name="facets"> container full of facets </param>
        public virtual void AddFacets(string name, IFacetAccessible facets)
        {
            _facetMap.Put(name, facets);
        }

        ///<summary>Add all of the given FacetAccessible to this BrowseResult </summary>
        ///<param name="facets"> map of facets to add to the result set </param>
        public virtual void AddAll(IDictionary<string, IFacetAccessible> facets)
        {
            _facetMap.PutAll(facets);
        }

        /// <summary>
        /// Gets or sets the hits
        /// </summary>
        public BrowseHit[] Hits
        {
            get { return hits == null ? NO_HITS : hits; }
            set { hits = value; }
        }

        /// <summary>
        /// Gets or sets the Search Time in milliseconds
        /// </summary>
        public long Time { get; set; }


        ///<summary>Gets all the facet collections </summary>
        public IDictionary<string, IFacetAccessible> FacetMap
        {
            get { return _facetMap; }
        }

        public MapReduceResult MapReduceResult { get; set; }

        public static string ToString(IDictionary<string, IFacetAccessible> map)
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
            buffer.Append("}").AppendLine();
            return buffer.ToString();
        }

        public override string ToString()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("hit count: ").Append(NumHits).AppendLine();
            buf.Append("total docs: ").Append(TotalDocs).AppendLine();
            buf.Append("facets: ").Append(ToString(this.FacetMap));
            buf.Append("hits: ").Append(Arrays.ToString(hits));
            return buf.ToString();
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (GroupAccessibles != null)
                {
                    foreach (var accessible in this.GroupAccessibles)
                    {
                        if (accessible != null)
                            accessible.Dispose();
                    }
                }
                if (this.SortCollector != null)
                    this.SortCollector.Dispose();
                if (this.FacetMap == null) return;
                foreach (var fa in this.FacetMap.Values)
                {
                    fa.Dispose();
                }
            }
        }

        public virtual void AddError(string message)
        {
            errors.Add(message);
        }

        public virtual IEnumerable<string> BoboErrors
        {
            get { return errors; }
        }
    }
}
