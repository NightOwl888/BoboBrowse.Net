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
namespace BoboBrowse.Net.Facets.Attribute
{
    using System;
    using System.Collections.Generic;

    public class AttributesFacetIterator : FacetIterator
    {
        private readonly IEnumerator<BrowseFacet> iterator;

        public AttributesFacetIterator(IEnumerable<BrowseFacet> facets)
        {
            iterator = facets.GetEnumerator();
        }

        public override bool HasNext()
        {
            return iterator.MoveNext();
        }

        // BoboBrowse.Net: Not supported in .NET anyway
        //public override void Remove()
        //{
        //    throw new NotSupportedException();
        //}

        public override string Next()
        {
            m_count = 0;
            BrowseFacet next = iterator.Current;
            if (next == null)
            {
                return null;
            }
            m_count = next.FacetValueHitCount;
            m_facet = next.Value;
            return next.Value;
        }

        public override string Next(int minHits)
        {
            while (iterator.MoveNext())
            {
                BrowseFacet next = iterator.Current;
                base.m_count = next.FacetValueHitCount;
                base.m_facet = next.Value;
                if (next.FacetValueHitCount >= minHits)
                {
                    return next.Value;
                }
            }
            return null;
        }

        public override string Format(object val)
        {
            return val != null ? val.ToString() : null;
        }
    }
}
