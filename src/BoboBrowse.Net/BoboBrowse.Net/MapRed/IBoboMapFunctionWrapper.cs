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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.MapRed
{
    using BoboBrowse.Net.Facets;

    /// <summary>
    /// Is the part of the bobo request, that maintains the map result intermediate state
    /// </summary>
    public interface IBoboMapFunctionWrapper
    {
        /// <summary>
        /// When there is no filter, map reduce will try to map the entire segment
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="facetCountCollectors"></param>
        void MapFullIndexReader(BoboSegmentReader reader, IFacetCountCollector[] facetCountCollectors);

        /// <summary>
        /// The basic callback method for a single doc
        /// </summary>
        /// <param name="docId"></param>
        /// <param name="reader"></param>
        void MapSingleDocument(int docId, BoboSegmentReader reader);

        /// <summary>
        /// The callback method, after the segment was processed
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="facetCountCollectors"></param>
        void FinalizeSegment(BoboSegmentReader reader, IFacetCountCollector[] facetCountCollectors);

        /// <summary>
        /// The callback method, after the partition was processed
        /// </summary>
        void FinalizePartition();

        MapReduceResult Result { get; }
    }
}
