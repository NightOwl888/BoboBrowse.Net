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
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Query.Scoring;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.IO;
    
    /// <summary>
    /// Same as <see cref="T:MultiValueFacetHandler"/>, multiple values are allowed, but the total possible values are limited to 32. 
    /// This is more efficient than <see cref="T:MultiValueFacetHandler"/> and has a smaller memory footprint.
    /// </summary>
    public class CompactMultiValueFacetHandler : FacetHandler<FacetDataCache>, IFacetScoreable
    {
        private const int MAX_VAL_COUNT = 32;
        private readonly TermListFactory m_termListFactory;
        private readonly string m_indexFieldName;

        /// <summary>
        /// Initializes a new instance of <see cref="T:CompactMultiValueFacetHandler"/> with the specified name,
        /// Lucene.Net index field name, and <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance.
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="indexFieldName">The name of the Lucene.Net index field this handler will utilize.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        public CompactMultiValueFacetHandler(string name, string indexFieldName, TermListFactory termListFactory)
            : base(name)
        {
            m_indexFieldName = indexFieldName;
            m_termListFactory = termListFactory;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:CompactMultiValueFacetHandler"/> with the specified name
        /// and <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance.
        /// The Lucene.Net index field must have the same name.
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        public CompactMultiValueFacetHandler(string name, TermListFactory termListFactory)
            : this(name, name, termListFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:CompactMultiValueFacetHandler"/> with the specified name
        /// and Lucene.Net index field name.
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="indexFieldName">The name of the Lucene.Net index field this handler will utilize.</param>
        public CompactMultiValueFacetHandler(string name, string indexFieldName)
            : this(name, indexFieldName, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:CompactMultiValueFacetHandler"/> with the specified name.
        /// The Lucene.Net index field must have the same name.
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        public CompactMultiValueFacetHandler(string name)
            : this(name, name, null)
        {
        }

        public override DocComparerSource GetDocComparerSource()
        {
            return new CompactMultiFacetDocComparerSource(this);
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> prop)
        {
            return new CompactMultiValueFacetFilter(this, value);
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, IDictionary<string, string> prop)
        {
            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>(vals.Length);

            foreach (string val in vals)
            {
                RandomAccessFilter f = BuildRandomAccessFilter(val, prop);
                if (f != null)
                {
                    filterList.Add(f);
                }
                else
                {
                    return EmptyFilter.Instance;
                }
            }
            if (filterList.Count == 1)
                return filterList[0];
            return new RandomAccessAndFilter(filterList);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, IDictionary<string, string> prop, bool isNot)
        {
            RandomAccessFilter filter = null;

            if (vals.Length > 0)
            {
                filter = new CompactMultiValueFacetFilter(this, vals);
            }
            else
            {
                filter = EmptyFilter.Instance;
            }
            if (isNot)
            {
                filter = new RandomAccessNotFilter(filter);
            }
            return filter;
        }

        private static int CountBits(int val)
        {
            int c = 0;
            for (c = 0; val > 0; c++)
            {
                val &= val - 1;
            }
            return c;
        }

        public override int GetNumItems(BoboSegmentReader reader, int id)
        {
            FacetDataCache dataCache = GetFacetData<FacetDataCache>(reader);
            if (dataCache == null) return 0;
            int encoded = dataCache.OrderArray.Get(id);
            return CountBits(encoded);
        }

        public override string[] GetFieldValues(BoboSegmentReader reader, int id)
        {
            FacetDataCache dataCache = GetFacetData<FacetDataCache>(reader);
            if (dataCache == null) return new string[0];
            int encoded = dataCache.OrderArray.Get(id);
            if (encoded == 0)
            {
                return new string[] { "" };
            }
            else
            {
                int count = 1;
                List<string> valList = new List<string>(MAX_VAL_COUNT);

                while (encoded != 0)
                {
                    if ((encoded & 0x00000001) != 0x0)
                    {
                        valList.Add(dataCache.ValArray.Get(count));
                    }
                    count++;
                    encoded = (int)(((uint)encoded) >> 1);
                }
                return valList.ToArray();
            }
        }

        public override object[] GetRawFieldValues(BoboSegmentReader reader, int id)
        {
            FacetDataCache dataCache = GetFacetData<FacetDataCache>(reader);
            if (dataCache == null) return new string[0];
            int encoded = dataCache.OrderArray.Get(id);
            if (encoded == 0)
            {
                return new object[0];
            }
            else
            {
                int count = 1;
                List<Object> valList = new List<Object>(MAX_VAL_COUNT);

                while (encoded != 0)
                {
                    if ((encoded & 0x00000001) != 0x0)
                    {
                        valList.Add(dataCache.ValArray.GetRawValue(count));
                    }
                    count++;
                    encoded = (int)(((uint)encoded) >> 1);
                }
                return valList.ToArray();
            }
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec)
        {
            return new CompactMultiValueFacetCountCollectorSource(this.GetFacetData<FacetDataCache>, m_name, sel, fspec);
        }

        private class CompactMultiValueFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly Func<BoboSegmentReader, FacetDataCache> m_getFacetData;
            private readonly string m_name;
            private readonly BrowseSelection m_sel;
            private readonly FacetSpec m_ospec;

            public CompactMultiValueFacetCountCollectorSource(Func<BoboSegmentReader, FacetDataCache> getFacetData, string name, BrowseSelection sel, FacetSpec ospec)
            {
                this.m_getFacetData = getFacetData;
                m_name = name;
                m_ospec = ospec;
                m_sel = sel;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                FacetDataCache dataCache = m_getFacetData(reader);
                return new CompactMultiValueFacetCountCollector(m_name, m_sel, dataCache, docBase, m_ospec);
            }
        }

        public override FacetDataCache Load(BoboSegmentReader reader)
        {
            int maxDoc = reader.MaxDoc;

            BigInt32Array order = new BigInt32Array(maxDoc);

            ITermValueList mterms = m_termListFactory == null ? new TermStringList() : m_termListFactory.CreateTermList();

            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();

            int t = 0; // current term number
            mterms.Add(null);
            minIDList.Add(-1);
            maxIDList.Add(-1);
            freqList.Add(0);
            t++;
            Terms terms = reader.GetTerms(m_indexFieldName);
            if (terms != null)
            {
                TermsEnum termsEnum = terms.GetIterator(null);
                BytesRef text;
                while ((text = termsEnum.Next()) != null)
                {
                    // store term text
                    // we expect that there is at most one term per document
                    if (t > MAX_VAL_COUNT)
                    {
                        throw new IOException("maximum number of value cannot exceed: " + MAX_VAL_COUNT);
                    }
                    string val = text.Utf8ToString();
                    mterms.Add(val);
                    int bit = (0x00000001 << (t - 1));
                    Term term = new Term(m_indexFieldName, val);
                    DocsEnum docsEnum = reader.GetTermDocsEnum(term);
                    //freqList.add(termEnum.docFreq());  // removed because the df doesn't take into account the 
                    // num of deletedDocs
                    int df = 0;
                    int minID = -1;
                    int maxID = -1;
                    int docID = -1;
                    while ((docID = docsEnum.NextDoc()) != DocsEnum.NO_MORE_DOCS)
                    {
                        df++;
                        order.Add(docID, order.Get(docID) | bit);
                        minID = docID;
                        while (docsEnum.NextDoc() != DocsEnum.NO_MORE_DOCS)
                        {
                            docID = docsEnum.DocID;
                            df++;
                            order.Add(docID, order.Get(docID) | bit);
                        }
                        maxID = docID;
                    }
                    freqList.Add(df);
                    minIDList.Add(minID);
                    maxIDList.Add(maxID);
                    t++;
                }
            }

            mterms.Seal();

            return new FacetDataCache(order, mterms, freqList.ToArray(), minIDList.ToArray(), maxIDList.ToArray(), TermCountSize.Large);
        }

        private class CompactMultiFacetDocComparerSource : DocComparerSource
        {
            private readonly CompactMultiValueFacetHandler m_facetHandler;
            public CompactMultiFacetDocComparerSource(CompactMultiValueFacetHandler facetHandler)
            {
                m_facetHandler = facetHandler;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                if (!(reader is BoboSegmentReader))
                    throw new InvalidOperationException("reader must be instance of BoboSegmentReader");
                var boboReader = (BoboSegmentReader)reader;
                FacetDataCache dataCache = m_facetHandler.GetFacetData<FacetDataCache>(boboReader);
                return new CompactMultiValueDocComparer(dataCache, m_facetHandler, boboReader);
            }

            public class CompactMultiValueDocComparer : DocComparer
            {
                private readonly FacetDataCache m_dataCache;
                private readonly IFacetHandler m_facetHandler;
                private readonly BoboSegmentReader m_reader;

                public CompactMultiValueDocComparer(FacetDataCache dataCache, IFacetHandler facetHandler, BoboSegmentReader reader)
                {
                    m_dataCache = dataCache;
                    m_facetHandler = facetHandler;
                    m_reader = reader;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    int encoded1 = m_dataCache.OrderArray.Get(doc1.Doc);
                    int encoded2 = m_dataCache.OrderArray.Get(doc2.Doc);
                    return encoded1 - encoded2;
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    return new StringArrayComparer(m_facetHandler.GetFieldValues(m_reader, doc.Doc));
                }
            }
        }

        public virtual BoboDocScorer GetDocScorer(BoboSegmentReader reader, IFacetTermScoringFunctionFactory scoringFunctionFactory, IDictionary<string, float> boostMap)
        {
            FacetDataCache dataCache = GetFacetData<FacetDataCache>(reader);
            float[] boostList = BoboDocScorer.BuildBoostList(dataCache.ValArray, boostMap);
            return new CompactMultiValueDocScorer(dataCache, scoringFunctionFactory, boostList);
        }

        private sealed class CompactMultiValueDocScorer : BoboDocScorer
        {
            private readonly FacetDataCache m_dataCache;
            internal CompactMultiValueDocScorer(FacetDataCache dataCache, IFacetTermScoringFunctionFactory scoreFunctionFactory, float[] boostList)
                : base(scoreFunctionFactory.GetFacetTermScoringFunction(dataCache.ValArray.Count, dataCache.OrderArray.Length), boostList)
            {
                m_dataCache = dataCache;
            }

            public override Explanation Explain(int doc)
            {
                int encoded = m_dataCache.OrderArray.Get(doc);

                int count = 1;
                List<float> scoreList = new List<float>(m_dataCache.ValArray.Count);
                List<Explanation> explList = new List<Explanation>(scoreList.Count);
                while (encoded != 0)
                {
                    if ((encoded & 0x00000001) != 0x0)
                    {
                        int idx = count - 1;
                        scoreList.Add(m_function.Score(m_dataCache.Freqs[idx], m_boostList[idx]));
                        explList.Add(m_function.Explain(m_dataCache.Freqs[idx], m_boostList[idx]));
                    }
                    count++;
                    encoded = (int)(((uint)encoded) >> 1);
                }
                Explanation topLevel = m_function.Explain(scoreList.ToArray());
                foreach (Explanation sub in explList)
                {
                    topLevel.AddDetail(sub);
                }
                return topLevel;
            }

            public override sealed float Score(int docid)
            {
                m_function.ClearScores();
                int encoded = m_dataCache.OrderArray.Get(docid);

                int count = 1;

                while (encoded != 0)
                {
                    int idx = count - 1;
                    if ((encoded & 0x00000001) != 0x0)
                    {
                        m_function.ScoreAndCollect(m_dataCache.Freqs[idx], m_boostList[idx]);
                    }
                    count++;
                    encoded = (int)(((uint)encoded) >> 1);
                }
                return m_function.GetCurrentScore();
            }
        }

        private sealed class CompactMultiValueFacetCountCollector : DefaultFacetCountCollector
        {
            private readonly new BigSegmentedArray m_array;
            private readonly int[] m_combinationCount = new int[16 * 8];
            private int m_noValCount = 0;
            private bool m_aggregated = false;


            internal CompactMultiValueFacetCountCollector(string name, BrowseSelection sel, FacetDataCache dataCache, int docBase, FacetSpec ospec)
                : base(name, dataCache, docBase, sel, ospec)
            {
                m_array = m_dataCache.OrderArray;
            }


            public override sealed void CollectAll()
            {
                m_count = BigInt32Array.FromArray(m_dataCache.Freqs);
                m_aggregated = true;
            }

            public override sealed void Collect(int docid)
            {
                int encoded = m_array.Get(docid);
                if (encoded == 0)
                {
                    m_noValCount++;
                }
                else
                {
                    int offset = 0;
                    while (true)
                    {
                        m_combinationCount[(encoded & 0x0F) + offset]++;
                        encoded = (int)(((uint)encoded) >> 4);
                        if (encoded == 0)
                            break;
                        offset += 16;
                    }
                }
            }

            public override BrowseFacet GetFacet(string value)
            {
                if (!m_aggregated)
                    AggregateCounts();
                return base.GetFacet(value);
            }

            public override int GetFacetHitsCount(object value)
            {
                if (!m_aggregated)
                    AggregateCounts();
                return base.GetFacetHitsCount(value);
            }

            public override BigSegmentedArray GetCountDistribution()
            {
                if (!m_aggregated)
                    AggregateCounts();
                return m_count;
            }

            public override ICollection<BrowseFacet> GetFacets()
            {
                if (!m_aggregated)
                    AggregateCounts();
                return base.GetFacets();
            }

            private void AggregateCounts()
            {
                m_count.Add(0, m_noValCount);

                for (int i = 1; i < m_combinationCount.Length; i++)
                {
                    int count = m_combinationCount[i];
                    if (count > 0)
                    {
                        int offset = (i >> 4) * 4;
                        int encoded = (i & 0x0F);
                        int index = 1;
                        while (encoded != 0)
                        {
                            if ((encoded & 0x00000001) != 0x0)
                            {
                                int idx = index + offset;
                                base.m_count.Add(idx, base.m_count.Get(idx) + count);
                            }
                            index++;
                            encoded = (int)(((uint)encoded) >> 1);
                        }
                    }
                }
                m_aggregated = true;
            }

            public override FacetIterator GetIterator()
            {
                if (!m_aggregated) AggregateCounts();
                return base.GetIterator();
            }
        }
    }
}