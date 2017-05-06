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
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Support;
    using System;
    using System.Globalization;

    /// <summary>
    /// NOTE: This was TermIntList in bobo-browse
    /// </summary>
    public class TermInt32List : TermNumberList<int>
    {
        private int[] m_elements;
        private bool m_withDummy = true;
        public const int VALUE_MISSING = int.MinValue;

        private int Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return 0;
            }
            else
            {
                try
                {
                    // Since this value is stored in a file, we should always store it and parse it with the invariant culture.
                    int result;
                    if (!int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                    {
                        // If the invariant culture doesn't work, fall back to the passed in format provider
                        // if the provider is null, this will use the culture of the current thread by default.
                        result = int.Parse(s, NumberStyles.Any, this.FormatProvider);
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    if (NumericUtil.IsPrefixCodedInt(s))
                    {
                        throw new NotSupportedException("Lucene.Net index field must be a formatted string data type (typically padded with leading zeros). NumericField (INT) is not supported.", ex);
                    }
                    throw ex;
                }
            }
        }

        public TermInt32List()
            : base()
        { }

        public TermInt32List(string formatString)
            : base(formatString)
        { }

        public TermInt32List(string formatString, IFormatProvider formatProvider)
            : base(formatString, formatProvider)
        { }

        public TermInt32List(int capacity, string formatString)
            : base(capacity, formatString)
        { }

        public TermInt32List(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity, formatString, formatProvider)
        { }


        public override void Add(string o)
        {
            if (m_innerList.Count == 0 && o != null) m_withDummy = false; // the first value added is not null
            int item = Parse(o);
            m_innerList.Add(item);
        }

        public override void Clear()
        {
            base.Clear();
        }

        public override string Get(int index)
        {
            return this[index];
        }

        public override string this[int index]// From IList<string>
        {
            get
            {
                if (index < m_innerList.Count)
                {
                    int val = m_elements[index];
                    if (m_withDummy && index == 0)
                    {
                        val = 0;
                    }
                    if (!string.IsNullOrEmpty(this.FormatString))
                    {
                        return val.ToString(this.FormatString, this.FormatProvider);
                    }
                    return val.ToString();
                }
                return "";
            }
            set
            {
                throw new NotSupportedException("not supported");
            }
        }

        public virtual int GetPrimitiveValue(int index)
        {
            if (index < m_elements.Length)
                return m_elements[index];
            else
                return VALUE_MISSING;
        }

        public override int IndexOf(object o)
        {
            if (m_withDummy)
            {
                if (o == null) return -1;
                int val;
                if (o is string)
                    val = Parse((string)o);
                else
                    val = (int)o;
                return Array.BinarySearch(m_elements, 1, m_elements.Length - 1, val);
            }
            else
            {
                int val;
                if (o is string)
                    val = Parse((string)o);
                else
                    val = (int)o;
                return Array.BinarySearch(m_elements, val);
            }
        }

        public virtual int IndexOf(int value)
        {

            if (m_withDummy)
                return Array.BinarySearch(m_elements, 1, m_elements.Length - 1, value);
            else
                return Array.BinarySearch(m_elements, value);
        }

        public virtual int IndexOfWithOffset(object value, int offset)
        {
            if (m_withDummy)
            {
                if (value == null || offset >= m_elements.Length)
                    return -1;
                int val = Parse(Convert.ToString(value));
                return Array.BinarySearch(m_elements, offset, m_elements.Length, val);
            }
            else
            {
                int val = Parse(Convert.ToString(value));
                return Array.BinarySearch(m_elements, offset, m_elements.Length, val);
            }
        }

        public virtual int IndexOfWithOffset(int value, int offset)
        {
            if (m_withDummy)
            {
                if (offset >= m_elements.Length)
                    return -1;
                return Array.BinarySearch(m_elements, offset, m_elements.Length - offset, value);
            }
            else
            {
                return Array.BinarySearch(m_elements, offset, m_elements.Length - offset, value);
            }
        }

        public override int IndexOfWithType(int val)
        {
            if (m_withDummy)
                return Array.BinarySearch(m_elements, 1, m_elements.Length - 1, val);
            else
                return Array.BinarySearch(m_elements, val);
        }

        public override void Seal()
        {
            m_innerList.TrimExcess();
            m_elements = m_innerList.ToArray();
            int negativeIndexCheck = m_withDummy ? 1 : 0;
            //reverse negative elements, because string order and numeric orders are completely opposite
            if (m_elements.Length > negativeIndexCheck && m_elements[negativeIndexCheck] < 0)
            {
                int endPosition = IndexOfWithType(0);
                if (endPosition < 0)
                {
                    endPosition = -1 * endPosition - 1;
                }
                int tmp;
                for (int i = 0; i < (endPosition - negativeIndexCheck) / 2; i++)
                {
                    tmp = m_elements[i + negativeIndexCheck];
                    m_elements[i + negativeIndexCheck] = m_elements[endPosition - i - 1];
                    m_elements[endPosition - i - 1] = tmp;
                }
            }
        }

        protected override object ParseString(string o)
        {
            return Parse(o);
        }

        public virtual bool Contains(int val)
        {
            if (m_withDummy)
                return Array.BinarySearch(m_elements, 1, m_elements.Length - 1, val) >= 0;
            else
                return Array.BinarySearch(m_elements, val) >= 0;
        }
    
        public override bool ContainsWithType(int val)
        {
            if (m_withDummy)
                return Array.BinarySearch(m_elements, 1, m_elements.Length - 1, val) >= 0;
            else
                return Array.BinarySearch(m_elements, val) >= 0;
        }

        public virtual int[] Elements
        {
            get { return m_elements; }
        }

        public override double GetDoubleValue(int index)
        {
            return m_elements[index];
        }
    }
}