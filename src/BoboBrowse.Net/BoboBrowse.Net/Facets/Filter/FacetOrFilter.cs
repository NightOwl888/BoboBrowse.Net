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
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;
    using Lucene.Net.Util;

    public class FacetOrFilter : RandomAccessFilter
    {
        protected readonly IFacetHandler m_facetHandler;
        protected readonly string[] m_vals;
        private readonly bool m_takeCompliment;
        private readonly IFacetValueConverter m_valueConverter;

        public FacetOrFilter(IFacetHandler facetHandler, string[] vals, bool takeCompliment)
            : this(facetHandler, vals, takeCompliment, FacetValueConverter_Fields.DEFAULT)
        {
        }

        public FacetOrFilter(IFacetHandler facetHandler, string[] vals, bool takeCompliment, IFacetValueConverter valueConverter)
        {
            m_facetHandler = facetHandler;
            m_vals = vals;
            m_takeCompliment = takeCompliment;
            m_valueConverter = valueConverter;
        }

        public override double GetFacetSelectivity(BoboSegmentReader reader)
        {
            double selectivity = 0;
            FacetDataCache dataCache = m_facetHandler.GetFacetData<FacetDataCache>(reader);
            int accumFreq = 0;
            foreach (string val in m_vals)
            {
                int idx = dataCache.ValArray.IndexOf(val);
                if (idx < 0)
                {
                    continue;
                }
                accumFreq += dataCache.Freqs[idx];
            }
            int total = reader.MaxDoc;
            selectivity = (double)accumFreq / (double)total;
            if (selectivity > 0.999)
            {
                selectivity = 1.0;
            }
            if (m_takeCompliment)
            {
                selectivity = 1.0 - selectivity;
            }
            return selectivity;
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboSegmentReader reader)
        {
            if (m_vals.Length == 0)
            {
                return EmptyDocIdSet.Instance;
            }
            else
            {
                return new FacetOrRandomAccessDocIdSet(m_facetHandler, reader, m_vals, m_valueConverter, m_takeCompliment);
            }
        }

        public class FacetOrRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly OpenBitSet m_bitset;
	        private readonly BigSegmentedArray m_orderArray;
	        private readonly FacetDataCache m_dataCache;
            private readonly int[] m_index;

            internal FacetOrRandomAccessDocIdSet(IFacetHandler facetHandler, BoboSegmentReader reader, 
                string[] vals, IFacetValueConverter valConverter, bool takeCompliment)
            {
		        m_dataCache = facetHandler.GetFacetData<FacetDataCache>(reader);
		        m_orderArray = m_dataCache.OrderArray;
	            m_index = valConverter.Convert(m_dataCache, vals);
	    
	            m_bitset = new OpenBitSet(m_dataCache.ValArray.Count);
	            foreach (int i in m_index)
	            {
	              m_bitset.FastSet(i);
	            }
      
                if (takeCompliment)
                {
                    // flip the bits
                    for (int i = 0; i < m_dataCache.ValArray.Count; ++i)
                    {
                        m_bitset.FastFlip(i);
                    }
                }
	        }

            public override bool Get(int docId)
            {
                return m_bitset.FastGet(m_orderArray.Get(docId));
            }

            public override DocIdSetIterator GetIterator()
            {
                return new FacetOrDocIdSetIterator(m_dataCache, m_bitset);
            }
        }

        public class FacetOrDocIdSetIterator : DocIdSetIterator
        {
            protected int m_doc;
            protected readonly FacetDataCache m_dataCache;
            protected int m_maxID;
            protected readonly OpenBitSet m_bitset;
            protected readonly BigSegmentedArray m_orderArray;

            public FacetOrDocIdSetIterator(FacetDataCache dataCache, OpenBitSet bitset)
            {
                m_dataCache = dataCache;
                m_orderArray = dataCache.OrderArray;
                m_bitset = bitset;

                m_doc = int.MaxValue;
                m_maxID = -1;
                int size = m_dataCache.ValArray.Count;
                for (int i = 0; i < size; ++i)
                {
                    if (!bitset.FastGet(i))
                    {
                        continue;
                    }
                    if (m_doc > m_dataCache.MinIDs[i])
                    {
                        m_doc = m_dataCache.MinIDs[i];
                    }
                    if (m_maxID < m_dataCache.MaxIDs[i])
                    {
                        m_maxID = m_dataCache.MaxIDs[i];
                    }
                }
                m_doc--;
                if (m_doc < 0)
                    m_doc = -1;
            }

            public override int DocID
            {
                get { return m_doc; }
            }

            public override int NextDoc()
            {
                m_doc = (m_doc < m_maxID) ? m_orderArray.FindValues(m_bitset, m_doc + 1, m_maxID) : NO_MORE_DOCS;
                return m_doc;
            }

            public override int Advance(int id)
            {
                if (m_doc < id)
                {
                    m_doc = (id <= m_maxID) ? m_orderArray.FindValues(m_bitset, id, m_maxID) : NO_MORE_DOCS;
                    return m_doc;
                }
                return NextDoc();
            }

            public override long GetCost()
            {
                return 0;
            }
        }
    }
}
