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

    public class TermFloatList : TermNumberList<float>
    {
        private float[] _elements;
        private bool withDummy = true;
        public const float VALUE_MISSING = float.MinValue;

        private float Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return 0.0f;
            }
            else
            {
                try
                {
                    // Since this value is stored in a file, we should always store it and parse it with the invariant culture.
                    float result;
                    if (!float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                    {
                        // If the invariant culture doesn't work, fall back to the passed in format provider
                        // if the provider is null, this will use the culture of the current thread by default.
                        result = float.Parse(s, NumberStyles.Any, this.FormatProvider);
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    if (NumericUtil.IsPrefixCodedFloat(s))
                    {
                        throw new NotSupportedException("Lucene.Net index field must be a formatted string data type (typically padded with leading zeros). NumericField (FLOAT) is not supported.", ex);
                    }
                    throw ex;
                }
            }
        }

        public TermFloatList()
            : base()
        { }

        public TermFloatList(string formatString)
            : base(formatString)
        { }

        public TermFloatList(string formatString, IFormatProvider formatProvider)
            : base(formatString, formatProvider)
        { }

        public TermFloatList(int capacity, string formatString)
            : base(capacity, formatString)
        { }

        public TermFloatList(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity, formatString, formatProvider)
        { }


        public override void Add(string o)
        {
            if (_innerList.Count == 0 && o != null) withDummy = false; // the first value added is not null
            float item = Parse(o);
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
                    float val = _elements[index];
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

        public virtual float GetPrimitiveValue(int index)
        {
            if (index < _elements.Length)
                return _elements[index];
            else
                return VALUE_MISSING;
        }

        public override int IndexOf(object o)
        {
            if (withDummy)
            {
                if (o == null) return -1;
                float val;
                if (o is string)
                    val = Parse((string)o);
                else
                    val = (int)o;
                return Array.BinarySearch(_elements, 1, _elements.Length - 1, val);
            }
            else
            {
                float val;
                if (o is string)
                    val = Parse((string)o);
                else
                    val = (int)o;
                return Array.BinarySearch(_elements, val);
            }
        }

        public virtual int IndexOf(float value)
        {
            if (withDummy)
                return Array.BinarySearch(_elements, 1, _elements.Length - 1, value);
            else
                return Array.BinarySearch(_elements, value);
        }

        public virtual int IndexOfWithOffset(object value, int offset)
        {
            if (withDummy)
            {
                if (value == null || offset >= _elements.Length)
                    return -1;
                float val = Parse(Convert.ToString(value));
                return Array.BinarySearch(_elements, offset, _elements.Length, val);
            }
            else
            {
                float val = Parse(Convert.ToString(value));
                return Array.BinarySearch(_elements, offset, _elements.Length, val);
            }
        }

        public virtual int IndexOfWithOffset(int value, int offset)
        {
            if (withDummy)
            {
                if (offset >= _elements.Length)
                    return -1;
                return Array.BinarySearch(_elements, offset, _elements.Length - offset, value);
            }
            else
            {
                return Array.BinarySearch(_elements, offset, _elements.Length - offset, value);
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
                int endPosition = IndexOfWithType(0);
                if (endPosition < 0)
                {
                    endPosition = -1 * endPosition - 1;
                }
                float tmp;
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

        public virtual bool Contains(float val)
        {
            if (withDummy)
                return Array.BinarySearch(_elements, 1, _elements.Length - 1, val) >= 0;
            else
                return Array.BinarySearch(_elements, val) >= 0;
        }

        public override bool ContainsWithType(float val)
        {
            if (withDummy)
                return Array.BinarySearch(_elements, 1, _elements.Length - 1, val) >= 0;
            else
                return Array.BinarySearch(_elements, val) >= 0;
        }

        public override int IndexOfWithType(float val)
        {
            if (withDummy)
                return Array.BinarySearch(_elements, 1, _elements.Length - 1, val);
            else
                return Array.BinarySearch(_elements, val);
        }

        public virtual float[] Elements
        {
            get { return _elements; }
        }

        public override double GetDoubleValue(int index)
        {
            return _elements[index];
        }
    }
}