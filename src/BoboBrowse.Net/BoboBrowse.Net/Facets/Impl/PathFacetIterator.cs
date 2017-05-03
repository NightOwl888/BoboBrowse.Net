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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Facets.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// author nnarkhed
    /// </summary>
    public class PathFacetIterator : FacetIterator
    {
        private readonly BrowseFacet[] m_facets;
        private int m_index;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="facets">a value ascending sorted list of BrowseFacets</param>
        public PathFacetIterator(IList<BrowseFacet> facets)
        {
            m_facets = facets.ToArray();
            m_index = -1;
            m_facet = null;
            m_count = 0;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#next()
        /// </summary>
        /// <returns></returns>
        public override string Next()
        {
            if ((m_index >= 0) && !HasNext())
                throw new IndexOutOfRangeException("No more facets in this iteration");
            m_index++;
            m_facet = m_facets[m_index].Value;
            m_count = m_facets[m_index].FacetValueHitCount;
            return m_facet;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#hasNext()
        /// </summary>
        /// <returns></returns>
        public override bool HasNext()
        {
            return ((m_index >= 0) && (m_index < (m_facets.Length - 1)));
        }


        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#remove()
        /// </summary>
        public override void Remove()
        {
            throw new NotSupportedException("remove() method not supported for Facet Iterators");
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#next(int)
        /// </summary>
        /// <param name="minHits"></param>
        /// <returns></returns>
        public override string Next(int minHits)
        {
            while (++m_index < m_facets.Length)
            {
                if (m_facets[m_index].FacetValueHitCount >= minHits)
                {
                    m_facet = m_facets[m_index].Value;
                    m_count = m_facets[m_index].FacetValueHitCount;
                    return m_facet;
                }
            }
            m_facet = null;
            m_count = 0;
            return m_facet; 
        }

        /// <summary>
        /// The string from here should be already formatted. No need to reformat.
        /// see com.browseengine.bobo.api.FacetIterator#format(java.lang.Object)
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public override string Format(object val)
        {
            return Convert.ToString(val);
        }
    }
}
