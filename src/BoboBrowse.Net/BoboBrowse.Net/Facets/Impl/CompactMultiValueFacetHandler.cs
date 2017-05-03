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
        private readonly TermListFactory _termListFactory;
        private readonly string _indexFieldName;

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
            _indexFieldName = indexFieldName;
            _termListFactory = termListFactory;
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

        public override DocComparatorSource GetDocComparatorSource()
        {
            return new CompactMultiFacetDocComparatorSource(this);
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
            return new CompactMultiValueFacetCountCollectorSource(this.GetFacetData<FacetDataCache>, _name, sel, fspec);
        }

        private class CompactMultiValueFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly Func<BoboSegmentReader, FacetDataCache> getFacetData;
            private readonly string _name;
            private readonly BrowseSelection _sel;
            private readonly FacetSpec _ospec;

            public CompactMultiValueFacetCountCollectorSource(Func<BoboSegmentReader, FacetDataCache> getFacetData, string name, BrowseSelection sel, FacetSpec ospec)
            {
                this.getFacetData = getFacetData;
                _name = name;
                _ospec = ospec;
                _sel = sel;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                FacetDataCache dataCache = getFacetData(reader);
                return new CompactMultiValueFacetCountCollector(_name, _sel, dataCache, docBase, _ospec);
            }
        }

        public override FacetDataCache Load(BoboSegmentReader reader)
        {
            int maxDoc = reader.MaxDoc;

            BigIntArray order = new BigIntArray(maxDoc);

            ITermValueList mterms = _termListFactory == null ? new TermStringList() : _termListFactory.CreateTermList();

            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();

            int t = 0; // current term number
            mterms.Add(null);
            minIDList.Add(-1);
            maxIDList.Add(-1);
            freqList.Add(0);
            t++;
            Terms terms = reader.GetTerms(_indexFieldName);
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
                    Term term = new Term(_indexFieldName, val);
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

        private class CompactMultiFacetDocComparatorSource : DocComparatorSource
        {
            private readonly CompactMultiValueFacetHandler _facetHandler;
            public CompactMultiFacetDocComparatorSource(CompactMultiValueFacetHandler facetHandler)
            {
                _facetHandler = facetHandler;
            }

            public override DocComparator GetComparator(AtomicReader reader, int docbase)
            {
                if (!(reader is BoboSegmentReader))
                    throw new InvalidOperationException("reader must be instance of BoboSegmentReader");
                var boboReader = (BoboSegmentReader)reader;
                FacetDataCache dataCache = _facetHandler.GetFacetData<FacetDataCache>(boboReader);
                return new CompactMultiValueDocComparator(dataCache, _facetHandler, boboReader);
            }

            public class CompactMultiValueDocComparator : DocComparator
            {
                private readonly FacetDataCache _dataCache;
                private readonly IFacetHandler _facetHandler;
                private readonly BoboSegmentReader _reader;

                public CompactMultiValueDocComparator(FacetDataCache dataCache, IFacetHandler facetHandler, BoboSegmentReader reader)
                {
                    _dataCache = dataCache;
                    _facetHandler = facetHandler;
                    _reader = reader;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    int encoded1 = _dataCache.OrderArray.Get(doc1.Doc);
                    int encoded2 = _dataCache.OrderArray.Get(doc2.Doc);
                    return encoded1 - encoded2;
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    return new StringArrayComparator(_facetHandler.GetFieldValues(_reader, doc.Doc));
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
            private readonly FacetDataCache _dataCache;
            internal CompactMultiValueDocScorer(FacetDataCache dataCache, IFacetTermScoringFunctionFactory scoreFunctionFactory, float[] boostList)
                : base(scoreFunctionFactory.GetFacetTermScoringFunction(dataCache.ValArray.Count, dataCache.OrderArray.Size()), boostList)
            {
                _dataCache = dataCache;
            }

            public override Explanation Explain(int doc)
            {
                int encoded = _dataCache.OrderArray.Get(doc);

                int count = 1;
                List<float> scoreList = new List<float>(_dataCache.ValArray.Count);
                List<Explanation> explList = new List<Explanation>(scoreList.Count);
                while (encoded != 0)
                {
                    if ((encoded & 0x00000001) != 0x0)
                    {
                        int idx = count - 1;
                        scoreList.Add(_function.Score(_dataCache.Freqs[idx], _boostList[idx]));
                        explList.Add(_function.Explain(_dataCache.Freqs[idx], _boostList[idx]));
                    }
                    count++;
                    encoded = (int)(((uint)encoded) >> 1);
                }
                Explanation topLevel = _function.Explain(scoreList.ToArray());
                foreach (Explanation sub in explList)
                {
                    topLevel.AddDetail(sub);
                }
                return topLevel;
            }

            public override sealed float Score(int docid)
            {
                _function.ClearScores();
                int encoded = _dataCache.OrderArray.Get(docid);

                int count = 1;

                while (encoded != 0)
                {
                    int idx = count - 1;
                    if ((encoded & 0x00000001) != 0x0)
                    {
                        _function.ScoreAndCollect(_dataCache.Freqs[idx], _boostList[idx]);
                    }
                    count++;
                    encoded = (int)(((uint)encoded) >> 1);
                }
                return _function.GetCurrentScore();
            }
        }

        private sealed class CompactMultiValueFacetCountCollector : DefaultFacetCountCollector
        {
            private readonly new BigSegmentedArray _array;
            private readonly int[] _combinationCount = new int[16 * 8];
            private int _noValCount = 0;
            private bool _aggregated = false;


            internal CompactMultiValueFacetCountCollector(string name, BrowseSelection sel, FacetDataCache dataCache, int docBase, FacetSpec ospec)
                : base(name, dataCache, docBase, sel, ospec)
            {
                _array = _dataCache.OrderArray;
            }


            public override sealed void CollectAll()
            {
                _count = BigIntArray.FromArray(_dataCache.Freqs);
                _aggregated = true;
            }

            public override sealed void Collect(int docid)
            {
                int encoded = _array.Get(docid);
                if (encoded == 0)
                {
                    _noValCount++;
                }
                else
                {
                    int offset = 0;
                    while (true)
                    {
                        _combinationCount[(encoded & 0x0F) + offset]++;
                        encoded = (int)(((uint)encoded) >> 4);
                        if (encoded == 0)
                            break;
                        offset += 16;
                    }
                }
            }

            public override BrowseFacet GetFacet(string value)
            {
                if (!_aggregated)
                    AggregateCounts();
                return base.GetFacet(value);
            }

            public override int GetFacetHitsCount(object value)
            {
                if (!_aggregated)
                    AggregateCounts();
                return base.GetFacetHitsCount(value);
            }

            public override BigSegmentedArray GetCountDistribution()
            {
                if (!_aggregated)
                    AggregateCounts();
                return _count;
            }

            public override IEnumerable<BrowseFacet> GetFacets()
            {
                if (!_aggregated)
                    AggregateCounts();
                return base.GetFacets();
            }

            private void AggregateCounts()
            {
                _count.Add(0, _noValCount);

                for (int i = 1; i < _combinationCount.Length; i++)
                {
                    int count = _combinationCount[i];
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
                                _count.Add(idx, _count.Get(idx) + count);
                            }
                            index++;
                            encoded = (int)(((uint)encoded) >> 1);
                        }
                    }
                }
                _aggregated = true;
            }

            public override FacetIterator GetIterator()
            {
                if (!_aggregated) AggregateCounts();
                return base.GetIterator();
            }
        }
    }
}