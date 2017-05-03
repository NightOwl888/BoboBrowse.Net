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
    using System;
    using System.Collections.Generic;

    public interface IFacetAccessible : IDisposable
    {
        ///<summary>Gets gathered top facets </summary>
        ///<returns>list of facets </returns>
        ICollection<BrowseFacet> GetFacets();

        ///<summary>Gets the facet given a value. This is a way for random accessing into the facet data structure. </summary>
        ///<param name="value">Facet value </param>
        ///<returns>a facet with count filled in </returns>
        BrowseFacet GetFacet(string value);

        /// <summary>
        /// Gets the facet count given a value. This is a way for random
        /// accessing the facet count.
        /// </summary>
        /// <param name="value">Facet value</param>
        /// <returns></returns>
        int GetFacetHitsCount(object value);

        /// <summary>
        /// Returns an iterator to visit all the facets
        /// </summary>
        FacetIterator GetIterator();
    }
}
