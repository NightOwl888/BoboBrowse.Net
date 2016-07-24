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

    public class TermShortList : TermNumberList<short>
    {
        private short[] _elements;
        private bool withDummy = true;
        public const short VALUE_MISSING = short.MinValue;

        private short Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return (short)0;
            }
            else
            {
                try
                {
                    // Since this value is stored in a file, we should always store it and parse it with the invariant culture.
                    short result;
                    if (!short.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                    {
                        // If the invariant culture doesn't work, fall back to the passed in format provider
                        // if the provider is null, this will use the culture of the current thread by default.
                        result = short.Parse(s, NumberStyles.Any, this.FormatProvider);
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

        public TermShortList()
            : base()
        { }

        public TermShortList(string formatString)
            : base(formatString)
        { }

        public TermShortList(string formatString, IFormatProvider formatProvider)
            : base(formatString, formatProvider)
        { }

        public TermShortList(int capacity, string formatString)
            : base(capacity, formatString)
        { }

        public TermShortList(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity, formatString, formatProvider)
        { }

        public override void Add(string o)
        {
            if (_innerList.Count == 0 && o != null) withDummy = false; // the first value added is not null
            short item = Parse(o);
            _innerList.Add(item);
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
                if (index < _innerList.Count)
                {
                    short val = _elements[index];
                    if (withDummy && index == 0)
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

        public virtual short GetPrimitiveValue(int index)
        {
            if (index < _innerList.Count)
                return _elements[index];
            else
                return VALUE_MISSING;
        }

        public override int IndexOf(object o)
        {
            if (withDummy)
            {
                if (o == null) return -1;
                short val;
                if (o is string)
                    val = Parse((string)o);
                else
                    val = (short)o;
                return Array.BinarySearch(_elements, 1, _elements.Length - 1, val);
            }
            else
            {
                short val;
                if (o is string)
                    val = Parse((string)o);
                else
                    val = (short)o;
                return Array.BinarySearch(_elements, val);
            }
        }

        public virtual int IndexOf(short val)
        {
            if (withDummy)
                return Array.BinarySearch(_elements, 1, _elements.Length - 1, val);
            else
                return Array.BinarySearch(_elements, val);
        }

        public override int IndexOfWithType(short val)
        {
            if (withDummy)
            {
                return Array.BinarySearch(_elements, 1, _elements.Length - 1, val);
            }
            else
            {
                return Array.BinarySearch(_elements, val);
            }
        }

        public override void Seal()
        {
            _innerList.TrimExcess();
            _elements = _innerList.ToArray();
            int negativeIndexCheck = withDummy ? 1 : 0;
            //reverse negative elements, because string order and numeric orders are completely opposite
            if (_elements.Length > negativeIndexCheck && _elements[negativeIndexCheck] < 0)
            {
                int endPosition = IndexOfWithType((short)0);
                if (endPosition < 0)
                {
                    endPosition = -1 * endPosition - 1;
                }
                short tmp;
                for (int i = 0; i < (endPosition - negativeIndexCheck) / 2; i++)
                {
                    tmp = _elements[i + negativeIndexCheck];
                    _elements[i + negativeIndexCheck] = _elements[endPosition - i - 1];
                    _elements[endPosition - i - 1] = tmp;
                }
            }
        }

        protected override object ParseString(string o)
        {
            return Parse(o);
        }

        public virtual bool Contains(short val)
        {
            if (withDummy)
                return Array.BinarySearch(_elements, 1, _elements.Length - 1, val) >= 0;
            else
                return Array.BinarySearch(_elements, val) >= 0;
        }

        public override bool ContainsWithType(short val)
        {
            if (withDummy)
                return Array.BinarySearch(_elements, 1, _elements.Length - 1, val) >= 0;
            else
                return Array.BinarySearch(_elements, val) >= 0;
        }

        public virtual short[] Elements
        {
            get { return _elements; }
        }

        public override double GetDoubleValue(int index)
        {
            return _elements[index];
        }
    }
}