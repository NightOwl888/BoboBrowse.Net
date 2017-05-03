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
namespace BoboBrowse.Net.Facets
{
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Collects facet counts for a given browse request
    /// </summary>
    public interface IFacetCountCollector : IFacetAccessible
    {
        ///<summary>Collect a hit. This is called for every hit, thus the implementation needs to be super-optimized.</summary>
        ///<param name="docid"> doc </param>
        void Collect(int docid);

        ///<summary>Collects all hits. This is called once per request by the facet engine in certain scenarios.</summary>
        void CollectAll();

        ///<summary>Gets the name of the facet </summary>
        ///<returns>facet name </returns>
        string Name { get; }

        ///<summary>Returns an integer array representing the distribution function of a given facet.</summary>
        ///<returns><see cref="T:BoboBrowse.Net.Util.BigSegmentedArray"/> of count values representing distribution of the facet values.</returns>
        BigSegmentedArray GetCountDistribution();
    }

    public static class FacetCountCollector
    {
        ///<summary>Empty facet list.  </summary>
        public static List<BrowseFacet> EMPTY_FACET_LIST = new List<BrowseFacet>();
    }
}
