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

namespace BoboBrowse.Net.Search
{
    using System;
    using System.Collections.Generic;    
    using System.Text;
    using Lucene.Net.Documents;

    ///<summary>A hit from a browse </summary>
    [Serializable]
    public class BrowseHit
    {
        public float Score { get; set; }

        public int DocId { get; set; }

        public Dictionary<string, string[]> FieldValues { get; set; }

        public Document StoredFields { get; set; }

        private readonly Dictionary<string, IComparable> comparableMap = new Dictionary<string, IComparable>();

        ///<summary>Get the field values </summary>
        ///<param name="field"> field name </param>
        ///<returns> field value array </returns>
        ///<seealso cref= #getField(String) </seealso>
        public virtual string[] GetFields(string field)
        {
            return FieldValues != null ? FieldValues[field] : null;
        }

        ///<summary>Get the field value </summary>
        ///<param name="field"> field name </param>
        ///<returns> field value </returns>
        ///<seealso cref= #getFields(String) </seealso>
        public virtual string GetField(string field)
        {
            string[] fields = GetFields(field);
            if (fields != null && fields.Length > 0)
            {
                return fields[0];
            }
            else
            {
                return null;
            }
        }

        public virtual void AddComparable(string field, IComparable comparable)
        {
            comparableMap.Add(field, comparable);
        }

        public virtual IComparable GetComparable(string field)
        {
            return comparableMap[field];
        }

        public string ToString(Dictionary<string, string[]> map)
        {
            StringBuilder buffer = new StringBuilder();
            foreach (KeyValuePair<string, string[]> e in map)
            {
                buffer.Append(e.Key);
                buffer.Append(":");
                buffer.Append(string.Join(", ", e.Value));
            }
            return buffer.ToString();
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("docid: ").Append(DocId);
            buffer.Append(" score: ").Append(Score).Append('\n');
            buffer.Append(" field values: ").Append(ToString(FieldValues)).Append('\n');
            return buffer.ToString();
        }
    }
}
