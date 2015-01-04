// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Support;
    using Lucene.Net.Documents;
    using System;
    using System.Globalization;

    /// <summary>
    /// Internal data are stored in a long[] with values generated from <see cref="M:DateTime.ToBinary"/>.
    /// </summary>
    public class TermDateList : TermLongList
    {
        public TermDateList()
            : base()
        {}

        public TermDateList(string formatString)
            : base(formatString)
        {}

        public TermDateList(string formatString, IFormatProvider formatProvider)
            : base(formatString, formatProvider)
        {}

        public TermDateList(int capacity, string formatString)
            : base(capacity, formatString)
        {}

        public TermDateList(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity, formatString, formatProvider)
        {}

        protected override long Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return 0L;
            }
            else
            {
                try
                {
                    DateTime result;
                    // Since this value is stored in a file, we should always store it and parse it with the invariant culture.
                    if (!DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    {
                        // Support the Lucene.Net date formatting (yyyyMMddHHmmssfff or any subset starting on the left) for convenience.
                        result = DateTools.StringToDate(s);
                    }
                    return result.ToBinary();
                }
                catch (Exception e)
                {
                    throw new RuntimeException(e.Message, e);
                }
            }
        }

        public override string Get(int index)
        {
            return Format(_elements[index]);
        }

        public override string this[int index]// From IList<string>
        {
            get
            {
                return Format(_elements[index]);
            }
            set
            {
                throw new NotSupportedException("not supported");
            }
        }

        public override string Format(object o)
        {
            long val;
            if (o is string)
            {
                val = Parse(Convert.ToString(o));
            }
            else
            {
                val = Convert.ToInt64(o);
            }

            if (string.IsNullOrEmpty(this.FormatString))
            {
                return Convert.ToString(o);
            }
            else
            {
                if (this.FormatProvider == null)
                {
                    return DateTime.FromBinary(val).ToString(this.FormatString);
                }
                return DateTime.FromBinary(val).ToString(this.FormatString, this.FormatProvider);
            }
        }
    }
}