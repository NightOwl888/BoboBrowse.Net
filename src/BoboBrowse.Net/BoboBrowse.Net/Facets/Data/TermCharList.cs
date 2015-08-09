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