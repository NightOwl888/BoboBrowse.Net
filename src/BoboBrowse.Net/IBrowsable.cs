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
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Sort;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;

    public interface IBrowsable : Searchable
    {
        void Browse(BrowseRequest req, 
	        Collector hitCollector,
	        IDictionary<string, IFacetAccessible> facets);

        void Browse(BrowseRequest req, 
	        Collector hitCollector,
	        IDictionary<string, IFacetAccessible> facets,
	        int start);

        void Browse(BrowseRequest req, 
	        Weight weight,
	        Collector hitCollector,
	        IDictionary<string, IFacetAccessible> facets,
	        int start);

        BrowseResult Browse(BrowseRequest req);

        IEnumerable<string> FacetNames { get; }

        void SetFacetHandler(IFacetHandler facetHandler);

        IFacetHandler GetFacetHandler(string name);

        Similarity Similarity { get; set; }

        string[] GetFieldVal(int docid, string fieldname);

        object[] GetRawFieldVal(int docid, string fieldname);

        int NumDocs();

        SortCollector GetSortCollector(SortField[] sort, Lucene.Net.Search.Query q, int offset, int count, bool fetchStoredFields, IEnumerable<string> termVectorsToFetch, bool forceScoring, string[] groupBy, int maxPerGroup, bool collectDocIdCache);

        Explanation Explain(Lucene.Net.Search.Query q, int docid);

        IDictionary<string, IFacetHandler> FacetHandlerMap { get; }
    }
}
