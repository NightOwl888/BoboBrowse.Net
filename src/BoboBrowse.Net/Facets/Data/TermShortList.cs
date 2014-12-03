namespace BoboBrowse.Net.Facets.Data
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

	public class TermShortList : TermNumberList
	{
		public TermShortList() 
            : base()
		{
		}

		public TermShortList(string formatString) 
            : base(formatString)
		{
		}

		public TermShortList(int capacity, string formatString) 
            : base(capacity, formatString)
		{
		}

        private short Parse(string s)
		{
			if (s==null || s.Length == 0)
			{
				return (short)0;
			}
			else
			{
				return Convert.ToInt16(s);
			}
		}

        public override void Add(string o)
        {
            Add(Parse(o));
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