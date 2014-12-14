// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Data
{
    using Common.Logging;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    public class TermShortList : TermNumberList<short>
    {
        private static ILog logger = LogManager.GetLogger<TermShortList>();
        private List<short> _elements = new List<short>();
        private short sanity = -1;
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
                return Convert.ToInt16(s);
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

        public override string this[int index]// From IList<string>
        {
            get
            {
                if (index < _innerList.Count)
                {
                    if (!string.IsNullOrEmpty(this.FormatString))
                    {
                        if (this.FormatProvider != null)
                        {
                            return _elements[index].ToString(this.FormatString, this.FormatProvider);
                        }
                        return _elements[index].ToString(this.FormatString);
                    }
                    return _elements[index].ToString();
                }
                return "";
            }
            set
            {
                throw new NotSupportedException("not supported");
            }
        }

        public short GetPrimitiveValue(int index)
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
                return _elements.Skip(1).ToList().BinarySearch(val);
            }
            else
            {
                short val;
                if (o is string)
                    val = Parse((string)o);
                else
                    val = (short)o;
                return _elements.BinarySearch(val);
            }
        }

        public int IndexOf(short val)
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

        public override int IndexOfWithType(short val)
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
            _elements = new List<short>(_innerList);
            int negativeIndexCheck = withDummy ? 1 : 0;
            //reverse negative elements, because string order and numeric orders are completely opposite
            if (_elements.Count > negativeIndexCheck && _elements[negativeIndexCheck] < 0)
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

        public bool Contains(short val)
        {
            if (withDummy)
                return _elements.Skip(1).ToList().BinarySearch(val) >= 0;
            else
                return _elements.BinarySearch(val) >= 0;
        }

        public override bool ContainsWithType(short val)
        {
            if (withDummy)
            {
                return _elements.Skip(1).ToList().BinarySearch(val) >= 0;
            }
            else
            {
                return _elements.BinarySearch(val) >= 0;
            }
        }

        public short[] Elements
        {
            get { return _elements.ToArray(); }
        }

        public override double GetDoubleValue(int index)
        {
            return _elements[index];
        }
    }
}