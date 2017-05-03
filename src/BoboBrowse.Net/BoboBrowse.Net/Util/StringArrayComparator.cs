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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.Support;
    using Lucene.Net.Support;
    using System;

    public class StringArrayComparer : IComparable<StringArrayComparer>, IComparable
    {
        private readonly string[] m_vals;

        public StringArrayComparer(string[] vals)
        {
            this.m_vals = vals;
        }

        public virtual int CompareTo(StringArrayComparer node)
        {
            string[] o = node.m_vals;
            if (m_vals == o)
            {
                return 0;
            }
            if (m_vals == null)
            {
                return -1;
            }
            if (o == null)
            {
                return 1;
            }
            for (int i = 0; i < m_vals.Length; ++i)
            {
                if (i >= o.Length)
                {
                    return 1;
                }
                //int compVal = vals[i].CompareTo(o[i]);
                int compVal = string.CompareOrdinal(m_vals[i], o[i]);
                if (m_vals[i].StartsWith("-") && o[i].StartsWith("-"))
                {
                    compVal *= -1;
                }
                if (compVal != 0) return compVal;
            }
            if (m_vals.Length == o.Length) return 0;
            return -1;
        }

        public int CompareTo(object obj)
        {
            return CompareTo((StringArrayComparer)obj);
        }

        public override string ToString()
        {
            return Arrays.ToString(m_vals);
        }
    }
}
