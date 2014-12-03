namespace BoboBrowse.Net.Facets.Data
{
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    
    public class TermLongList : TermNumberList
    {
        public TermLongList()
            : base()
        { }

        public TermLongList(string formatString)
            : base(formatString)
        { }

        public TermLongList(int capacity, string formatString)
            : base(capacity, formatString)
        { }

        private long Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return 0;
            }
            else
            {
                return NumericUtils.PrefixCodedToLong(s);
            }
        }

        public override void Add(string @value)
        {
            Add(Parse(@value));
        }

        public override int IndexOf(object o)
        {
            long val = long.Parse((string)o, CultureInfo.InvariantCulture);
            return this.BinarySearch(val);
        }

        protected override object ParseString(string o)
        {
            return Parse(o);
        }
    }
}