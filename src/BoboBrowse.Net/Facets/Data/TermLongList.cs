// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Data
{
    using Common.Logging;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    public class TermLongList : TermNumberList<long>
    {
        private static ILog logger = LogManager.GetLogger<TermLongList>();
        protected List<long> _elements = new List<long>();
        private long sanity = -1;
        private bool withDummy = true;
        public const long VALUE_MISSING = long.MinValue;

        protected virtual long Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return 0;
            }
            else
            {
                return Convert.ToInt64(s);
            }
        }

        public TermLongList()
            : base()
        { }

        public TermLongList(string formatString)
            : base(formatString)
        { }

        public TermLongList(string formatString, IFormatProvider formatProvider)
            : base(formatString, formatProvider)
        { }

        public TermLongList(int capacity, string formatString)
            : base(capacity, formatString)
        { }

        public TermLongList(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity, formatString, formatProvider)
        { }

        

        public override void Add(string o)
        {
            if (_innerList.Count == 0 && o != null) withDummy = false; // the first value added is not null
            long item = Parse(o);
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
                    long val = _elements[index];
                    if (withDummy && index == 0)
                    {
                        val = 0L;
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

        public long GetPrimitiveValue(int index)
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
                long val;
                if (o is string)
                    val = Parse((string)o);
                else
                    val = (long)o;
                return _elements.Skip(1).ToList().BinarySearch(val);
            }
            else
            {
                if (o == null) return -1;
                long val;
                if (o is string)
                    val = Parse((string)o);
                else
                    val = (long)o;
                return _elements.BinarySearch(val);
            }
        }

        public int IndexOf(long value)
        {
            if (withDummy)
            {
                return _elements.Skip(1).ToList().BinarySearch(value);
            }
            else
            {
                return _elements.BinarySearch(value);
            }
        }

        public override int IndexOfWithType(long o)
        {
            if (withDummy)
            {
                return _elements.Skip(1).ToList().BinarySearch(value);
            }
            else
            {
                return _elements.BinarySearch(value);
            }
        }

        public override void Seal()
        {
            _innerList.TrimExcess();
            _elements = new List<long>(_innerList);
            int negativeIndexCheck = withDummy ? 1 : 0;
            //reverse negative elements, because string order and numeric orders are completely opposite
            if (_elements.Count > negativeIndexCheck && _elements[negativeIndexCheck] < 0)
            {
                int endPosition = IndexOfWithType(0L);
                if (endPosition < 0)
                {
                    endPosition = -1 * endPosition - 1;
                }
                long tmp;
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

        public bool Contains(long val)
        {
            if (withDummy)
                return _elements.Skip(1).ToList().BinarySearch(val) >= 0;
            else
                return _elements.BinarySearch(val) >= 0;
        }

        public override bool ContainsWithType(long val)
        {
            if (withDummy)
                return _elements.Skip(1).ToList().BinarySearch(val) >= 0;
            else
                return _elements.BinarySearch(val) >= 0;
        }

        public long[] Elements
        {
            get { return _elements.ToArray(); }
        }

        public override double GetDoubleValue(int index)
        {
            return _elements[index];
        }
    }
}