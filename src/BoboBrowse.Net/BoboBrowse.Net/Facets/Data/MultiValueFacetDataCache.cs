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
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;

    public class MultiValueFacetDataCache : FacetDataCache
    {
        //private static long serialVersionUID = 1L; // NOT USED
        private static ILog logger = LogManager.GetLogger(typeof(MultiValueFacetDataCache));

        protected readonly BigNestedIntArray _nestedArray;
        protected int _maxItems = BigNestedIntArray.MAX_ITEMS;
        protected bool _overflow = false;

        public MultiValueFacetDataCache()
        {
            _nestedArray = new BigNestedIntArray();
        }

        public BigNestedIntArray NestedArray
        {
            get { return _nestedArray; }
        }

        public virtual int MaxItems
        {
            set
            {
                _maxItems = Math.Min(value, BigNestedIntArray.MAX_ITEMS);
                _nestedArray.MaxItems = _maxItems;
            }
        }

        public override int GetNumItems(int docid)
        {
            return _nestedArray.GetNumItems(docid);
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
            BigNestedIntArray.BufferedLoader loader = GetBufferedLoader(maxdoc, workArea);

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

            _overflow = false;
            Terms terms = reader.Terms(field);
            if (terms != null) 
            { 
                TermsEnum termsEnum = terms.Iterator(null);
                BytesRef text;
                while ((text = termsEnum.Next()) != null)
                {
                    string strText = text.Utf8ToString();
                    list.Add(strText);

                    Term term = new Term(field, strText);
                    DocsEnum docsEnum = reader.TermDocsEnum(term);
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
                            docID = docsEnum.DocID();
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
                _nestedArray.Load(maxdoc + 1, loader);
            }
            catch (Exception e)
            {
                throw new RuntimeException("failed to load due to " + e.ToString(), e);
            }

            this.valArray = list;
            this.freqs = freqList.ToArray();
            this.minIDs = minIDList.ToArray();
            this.maxIDs = maxIDList.ToArray();

            int doc = 0;
            while (doc < maxdoc && !_nestedArray.Contains(doc, 0, true))
            {
                ++doc;
            }
            if (doc < maxdoc)
            {
                this.minIDs[0] = doc;
                doc = maxdoc - 1;
                while (doc >= 0 && !_nestedArray.Contains(doc, 0, true))
                {
                    --doc;
                }
                this.maxIDs[0] = doc;
            }
            this.freqs[0] = maxdoc - (int)bitset.Cardinality();
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
            BigNestedIntArray.Loader loader = new AllocOnlyLoader(_maxItems, sizeTerm, reader);
            int negativeValueCount = GetNegativeValueCount(reader, field);
            try
            {
                _nestedArray.Load(maxdoc + 1, loader);
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

            _overflow = false;

            Terms terms = reader.Terms(field);
            if (terms != null)
            {
                TermsEnum termsEnum = terms.Iterator(null);
                BytesRef text;
                while ((text = termsEnum.Next()) != null)
                {
                    string strText = text.Utf8ToString();
                    list.Add(strText);

                    Term term = new Term(field, strText);
                    DocsEnum docsEnum = reader.TermDocsEnum(term);

                    int df = 0;
                    int minID = -1;
                    int maxID = -1;
                    int docID = -1;
                    while ((docID = docsEnum.NextDoc()) != DocsEnum.NO_MORE_DOCS)
                    {
                        df++;
                        if (!_nestedArray.AddData(docID, t)) LogOverflow(fieldName);
                        minID = docID;
                        bitset.FastSet(docID);
                        int valId = (t - 1 < negativeValueCount) ? (negativeValueCount - t + 1) : t;
                        while (docsEnum.NextDoc() != DocsEnum.NO_MORE_DOCS)
                        {
                            docID = docsEnum.DocID();
                            df++;
                            if (!_nestedArray.AddData(docID, valId)) LogOverflow(fieldName);
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

            this.valArray = list;
            this.freqs = freqList.ToArray();
            this.minIDs = minIDList.ToArray();
            this.maxIDs = maxIDList.ToArray();

            int doc = 0;
            while (doc < maxdoc && !_nestedArray.Contains(doc, 0, true))
            {
                ++doc;
            }
            if (doc < maxdoc)
            {
                this.minIDs[0] = doc;
                doc = maxdoc - 1;
                while (doc >= 0 && !_nestedArray.Contains(doc, 0, true))
                {
                    --doc;
                }
                this.maxIDs[0] = doc;
            }
            this.freqs[0] = maxdoc - (int)bitset.Cardinality();
        }

        protected virtual void LogOverflow(string fieldName)
        {
            if (!_overflow)
            {
                logger.Error("Maximum value per document: " + _maxItems + " exceeded, fieldName=" + fieldName);
                _overflow = true;
            }
        }

        protected virtual BigNestedIntArray.BufferedLoader GetBufferedLoader(int maxdoc, BoboSegmentReader.WorkArea workArea)
        {
            if (workArea == null)
            {
                return new BigNestedIntArray.BufferedLoader(maxdoc, _maxItems, new BigIntBuffer());
            }
            else
            {
                BigIntBuffer buffer = workArea.Get<BigIntBuffer>();
                if (buffer == null)
                {
                    buffer = new BigIntBuffer();
                    workArea.Put(buffer);
                }
                else
                {
                    buffer.Reset();
                }

                BigNestedIntArray.BufferedLoader loader = workArea.Get<BigNestedIntArray.BufferedLoader>();
                if (loader == null || loader.Capacity < maxdoc)
                {
                    loader = new BigNestedIntArray.BufferedLoader(maxdoc, _maxItems, buffer);
                    workArea.Put(loader);
                }
                else
                {
                    loader.Reset(maxdoc, _maxItems, buffer);
                }
                return loader;
            }
        }

        /// <summary>
        /// A loader that allocate data storage without loading data to BigNestedIntArray.
        /// Note that this loader supports only non-negative integer data.
        /// </summary>
        public sealed class AllocOnlyLoader : BigNestedIntArray.Loader
        {
            private readonly AtomicReader _reader;
            private readonly Term _sizeTerm;
            private readonly int _maxItems;

            public AllocOnlyLoader(int maxItems, Term sizeTerm, AtomicReader reader)
            {
                _maxItems = Math.Min(maxItems, BigNestedIntArray.MAX_ITEMS);
                _sizeTerm = sizeTerm;
                _reader = reader;
            }

            public override void Load()
            {
                DocsAndPositionsEnum docPosEnum = _reader.TermPositionsEnum(_sizeTerm);
                if (docPosEnum == null)
                {
                    return;
                }
                int docID = -1;
                while ((docID = docPosEnum.NextDoc()) != DocsEnum.NO_MORE_DOCS)
                {
                    if (docPosEnum.Freq() > 0)
                    {
                        docPosEnum.NextPosition();
                        int len = BytesToInt(docPosEnum.Payload.Bytes);
                        Allocate(docID, Math.Min(len, _maxItems), true);
                    }
                }
            }

            private static int BytesToInt(byte[] bytes)
            {
                return ((bytes[3] & 0xFF) << 24) | ((bytes[2] & 0xFF) << 16) | ((bytes[1] & 0xFF) << 8) | (bytes[0] & 0xFF);
            }
        }
    }

    public sealed class MultiFacetDocComparatorSource : DocComparatorSource
    {
        private readonly MultiDataCacheBuilder cacheBuilder;
        public MultiFacetDocComparatorSource(MultiDataCacheBuilder multiDataCacheBuilder)
        {
            cacheBuilder = multiDataCacheBuilder;
        }

        public override DocComparator GetComparator(AtomicReader reader, int docbase)
        {
            if (!(reader is BoboSegmentReader))
                throw new ArgumentException("reader must be instance of " + typeof(BoboSegmentReader).Name);
            BoboSegmentReader boboReader = (BoboSegmentReader)reader;
            MultiValueFacetDataCache dataCache = (MultiValueFacetDataCache)cacheBuilder.Build(boboReader);
            return new MultiFacetDocComparator(dataCache);
        }

        public sealed class MultiFacetDocComparator : DocComparator
        {
            private readonly MultiValueFacetDataCache _dataCache;

            public MultiFacetDocComparator(MultiValueFacetDataCache dataCache)
            {
                _dataCache = dataCache;
            }

            public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
            {
                return _dataCache.NestedArray.Compare(doc1.Doc, doc2.Doc);
            }

            public override IComparable Value(ScoreDoc doc)
            {
                string[] vals = _dataCache.NestedArray.GetTranslatedData(doc.Doc, _dataCache.ValArray);
                return new StringArrayComparator(vals);
            }
        }
    }
}
