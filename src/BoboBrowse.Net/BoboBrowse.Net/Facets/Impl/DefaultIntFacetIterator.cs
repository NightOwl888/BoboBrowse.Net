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
    /// NOTE: This was DefaultIntFacetIterator in bobo-browse
    /// </summary>
    public class DefaultInt32FacetIterator : Int32FacetIterator
    {
        private readonly TermInt32List m_valList;
        private readonly BigSegmentedArray _count;
        private readonly int m_countlength;
        private readonly int m_countLengthMinusOne;
        private int m_index;

        public DefaultInt32FacetIterator(TermInt32List valList, BigSegmentedArray countarray, int countlength, bool zeroBased)
        {
            m_valList = valList;
            _count = countarray;
            m_countlength = countlength;
            m_countLengthMinusOne = countlength - 1;
            m_index = -1;
            if (!zeroBased)
                m_index++;
            m_facet = TermInt32List.VALUE_MISSING;
            base.m_count = 0;
        }

        /// <summary>
        /// Added in .NET version as as an accessor to the _valList field.
        /// </summary>
        public virtual TermInt32List ValList
        {
            get { return m_valList; }
        }

        new public virtual string Facet
        {
            get
            {
                if (m_facet == -1) return null;
                return m_valList.Format(m_facet);
            }
        }

        public override string Format(int val)
        {
            return m_valList.Format(val);
        }

        public override string Format(object val)
        {
            return m_valList.Format(val);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#getFacetCount()
        /// </summary>
        public virtual int FacetCount
        {
            get { return base.m_count; }
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
            if ((m_index >= 0) && (m_index >= m_countLengthMinusOne))
                throw new IndexOutOfRangeException("No more facets in this iteration");
            m_index++;
            m_facet = m_valList.GetPrimitiveValue(m_index);
            base.m_count = _count.Get(m_index);
            return m_valList.Get(m_index);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.IntFacetIterator#nextInt()
        /// </summary>
        /// <returns></returns>
        public override int NextInt32()
        {
            if (m_index >= m_countLengthMinusOne)
                throw new IndexOutOfRangeException("No more facets in this iteration");
            m_index++;
            m_facet = m_valList.GetPrimitiveValue(m_index);
            base.m_count = _count.Get(m_index);
            return m_facet;
        }

        // BoboBrowse.Net: Not supported in .NET anyway
        //public override void Remove()
        //{
        //    throw new NotSupportedException("remove() method not supported for Facet Iterators");
        //}

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
                if (_count.Get(m_index) >= minHits)
                {
                    m_facet = m_valList.GetPrimitiveValue(m_index);
                    base.m_count = _count.Get(m_index);
                    return m_valList.Format(m_facet);
                }
            }
            m_facet = TermInt32List.VALUE_MISSING;
            base.m_count = 0;
            return null;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.IntFacetIterator#nextInt(int)
        /// </summary>
        /// <param name="minHits"></param>
        /// <returns></returns>
        public override int NextInt32(int minHits)
        {
            while (++m_index < m_countlength)
            {
                if (_count.Get(m_index) >= minHits)
                {
                    m_facet = m_valList.GetPrimitiveValue(m_index);
                    base.m_count = _count.Get(m_index);
                    return m_facet;
                }
            }
            m_facet = TermInt32List.VALUE_MISSING;
            base.m_count = 0;
            return m_facet;    
        }
    }
}
