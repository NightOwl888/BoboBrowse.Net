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
    using BoboBrowse.Net.Facets;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;

    public interface IBrowsable : Searchable
    {
        void Browse(BrowseRequest req, Collector hitCollector, Dictionary<string, IFacetAccessible> facets); // throws BrowseException;

        BrowseResult Browse(BrowseRequest req); // throws BrowseException;

        void SetFacetHandler(FacetHandler facetHandler); // throws IOException;

        FacetHandler GetFacetHandler(string name);

        //Similarity GetSimilarity();

        //void SetSimilarity(Similarity similarity);

        string[] GetFieldVal(int docid, string fieldname); // throws IOException;

        object[] GetRawFieldVal(int docid, string fieldname); // throws IOException;

        int NumDocs();

        Explanation Explain(Lucene.Net.Search.Query q, int docid); // throws IOException;

        TopDocsSortedHitCollector GetSortedHitCollector(SortField[] sort, int offset, int count, bool fetchStoredFields);
    }
}
