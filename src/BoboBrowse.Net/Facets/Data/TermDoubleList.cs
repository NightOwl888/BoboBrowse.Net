namespace BoboBrowse.Net.Facets.Data
{
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public class TermDoubleList : TermNumberList
    {
        public TermDoubleList()
            : base()
        {}

        public TermDoubleList(string formatString)
            : base(formatString)
        {}

        public TermDoubleList(string formatString, IFormatProvider formatProvider)
            : base(formatString, formatProvider)
        {}

        public TermDoubleList(int capacity, string formatString)
            : base(capacity, formatString)
        {}

        public TermDoubleList(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity, formatString, formatProvider)
        { }

        private double Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return 0.0;
            }
            else
            {
                return NumericUtils.PrefixCodedToDouble(s);
            }
        }

        public override void Add(string @value)
        {
            Add(Parse(@value));
        }

        public override int IndexOf(object o)
        {
            double val = double.Parse((string)o, CultureInfo.InvariantCulture);
            return this.BinarySearch(val);
        }

        protected override object ParseString(string o)
        {
            return Parse(o);
        }
    }
}