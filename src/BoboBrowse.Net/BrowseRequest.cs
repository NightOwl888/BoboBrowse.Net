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
// EXCEPTION: MapReduceResult
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    
    /// <summary>
    /// Browse Request.
    /// author jwang
    /// </summary>
    [Serializable]
    public class BrowseRequest
    {
        private static long serialVersionUID = 3172092238778154933L;

        /// <summary>
        /// The transaction ID
        /// </summary>
        private long tid = -1;

        /// <summary>
        /// Gets or sets the transaction ID.
        /// </summary>
        public sealed long Tid
        {
            get { return tid; }
            set { tid = value; }
        }

        // Fields
        private readonly Dictionary<string, BrowseSelection> _selections;
        private readonly List<SortField> _sortSpecs;


        public virtual IEnumerable<string> TermVectorsToFetch { get; set; }

        public virtual bool ShowExplanation { get; set; }

        public virtual IEnumerable<string> GetSelectionNames()
        {
            return _selections.Keys;
        }

        public virtual void RemoveSelection(string name)
        {
            _selections.Remove(name);
        }

        public virtual IDictionary<string, FacetSpec> FacetSpecs { get; set; }

        public virtual IDictionary<string, FacetHandlerInitializerParam> FacetHandlerDataMap { get; set; }

        public virtual int SelectionCount
        {
            get { return _selections.Count; }
        }

        /// <summary>
        /// Gets or sets the default filter
        /// </summary>
        public virtual Filter Filter { get; set; }

        public virtual void ClearSelections()
        {
            _selections.Clear();
        }

        ///<summary>Gets the number of facet specs </summary>
        ///<returns> number of facet pecs </returns>
        ///<seealso cref= #setFacetSpec(String, FacetSpec) </seealso>
        ///<seealso cref= #getFacetSpec(String) </seealso>
        public virtual int FacetSpecCount
        {
            get { return FacetSpecs.Count; }
        }

        public BrowseRequest()
        {
            _selections = new Dictionary<string, BrowseSelection>();
            _sortSpecs = new List<SortField>();
            this.FacetSpecs = new Dictionary<string, FacetSpec>();
            this.FacetHandlerDataMap = new Dictionary<string, FacetHandlerInitializerParam>();
            Filter = null;
            FetchStoredFields = false;
            GroupBy = null;
            MaxPerGroup = 0;
            CollectDocIdCache = false;
        }

        public virtual void ClearSort()
        {
            _sortSpecs.Clear();
        }

        public virtual bool FetchStoredFields { get; set; }

        public virtual string[] GroupBy { get; set; }

        public virtual int MaxPerGroup { get; set; }

        public virtual bool CollectDocIdCache { get; set; }

        /// <summary>
        /// Sets a facet spec
        /// </summary>
        /// <param name="name">field name</param>
        /// <param name="facetSpec">Facet spec</param>
        public virtual void SetFacetSpec(string name, FacetSpec facetSpec)
        {
            FacetSpecs.Add(name, facetSpec);
        }

        /// <summary>
        /// Gets a facet spec
        /// </summary>
        /// <param name="name">field name</param>
        /// <returns>facet spec</returns>
        public virtual FacetSpec GetFacetSpec(string name)
        {
            FacetSpec result;
            FacetSpecs.TryGetValue(name, out result);
            return result;
        }

        /// <summary>
        /// Sets a facet handler.
        /// </summary>
        /// <param name="name">the name of the <b>RuntimeFacetHandler</b>.</param>
        /// <param name="data">the data Bobo is to use to initialize the corresponding RuntimeFacetHandler.</param>
        public virtual void SetFacetHandlerData(string name, FacetHandlerInitializerParam data)
        {
            this.FacetHandlerDataMap.Put(name, data);
        }

        /// <summary>
        /// Gets a facet handler.
        /// </summary>
        /// <param name="name">the name of the <b>RuntimeFacetHandler</b>.</param>
        /// <returns>the data Bobo is to use to initialize the corresponding RuntimeFacetHandler.</returns>
        public virtual FacetHandlerInitializerParam GetFacetHandlerData(string name)
        {
            return this.FacetHandlerDataMap.Get(name);
        }

        /// <summary>
        /// Gets or sets the number of hits to return. Part of the paging parameters.
        /// </summary>
        public virtual int Count { get; set; }

        /// <summary>
        /// Gets or sets of the offset. Part of the paging parameters.
        /// </summary>
        public virtual int Offset { get; set; }

        /// <summary>
        /// Gets or sets the search query
        /// </summary>
        public Lucene.Net.Search.Query Query { get; set; }

        ///<summary>Adds a browse selection </summary>
        ///<param name="sel"> selection </param>
        ///<seealso cref= #getSelections() </seealso>
        public virtual void AddSelection(BrowseSelection sel)
        {
            string[] vals = sel.Values;
            if (vals == null || vals.Length == 0)
            {
                string[] notVals = sel.NotValues;
                if (notVals == null || notVals.Length == 0) // skip adding useless selections
                {
                    return;
                }
            }
            _selections.Put(sel.FieldName, sel);
        }

        ///<summary>Gets all added browse selections </summary>
        ///<returns> added selections </returns>
        ///<seealso cref= #addSelection(BrowseSelection) </seealso>
        public virtual BrowseSelection[] GetSelections()
        {
            return _selections.Values.ToArray();
        }

        ///<summary> Gets selection by field name </summary>
        ///<param name="fieldname"> </param>
        ///<returns> selection on the field </returns>
        public virtual BrowseSelection GetSelection(string fieldname)
        {
            return _selections.Get(fieldname);
        }

        public virtual IDictionary<string, BrowseSelection> GetAllSelections()
        {
            return _selections;
        }

        public virtual void PutAllSelections(IDictionary<string, BrowseSelection> map)
        {
            _selections.PutAll(map);
        }

        //// Not implemented, because there is no clear purpose
        //// and the Java implementation is unclear.
        //public BoboMapFunctionWrapper MapReduceWrapper { get; set; }

        ///	 <summary> Add a sort spec </summary>
        ///	 <param name="sortSpec"> sort spec </param>
        public virtual void AddSortField(SortField sortSpec)
        {
            _sortSpecs.Add(sortSpec);
        }

        /// <summary>
        /// Gets or sets the sort criteria
        /// </summary>
        public SortField[] Sort
        {
            get
            {
                return _sortSpecs.ToArray();
            }
            set
            {
                _sortSpecs.Clear();
                for (int i = 0; i < value.Length; ++i)
                {
                    _sortSpecs.Add(value[i]);
                }
            }
        }
       
        public override string ToString()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("query: ").Append(Query).Append('\n');
            buf.Append("page: [").Append(Offset).Append(',').Append(Count).Append("]\n");
            buf.Append("sort spec: ").Append(_sortSpecs).Append('\n');
            buf.Append("selections: ").Append(_selections).Append('\n');
            buf.Append("facet spec: ").Append(FacetSpecs).Append('\n');
            buf.Append("fetch stored fields: ").Append(FetchStoredFields);
            buf.Append("group by: ").Append(string.Join(",", GroupBy));
            return buf.ToString();
        }
    }
}
