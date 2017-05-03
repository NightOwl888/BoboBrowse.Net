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
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Util;
    using System.Collections.Generic;

    public class FacetHitcountComparerFactory : IComparerFactory
    {
        private class FacetHitComparer : IComparer<int>
        {
            internal BigSegmentedArray m_counts;

            public virtual int Compare(int f1, int f2)
            {
                int val = m_counts.Get(f1) - m_counts.Get(f2);
                if (val == 0)
                {
                    val = f2 - f1;
                }
                return val;
            }
        }

        public virtual IComparer<int> NewComparer(IFieldValueAccessor valueList, BigSegmentedArray counts)
        {
            return new FacetHitComparer { m_counts = counts };
        }

        private class DefaultFacetHitsComparer : IComparer<BrowseFacet>
        {
            public virtual int Compare(BrowseFacet f1, BrowseFacet f2)
            {
                int val = f2.FacetValueHitCount - f1.FacetValueHitCount;
                if (val == 0)
                {
                    val = string.CompareOrdinal(f1.Value, f2.Value);
                }
                return val;
            }
        }

        public static IComparer<BrowseFacet> FACET_HITS_COMPARER = new DefaultFacetHitsComparer();

        public virtual IComparer<BrowseFacet> NewComparer()
        {
            return FACET_HITS_COMPARER;
        }
    }
}
