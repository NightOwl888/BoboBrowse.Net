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
    using System;

    public abstract class TermListFactory
    {
        public abstract ITermValueList CreateTermList(int capacity);
        public abstract ITermValueList CreateTermList();
        public abstract Type Type { get; }

        private class DefaultTermListFactory
            : TermListFactory
        {
            public override ITermValueList CreateTermList(int capacity)
            {
                return new TermStringList(capacity);
            }

            public override ITermValueList CreateTermList()
            {
                return new TermStringList();
            }

            public override Type Type
            {
                get { return typeof(string); }
            }
        }

        public static TermListFactory StringListFactory = new DefaultTermListFactory();
    }
}
