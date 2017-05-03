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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Support;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    ///<summary>A hit from a browse.</summary>
    [Serializable]
    public class BrowseHit
    {
        //private static long serialVersionUID = 1L; // NOT USED

        [Serializable]
        public class BoboTerm
        {
            //private static long serialVersionUID = 1L; // NOT USED

            public BoboTerm()
            {
                Positions = new List<int>();
                StartOffsets = new List<int>();
                EndOffsets = new List<int>();
            }

            public string Term { get; set; }
            public int Freq { get; set; }
            public IList<int> Positions { get; set; }
            public IList<int> StartOffsets { get; set; }
            public IList<int> EndOffsets { get; set; }
        }

        [Serializable]
        public class SerializableField
        {

            //private static long serialVersionUID = 1L; // NOT USED
            private readonly string name;

            /** Field's value */
            private object fieldsData;

            public SerializableField(IIndexableField field)
            {
                // TODO: Fix this property and push to Lucene.Net
                name = field.Name;
                if (field.GetNumericValue() != null)
                {
                    fieldsData = field.GetNumericValue();
                }
                else if (field.GetStringValue() != null)
                {
                    fieldsData = field.GetStringValue();
                }
                else if (field.GetBinaryValue() != null)
                {
                    // TODO: Fix this property and push to Lucene.Net
                    fieldsData = field.GetBinaryValue().Bytes;
                }
                else
                {
                    throw new NotSupportedException("Doesn't support this field type so far");
                }
            }

            public string Name
            {
                get { return name; }
            }

            public string StringValue
            {
                get
                {
                    if (fieldsData is string || NumericUtil.IsNumeric(fieldsData))
                    {
                        return fieldsData.ToString();
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public object NumericValue
            {
                get
                {
                    if (NumericUtil.IsNumeric(fieldsData))
                    {
                        return (Decimal)fieldsData;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public byte[] BinaryValue
            {
                get
                {
                    if (fieldsData is byte[])
                    {
                        return (byte[])fieldsData;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public override bool Equals(object o)
            {
                if (!(o is SerializableField))
                {
                    return false;
                }
                SerializableField other = (SerializableField)o;
                if (!name.Equals(other.Name))
                {
                    return false;
                }
                string value = StringValue;
                if (value != null)
                {
                    if (value.Equals(other.StringValue))
                    {
                        return true;
                    }
                    return false;
                }
                byte[] binValue = BinaryValue;
                if (binValue != null)
                {
                    return Arrays.Equals(binValue, other.BinaryValue);
                }
                return false;
            }

            // Required by .NET because Equals() was overridden.
            // Source: http://stackoverflow.com/questions/70303/how-do-you-implement-gethashcode-for-structure-with-two-string#21604191
            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = 0;

                    // String properties
                    hashCode = (hashCode * 397) ^ (name != null ? name.GetHashCode() : string.Empty.GetHashCode());

                    string value = StringValue;
                    hashCode = (hashCode * 397) ^ (value != null ? value.GetHashCode() : string.Empty.GetHashCode());

                    // int properties
                    //hashCode = (hashCode * 397) ^ _hitcount;

                    return hashCode;
                }
            }
        }


        [Serializable]
        public class SerializableExplanation
        {
            //private static const long serialVersionUID = 1L; // NOT USED

            private float value; // the value of this node
            private string description;
            private IList<SerializableExplanation> details; // sub-explanations

            public SerializableExplanation(Explanation explanation)
            {
                Value = explanation.Value;
                Description = explanation.Description;
                Explanation[] details = explanation.GetDetails();
                if (details == null)
                {
                    return;
                }
                foreach (Explanation exp in details)
                {
                    AddDetail(new SerializableExplanation(exp));
                }
            }

            /// <summary>
            /// Gets or sets the value assigned to this explanation node.
            /// </summary>
            public float Value
            {
                get { return value; }
                set { this.value = value; }
            }

            /// <summary>
            /// Gets or sets the description of this explanation node.
            /// </summary>
            public string Description
            {
                get { return description; }
                set { description = value; }
            }

            /// <summary>
            /// Gets the sub-nodes of this explanation node.
            /// </summary>
            public SerializableExplanation[] Details
            {
                get
                {
                    if (details == null)
                    {
                        return null;
                    }
                    return details.ToArray();
                }
            }

            /// <summary>
            /// Adds a sub-node to this explanation node.
            /// </summary>
            /// <param name="detail"></param>
            public void AddDetail(SerializableExplanation detail)
            {
                if (details == null)
                {
                    details = new List<SerializableExplanation>();
                }
                details.Add(detail);
            }


            public override string ToString()
            {
                return ToString(0);
            }

            protected string ToString(int depth)
            {
                StringBuilder buffer = new StringBuilder();
                for (int i = 0; i < depth; i++)
                {
                    buffer.Append("  ");
                }
                buffer.Append(Value + " = " + Description);
                buffer.Append("\n");

                SerializableExplanation[] details = Details;
                if (details != null)
                {
                    for (int i = 0; i < details.Length; i++)
                    {
                        buffer.Append(details[i].ToString(depth + 1));
                    }
                }

                return buffer.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        public virtual float Score { get; set; }

        /// <summary>
        /// Get the field values.
        /// </summary>
        /// <param name="field">field name</param>
        /// <returns>field value array</returns>
        /// <seealso cref="GetField(string)"/>
        public virtual string[] GetFields(string field)
        {
            return this.FieldValues != null ? this.FieldValues.Get(field) : null;
        }

        /// <summary>
        /// Get the raw field values.
        /// </summary>
        /// <param name="field">field name</param>
        /// <returns>field value array</returns>
        public virtual object[] GetRawFields(string field)
        {
            return this.RawFieldValues != null ? this.RawFieldValues.Get(field) : null;
        }

        /// <summary>
        /// Gets the field value by field name.
        /// </summary>
        /// <param name="field">field name</param>
        /// <returns>field value</returns>
        /// <seealso cref="GetFields(string)"/>
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
        /// Get the raw field value.
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

        public BrowseHit()
        {
            TermVectorMap = new Dictionary<string, IList<BoboTerm>>();
        }

        [NonSerialized]
        private IComparable _comparable;

        /// <summary>
        /// Gets or sets a dictionary of field names to <see cref="T:IList{BoboTerm}"/> instances. 
        /// These are populated when specified in the <see cref="P:BrowseRequest.TermVectorsToFetch"/> property.
        /// A term vector is a list of the document's terms and their number of occurrences in that document.
        /// </summary>
        public virtual IDictionary<string, IList<BoboTerm>> TermVectorMap { get; set; }

        /// <summary>
        /// Gets or sets the position of the <see cref="P:GroupField"/> inside groupBy request.
        /// NOTE: This does not appear to be in use by BoboBrowse.
        /// </summary>
        public virtual int GroupPosition { get; set; }

        /// <summary>
        /// Gets or sets the group field inside groupBy request.
        /// </summary>
        public virtual string GroupField { get; set; }

        /// <summary>
        /// Gets or sets the string value of the field that is currently the groupBy request.
        /// </summary>
        public virtual string GroupValue { get; set; }

        /// <summary>
        /// Gets or sets the primitive value of the field that is currently the groupBy request.
        /// </summary>
        public virtual object RawGroupValue { get; set; }

        /// <summary>
        /// Gets or sets the total FacetValueHitCount of the groupBy request.
        /// </summary>
        public virtual int GroupHitsCount { get; set; }

        /// <summary>
        /// Gets or sets the hits of the group.
        /// NOTE: This field does not appear to be in use by BoboBrowse.
        /// </summary>
        public virtual BrowseHit[] GroupHits { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="T:Lucene.Net.Search.Explanation"/>. This will be set if the <see cref="P:BrowseRequest.ShowExplanation"/> property is set to true.
        /// An <see cref="T:Lucene.Net.Search.Explanation"/> describes the score computation for document and query.
        /// </summary>
        public virtual SerializableExplanation Explanation { get; set; }

        public void SetExplanation(Explanation explanation)
        {
            Explanation = new SerializableExplanation(explanation);
            //return this;
        }

        /// <summary>
        /// Gets or sets the <see cref="System.IComparable"/> value that is used to compare the current hit to other hits for sorting purposes.
        /// </summary>
        public virtual IComparable Comparable
        {
            get { return _comparable; }
            set { _comparable = value; }
        }

        /// <summary>
        /// Gets or sets the internal document id.
        /// </summary>
        public virtual int DocId { get; set; }

        /// <summary>
        /// Gets or sets the field values.
        /// </summary>
        public virtual IDictionary<string, string[]> FieldValues { get; set; }

        /// <summary>
        /// Gets or sets the raw field value map.
        /// </summary>
        public virtual IDictionary<string, object[]> RawFieldValues { get; set; }

        /// <summary>
        /// Gets or sets the stored fields.
        /// </summary>
        public virtual IList<SerializableField> StoredFields { get; set; }

        /// <summary>
        /// Adds all of the fields in a <see cref="T:Lucene.Net.Documents.Document"/> to 
        /// the StoredFields property.
        /// </summary>
        /// <param name="doc"></param>
        public virtual void SetStoredFields(Document doc)
        {
            if (doc == null)
            {
                StoredFields = null;
                //return this;
            }
            StoredFields = new List<SerializableField>();
            var it = doc.GetEnumerator();
            while (it.MoveNext())
            {
                StoredFields.Add(new SerializableField(it.Current));
            }
            //return this;
        }

        public byte[] GetFieldBinaryValue(string fieldName)
        {
            if (StoredFields == null)
            {
                return null;
            }
            foreach (SerializableField field in StoredFields)
            {
                if (fieldName.Equals(field.Name))
                {
                    return field.BinaryValue;
                }
            }
            return null;
        }

        public string GetFieldStringValue(string fieldName)
        {
            if (StoredFields == null)
            {
                return null;
            }
            foreach (SerializableField field in StoredFields)
            {
                if (fieldName.Equals(field.Name))
                {
                    return field.StringValue;
                }
            }
            return null;
        }
        
        public string ToString(IDictionary<string, string[]> map)
        {
            StringBuilder buffer = new StringBuilder();
            foreach (KeyValuePair<string, string[]> e in map)
            {
                buffer.Append(e.Key);
                buffer.Append(":");
                var vals = e.Value;
                buffer.Append(vals == null ? null : string.Join(", ", e.Value));
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Gets a string representation of the current BrowseHit.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("docid: ").Append(DocId).AppendLine();
            buffer.Append(" score: ").Append(Score).AppendLine();
            buffer.Append(" field values: ").Append(ToString(FieldValues)).AppendLine();
            return buffer.ToString();
        }
    }
}
