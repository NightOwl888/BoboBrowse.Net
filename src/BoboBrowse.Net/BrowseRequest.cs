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

namespace BoboBrowse.Net
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Lucene.Net.Search;

    [Serializable]
    public class BrowseRequest
    {
        private readonly Dictionary<string, BrowseSelection> selections;
        private readonly List<SortField> sortFields;

        public Query Query { get; set; }

        public int Offset { get; set; }

        public int Count { get; set; }

        public bool FetchStoredFields { get; set; }

        public Dictionary<string, FacetSpec> FacetSpecs { get; set; }

        public Filter Filter { get; set; }

        public BrowseRequest()
        {
            selections = new Dictionary<string, BrowseSelection>();
            sortFields = new List<SortField>();
            FacetSpecs = new Dictionary<string, FacetSpec>();
            Filter = null;
            FetchStoredFields = false;
        }

        public virtual IEnumerable<string> GetSelectionNames()
        {
            return selections.Keys;
        }

        public virtual void RemoveSelection(string name)
        {
            selections.Remove(name);
        }

        public int SelectionCount
        {
            get { return selections.Count; }
        }

        public virtual void ClearSelections()
        {
            selections.Clear();
        }

        ///<summary>Gets the number of facet specs </summary>
        ///<returns> number of facet pecs </returns>
        ///<seealso cref= #setFacetSpec(String, FacetSpec) </seealso>
        ///<seealso cref= #getFacetSpec(String) </seealso>
        public int FacetSpecCount
        {
            get { return FacetSpecs.Count; }
        }

        public virtual void ClearSort()
        {
            sortFields.Clear();
        }

        public virtual void SetFacetSpec(string name, FacetSpec facetSpec)
        {
            FacetSpecs.Add(name, facetSpec);
        }

        public virtual FacetSpec GetFacetSpec(string name)
        {
            FacetSpec result;
            FacetSpecs.TryGetValue(name, out result);
            return result;
        }

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
            selections.Add(sel.FieldName, sel);
        }

        ///<summary>Gets all added browse selections </summary>
        ///<returns> added selections </returns>
        ///<seealso cref= #addSelection(BrowseSelection) </seealso>
        public virtual BrowseSelection[] GetSelections()
        {
            return selections.Values.ToArray();
        }

        ///<summary> Gets selection by field name </summary>
        ///<param name="fieldname"> </param>
        ///<returns> selection on the field </returns>
        public virtual BrowseSelection GetSelection(string fieldname)
        {
            BrowseSelection result;
            selections.TryGetValue(fieldname, out result);
            return result;
        }

        public virtual Dictionary<string, BrowseSelection> GetAllSelections()
        {
            return selections;
        }

        /*public virtual void putAllSelections(Dictionary<string, BrowseSelection> map)
        {
            //FIXME: There is no .NET Dictionary equivalent to the Java 'putAll' method:
            _selections.putAll(map);
        }*/

        ///	 <summary> * Add a sort spec </summary>
        ///	 * <param name="sortSpec"> sort spec </param>
        ///	 * <seealso cref= #getSort()  </seealso>
        ///	 * <seealso cref= #setSort(SortField[]) </seealso>
        public virtual void AddSortField(SortField sortSpec)
        {
            sortFields.Add(sortSpec);
        }

        public SortField[] Sort
        {
            get
            {
                return sortFields.ToArray();
            }
            set
            {
                sortFields.Clear();
                for (int i = 0; i < value.Length; ++i)
                {
                    sortFields.Add(value[i]);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("query: ").Append(Query).Append('\n');
            buf.Append("page: [").Append(Offset).Append(',').Append(Count).Append("]\n");
            buf.Append("sort spec: ").Append(sortFields).Append('\n');
            buf.Append("selections: ").Append(selections).Append('\n');
            buf.Append("facet spec: ").Append(FacetSpecs).Append('\n');
            buf.Append("fetch stored fields: ").Append(FetchStoredFields);
            return buf.ToString();
        }
    }
}
