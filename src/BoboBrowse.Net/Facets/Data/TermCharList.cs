// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Data
{
    using System;
    using System.Collections.Generic;

    public class TermCharList : TermValueList<char>
    {
        private List<char> _elements = new List<char>();
        
        private char Parse(string s)
        {
            return string.IsNullOrEmpty(s) ? (char)0 : s[0];
        }

        public TermCharList()
            : base()
        {
        }

        public TermCharList(int capacity)
            : base(capacity)
        {
        }

        public override void Add(string o)
        {
            _innerList.Add(Parse(o));
        }

        public override bool ContainsWithType(char val)
        {
            return _elements.BinarySearch(val) >= 0;
        }

        public override int IndexOf(object o)
        {
            char val;
            if (o is string)
                val = Parse((string)o);
            else
                val = (char)o;
            return _innerList.BinarySearch(val);
        }

        public override int IndexOfWithType(char val)
        {
            return _elements.BinarySearch(val);
        }

        public override void Seal()
        {
            _innerList.TrimExcess();
            _elements = new List<char>(_innerList);
        }

        public override string Format(object o)
        {
            return Convert.ToString(o);
        }
    }
}