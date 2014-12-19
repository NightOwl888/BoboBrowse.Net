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
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;
    using Lucene.Net.Documents;
    using System;
    using System.Collections.Generic;
    using System.Text;

    ///<summary>A hit from a browse </summary>
    [Serializable]
    public class BrowseHit
    {
        private static long serialVersionUID = 1L;

        [Serializable]
        public class TermFrequencyVector
        {
            private static long serialVersionUID = 1L;
            public readonly string[] terms;
            public readonly int[] freqs;

            public TermFrequencyVector(string[] terms, int[] freqs)
            {
                this.terms = terms;
                this.freqs = freqs;
            }
        }

        /// <summary>
        /// Gets or sets the score
        /// </summary>
        public float Score { get; set; }

        ///<summary>Get the field values </summary>
        ///<param name="field"> field name </param>
        ///<returns> field value array </returns>
        ///<seealso cref= #getField(string) </seealso>
        public virtual string[] GetFields(string field)
        {
            return this.FieldValues != null ? this.FieldValues[field] : null;
        }

        /// <summary>
        /// Get the raw field values
        /// </summary>
        /// <param name="field">field name</param>
        /// <returns>field value array</returns>
        public virtual object[] GetRawFields(string field)
        {
            return this.RawFieldValues != null ? this.RawFieldValues.Get(field) : null;
        }

        ///<summary>Get the field value </summary>
        ///<param name="field"> field name </param>
        ///<returns> field value </returns>
        ///<seealso cref= #getFields(string) </seealso>
        public virtual string GetField(string field)
        {
            string[] fields = this.GetFields(field);
            if (fields != null && fields.Length > 0)
            {
                return fields[0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get the raw field value
        /// </summary>
        /// <param name="field">field name</param>
        /// <returns>raw field value</returns>
        public virtual object GetRawField(string field)
        {
            object[] fields = this.GetRawFields(field);
            if (fields != null && fields.Length > 0)
            {
                return fields[0];
            }
            else
            {
                return null;
            }
        }

        public virtual Dictionary<string, TermFrequencyVector> TermFreqMap { get; set; }

        public virtual int GroupPosition { get; set; }

        public virtual string GroupField { get; set; }

        public virtual string GroupValue { get; set; }

        public virtual object RawGroupValue { get; set; }

        public virtual int GroupHitsCount { get; set; }

        public virtual BrowseHit[] GroupHits { get; set; }

        public virtual Explanation Explanation { get; set; }

        public virtual IComparable Comparable { get; set; }

        /// <summary>
        /// Gets or sets the internal document id
        /// </summary>
        public int DocId { get; set; }

        /// <summary>
        /// Gets or sets the field values
        /// </summary>
        public Dictionary<string, string[]> FieldValues { get; set; }

        /// <summary>
        /// Gets or sets the raw field value map
        /// </summary>
        public Dictionary<string, object[]> RawFieldValues { get; set; }

        /// <summary>
        /// Gets or sets the stored fields
        /// </summary>
        public Document StoredFields { get; set; }


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
            buffer.Append(" score: ").Append(Score).AppendLine();
            buffer.Append(" field values: ").Append(ToString(FieldValues)).AppendLine();
            return buffer.ToString();
        }
    }
}
