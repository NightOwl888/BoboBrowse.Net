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

    public interface IFacetAccessible
    {
        ///<summary>Gets gathered top facets </summary>
        ///<returns>list of facets </returns>
        IEnumerable<BrowseFacet> GetFacets();

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
        /// Responsible for release resources used. If the implementing class
        /// does not use a lot of resources,
        /// it does not have to do anything.
        /// </summary>
        void Close();

        /// <summary>
        /// Returns an iterator to visit all the facets
        /// </summary>
        FacetIterator Iterator();
    }
}
