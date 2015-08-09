//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
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
namespace BoboBrowse.Net
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Specifies how facets are to be returned for a browse, e.g. top 10 facets of car types ordered by count with a min count of 5.
    /// </summary>
    [Serializable]
    public class FacetSpec : ICloneable
    {
        //private static long serialVersionUID = 1L; // NOT USED

        ///<summary>Sort options for facets.</summary>
        public enum FacetSortSpec
        {
            ///<summary>Order by the facet values in lexicographical ascending order.</summary>
            OrderValueAsc,
            ///<summary>Order by the facet hit counts in descending order.</summary>
            OrderHitsDesc,
            ///<summary>Custom order, must have a comparator.</summary>
            OrderByCustom
        }       

        /// <summary>
        /// Initializes a new instance of the <see cref="T:FacetSpec"/> class.
        /// </summary>
        public FacetSpec()
        {
            OrderBy = FacetSortSpec.OrderValueAsc;
            MinHitCount = 1;
            ExpandSelection = false;
            CustomComparatorFactory = null;
            Properties = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets a custom comparator factory instance. This is required when specifying 
        /// <see cref="T:FacetSortSpec.OrderByCustom"/> for the <see cref="P:OrderBy"/> property.
        /// </summary>
        public virtual IComparatorFactory CustomComparatorFactory { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of hits a choice would need to have to be returned.
        /// </summary>
        public virtual int MinHitCount { get; set; }

        /// <summary>
        /// Gets or sets the current choice sort order.
        /// </summary>
        public virtual FacetSortSpec OrderBy { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of choices to return. Default = 0 which means all.
        /// </summary>
        public virtual int MaxCount { get; set; }

        /// <summary>
        /// Gets a string representation of the current <see cref="T:FacetSpec"/> instance.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("orderBy: ").Append(OrderBy).Append("\n");
            buffer.Append("max count: ").Append(MaxCount).Append("\n");
            buffer.Append("min hit count: ").Append(MinHitCount).Append("\n");
            buffer.Append("expandSelection: ").Append(ExpandSelection);
            return buffer.ToString();
        }

        ///<summary>Gets or sets whether we are expanding sibling choices.</summary>
        public virtual bool ExpandSelection { get; set; }

        /// <summary>
        /// Gets or sets custom properties for the facet search. For example, 
        /// <see cref="T:BoboBrowse.Net.Facets.Attribute.AttributeFacetHandler"/> 
        /// uses this to perform custom facet filtering.
        /// </summary>
        public virtual IDictionary<string, string> Properties { get; set; }

        /// <summary>
        /// Creates a clone of the current FacetSpec including all properties.
        /// </summary>
        /// <returns>The cloned instance.</returns>
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
