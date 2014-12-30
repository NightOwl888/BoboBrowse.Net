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
    using System;
    using System.Collections.Generic;
    using System.Text;

    ///<summary>specifies how facets are to be returned for a browse</summary>
    [Serializable]
    public class FacetSpec : ICloneable
    {
        //private static long serialVersionUID = 1L; // NOT USED

        ///<summary>Sort options for facets </summary>
        public enum FacetSortSpec
        {
            ///<summary>Order by the facet values in lexicographical ascending order </summary>
            OrderValueAsc,
            ///<summary>Order by the facet hit counts in descending order </summary>
            OrderHitsDesc,
            ///<summary>custom order, must have a comparator </summary>
            OrderByCustom
        }       

        /// <summary>
        /// Constructor.
        /// </summary>
        public FacetSpec()
        {
            OrderBy = FacetSortSpec.OrderValueAsc;
            MinHitCount = 1;
            ExpandSelection = false;
            CustomComparatorFactory = null;
            Properties = new Dictionary<string, string>();
        }

        public virtual IComparatorFactory CustomComparatorFactory { get; set; }

        ///<summary>Gets or sets the minimum number of hits a choice would need to have to be returned. </summary>
        public virtual int MinHitCount { get; set; }

        ///<summary>Gets or sets the current choice sort order</summary>
        public virtual FacetSortSpec OrderBy { get; set; }

        ///<summary>Gets or sets the maximum number of choices to return. Default = 0 which means all </summary>
        public virtual int MaxCount { get; set; }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("orderBy: ").Append(OrderBy).Append("\n");
            buffer.Append("max count: ").Append(MaxCount).Append("\n");
            buffer.Append("min hit count: ").Append(MinHitCount).Append("\n");
            buffer.Append("expandSelection: ").Append(ExpandSelection);
            return buffer.ToString();
        }

        ///<summary>Gets or sets whether we are expanding sibling choices</summary>
        public virtual bool ExpandSelection { get; set; }

        /// <summary>
        /// Gets or sets custom properties for the facet search. For example AttributeFacetHandler uses this to perform custom facet filtering
        /// </summary>
        public virtual IDictionary<string, string> Properties { get; set; }

        public virtual object Clone()
        {
            var properties = this.Properties;
            var clonedProperties = new Dictionary<string, string>(properties);

            return new FacetSpec()
            {
                CustomComparatorFactory = this.CustomComparatorFactory,
                ExpandSelection = this.ExpandSelection,
                MaxCount = this.MaxCount,
                MinHitCount = this.MinHitCount,
                OrderBy = this.OrderBy,
                Properties = clonedProperties
            };
        }
    }
}
