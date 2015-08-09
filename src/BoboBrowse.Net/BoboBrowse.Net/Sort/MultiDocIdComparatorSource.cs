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

﻿// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Sort
{
    using Lucene.Net.Index;

    public class MultiDocIdComparatorSource : DocComparatorSource
    {
        private DocComparatorSource[] _compSources;

        public MultiDocIdComparatorSource(DocComparatorSource[] compSources)
        {
            _compSources = compSources;
        }

        public override DocComparator GetComparator(IndexReader reader, int docbase)
        {
            DocComparator[] comparators = new DocComparator[_compSources.Length];
            for (int i = 0; i < _compSources.Length; ++i)
            {
                comparators[i] = _compSources[i].GetComparator(reader, docbase);
            }
            return new MultiDocIdComparator(comparators);
        }
    }
}
