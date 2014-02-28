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
    using System.Text;

    ///<summary>specifies how facets are to be returned for a browse</summary>
    [Serializable]
    public class FacetSpec
    {
        ///<summary>Sort options for facets </summary>
        public enum FacetSortSpec
        {
            ///<summary>Order by the facet values in lexographical ascending order </summary>
            OrderValueAsc,
            ///<summary>Order by the facet hit counts in descending order </summary>
            OrderHitsDesc,
            ///<summary>custom order, must have a comparator </summary>
            OrderByCustom
        }       

        public FacetSpec()
        {
            OrderBy = FacetSortSpec.OrderValueAsc;
            MinHitCount = 1;
            ExpandSelection = false;
            CustomComparatorFactory = null;
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("orderBy: ").Append(OrderBy).Append("\n");
            buffer.Append("max count: ").Append(MaxCount).Append("\n");
            buffer.Append("min hit count: ").Append(MinHitCount).Append("\n");
            buffer.Append("expandSelection: ").Append(ExpandSelection);
            return buffer.ToString();
        }

        ///<summary>The minimum number of hits a choice would need to have to be returned. </summary>
        public int MinHitCount { get; set; }

        ///<summary>The maximum number of choices to return. Default = 0 which means all </summary>
        public int MaxCount { get; set; }

        ///<summary>Whether we are expanding sibling choices </summary>
        public bool ExpandSelection { get; set; }

        ///<summary>Get the current choice sort order </summary>
        public FacetSortSpec OrderBy { get; set; }

        public string Prefix { get; set; }

        public IComparatorFactory CustomComparatorFactory { get; set; }
    }
}
