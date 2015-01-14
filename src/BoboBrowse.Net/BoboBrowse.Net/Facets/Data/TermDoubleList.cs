// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Support;
    using Common.Logging;
    using System;
    using System.Globalization;

    public class TermDoubleList : TermNumberList<double>
    {
        private static ILog logger = LogManager.GetLogger(typeof(TermDoubleList));
        private double[] _elements;
        private bool withDummy = true;
        public const double VALUE_MISSING = double.MinValue;

        private double Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return 0.0;
            }
            else
            {
                try
                {
                    // Since this value is stored in a file, we should always store it and parse it with the invariant culture.
                    double result;
                    if (!double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                    {
                        // If the invariant culture doesn't work, fall back to the passed in format provider
                        // if the provider is null, this will use the culture of the current thread by default.
                        result = double.Parse(s, NumberStyles.Any, this.FormatProvider);
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    if (NumericUtil.IsPrefixCodedDouble(s))
                    {
                        throw new NotSupportedException("Lucene.Net index field must be a formatted string data type (typically padded with leading zeros). NumericField (DOUBLE) is not supported.", ex);
                    }
                    throw ex;
                }
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


        public override void Add(string value)
        {
            if (_innerList.Count == 0 && value != null) withDummy = false; // the first value added is not null
            double item = Parse(value);
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
                    double val = _elements[index];
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

        public virtual double GetPrimitiveValue(int index)
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
                double val;
                if (o is string)
                    val = Parse((string)o);
                else
                    val = (int)o;
                return Array.BinarySearch(_elements, 1, _elements.Length - 1, val);
            }
            else
            {
                double val;
                if (o is string)
                    val = Parse((string)o);
                else
                    val = (int)o;
                return Array.BinarySearch(_elements, val);
            }
        }

        public virtual int IndexOf(double value)
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
                double val = Parse(Convert.ToString(value));
                return Array.BinarySearch(_elements, offset, _elements.Length, val);
            }
            else
            {
                double val = Parse(Convert.ToString(value));
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

        public virtual bool Contains(double val)
        {
            if (withDummy)
                return Array.BinarySearch(_elements, 1, _elements.Length - 1, val) >= 0;
            else
                return Array.BinarySearch(_elements, val) >= 0;
        }

        public override bool ContainsWithType(double val)
        {
            if (withDummy)
                return Array.BinarySearch(_elements, 1, _elements.Length - 1, val) >= 0;
            else
                return Array.BinarySearch(_elements, val) >= 0;
        }

        public override int IndexOfWithType(double val)
        {
            if (withDummy)
                return Array.BinarySearch(_elements, 1, _elements.Length - 1, val);
            else
                return Array.BinarySearch(_elements, val);
        }

        public virtual double[] Elements
        {
            get { return _elements; }
        }

        public override double GetDoubleValue(int index)
        {
            return _elements[index];
        }
    }
}