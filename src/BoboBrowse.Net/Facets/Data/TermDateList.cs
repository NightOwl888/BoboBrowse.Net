
namespace BoboBrowse.Net.Facets.Data
{
    using Lucene.Net.Documents;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    ///<summary>Internal data are stored in a long[] with values generated from <seealso cref="Date#getTime()"/> </summary>
	public class TermDateList : TermValueList<object>
	{
        public TermDateList()
        {
        }

        public TermDateList(string formatString)
		{
            this.FormatString = formatString;
		}

        public TermDateList(string formatString, IFormatProvider formatProvider)
        {
            this.FormatString = formatString;
            this.FormatProvider = formatProvider;
        }

		public TermDateList(int capacity, string formatString) 
            : base(capacity)
		{
            this.FormatString = formatString;
        }

        public TermDateList(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity)
        {
            this.FormatString = formatString;
            this.FormatProvider = formatProvider;
        }

        public string FormatString { get; protected set; }
        public IFormatProvider FormatProvider { get; protected set; }

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
            if (o != null)
            {
                if (!string.IsNullOrEmpty(this.FormatString))
                {
                    return ((DateTime)o).ToString(this.FormatString, this.FormatProvider);
                }
                else
                {
                    return DateTools.DateToString((DateTime)o, DateTools.Resolution.MINUTE);
                }
            }
            return null;
		}

		public override int IndexOf(object o)
		{
			DateTime? val = Parse((string)o);
            return this.BinarySearch(val);
		}
	}
}