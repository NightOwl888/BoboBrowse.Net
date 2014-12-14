// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Data
{
    using Common.Logging;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    public class TermDoubleList : TermNumberList<double>
    {
        private static ILog logger = LogManager.GetLogger<TermDoubleList>();
        private List<double> _elements = new List<double>();
        public const double VALUE_MISSING = double.MinValue;

        private double Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return 0.0;
            }
            else
            {
                // TODO: Should we be using a format provider?
                return Convert.ToDouble(s);
            }
        }

        public TermDoubleList()
            : base()
        { }

        public TermDoubleList(string formatString)
            : base(formatString)
        { }

        public TermDoubleList(string formatString, IFormatProvider formatProvider)
            : base(formatString, formatProvider)
        { }

        public TermDoubleList(int capacity, string formatString)
            : base(capacity, formatString)
        { }

        public TermDoubleList(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity, formatString, formatProvider)
        { }

        
        public override void Add(string @value)
        {
            _innerList.Add(Parse(@value));
        }

        public override string this[int index]// From IList<string>
        {
            get
            {
                if (index < _innerList.Count)
                {
                    double val = _elements[index];
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

        public double GetPrimitiveValue(int index)
        {
            if (index < _elements.Count)
                return _elements[index];
            else
                return VALUE_MISSING;
        }

        public override int IndexOf(object o)
        {
            double val;
            if (o is string)
                val = Parse((string)o);
            else
                val = (double)o;
            // TODO: This doesn't look right - the other types do the binary search on _elements
            return _innerList.BinarySearch(val);
        }

        public int IndexOf(double val)
        {
            return _elements.BinarySearch(val);
        }

        public override void Seal()
        {
            _innerList.TrimExcess();
            _elements = new List<double>(_innerList);
            int negativeIndexCheck = 1;
            //reverse negative elements, because string order and numeric orders are completely opposite
            if (_elements.Count > negativeIndexCheck && _elements[negativeIndexCheck] < 0)
            {
                int endPosition = IndexOfWithType((short)0);
                if (endPosition < 0)
                {
                    endPosition = -1 * endPosition - 1;
                }
                double tmp;
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

        public bool Contains(double val)
        {
            return _elements.BinarySearch(val) >= 0;
        }

        public override bool ContainsWithType(double val)
        {
            return _elements.BinarySearch(val) >= 0;
        }

        public override int IndexOfWithType(double val)
        {
            return _elements.BinarySearch(val);
        }

        protected override double GetDoubleValue(int index)
        {
            return _elements[index];
        }
    }
}