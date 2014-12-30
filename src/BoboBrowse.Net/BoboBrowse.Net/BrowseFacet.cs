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
namespace BoboBrowse.Net
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This class represents a facet
    /// </summary>
    [Serializable]
    public class BrowseFacet
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private string _value;
        private int _hitcount;

        public BrowseFacet() { }

        public BrowseFacet(string value, int hitCount)
        {
            _value = value;
            _hitcount = hitCount;
        }

        /// <summary>
        /// Gets or sets the facet value
        /// </summary>
        public string Value 
        {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// Gets or sets the hit count
        /// </summary>
        [Obsolete("Use FacetValueHitCount instead")]
        public int HitCount
        {
            get { return _hitcount; }
            set { _hitcount = value; }
        }

        /// <summary>
        /// Gets or sets the hit count
        /// </summary>
        public int FacetValueHitCount
        {
            get { return _hitcount; }
            set { _hitcount = value; }
        }

        public override string ToString()
        {
            return string.Concat(Value, "(", _hitcount, ")");
        }

        public override bool Equals(object obj)
        {
            bool equals = false;

            if (obj is BrowseFacet)
            {
                BrowseFacet c2 = (BrowseFacet)obj;
                if (_hitcount == c2._hitcount && _value.Equals(c2._value))
                {
                    equals = true;
                }
            }
            return equals;
        }

        public virtual IEnumerable<BrowseFacet> Merge(IEnumerable<BrowseFacet> v, IComparer<BrowseFacet> comparator)
        {
            int i = 0;
            foreach (var facet in v)
            {
                int val = comparator.Compare(this, facet);
                if (val == 0)
                {
                    facet._hitcount += _hitcount;
                    return v;
                }
                i++;
            }
            var result = new List<BrowseFacet>(v);
            result.Add(this);
            return result;
        }
    }
}
