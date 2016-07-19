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

        public virtual FacetIterator GetIterator()
        {
            return new PathFacetIterator(_facets);
        }
    }
}
