//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
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

// Version compatibility level: 3.2.0
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
                return DateTime.FromBinary(val).ToString(this.FormatString, this.FormatProvider);
            }
        }
    }
}