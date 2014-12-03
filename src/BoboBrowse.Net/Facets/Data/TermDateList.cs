
namespace BoboBrowse.Net.Facets.Data
{
    using Lucene.Net.Documents;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    ///<summary>Internal data are stored in a long[] with values generated from <seealso cref="Date#getTime()"/> </summary>
	public class TermDateList : TermValueList<object>
	{
        public string FormatString { get; protected set; }

        public TermDateList()
        {
        }

        public TermDateList(string formatString)
		{
            FormatString = formatString;
		}

		public TermDateList(int capacity, string formatString) : base(capacity)
		{
            FormatString = formatString;
        }

		private DateTime? Parse(string s)
		{
            if (s == null || s.Length == 0)
            {
                return null;
            }
            else
            {
                return DateTools.StringToDate(s);
            }
		}

		public override void Add(string @value)
		{
            Add(Parse(@value));
		}

		public override string Format(object o)
		{
            return (null == o)
                ? null
                : DateTools.DateToString((DateTime)o, DateTools.Resolution.MINUTE);
		}

		public override int IndexOf(object o)
		{
			DateTime? val = Parse((string)o);
            return this.BinarySearch(val);
		}
	}
}