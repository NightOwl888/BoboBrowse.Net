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
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Facets.Range;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using BoboBrowse.Net.Support.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;

    public class MultiValueFacetDataCache : FacetDataCache
    {
        //private static long serialVersionUID = 1L; // NOT USED
        private static readonly ILog logger = LogProvider.For<MultiValueFacetDataCache>();

        protected readonly BigNestedInt32Array m_nestedArray;
        protected int m_maxItems = BigNestedInt32Array.MAX_ITEMS;
        protected bool m_overflow = false;

        public MultiValueFacetDataCache()
        {
            m_nestedArray = new BigNestedInt32Array();
        }

        public BigNestedInt32Array NestedArray
        {
            get { return m_nestedArray; }
        }

        public virtual int MaxItems
        {
            set
            {
                m_maxItems = Math.Min(value, BigNestedInt32Array.MAX_ITEMS);
                m_nestedArray.MaxItems = m_maxItems;
            }
        }

        public override int GetNumItems(int docid)
        {
            return m_nestedArray.GetNumItems(docid);
        } 

        public override void Load(string fieldName, AtomicReader reader, TermListFactory listFactory)
        {
            this.Load(fieldName, reader, listFactory, new BoboSegmentReader.WorkArea());
        }

        /// <summary>
        /// loads multi-value facet data. This method uses a workarea to prepare loading.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="reader"></param>
        /// <param name="listFactory"></param>
        /// <param name="workArea"></param>
        public virtual void Load(string fieldName, AtomicReader reader, TermListFactory listFactory, BoboSegmentReader.WorkArea workArea)
        {
            string field = string.Intern(fieldName);
            int maxdoc = reader.MaxDoc;
            BigNestedInt32Array.BufferedLoader loader = GetBufferedLoader(maxdoc, workArea);

            ITermValueList list = (listFactory == null ? (ITermValueList)new TermStringList() : listFactory.CreateTermList());
            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();
            OpenBitSet bitset = new OpenBitSet();
            int negativeValueCount = GetNegativeValueCount(reader, field);
            int t = 1; // valid term id starts from 1
            list.Add(null);
            minIDList.Add(-1);
            maxIDList.Add(-1);
            freqList.Add(0);

            m_overflow = false;
            Terms terms = reader.GetTerms(field);
            if (terms != null) 
            { 
                TermsEnum termsEnum = terms.GetIterator(null);
                BytesRef text;
                while ((text = termsEnum.Next()) != null)
                {
                    string strText = text.Utf8ToString();
                    list.Add(strText);

                    Term term = new Term(field, strText);
                    DocsEnum docsEnum = reader.GetTermDocsEnum(term);
                    int df = 0;
                    int minID = -1;
                    int maxID = -1;
                    int docID = -1;
                    int valId = (t - 1 < negativeValueCount) ? (negativeValueCount - t + 1) : t;
                    while ((docID = docsEnum.NextDoc()) != DocsEnum.NO_MORE_DOCS)
                    {
                        df++;
                        if (!loader.Add(docID, valId)) LogOverflow(fieldName);
                        minID = docID;
                        bitset.FastSet(docID);
                        while (docsEnum.NextDoc() != DocsEnum.NO_MORE_DOCS)
                        {
                            docID = docsEnum.DocID;
                            df++;
                            if (!loader.Add(docID, valId)) LogOverflow(fieldName);
                            bitset.FastSet(docID);
                        }
                        maxID = docID;
                    }
                    freqList.Add(df);
                    minIDList.Add(minID);
                    maxIDList.Add(maxID);
                    t++;
                }
            }

            list.Seal();

            try
            {
                m_nestedArray.Load(maxdoc + 1, loader);
            }
            catch (Exception e)
            {
                throw new RuntimeException("failed to load due to " + e.ToString(), e);
            }

            this.m_valArray = list;
            this.m_freqs = freqList.ToArray();
            this.m_minIDs = minIDList.ToArray();
            this.m_maxIDs = maxIDList.ToArray();

            int doc = 0;
            while (doc < maxdoc && !m_nestedArray.Contains(doc, 0, true))
            {
                ++doc;
            }
            if (doc < maxdoc)
            {
                this.m_minIDs[0] = doc;
                doc = maxdoc - 1;
                while (doc >= 0 && !m_nestedArray.Contains(doc, 0, true))
                {
                    --doc;
                }
                this.m_maxIDs[0] = doc;
            }
            this.m_freqs[0] = maxdoc - (int)bitset.Cardinality();
        }

        /// <summary>
        /// loads multi-value facet data. This method uses the count payload to allocate storage before loading data.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="reader"></param>
        /// <param name="listFactory"></param>
        /// <param name="sizeTerm"></param>
        public virtual void Load(string fieldName, AtomicReader reader, TermListFactory listFactory, Term sizeTerm)
        {
            string field = string.Intern(fieldName);
            int maxdoc = reader.MaxDoc;
            BigNestedInt32Array.Loader loader = new AllocOnlyLoader(m_maxItems, sizeTerm, reader);
            int negativeValueCount = GetNegativeValueCount(reader, field);
            try
            {
                m_nestedArray.Load(maxdoc + 1, loader);
            }
            catch (Exception e)
            {
                throw new RuntimeException("failed to load due to " + e.ToString(), e);
            }

            ITermValueList list = (listFactory == null ? (ITermValueList)new TermStringList() : listFactory.CreateTermList());
            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();
            OpenBitSet bitset = new OpenBitSet(maxdoc + 1);

            int t = 1; // valid term id starts from 1
            list.Add(null);
            minIDList.Add(-1);
            maxIDList.Add(-1);
            freqList.Add(0);

            m_overflow = false;

            Terms terms = reader.GetTerms(field);
            if (terms != null)
            {
                TermsEnum termsEnum = terms.GetIterator(null);
                BytesRef text;
                while ((text = termsEnum.Next()) != null)
                {
                    string strText = text.Utf8ToString();
                    list.Add(strText);

                    Term term = new Term(field, strText);
                    DocsEnum docsEnum = reader.GetTermDocsEnum(term);

                    int df = 0;
                    int minID = -1;
                    int maxID = -1;
                    int docID = -1;
                    while ((docID = docsEnum.NextDoc()) != DocsEnum.NO_MORE_DOCS)
                    {
                        df++;
                        if (!m_nestedArray.AddData(docID, t)) LogOverflow(fieldName);
                        minID = docID;
                        bitset.FastSet(docID);
                        int valId = (t - 1 < negativeValueCount) ? (negativeValueCount - t + 1) : t;
                        while (docsEnum.NextDoc() != DocsEnum.NO_MORE_DOCS)
                        {
                            docID = docsEnum.DocID;
                            df++;
                            if (!m_nestedArray.AddData(docID, valId)) LogOverflow(fieldName);
                            bitset.FastSet(docID);
                        }
                        maxID = docID;
                    }
                    freqList.Add(df);
                    minIDList.Add(minID);
                    maxIDList.Add(maxID);
                    t++;
                }
            }

            list.Seal();

            this.m_valArray = list;
            this.m_freqs = freqList.ToArray();
            this.m_minIDs = minIDList.ToArray();
            this.m_maxIDs = maxIDList.ToArray();

            int doc = 0;
            while (doc < maxdoc && !m_nestedArray.Contains(doc, 0, true))
            {
                ++doc;
            }
            if (doc < maxdoc)
            {
                this.m_minIDs[0] = doc;
                doc = maxdoc - 1;
                while (doc >= 0 && !m_nestedArray.Contains(doc, 0, true))
                {
                    --doc;
                }
                this.m_maxIDs[0] = doc;
            }
            this.m_freqs[0] = maxdoc - (int)bitset.Cardinality();
        }

        protected virtual void LogOverflow(string fieldName)
        {
            if (!m_overflow)
            {
                logger.Error("Maximum value per document: " + m_maxItems + " exceeded, fieldName=" + fieldName);
                m_overflow = true;
            }
        }

        protected virtual BigNestedInt32Array.BufferedLoader GetBufferedLoader(int maxdoc, BoboSegmentReader.WorkArea workArea)
        {
            if (workArea == null)
            {
                return new BigNestedInt32Array.BufferedLoader(maxdoc, m_maxItems, new BigInt32Buffer());
            }
            else
            {
                BigInt32Buffer buffer = workArea.Get<BigInt32Buffer>();
                if (buffer == null)
                {
                    buffer = new BigInt32Buffer();
                    workArea.Put(buffer);
                }
                else
                {
                    buffer.Reset();
                }

                BigNestedInt32Array.BufferedLoader loader = workArea.Get<BigNestedInt32Array.BufferedLoader>();
                if (loader == null || loader.Capacity < maxdoc)
                {
                    loader = new BigNestedInt32Array.BufferedLoader(maxdoc, m_maxItems, buffer);
                    workArea.Put(loader);
                }
                else
                {
                    loader.Reset(maxdoc, m_maxItems, buffer);
                }
                return loader;
            }
        }

        /// <summary>
        /// A loader that allocate data storage without loading data to BigNestedIntArray.
        /// Note that this loader supports only non-negative integer data.
        /// </summary>
        public sealed class AllocOnlyLoader : BigNestedInt32Array.Loader
        {
            private readonly AtomicReader m_reader;
            private readonly Term m_sizeTerm;
            private readonly int m_maxItems;

            public AllocOnlyLoader(int maxItems, Term sizeTerm, AtomicReader reader)
            {
                m_maxItems = Math.Min(maxItems, BigNestedInt32Array.MAX_ITEMS);
                m_sizeTerm = sizeTerm;
                m_reader = reader;
            }

            public override void Load()
            {
                DocsAndPositionsEnum docPosEnum = m_reader.GetTermPositionsEnum(m_sizeTerm);
                if (docPosEnum == null)
                {
                    return;
                }
                int docID = -1;
                while ((docID = docPosEnum.NextDoc()) != DocsEnum.NO_MORE_DOCS)
                {
                    if (docPosEnum.Freq > 0)
                    {
                        docPosEnum.NextPosition();
                        int len = BytesToInt(docPosEnum.GetPayload().Bytes);
                        Allocate(docID, Math.Min(len, m_maxItems), true);
                    }
                }
            }

            private static int BytesToInt(byte[] bytes)
            {
                return ((bytes[3] & 0xFF) << 24) | ((bytes[2] & 0xFF) << 16) | ((bytes[1] & 0xFF) << 8) | (bytes[0] & 0xFF);
            }
        }
    }

    public sealed class MultiFacetDocComparerSource : DocComparerSource
    {
        private readonly MultiDataCacheBuilder m_cacheBuilder;
        public MultiFacetDocComparerSource(MultiDataCacheBuilder multiDataCacheBuilder)
        {
            m_cacheBuilder = multiDataCacheBuilder;
        }

        public override DocComparer GetComparer(AtomicReader reader, int docbase)
        {
            if (!(reader is BoboSegmentReader))
                throw new ArgumentException("reader must be instance of " + typeof(BoboSegmentReader).Name);
            BoboSegmentReader boboReader = (BoboSegmentReader)reader;
            MultiValueFacetDataCache dataCache = (MultiValueFacetDataCache)m_cacheBuilder.Build(boboReader);
            return new MultiFacetDocComparer(dataCache);
        }

        public sealed class MultiFacetDocComparer : DocComparer
        {
            private readonly MultiValueFacetDataCache m_dataCache;

            public MultiFacetDocComparer(MultiValueFacetDataCache dataCache)
            {
                m_dataCache = dataCache;
            }

            public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
            {
                return m_dataCache.NestedArray.Compare(doc1.Doc, doc2.Doc);
            }

            public override IComparable Value(ScoreDoc doc)
            {
                string[] vals = m_dataCache.NestedArray.GetTranslatedData(doc.Doc, m_dataCache.ValArray);
                return new StringArrayComparer(vals);
            }
        }
    }
}
