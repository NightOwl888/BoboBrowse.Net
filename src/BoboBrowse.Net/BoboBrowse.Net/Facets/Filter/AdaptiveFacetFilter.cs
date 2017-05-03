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
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets.Data;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System.Collections.Generic;
    using System.Linq;

    public interface IFacetDataCacheBuilder
    {
        FacetDataCache Build(BoboSegmentReader reader);
        string Name { get; }
        string IndexFieldName { get; }
    }

    public class AdaptiveFacetFilter : RandomAccessFilter
    {
        private readonly RandomAccessFilter m_facetFilter;
	    private readonly IFacetDataCacheBuilder m_facetDataCacheBuilder;
        private readonly IList<string> m_valSet;
	    private bool m_takeComplement = false;

        /// <summary>
        /// If takeComplement is true, we still return the filter for NotValues.
        /// Therefore, the calling function of this class needs to apply NotFilter on top
        /// of this filter if takeComplement is true.
        /// </summary>
        /// <param name="facetDataCacheBuilder"></param>
        /// <param name="facetFilter"></param>
        /// <param name="val"></param>
        /// <param name="takeComplement"></param>
        public AdaptiveFacetFilter(IFacetDataCacheBuilder facetDataCacheBuilder, RandomAccessFilter facetFilter, string[] val, bool takeComplement)
        {
            m_facetFilter = facetFilter;
            m_facetDataCacheBuilder = facetDataCacheBuilder;
            m_valSet = val;
            m_takeComplement = takeComplement;
        }

        public override double GetFacetSelectivity(BoboSegmentReader reader)
        {
            double selectivity = m_facetFilter.GetFacetSelectivity(reader);
            if (m_takeComplement)
                return 1.0 - selectivity;
            return selectivity;
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboSegmentReader reader)
        {
            RandomAccessDocIdSet innerDocSet = m_facetFilter.GetRandomAccessDocIdSet(reader);
            if (innerDocSet == EmptyDocIdSet.Instance)
            {
                return innerDocSet;
            }

            FacetDataCache dataCache = m_facetDataCacheBuilder.Build(reader);
            int totalCount = reader.MaxDoc;
            ITermValueList valArray = dataCache.ValArray;
            int freqCount = 0;

            var validVals = new List<string>(m_valSet.Count);
            foreach (string val in m_valSet)
            {
                int idx = valArray.IndexOf(val);
                if (idx >= 0)
                {
                    validVals.Add(valArray.Get(idx));  // get and format the value
                    freqCount += dataCache.Freqs[idx];
                }
            }

            if (validVals.Count == 0)
            {
                return EmptyDocIdSet.Instance;
            }

            // takeComplement is only used to choose between TermListRandomAccessDocIdSet and innerDocSet
            int validFreqCount = m_takeComplement ? (totalCount - freqCount) : freqCount;

            if (m_facetDataCacheBuilder.IndexFieldName != null && ((validFreqCount << 1) < totalCount))
            {
                return new TermListRandomAccessDocIdSet(m_facetDataCacheBuilder.IndexFieldName, innerDocSet, validVals, reader);
            }
            else
            {
                return innerDocSet;
            }
        }

        public class TermListRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly RandomAccessDocIdSet m_innerSet;
		    private readonly IList<string> m_vals;
		    private readonly AtomicReader m_reader;
		    private readonly string m_name;
            private const int OR_THRESHOLD = 5;

            internal TermListRandomAccessDocIdSet(string name, RandomAccessDocIdSet innerSet, IList<string> vals, AtomicReader reader)
            {
                m_name = name;
                m_innerSet = innerSet;
                m_vals = vals;
                m_reader = reader;
            }

            public class TermDocIdSet : DocIdSet
            {
                private readonly Term m_term;
                private readonly AtomicReader m_reader;

                public TermDocIdSet(AtomicReader reader, string name, string val)
                {
                    this.m_reader = reader;
                    m_term = new Term(name, val);
                }

                public override DocIdSetIterator GetIterator()
                {
                    DocsEnum docsEnum = m_reader.GetTermDocsEnum(m_term);
                    if (docsEnum == null)
                    {
                        return EmptyDocIdSet.Instance.GetIterator();
                    }
                    return docsEnum;
                }
            }

            public override bool Get(int docId)
            {
                return m_innerSet.Get(docId);
            }

            public override DocIdSetIterator GetIterator()
            {
                if (m_vals.Count == 0)
                {
                    return EmptyDocIdSet.Instance.GetIterator();
                }
                if (m_vals.Count == 1)
                {
                    return new TermDocIdSet(m_reader, m_name, m_vals[0]).GetIterator();
                }
                else
                {
                    if (m_vals.Count < OR_THRESHOLD)
                    {
                        List<DocIdSet> docSetList = new List<DocIdSet>(m_vals.Count);
                        foreach (string val in m_vals)
                        {
                            docSetList.Add(new TermDocIdSet(m_reader, m_name, val));
                        }
                        return new OrDocIdSet(docSetList).GetIterator();
                    }
                    else
                    {
                        return m_innerSet.GetIterator();
                    }
                }
            }
        }
    }
}
