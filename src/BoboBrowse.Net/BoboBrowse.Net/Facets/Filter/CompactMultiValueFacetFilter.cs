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

    public class CompactMultiValueFacetFilter : RandomAccessFilter
    {
        private readonly FacetHandler<FacetDataCache> m_facetHandler;

        private readonly string[] m_vals;

        public CompactMultiValueFacetFilter(FacetHandler<FacetDataCache> facetHandler, string val)
            : this(facetHandler, new string[] { val })
        {
        }

        public CompactMultiValueFacetFilter(FacetHandler<FacetDataCache> facetHandler, string[] vals)
        {
            m_facetHandler = facetHandler;
            m_vals = vals;
        }

        public override double GetFacetSelectivity(BoboSegmentReader reader)
        {
            double selectivity = 0;
            FacetDataCache dataCache = m_facetHandler.GetFacetData<FacetDataCache>(reader);
            int[] idxes = FacetDataCache.Convert(dataCache, m_vals);
            if(idxes == null)
            {
                return 0.0;
            }
            int accumFreq = 0;
            foreach (int idx in idxes)
            {
                accumFreq += dataCache.Freqs[idx];
            }
            int total = reader.MaxDoc;
            selectivity = (double)accumFreq / (double)total;
            if (selectivity > 0.999) 
            {
                selectivity = 1.0;
            }
            return selectivity;
        }

        private sealed class CompactMultiValueFacetDocIdSetIterator : DocIdSetIterator
        {
            private readonly int m_bits;
            private int m_doc;
            private readonly int m_maxID;
            private readonly BigSegmentedArray m_orderArray;

            public CompactMultiValueFacetDocIdSetIterator(FacetDataCache dataCache, int[] index, int bits)
            {
                m_bits = bits;
                m_doc = int.MaxValue;
                m_maxID = -1;
                m_orderArray = dataCache.OrderArray;
                foreach (int i in index)
                {
                    if (m_doc > dataCache.MinIDs[i])
                    {
                        m_doc = dataCache.MinIDs[i];
                    }
                    if (m_maxID < dataCache.MaxIDs[i])
                    {
                        m_maxID = dataCache.MaxIDs[i];
                    }
                }
                m_doc--;
                if (m_doc < 0)
                {
                    m_doc = -1;
                }
            }

            public sealed override int DocID
            {
                get { return m_doc; }
            }

            public sealed override int NextDoc()
            {
                m_doc = (m_doc < m_maxID) ? m_orderArray.FindBits(m_bits, (m_doc + 1), m_maxID) : NO_MORE_DOCS;
                return m_doc;
            }

            public sealed override int Advance(int id)
            {
                if (m_doc < id)
                {
                    m_doc = (id <= m_maxID) ? m_orderArray.FindBits(m_bits, id, m_maxID) : NO_MORE_DOCS;
                    return m_doc;
                }
                return NextDoc();
            }

            public override long GetCost()
            {
                return 0;
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboSegmentReader reader)
        {
            FacetDataCache dataCache = m_facetHandler.GetFacetData<FacetDataCache>(reader);
            int[] indexes = FacetDataCache.Convert(dataCache, m_vals);

            int bits;

            bits = 0x0;
            foreach (int i in indexes)
            {
                bits |= 0x00000001 << (i - 1);
            }

            int finalBits = bits;

            BigSegmentedArray orderArray = dataCache.OrderArray;

            if (indexes.Length == 0)
            {
                return EmptyDocIdSet.Instance;
            }
            else
            {
                return new CompactMultiValueFacetFilterDocIdSet(dataCache, indexes, finalBits, orderArray);
            }
        }

        private class CompactMultiValueFacetFilterDocIdSet : RandomAccessDocIdSet
        {
            private readonly FacetDataCache m_dataCache;
            private readonly int[] m_indexes;
            private readonly int m_finalBits;
            private readonly BigSegmentedArray m_orderArray;

            public CompactMultiValueFacetFilterDocIdSet(FacetDataCache dataCache, int[] indexes, int finalBits, BigSegmentedArray orderArray)
            {
                this.m_dataCache = dataCache;
                this.m_indexes = indexes;
                this.m_finalBits = finalBits;
                this.m_orderArray = orderArray;
            }

            public override DocIdSetIterator GetIterator()
            {
                return new CompactMultiValueFacetDocIdSetIterator(this.m_dataCache, this.m_indexes, this.m_finalBits);
            }

            public override bool Get(int docId)
            {
                return (m_orderArray.Get(docId) & this.m_finalBits) != 0x0;
            }
        } 
    }
}
