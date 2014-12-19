// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Data
{
    using Common.Logging;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    public class TermIntList : TermNumberList<int>
    {
        private static ILog logger = LogManager.GetLogger<TermIntList>();
        private List<int> _elements = new List<int>();
        private int sanity = -1;
        private bool withDummy = true;
        public const int VALUE_MISSING = int.MinValue;

        private int Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return 0;
            }
            else
            {
                return Convert.ToInt32(s);
            }
        }

        public TermIntList()
            : base()
        { }

        public TermIntList(string formatString)
            : base(formatString)
        { }

        public TermIntList(string formatString, IFormatProvider formatProvider)
            : base(formatString, formatProvider)
        { }

        public TermIntList(int capacity, string formatString)
            : base(capacity, formatString)
        { }

        public TermIntList(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity, formatString, formatProvider)
        { }


        public override void Add(string o)
        {
            if (_innerList.Count == 0 && o != null) withDummy = false; // the first value added is not null
            int item = Parse(o);
            _innerList.Add(item);
        }

        public override void Clear()
        {
            base.Clear();
        }

        public override string this[int index]// From IList<string>
        {
            get
            {
                if (index < _innerList.Count)
                {
                    int val = _elements[index];
                    if (withDummy && index == 0)
                    {
                        val = 0;
                    }
                    if (!string.IsNullOrEmpty(this.FormatString))
                    {
                        if (this.FormatProvider != null)
                        {
                            return val.ToString(this.FormatString, this.FormatProvider);
                        }
                        return val.ToString(this.FormatString);
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

        public int GetPrimitiveValue(int index)
        {
            if (index < _elements.Count)
                return _elements[index];
            else
                return VALUE_MISSING;
        }

        public override int IndexOf(object o)
        {
            if (withDummy)
            {
                if (o == null) return -1;
                int val;
                if (o is string)
                    val = Parse((string)o);
                else
                    val = (int)o;
                return _elements.Skip(1).ToList().BinarySearch(val);
            }
            else
            {
                int val;
                if (o is string)
                    val = Parse((string)o);
                else
                    val = (int)o;
                return _elements.BinarySearch(val);
            }
        }

        public int IndexOf(int value)
        {
            if (withDummy)
                return _elements.Skip(1).ToList().BinarySearch(value);
            else
                return _elements.BinarySearch(value);
        }

        public int IndexOfWithOffset(int value, int offset)
        {
            if (withDummy)
            {
                if (offset >= _elements.Count)
                    return -1;
                return _elements.Skip(offset).ToList().BinarySearch(value);
            }
            else
            {
                return _elements.Skip(offset).ToList().BinarySearch(value);
            }
        }

        public override int IndexOfWithType(int val)
        {
            if (withDummy)
            {
                return _elements.Skip(1).ToList().BinarySearch(val);
            }
            else
            {
                return _elements.BinarySearch(val);
            }
        }

        public override void Seal()
        {
            _innerList.TrimExcess();
            _elements = new List<int>(_innerList);
            int negativeIndexCheck = withDummy ? 1 : 0;
            //reverse negative elements, because string order and numeric orders are completely opposite
            if (_elements.Count > negativeIndexCheck && _elements[negativeIndexCheck] < 0)
            {
                int endPosition = IndexOfWithType(0);
                if (endPosition < 0)
                {
                    endPosition = -1 * endPosition - 1;
                }
                int tmp;
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

        public bool Contains(int val)
        {
            if (withDummy)
                return _elements.Skip(1).ToList().BinarySearch(val) >= 0;
            else
                return _elements.BinarySearch(val) >= 0;
        }
    
        public override bool ContainsWithType(int val)
        {
            if (withDummy)
                return _elements.Skip(1).ToList().BinarySearch(val) >= 0;
            else
                return _elements.BinarySearch(val) >= 0;
        }

        public int[] Elements
        {
            get { return _elements.ToArray(); }
        }

        public override double GetDoubleValue(int index)
        {
            return _elements[index];
        }
    }
}