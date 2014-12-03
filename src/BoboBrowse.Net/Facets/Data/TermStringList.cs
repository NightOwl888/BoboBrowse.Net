

namespace BoboBrowse.Net.Facets.Data
{
    using System;
    using System.Collections.Generic;

    public class TermStringList : TermValueList<string>
    {
        public override void Add(string o)
        {
            if (o == null)
            {
                o = "";
            }
            ((List<string>)this).Add(o);
        }

        public override List<string> GetInnerList()
        {
            return this;
        }

        public override bool Contains(object o)
        {
            return IndexOf((string)o) >= 0;
        }

        public override string Format(object o)
        {
            return (string)o;
        }

        public override int IndexOf(object o)
        {
            return BinarySearch((string)o, System.StringComparer.Ordinal);
        }
    }
}
