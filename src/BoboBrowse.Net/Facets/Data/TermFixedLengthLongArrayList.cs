// TODO: Finish implementation

//// Version compatibility level: 3.1.0
//namespace BoboBrowse.Net.Facets.Data
//{
//    using BoboBrowse.Net.Support;
//    using Common.Logging;
//    using System;
//    using System.Collections;
//    using System.Collections.Generic;
//    using System.Linq;
//    using System.Text;

//    public class TermFixedLengthLongArrayList : TermValueList<long[]>
//    {
//        private static ILog logger = LogManager.GetLogger<TermFixedLengthLongArrayList>();

//        protected long[] _elements = null;
//        protected int width;

//        private long[] sanity;
//        private bool withDummy = true;

//        public TermFixedLengthLongArrayList(int width)
//        {
//            this.width = width;
//            sanity = new long[width];
//            sanity[width - 1] = -1;
//        }

//        public TermFixedLengthLongArrayList(int width, int capacity)
//            : base(capacity * width)
//        {
//            this.width = width;
//            sanity = new long[width];
//            sanity[width - 1] = -1;
//        }

//        protected long[] Parse(string s)
//        {
//            long[] r = new long[width];

//            if (s == null || s.Length == 0)
//                return r;

//            string[] a = s.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
//            if (a.Length != width)
//                throw new RuntimeException(s + " is not a " + width + " fixed width long.");

//            for (int i = 0; i < width; ++i)
//            {
//                r[i] = long.Parse(a[i]);
//                //if (r[i] < 0)
//                // throw new RuntimeException("We only support non-negative numbers: " + s);
//            }

//            return r;
//        }

//        public override void Add(string o)
//        {
//            int i = 0;
//            long cmp = 0;

//            if (_innerList.Count == 0 && o != null) // the first value added is not null
//                withDummy = false;

//            long[] item = Parse(o);

//            for (i = 0; i < width; ++i)
//            {
//                cmp = item[i] - sanity[i];
//                if (cmp != 0)
//                    break;
//            }

//            _innerList.Add(item);

//            //if (cmp<=0)
//            //  throw new RuntimeException("Values need to be added in ascending order and we only support non-negative numbers: " + o);

//            // TODO: make the Add method return a boolean so this will work
//            for (i = 0; i < width; ++i)
//            {
//                if (!_innerList.Add(item[i]))
//                {
//                    if (i > 0)
//                    {
//                        _innerList.RemoveElements(_innerList.Count - i,
//                                                                    _innerList.Count - 1);
//                    }
//                    return false;
//                }
//            }

//            if (_innerList.Count > width || !withDummy)
//                for (i = 0; i < width; ++i)
//                    sanity[i] = item[i];

//            return true;
//        }

//        public override void Clear()
//        {
//            base.Clear();
//        }

//        public override string Get(int index)
//        {
//            index = index * width;
//            StringBuilder sb = new StringBuilder();
//            sb.Append(_elements[index]);

//            int left = width;
//            ++index;
//            while (left > 1)
//            {
//                sb.Append(',');
//                sb.Append(_elements[index]);
//                --left; ++index;
//            }

//            return sb.ToString();
//        }

//        public override object GetRawValue(int index)
//        {
//            long[] val = new long[width];

//            index = index * width;
//            for (int i = 0; i < width; ++i)
//            {
//                val[i] = _elements[index + i];
//            }

//            return val;
//        }

//        public override IEnumerator<string> GetEnumerator()
//        {
//            return new TermListLengthLongArrayListEnumerator(_innerList, width, this.Format);
//        }

//        public class TermListLengthLongArrayListEnumerator : IEnumerator<string>
//        {
//            protected readonly IList<long[]> _innerList;
//            protected readonly int _width;
//            protected readonly Func<object, string> _format;
//            protected string _current;
//            protected int _index;

//            public TermListLengthLongArrayListEnumerator(IList<long[]> innerList, int width, Func<object, string> format)
//            {
//                _innerList = innerList;
//                _width = width;
//            }

//            public string Current
//            {
//                get { return this.GetCurrent(); }
//            }

//            public void Dispose()
//            {
//            }

//            object IEnumerator.Current
//            {
//                get { return this.GetCurrent(); }
//            }

//            public bool MoveNext()
//            {
//                if (this.HasNext())
//                {
//                    _index++;
//                    return true;
//                }
//                return false;
//            }

//            private bool HasNext()
//            {
//                return (_innerList.Count > _index + 1);
//            }

//            public void Reset()
//            {
//            }

//            private string GetCurrent()
//            {
//                long[] val = new long[_width];
//                for (int i = 0; i < _width; ++i)
//                {
//                    val[i] = _innerList[_index][i];
//                }
//                return _format(val);
//            }

//            // TODO: Work out what the remove function is supposed to do
//        }

//        public override int Size
//        {
//            get
//            {
//                return _innerList.Count / width;
//            }
//        }

//        public override int Count
//        {
//            get
//            {
//                return _innerList.Count / width;
//            }
//        }

//        //public object[] ToArray()
//        //{
//        //    object[] retArray = new object[this.Size];
//        //    for (int i = 0; i < retArray.Length; ++i)
//        //    {
//        //        retArray[i] = this.Get(i);
//        //    }
//        //    return retArray;
//        //}

//        public long[][] ToArray()
//        {
//            long[][] retArray = new long[this.Size][];
//            for (int i = 0; i < retArray.Length; ++i)
//            {
//                retArray[i] = (long[])GetRawValue(i);
//            }
//            return retArray;
//        }

//        public override string Format(object o)
//        {
//            if (o == null)
//                return null;

//            if (o is string)
//                o = Parse((string)o);

//            long[] val = (long[])o;

//            if (val.Length == 0)
//                return null;

//            StringBuilder sb = new StringBuilder();
//            sb.Append(val[0]);

//            for (int i = 1; i < val.Length; ++i)
//            {
//                sb.Append(',');
//                sb.Append(val[i]);
//            }
//            return sb.ToString();
//        }

//        protected int BinarySearch(long[] key)
//        {
//            return BinarySearch(key, 0, _elements.Length / width - 1);
//        }

//        protected int BinarySearch(long[] key, int low, int high)
//        {
//            int mid = 0;
//            long cmp = -1;
//            int index, i;

//            while (low <= high)
//            {
//                mid = (low + high) / 2;
//                index = mid * width;
//                for (i = 0; i < width; ++i, ++index)
//                {
//                    cmp = key[i] - _elements[index];

//                    if (cmp != 0)
//                        break;
//                }
//                if (cmp > 0)
//                    low = mid + 1;
//                else if (cmp < 0)
//                    high = mid - 1;
//                else
//                    return mid;
//            }
//            return -(mid + 1);
//        }

//        public override int IndexOf(object o)
//        {
//            if (withDummy)
//            {
//                if (o is string)
//                    o = Parse((string)o);
//                return BinarySearch((long[])o, 1, _elements.Length / width - 1);
//            }
//            else
//            {
//                if (o is string)
//                    o = Parse((string)o);
//                return BinarySearch((long[])o);
//            }
//        }

//        public int IndexOf(long[] val)
//        {
//            if (withDummy)
//            {
//                return BinarySearch(val, 1, _elements.Length / width - 1);
//            }
//            else
//                return BinarySearch(val);
//        }

//        public override int IndexOfWithType(long[] val)
//        {
//            if (withDummy)
//            {
//                return BinarySearch(val, 1, _elements.Length / width - 1);
//            }
//            else
//                return BinarySearch(val);
//        }

//        public override IComparable GetComparableValue(int index)
//        {
//            return Get(index);
//        }

//        public override void Seal()
//        {
//            _innerList.TrimExcess();
//            _elements = _innerList.ToArray();
//        }

//        public bool Contains(long[] val)
//        {
//            if (withDummy)
//            {
//                return BinarySearch(val, 1, _elements.Length / width - 1) >= 0;
//            }
//            else
//                return BinarySearch(val) >= 0;
//        }

//        public override bool ContainsWithType(long[] val)
//        {
//            if (withDummy)
//            {
//                return BinarySearch(val, 1, _elements.Length / width - 1) >= 0;
//            }
//            else
//                return BinarySearch(val) >= 0;
//        }
//    }
//}
