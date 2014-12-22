//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Written in Java.
//* 
//* Copyright (C) 2005-2006  John Wang
//*
//* This library is free software; you can redistribute it and/or
//* modify it under the terms of the GNU Lesser General Public
//* License as published by the Free Software Foundation; either
//* version 2.1 of the License, or (at your option) any later version.
//*
//* This library is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class FacetHitcountComparatorFactory : IComparatorFactory
    {
        private class FacetHitComparer : IComparer<int>
        {
            internal int[] counts;

            public int Compare(int f1, int f2)
            {
                int val = counts[f1] - counts[f2];
                if (val == 0)
                {
                    val = f2 - f1;
                }
                return val;
            }
        }

        public virtual IComparer<int> NewComparator(IFieldValueAccessor valueList, int[] counts)
        {
            return new FacetHitComparer { counts = counts };
        }

        private class DefaultFacetHitsComparer : IComparer<BrowseFacet>
        {
            public int Compare(BrowseFacet f1, BrowseFacet f2)
            {
                int val = f2.HitCount - f1.HitCount;
                if (val == 0)
                {
                    val = f1.Value.CompareTo(f2.Value);
                }
                return val;
            }
        }

        public static IComparer<BrowseFacet> FACET_HITS_COMPARATOR = new DefaultFacetHitsComparer();

        public IComparer<BrowseFacet> NewComparator()
        {
            return FACET_HITS_COMPARATOR;
        }
    }
}
