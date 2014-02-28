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

namespace BoboBrowse.Net
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class BrowseFacet
    {
        public BrowseFacet() { }

        public BrowseFacet(object value, int hitCount)
        {
            this.Value = value;
            this.HitCount = hitCount;
        }

        public virtual List<BrowseFacet> Merge(List<BrowseFacet> v, IComparer<BrowseFacet> comparator)
        {
            int i = 0;
            foreach (var facet in v)
            {
                int val = comparator.Compare(this, facet);
                if (val == 0)
                {
                    facet.HitCount += HitCount;
                    return v;
                }                
                i++;
            }
            v.Add(this);
            return v;
        }

        public override string ToString()
        {
            return string.Concat(Value, "(", HitCount, ")");
        }

        public override bool Equals(object obj)
        {
            bool equals = false;

            if (obj is BrowseFacet)
            {
                BrowseFacet c2 = (BrowseFacet)obj;
                if (HitCount == c2.HitCount && Value.Equals(c2.Value))
                {
                    equals = true;
                }
            }
            return equals;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public int HitCount { get; set; }

        public object Value { get; set; }
    }
}
