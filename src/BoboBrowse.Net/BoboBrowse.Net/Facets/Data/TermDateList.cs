// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Support;
    using System;

    ///<summary>Internal data are stored in a long[] with values generated from <seealso cref="Date#getTime()"/> </summary>
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
                    return DateTime.Parse(s, this.FormatProvider).ToBinary();
                }
                catch (Exception e)
                {
                    throw new RuntimeException(e.Message, e);
                }
            }
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