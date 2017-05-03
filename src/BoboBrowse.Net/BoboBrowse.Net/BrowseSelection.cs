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

﻿// Version compatibility level: 4.0.2
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Support;
    using Lucene.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// A selection or filter to be applied, e.g. Color=Red.
    /// </summary>
    [Serializable]
    public class BrowseSelection
    {
        public enum ValueOperation
        {
            ValueOperationOr,
            ValueOperationAnd
        }

        //private static long serialVersionUID = 1L; // NOT USED

        private readonly List<string> values;
        private readonly List<string> notValues;

        /// <summary>
        /// A dictionary of custom properties that can be used by custom (or some built-in) 
        /// implementations of facet handlers, filters, and collectors.
        /// </summary>
        public virtual IDictionary<string, string> SelectionProperties { get; private set; }

        /// <summary>
        /// Sets a specific selection property with the given key and value.
        /// </summary>
        /// <param name="key">A key for the property.</param>
        /// <param name="val">The value for the property.</param>
        public virtual void SetSelectionProperty(string key, string val)
        {
            SelectionProperties.Put(key, val);
        }

        /// <summary>
        /// Sets a group of selection properties all at once. If the property already exists, 
        /// it will be overwritten; otherwise it will be added.
        /// </summary>
        /// <param name="props"></param>
        public virtual void SetSelectionProperties(IDictionary<string, string> props)
        {
            SelectionProperties.PutAll(props);
        }

        /// <summary>
        /// Gets if strict applied for counting. Used if the field is of type <b><i>path</i></b>.
        /// </summary>
        [Obsolete("use SelectionProperties")]
        public virtual bool IsStrict
        {
            get { return Convert.ToBoolean(SelectionProperties.Get(PathFacetHandler.SEL_PROP_NAME_STRICT)); }
            set { SelectionProperties.Put(PathFacetHandler.SEL_PROP_NAME_STRICT, value.ToString()); }
        }

        /// <summary>
        /// the depth.  Used if the field is of type <b><i>path</i></b>. 
        /// </summary>
        [Obsolete("use SelectionProperties")]
        public virtual int Depth
        {
            get
            {
                int value;
                return
                    int.TryParse(SelectionProperties.Get(PathFacetHandler.SEL_PROP_NAME_DEPTH), out value)
                    ? value
                    : 0;
            }
            set
            {
                SelectionProperties.Put(PathFacetHandler.SEL_PROP_NAME_DEPTH, value.ToString());
            }
        }

        /// <summary>
        /// Gets or sets the field name.
        /// </summary>
        public virtual string FieldName { get; private set; }

        /// <summary>
        /// Gets or sets the selected values.
        /// </summary>
        public virtual string[] Values
        {
            get { return values.ToArray(); }
            set
            {
                values.Clear();
                values.AddRange(value);
            }
        }

        /// <summary>
        /// Gets or sets the selected NOT values.
        /// </summary>
        public virtual string[] NotValues
        {
            get { return notValues.ToArray(); }
            set
            {
                notValues.Clear();
                notValues.AddRange(value);
            }
        }

        /// <summary>
        /// Adds a selection value.
        /// </summary>
        /// <param name="val">Value to select.</param>
        public virtual void AddValue(string val)
        {
            values.Add(val);
        }

        /// <summary>
        /// Adds a selection NOT value.
        /// </summary>
        /// <param name="val">Value to NOT select.</param>
        public virtual void AddNotValue(string val)
        {
            notValues.Add(val);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:BrowseSelection"/> class with the given field name.
        /// </summary>
        /// <param name="fieldName">A field name.</param>
        public BrowseSelection(string fieldName)
        {
            this.FieldName = fieldName;
            this.values = new List<string>();
            this.notValues = new List<string>();
            this.SelectionOperation = ValueOperation.ValueOperationOr;
            this.SelectionProperties = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets value operation.
        /// </summary>
        public virtual ValueOperation SelectionOperation { get; set; }

        /// <summary>
        /// Gets a string representation of <see cref="T:BrowseSelection"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("name: ").Append(FieldName).Append(" ");
            buf.Append("values: " + string.Join(",", values.ToArray())).Append(" ");
            buf.Append("nots: " + string.Join(",", notValues.ToArray())).Append(" ");
            buf.Append("op: " + SelectionOperation.ToString()).Append(" ");
            buf.Append("sel props: " + SelectionProperties.ToDisplayString());
            return buf.ToString();
        }
    }
}
