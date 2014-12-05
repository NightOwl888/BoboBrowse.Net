namespace BoboBrowse.Net.Facets.Data
{
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    
	public class TermIntList : TermNumberList
	{
        public TermIntList()
            : base()
        { }

        public TermIntList(string formatString)
            : base(formatString)
        { }

        public TermIntList(string formatString, IFormatProvider formatProvider)
            : base(formatString, formatProvider)
        {}

        public TermIntList(int capacity, string formatString)
            : base(capacity, formatString)
        { }

        public TermIntList(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity, formatString, formatProvider)
        { }

        private int Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return 0;
            }
            else
            {
                return NumericUtils.PrefixCodedToInt(s);
            }
        }

		public override void Add(string @value)
		{
            Add(Parse(@value));
		}

        public override int IndexOf(object o)
		{
			int val = int.Parse((string)o, CultureInfo.InvariantCulture);
			return this.BinarySearch(val);
		}

        protected override object ParseString(string o)
        {
            return Parse(o);
        }
	}
}