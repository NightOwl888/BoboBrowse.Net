//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
//*
//* Copyright (C) 2005-2015  John Wang
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//*   http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Support;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    public class TermFixedLengthLongArrayList : TermValueList<long[]>
    {
        private List<long> m_innerList2 = new List<long>();
        protected long[] m_elements = null;
        protected int m_width;

        private long[] m_sanity;
        private bool m_withDummy = true;

        public TermFixedLengthLongArrayList(int width)
            : base()
        {
            this.m_width = width;
            m_sanity = new long[width];
            m_sanity[width - 1] = -1;
        }

        public TermFixedLengthLongArrayList(int width, int capacity)
            : base(capacity * width)
        {
            this.m_width = width;
            m_sanity = new long[width];
            m_sanity[width - 1] = -1;
        }

        protected long[] Parse(string s)
        {
            long[] r = new long[m_width];

            if (s == null || s.Length == 0)
                return r;

            string[] a = s.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (a.Length != m_width)
                throw new RuntimeException(s + " is not a " + m_width + " fixed width long.");

            for (int i = 0; i < m_width; ++i)
            {
                // Try the invariant culture first.
                long result;
                if (!long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                {
                    // If the invariant culture doesn't work, fall back to the format provider
                    // of the current thread by default.
                    result = long.Parse(s, NumberStyles.Any);
                }
                r[i] = result;

                //if (r[i] < 0)
                // throw new RuntimeException("We only support non-negative numbers: " + s);
            }

            return r;
        }

        public override void Add(string o)
        {
            int i = 0;
            long cmp = 0;

            if (m_innerList2.Count == 0 && o != null) // the first value added is not null
                m_withDummy = false;

            long[] item = Parse(o);

            for (i = 0; i < m_width; ++i)
            {
                cmp = item[i] - m_sanity[i];
                if (cmp != 0)
                    break;
            }

            //if (cmp<=0)
            //  throw new RuntimeException("Values need to be added in ascending order and we only support non-negative numbers: " + o);

            for (i = 0; i < m_width; ++i)
            {
                m_innerList2.Add(item[i]);

                if (i > 0)
                {
                    //_innerList2.RemoveRange(_innerList.Count - i, (_innerList2.Count - 1) - (_innerList2.Count - i));
                    m_innerList2.RemoveElements(m_innerList2.Count - i, m_innerList2.Count - 1);
                }
            }

            if (m_innerList2.Count > m_width || !m_withDummy)
                for (i = 0; i < m_width; ++i)
                    m_sanity[i] = item[i];
        }

        public override void Clear()
        {
            m_innerList2.Clear();
        }

        public override string Get(int index)
        {
            index = index * m_width;
            StringBuilder sb = new StringBuilder();
            sb.Append(m_elements[index]);

            int left = m_width;
            ++index;
            while (left > 1)
            {
                sb.Append(',');
                sb.Append(m_elements[index]);
                --left; ++index;
            }

            return sb.ToString();
        }

        public override string this[int index]
        {
            get
            {
                return Get(index);
            }
            set
            {
                base[index] = value;
            }
        }

        public override object GetRawValue(int index)
        {
            long[] val = new long[m_width];

            index = index * m_width;
            for (int i = 0; i < m_width; ++i)
            {
                val[i] = m_elements[index + i];
            }

            return val;
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return new TermListLengthLongArrayListEnumerator(m_innerList2, m_width, this.Format);
        }

        public class TermListLengthLongArrayListEnumerator : IEnumerator<string>
        {
            protected readonly IList<long> m_innerList2;
            protected readonly int m_width;
            protected readonly Func<object, string> m_format;
            protected int m_index = 0;

            public TermListLengthLongArrayListEnumerator(IList<long> innerList2, int width, Func<object, string> format)
            {
                m_innerList2 = innerList2;
                m_width = width;
                m_format = format;
            }

            public string Current
            {
                get { return this.GetCurrent(); }
            }

            public void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get { return this.GetCurrent(); }
            }

            public bool MoveNext()
            {
                if (this.HasNext())
                {
                    m_index++;
                    return true;
                }
                return false;
            }

            private bool HasNext()
            {
                return (m_innerList2.Count > (m_index + 1));
            }

            public void Reset()
            {
            }

            private string GetCurrent()
            {
                long[] val = new long[m_width];
                for (int i = 0; i < m_width; ++i)
                {
                    val[i] = m_innerList2.Get(m_index, int.MinValue);
                }
                return m_format(val);
            }
        }

        public override int Count
        {
            get
            {
                return m_innerList2.Count / m_width;
            }
        }

        public object[] ToArray()
        {
            object[] retArray = new object[this.Count];
            for (int i = 0; i < retArray.Length; ++i)
            {
                retArray[i] = Get(i);
            }
            return retArray;
        }

        public long[][] ToArray(long[][] a)
        {
            long[][] retArray = new long[this.Count][];
            for (int i = 0; i < retArray.Length; ++i)
            {
                retArray[i] = (long[])GetRawValue(i);
            }
            return retArray;
        }

        public override string Format(object o)
        {
            if (o == null)
                return null;

            if (o is string)
                o = Parse((string)o);

            long[] val = (long[])o;

            if (val.Length == 0)
                return null;

            StringBuilder sb = new StringBuilder();
            sb.Append(val[0]);

            for (int i = 1; i < val.Length; ++i)
            {
                sb.Append(',');
                sb.Append(val[i]);
            }
            return sb.ToString();
        }

        public long[] GetPrimitiveValue(int index)
        {
            index = index * m_width;
            long[] r = new long[m_width];

            if (index < m_elements.Length)
            {
                for (int i = 0; i < m_width; ++i, ++index)
                    r[i] = m_elements[index];
            }
            else
            {
                r[m_width - 1] = -1;
            }
            return r;
        }

        protected virtual int BinarySearch(long[] key)
        {
            return BinarySearch(key, 0, m_elements.Length / m_width - 1);
        }

        protected virtual int BinarySearch(long[] key, int low, int high)
        {
            int mid = 0;
            long cmp = -1;
            int index, i;

            while (low <= high)
            {
                mid = (low + high) / 2;
                index = mid * m_width;
                for (i = 0; i < m_width; ++i, ++index)
                {
                    cmp = key[i] - m_elements[index];

                    if (cmp != 0)
                        break;
                }
                if (cmp > 0)
                    low = mid + 1;
                else if (cmp < 0)
                    high = mid - 1;
                else
                    return mid;
            }
            return -(mid + 1);
        }

        public override int IndexOf(object o)
        {
            if (m_withDummy)
            {
                if (o is string)
                    o = Parse((string)o);
                return BinarySearch((long[])o, 1, m_elements.Length / m_width - 1);
            }
            else
            {
                if (o is string)
                    o = Parse((string)o);
                return BinarySearch((long[])o);
            }
        }

        public virtual int IndexOf(long[] val)
        {
            if (m_withDummy)
            {
                return BinarySearch(val, 1, m_elements.Length / m_width - 1);
            }
            else
                return BinarySearch(val);
        }

        public override int IndexOfWithType(long[] val)
        {
            if (m_withDummy)
            {
                return BinarySearch(val, 1, m_elements.Length / m_width - 1);
            }
            else
                return BinarySearch(val);
        }

        public override IComparable GetComparableValue(int index)
        {
            return Get(index);
        }

        public override void Seal()
        {
            m_innerList2.TrimExcess();
            m_elements = m_innerList2.ToArray();
        }

        public bool Contains(long[] val)
        {
            if (m_withDummy)
            {
                return BinarySearch(val, 1, m_elements.Length / m_width - 1) >= 0;
            }
            else
                return BinarySearch(val) >= 0;
        }

        public override bool ContainsWithType(long[] val)
        {
            if (m_withDummy)
            {
                return BinarySearch(val, 1, m_elements.Length / m_width - 1) >= 0;
            }
            else
                return BinarySearch(val) >= 0;
        }



        // Overrides required for mapping the _innerList to _innerList2 
        // (because in .NET generic type matters).
        public override List<string> GetInnerList()
        {
            //return new List<string>(_innerList2.Select(x => Format(x)));
            return new List<string>(m_innerList2.Select(x => Convert.ToString(x)));
        }

        public override Type Type
        {
            get { return typeof(long); }
        }

        public override bool IsEmpty()
        {
            return m_innerList2.Count == 0;
        }

        public override void CopyTo(string[] array, int arrayIndex)
        {
            m_innerList2.Select(x => Format(x)).ToList().CopyTo(array, arrayIndex);
        }
    }
}
