// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Support;
    using Common.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class TermFixedLengthLongArrayList : TermValueList<long[]>
    {
        private static ILog logger = LogManager.GetLogger<TermFixedLengthLongArrayList>();

        protected long[] _elements = null;
        protected int width;

        private long[] sanity;
        private bool withDummy = true;

        public TermFixedLengthLongArrayList(int width)
        {
            this.width = width;
            sanity = new long[width];
            sanity[width - 1] = -1;
        }

        public TermFixedLengthLongArrayList(int width, int capacity)
            : base(capacity * width)
        {
            this.width = width;
            sanity = new long[width];
            sanity[width - 1] = -1;
        }

        protected long[] Parse(string s)
        {
            long[] r = new long[width];

            if (s == null || s.Length == 0)
                return r;

            string[] a = s.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (a.Length != width)
                throw new RuntimeException(s + " is not a " + width + " fixed width long.");

            for (int i = 0; i < width; ++i)
            {
                r[i] = long.Parse(a[i]);
                //if (r[i] < 0)
                // throw new RuntimeException("We only support non-negative numbers: " + s);
            }

            return r;
        }

        public override void Add(string o)
        {
            int i = 0;
            long cmp = 0;

            if (_innerList.Count == 0 && o != null) // the first value added is not null
                withDummy = false;

            long[] item = Parse(o);

            for (i = 0; i < width; ++i)
            {
                cmp = item[i] - sanity[i];
                if (cmp != 0)
                    break;
            }

            _innerList.Add(item);

            //if (cmp<=0)
            //  throw new RuntimeException("Values need to be added in ascending order and we only support non-negative numbers: " + o);

            //for (i = 0; i < width; ++i)
            //{
            //    if (!((LongArrayList)_innerList).add(item[i]))
            //    {
            //        if (i > 0)
            //        {
            //            ((LongArrayList)_innerList).removeElements(_innerList.size() - i,
            //                                                        _innerList.size() - 1);
            //        }
            //        //return false;
            //    }
            //}

            if (_innerList.Count > width || !withDummy)
                for (i = 0; i < width; ++i)
                    sanity[i] = item[i];

            //return true;
        }
    }
}
