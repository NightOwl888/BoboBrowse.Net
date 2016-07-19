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
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Support;
    using System;

    public class TermStringList : TermValueList<string>
    {
        private string sanity = null;
        private bool withDummy = true;

        public TermStringList(int capacity)
            : base(capacity)
        { }

        public TermStringList()
            : base()
        { }

        public override void Add(string o)
        {
            if (_innerList.Count == 0 && o != null) withDummy = false; // the first value added is not null
            if (o == null) o = "";
            if (sanity != null && string.CompareOrdinal(sanity, o) >= 0)
                throw new RuntimeException("Values need to be added in ascending order. Previous value: " + sanity + " adding value: " + o);
            if (_innerList.Count > 0 || !withDummy) sanity = o;
            _innerList.Add(o);
        }

        public override bool Contains(object o)
        {
            if (withDummy)
            {
                return IndexOf(o) > 0;
            }
            else
            {
                return IndexOf(o) >= 0;
            }
        }

        public override string Format(object o)
        {
            return Convert.ToString(o);
        }

        public override int IndexOf(object o)
        {
            if (withDummy)
            {
                if (o == null) return -1;

                if (o.Equals(""))
                {
                    if (_innerList.Count > 1 && "".Equals(_innerList[1]))
                        return 1;
                    else if (_innerList.Count < 2)
                        return -1;
                }
                return _innerList.BinarySearch(Convert.ToString(o), StringComparer.Ordinal);
            }
            else
            {
                return _innerList.BinarySearch(Convert.ToString(o), StringComparer.Ordinal);
            }
        }

        public override bool ContainsWithType(string val)
        {
            if (withDummy)
            {
                if (val == null) return false;
                if (val.Equals(""))
                {
                    return _innerList.Count > 1 && "".Equals(_innerList[1]);
                }
                return _innerList.BinarySearch(val) >= 0;
            }
            else
            {
                return _innerList.BinarySearch(val) >= 0;
            }
        }

        public override int IndexOfWithType(string o)
        {
            if (withDummy)
            {
                if (o == null) return -1;
                if (o.Equals(""))
                {
                    if (_innerList.Count > 1 && "".Equals(_innerList[1]))
                        return 1;
                    else if (_innerList.Count < 2)
                        return -1;
                }
                return _innerList.BinarySearch((string)o);
            }
            else
            {
                return _innerList.BinarySearch((string)o);
            }
        }
    }
}
