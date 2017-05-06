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

    public class MultiValueORFacetFilter : RandomAccessFilter
    {
        private readonly IFacetHandler m_facetHandler;
        private readonly string[] m_vals;
        private readonly bool m_takeCompliment;
        private readonly IFacetValueConverter m_valueConverter;

        public MultiValueORFacetFilter(IFacetHandler facetHandler, string[] vals, bool takeCompliment)
            : this(facetHandler, vals, FacetValueConverter_Fields.DEFAULT, takeCompliment)
        {}
  
        public MultiValueORFacetFilter(IFacetHandler facetHandler, string[] vals, IFacetValueConverter valueConverter, bool takeCompliment)
        {
            m_facetHandler = facetHandler;
            m_vals = vals;
            m_valueConverter = valueConverter;
            m_takeCompliment = takeCompliment;
        }

        public override double GetFacetSelectivity(BoboSegmentReader reader)
        {
            double selectivity = 0;
            MultiValueFacetDataCache dataCache = m_facetHandler.GetFacetData<MultiValueFacetDataCache>(reader);
            int[] idxes = m_valueConverter.Convert(dataCache, m_vals);
            if (idxes == null)
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


        public sealed class MultiValueOrFacetDocIdSetIterator : FacetOrFilter.FacetOrDocIdSetIterator
        {
            private readonly BigNestedInt32Array m_nestedArray;
            public MultiValueOrFacetDocIdSetIterator(MultiValueFacetDataCache dataCache, OpenBitSet bs)
                : base(dataCache, bs)
            {
                m_nestedArray = dataCache.NestedArray;
            }           

            public override int NextDoc()
            {
                m_doc = (m_doc < m_maxID) ? m_nestedArray.FindValues(m_bitset, (m_doc + 1), m_maxID) : NO_MORE_DOCS;
                return m_doc;
            }

            public override int Advance(int id)
            {
                if (m_doc < id)
                {
                    m_doc = (id <= m_maxID) ? m_nestedArray.FindValues(m_bitset, id, m_maxID) : NO_MORE_DOCS;
                    return m_doc;
                }
                return NextDoc();
            }            
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboSegmentReader reader)
        {
            MultiValueFacetDataCache dataCache = m_facetHandler.GetFacetData<MultiValueFacetDataCache>(reader);
            int[] index = m_valueConverter.Convert(dataCache, m_vals);
            //BigNestedIntArray nestedArray = dataCache.NestedArray;
            OpenBitSet bitset = new OpenBitSet(dataCache.ValArray.Count);

            foreach (int i in index)
            {
                bitset.FastSet(i);
            }

            if (m_takeCompliment)
            {
                // flip the bits
                int size = dataCache.ValArray.Count;
                for (int i = 0; i < size; ++i)
                {
                    bitset.FastFlip(i);
                }
            }

            long count = bitset.Cardinality();

            if (count == 0)
            {
                return new EmptyRandomAccessDocIdSet();
            }
            else
            {
                return new MultiRandomAccessDocIdSet(dataCache, bitset);
            }
        }

        private class EmptyRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private DocIdSet m_empty = EmptyDocIdSet.Instance;

            public override bool Get(int docId)
            {
                return false;
            }

            public override DocIdSetIterator GetIterator()
            {
                return m_empty.GetIterator();
            }
        }

        private class MultiRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly MultiValueFacetDataCache m_dataCache;
            private readonly OpenBitSet m_bitset;
            private readonly BigNestedInt32Array m_nestedArray;

            public MultiRandomAccessDocIdSet(MultiValueFacetDataCache dataCache, OpenBitSet bitset)
            {
                this.m_dataCache = dataCache;
                this.m_bitset = bitset;
                this.m_nestedArray = dataCache.NestedArray;
            }

            public override DocIdSetIterator GetIterator()
            {
                return new MultiValueOrFacetDocIdSetIterator(this.m_dataCache, this.m_bitset);
            }

            public override bool Get(int docId)
            {
                return this.m_nestedArray.Contains(docId, this.m_bitset);
            }
        }
    }
}
