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

﻿// Version compatibility level: 3.2.0
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.MapRed;
    using BoboBrowse.Net.Support;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    
    /// <summary>
    /// Browse Request. A set of BrowseSelections, a keyword text query, and a set of FacetSpecs.
    /// author jwang
    /// </summary>
    [Serializable]
    public class BrowseRequest
    {
        //private static long serialVersionUID = 3172092238778154933L; // NOT USED

        /// <summary>
        /// The transaction ID
        /// </summary>
        private long tid = -1;

        /// <summary>
        /// Gets or sets the transaction ID.
        /// </summary>
        public long Tid
        {
            get { return tid; }
            set { tid = value; }
        }

        // Fields
        private readonly Dictionary<string, BrowseSelection> _selections;
        private readonly List<SortField> _sortSpecs;

        /// <summary>
        /// Gets or sets a list of term vectors to fetch from the Lucene.Net index. The values are populated in the <see cref="P:BrowseHit.TermFreqMap"/>.
        /// A term vector is a list of the document's terms and their number of occurrences in that document.
        /// </summary>
        public virtual IEnumerable<string> TermVectorsToFetch { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether to set a <see cref="T:Lucene.Net.Search.Explanation"/> to the <see cref="P:BrowseHit.Explanation"/> property.
        /// An <see cref="T:Lucene.Net.Search.Explanation"/> describes the score computation for document and query.
        /// </summary>
        public virtual bool ShowExplanation { get; set; }

        /// <summary>
        /// Gets a list of the names of the current selections.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> GetSelectionNames()
        {
            return _selections.Keys;
        }

        /// <summary>
        /// Removes a selection by name.
        /// </summary>
        /// <param name="name"></param>
        public virtual void RemoveSelection(string name)
        {
            _selections.Remove(name);
        }

        /// <summary>
        /// A dictionary of named FacetSpec instances.
        /// <see cref="T:FacetSpec"/> specifies how facets are to be returned on the <see cref="T:BrowseResult"/>.
        /// </summary>
        public virtual IDictionary<string, FacetSpec> FacetSpecs { get; set; }

        /// <summary>
        /// Gets or sets the map between <b>RuntimeFacetHandler</b> names and their corresponding initialization data.
        /// </summary>
        public virtual IDictionary<string, FacetHandlerInitializerParam> FacetHandlerDataMap { get; set; }

        /// <summary>
        /// Gets the number of selections in the current request.
        /// </summary>
        public virtual int SelectionCount
        {
            get { return _selections.Count; }
        }

        /// <summary>
        /// Gets or sets the default filter.
        /// </summary>
        public virtual Filter Filter { get; set; }

        public virtual void ClearSelections()
        {
            _selections.Clear();
        }

        /// <summary>
        /// Gets the number of facet specs.
        /// </summary>
        /// <seealso cref="M:SetFacetSpec"/>
        /// <seealso cref="M:GetFacetSpec"/>
        public virtual int FacetSpecCount
        {
            get { return FacetSpecs.Count; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:BrowseRequest"/> class.
        /// </summary>
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

        /// <summary>
        /// Clears the list of <see cref="T:Lucene.Net.Search.SortField"/> instances for the <see cref="P:BrowseResult.Hits"/>.
        /// </summary>
        public virtual void ClearSort()
        {
            _sortSpecs.Clear();
        }

        /// <summary>
        /// Gets or sets a flag indicating whether to return a reference to the Lucene.Net Document
        /// object in the <see cref="P:BrowseHit.StoredFields"/> property.
        /// </summary>
        public virtual bool FetchStoredFields { get; set; }

        public virtual string[] GroupBy { get; set; }

        /// <summary>
        /// This setting does nothing. Left in place for parity with the Java version, which also has this field that does nothing.
        /// </summary>
        public virtual int MaxPerGroup { get; set; }

        /// <summary>
        /// This setting does nothing. The Java version had some kind of caching mechanism that was triggered by this setting that was
        /// not implemented in this version.
        /// </summary>
        [Obsolete("CollectDocIdCache not implemented.")]
        public virtual bool CollectDocIdCache { get; set; }

        /// <summary>
        /// Sets a <see cref="T:FacetSpec"/> and its related field name.
        /// <see cref="T:FacetSpec"/> specifies how facets are to be returned on the <see cref="T:BrowseResult"/>.
        /// </summary>
        /// <param name="name">field name</param>
        /// <param name="facetSpec">Facet spec</param>
        public virtual void SetFacetSpec(string name, FacetSpec facetSpec)
        {
            FacetSpecs.Add(name, facetSpec);
        }

        /// <summary>
        /// Gets a <see cref="T:FacetSpec"/> by field name.
        /// <see cref="T:FacetSpec"/> specifies how facets are to be returned on the <see cref="T:BrowseResult"/>.
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
        /// Similar to the Take() method in a LINQ query.
        /// </summary>
        public virtual int Count { get; set; }

        /// <summary>
        /// Gets or sets of the offset. Part of the paging parameters.
        /// Similar to the Skip() method in a LINQ query, but is 0-based instead of 1-based.
        /// </summary>
        public virtual int Offset { get; set; }

        /// <summary>
        /// Gets or sets the search query
        /// </summary>
        public virtual Lucene.Net.Search.Query Query { get; set; }

        /// <summary>
        /// Adds a browse selection. This typically corresponds to the selections a user would make on the user interface.
        /// </summary>
        /// <param name="sel">selection</param>
        /// <seealso cref="M:GetSelections"/>
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

        /// <summary>
        /// Gets all added browse selections.
        /// </summary>
        /// <returns>added selections</returns>
        /// <seealso cref="M:AddSelections"/>
        public virtual BrowseSelection[] GetSelections()
        {
            return _selections.Values.ToArray();
        }

        /// <summary>
        /// Gets a selection by field name.
        /// </summary>
        /// <param name="fieldname">The field name.</param>
        /// <returns>The selection on the field.</returns>
        public virtual BrowseSelection GetSelection(string fieldname)
        {
            return _selections.Get(fieldname);
        }

        /// <summary>
        /// Gets a dictionary of all current selections.
        /// </summary>
        /// <returns></returns>
        public virtual IDictionary<string, BrowseSelection> GetAllSelections()
        {
            return _selections;
        }

        /// <summary>
        /// Adds all of the selection entries to the current dictionary of selections.
        /// </summary>
        /// <param name="map">A dictionary of field name to <see cref="T:BrowseSelection"/> pairs.</param>
        public virtual void PutAllSelections(IDictionary<string, BrowseSelection> map)
        {
            _selections.PutAll(map);
        }

        public virtual IBoboMapFunctionWrapper MapReduceWrapper { get; set; }

        /// <summary>
        /// Add a sort specification for the <see cref="P:BrowseResult.Hits"/>.
        /// </summary>
        /// <param name="sortSpec">sort specification</param>
        public virtual void AddSortField(SortField sortSpec)
        {
            _sortSpecs.Add(sortSpec);
        }

        /// <summary>
        /// Gets or sets the sort criteria for the <see cref="P:BrowseResult.Hits"/>.
        /// </summary>
        public virtual SortField[] Sort
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

        /// <summary>
        /// Gets a string representation of the current request.
        /// </summary>
        /// <returns></returns>
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
