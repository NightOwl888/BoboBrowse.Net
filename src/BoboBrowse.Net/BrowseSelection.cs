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
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Text;

    [Serializable]
    public class BrowseSelection
    {
        public enum ValueOperation
        {
            ValueOperationOr,
            ValueOperationAnd
        }

        private static long serialVersionUID = 1L;

        private readonly List<string> values;
        private readonly List<string> notValues;

        public virtual Properties SelectionProperties { get; private set; }

        public virtual void SetSelectionProperty(string key, string val)
        {
            SelectionProperties.SetProperty(key, val);
        }

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
            get { return Convert.ToBoolean(SelectionProperties.GetProperty(PathFacetHandler.SEL_PROP_NAME_STRICT)); }
            set { SelectionProperties.SetProperty(PathFacetHandler.SEL_PROP_NAME_STRICT, value.ToString()); }
        }

        /// <summary>
        /// the depth.  Used if the field is of type <b><i>path</i></b>. 
        /// </summary>
        public virtual int Depth
        {
            get
            {
                int value;
                return
                    int.TryParse(SelectionProperties.GetProperty(PathFacetHandler.SEL_PROP_NAME_DEPTH), out value)
                    ? value
                    : 0;
            }
            set
            {
                SelectionProperties.SetProperty(PathFacetHandler.SEL_PROP_NAME_DEPTH, value.ToString());
            }
        }

        /// <summary>
        /// Gets or sets the field name
        /// </summary>
        public virtual string FieldName { get; private set; }

        /// <summary>
        /// Gets or sets the selected values
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
        /// Gets or sets the selected NOT values
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

        ///<summary>Add a select value </summary>
        ///<param name="val"> select value </param>
        public virtual void AddValue(string val)
        {
            values.Add(val);
        }

        ///<summary>Add a select NOT value </summary>
        ///<param name="val"> select NOT value </param>
        public virtual void AddNotValue(string val)
        {
            notValues.Add(val);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fieldName">field name</param>
        public BrowseSelection(string fieldName)
        {
            this.FieldName = fieldName;
            this.values = new List<string>();
            this.notValues = new List<string>();
            this.SelectionOperation = ValueOperation.ValueOperationOr;
            this.SelectionProperties = new Properties();
        }

        /// <summary>
        /// Gets or sets value operation.
        /// </summary>
        public virtual ValueOperation SelectionOperation { get; set; }

        public override string ToString()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("name: ").Append(FieldName);
            buf.Append("values: " + string.Join(",", values));
            buf.Append("nots: " + string.Join(",", notValues));
            buf.Append("op: " + SelectionOperation.ToString());
            buf.Append("sel props: " + SelectionProperties.ToString());
            return buf.ToString();
        }
    }
}
