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
            : this(capacity, formatString, null)
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
                return Convert.ToDecimal(o, CultureInfo.InvariantCulture).ToString(this.FormatString, this.FormatProvider);
            }
        }
    }
}