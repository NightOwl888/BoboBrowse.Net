namespace BoboBrowse.Net.Facets.Data
{
    using System;
    using System.Collections.Generic;

	public class TermCharList : TermValueList<object>
	{

		private char Parse(string s)
		{
			return string.IsNullOrEmpty(s) ? (char)0 : s[0];
		}

		public TermCharList() : base()
		{
		}

		public TermCharList(int capacity) : base(capacity)
		{
		}

		public override void Add(string o)
		{
            Add(Parse(o));
		}

        //protected internal override List<?> buildPrimitiveList(int capacity)
        //{
        //    return capacity>0 ? new CharArrayList(capacity) : new CharArrayList();
        //}

		public override int IndexOf(object o)
		{
			char val = Parse((string)o);
            return this.BinarySearch(val);
		}

        //public override void Seal()
        //{
        //    ((CharArrayList)_innerList).Trim();
        //}

		public override string Format(object o)
		{
			return Convert.ToString(o);
		}
	}
}