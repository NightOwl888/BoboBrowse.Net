namespace BoboBrowse.Net.Facets.Data
{
    using System;
    using System.Globalization;

    public abstract class TermNumberList<T> : TermValueList<T>
    {
        private const string DEFAULT_FORMATTING_STRING = "0000000000";

        protected TermNumberList()
            : this(DEFAULT_FORMATTING_STRING)
        { }

        protected TermNumberList(string formatString)
            : this(0, formatString)
        { }

        protected TermNumberList(string formatString, IFormatProvider formatProvider)
            : this(0, formatString, formatProvider)
        { }

        protected TermNumberList(int capacity, string formatString)
            : this(capacity, formatString, CultureInfo.InvariantCulture)
        { }

        protected TermNumberList(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity)
        {
            this.FormatString = formatString;
            this.FormatProvider = formatProvider;
        }

        public virtual string FormatString { get; protected set; }

        public virtual IFormatProvider FormatProvider { get; protected set; }

        protected abstract object ParseString(string o);

        public abstract double GetDoubleValue(int index);

        public override string Format(object o)
        {
            if (o == null)
                return null;
            if (o is string)
            {
                o = this.ParseString((string)o);
            }

            if (string.IsNullOrEmpty(this.FormatString))
            {
                return Convert.ToString(o);
            }
            else
            {
                if (this.FormatProvider != null)
                {
                    return Convert.ToDecimal(o, this.FormatProvider).ToString(this.FormatString, this.FormatProvider);
                }

                return Convert.ToDecimal(o).ToString(this.FormatString);
            }
        }
    }
}