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
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Util;

    public class MultiValuedPathFacetCountCollector : PathFacetCountCollector
    {
        private readonly BigNestedInt32Array m_array;

        public MultiValuedPathFacetCountCollector(string name, string sep, BrowseSelection sel, 
            FacetSpec ospec, FacetDataCache dataCache)
            : base(name, sep, sel, ospec, dataCache)
        {
            m_array = ((MultiValueFacetDataCache)(dataCache)).NestedArray;
        }

        public override sealed void Collect(int docid) 
        {
            m_array.CountNoReturn(docid, m_count);
        }

        public override sealed void CollectAll()
        {
            m_count = BigInt32Array.FromArray(m_dataCache.Freqs);
        }
    }
}
