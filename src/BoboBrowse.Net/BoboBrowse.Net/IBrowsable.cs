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
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Sort;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Search.Similarities;
    using System;
    using System.Collections.Generic;

    public interface IBrowsable : IDisposable
    {
        /// <summary>
        /// Generates a merged BrowseResult from the supplied <see cref="T:BrowseRequest"/>.
        /// The results are put into a Lucene.Net <see cref="T:Lucene.Net.Search.Collector"/> and a <see cref="T:System.Collections.Generic.IDictionary{System.String, IFacetAccessible}"/>.
        /// </summary>
        /// <param name="req"><see cref="T:BrowseRequest"/> for generating the facets.</param>
        /// <param name="hitCollector">A <see cref="T:Lucene.Net.Search.Collector"/> for the hits generated during a search.</param>
        /// <param name="facets">A dictionary of all of the facet collections (output).</param>
        /// <param name="start">The offset value for the document number.</param>
        void Browse(BrowseRequest req, 
	        ICollector hitCollector,
	        IDictionary<string, IFacetAccessible> facets,
	        int start);

        /// <summary>
        /// Generates a merged BrowseResult from the supplied <see cref="T:BrowseRequest"/>.
        /// </summary>
        /// <param name="req"><see cref="T:BrowseRequest"/> for generating the facets.</param>
        /// <returns><see cref="T:BrowseResult"/> of the results corresponding to the <see cref="T:BrowseRequest"/>.</returns>
        BrowseResult Browse(BrowseRequest req);

        /// <summary>
        /// Gets the Index Reader.
        /// </summary>
        /// <returns></returns>
        IndexReader IndexReader { get; }

        /// <summary>
        /// Gets a set of facet names.
        /// </summary>
        /// <returns>set of facet names</returns>
        IEnumerable<string> FacetNames { get; }

        /// <summary>
        /// Sets a facet handler for each sub-browser instance.
        /// </summary>
        /// <param name="facetHandler">A facet handler.</param>
        void SetFacetHandler(IFacetHandler facetHandler);

        /// <summary>
        /// Gets a facet handler by facet name.
        /// </summary>
        /// <param name="name">The facet name.</param>
        /// <returns>The facet handler instance.</returns>
        IFacetHandler GetFacetHandler(string name);

        IDictionary<string, IFacetHandler> FacetHandlerMap { get; }

        Similarity Similarity { set; }

        /// <summary>
        /// Return the string representation of the values of a field for the given doc.
        /// </summary>
        /// <param name="docid">The document id.</param>
        /// <param name="fieldname">The field name.</param>
        /// <returns>A string array of field values.</returns>
        string[] GetFieldVal(int docid, string fieldname);

        /// <summary>
        /// Return the raw (primitive) field values for the given doc.
        /// </summary>
        /// <param name="docid">The document id.</param>
        /// <param name="fieldname">The field name.</param>
        /// <returns>An object array of raw field values.</returns>
        object[] GetRawFieldVal(int docid, string fieldname);

        /// <summary>
        /// Gets the total number of documents in all sub browser instances.
        /// </summary>
        /// <returns>The total number of documents.</returns>
        int NumDocs { get; }

        SortCollector GetSortCollector(SortField[] sort, Lucene.Net.Search.Query q, int offset, int count, 
            bool fetchStoredFields, ICollection<string> termVectorsToFetch, string[] groupBy, int maxPerGroup, 
            bool collectDocIdCache);

        Explanation Explain(Lucene.Net.Search.Query q, int docid);
    }
}
