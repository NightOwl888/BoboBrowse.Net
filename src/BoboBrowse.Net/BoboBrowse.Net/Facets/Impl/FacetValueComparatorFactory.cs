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
    using BoboBrowse.Net.Util;
    using System.Collections.Generic;

    public class FacetValueComparatorFactory : IComparatorFactory
    {
        public virtual IComparer<int> NewComparator(IFieldValueAccessor fieldValueAccessor, BigSegmentedArray counts)
        {
            return new FacetValueComparatorFactoryComparator();
        }

        private class FacetValueComparatorFactoryComparator : IComparer<int>
        {
            public virtual int Compare(int o1, int o2)
            {
                return o2 - o1;
            }
        }

        public virtual IComparer<BrowseFacet> NewComparator()
        {
            return new FacetValueComparatorFactoryBrowseFacetComparator();
        }

        private class FacetValueComparatorFactoryBrowseFacetComparator : IComparer<BrowseFacet>
        {
            public virtual int Compare(BrowseFacet o1, BrowseFacet o2)
            {
                return string.CompareOrdinal(o1.Value, o2.Value);
            }
        }
    }
}
