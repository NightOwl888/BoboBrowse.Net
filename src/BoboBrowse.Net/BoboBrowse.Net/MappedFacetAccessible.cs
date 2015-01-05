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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [Serializable]
    public class MappedFacetAccessible : IFacetAccessible
    {
        private const long serialVersionUID = 1L;

        private readonly IDictionary<object, BrowseFacet> _facetMap;
        private readonly BrowseFacet[] _facets;

        public MappedFacetAccessible(BrowseFacet[] facets)
        {
            _facetMap = new Dictionary<object, BrowseFacet>();
            foreach (BrowseFacet facet in facets)
            {
                _facetMap.Put(facet.Value, facet);
            }
            _facets = facets;
        }

        public virtual BrowseFacet GetFacet(string value)
        {
            return _facetMap.Get(value);
        }

        public virtual int GetFacetHitsCount(object value)
        {
            BrowseFacet facet = _facetMap.Get(value);
            if (facet != null)
                return facet.FacetValueHitCount;
            return 0;
        }

        public virtual IEnumerable<BrowseFacet> GetFacets()
        {
            return _facets.ToList();
        }

        public virtual void Dispose()
        { }

        public virtual FacetIterator Iterator()
        {
            return new PathFacetIterator(_facets);
        }
    }
}
