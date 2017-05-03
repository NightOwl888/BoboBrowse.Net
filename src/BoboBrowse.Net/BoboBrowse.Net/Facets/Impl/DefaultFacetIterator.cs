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
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Util;
    using System;

    /// <summary>
    /// author nnarkhed
    /// </summary>
    public class DefaultFacetIterator : FacetIterator
    {
        private readonly ITermValueList m_valList;
        new private readonly BigSegmentedArray m_count;
        private readonly int m_countlength;
        private readonly int m_countLengthMinusOne;
        private int m_index;


        public DefaultFacetIterator(ITermValueList valList, BigSegmentedArray countarray, int countlength, bool zeroBased)
        {
            m_valList = valList;
            m_count = countarray;
            m_countlength = countlength;
            m_index = -1;
            m_countLengthMinusOne = m_countlength - 1;
            if (!zeroBased)
                m_index++;
            m_facet = null;
            base.m_count = 0;
        }

        /// <summary>
        /// Added in .NET version as as an accessor to the _valList field.
        /// </summary>
        public virtual ITermValueList ValList
        {
            get { return m_valList; }
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#hasNext()
        /// </summary>
        /// <returns></returns>
        public override bool HasNext()
        {
            return (m_index < m_countLengthMinusOne);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#next()
        /// </summary>
        /// <returns></returns>
        public override string Next()
        {
            m_index++;
            m_facet = Convert.ToString(m_valList.GetRawValue(m_index));
            base.m_count = m_count.Get(m_index);
            return Format(m_facet);
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
            while (++m_index < m_countlength)
            {
                if (m_count.Get(m_index) >= minHits)
                {
                    m_facet = Convert.ToString(m_valList.GetRawValue(m_index));
                    base.m_count = m_count.Get(m_index);
                    return Format(m_facet);
                }
            }
            m_facet = null;
            base.m_count = 0;
            return null;   
        }

        public override string Format(object val)
        {
            return m_valList.Format(val);
        }
    }
}
