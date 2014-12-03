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
    using System.Text;
    using BoboBrowse.Net.Facets.Impl;

    [Serializable]
    public class BrowseSelection
    {
        public enum ValueOperation
        {
            ValueOperationOr,
            ValueOperationAnd
        }

        private readonly List<string> values;
        private readonly List<string> notValues;

        public BrowseSelection(string fieldName)
        {
            this.values = new List<string>();
            this.notValues = new List<string>();

            this.FieldName = fieldName;
            this.SelectionOperation = ValueOperation.ValueOperationOr;
            this.SelectionProperties = new Properties();
        }

        public Properties SelectionProperties { get; private set; }

        public string FieldName { get; private set; }

        public ValueOperation SelectionOperation { get; set; }

        public bool IsStrict
        {
            get { return Convert.ToBoolean(SelectionProperties.GetProperty(PathFacetHandler.SEL_PROP_NAME_STRICT)); }
            set { SelectionProperties.SetProperty(PathFacetHandler.SEL_PROP_NAME_STRICT, value.ToString()); }
        }

        ///<summary>the depth.  Used if the field is of type <b><i>path</i></b>. </summary>
        public int Depth
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

        public void SetSelectionProperty(string key, string val)
        {
            SelectionProperties.SetProperty(key, val);
        }

        public string[] Values
        {
            get { return values.ToArray(); }
            set
            {
                values.Clear();
                values.AddRange(value);
            }
        }

        public string[] NotValues
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
        public void AddValue(string val)
        {
            values.Add(val);
        }

        ///<summary>Add a select NOT value </summary>
        ///<param name="val"> select NOT value </param>
        public void AddNotValue(string val)
        {
            notValues.Add(val);
        }


        public override string ToString()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("name: ").Append(FieldName);
            buf.Append("values: " + values);
            buf.Append("nots: " + notValues);
            buf.Append("op: " + SelectionOperation);
            buf.Append("sel props: " + SelectionProperties);
            return buf.ToString();
        }
    }
}
