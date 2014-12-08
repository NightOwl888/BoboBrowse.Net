//* 
//* Copyright (C) 2005-2006  John Wang
//*
//* This library is free software; you can redistribute it and/or
//* modify it under the terms of the GNU Lesser General Public
//* License as published by the Free Software Foundation; either
//* version 2.1 of the License, or (at your option) any later version.
//*
//* This library is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Util;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    

    public class MultiValueFacetDataCache<T> : FacetDataCache<T>
    {
        private static long serialVersionUID = 1L;
        private static ILog logger = LogManager.GetLogger<MultiValueFacetDataCache<T>>();

        public readonly BigNestedIntArray _nestedArray;
        private int _maxItems = BigNestedIntArray.MAX_ITEMS;
        private bool _overflow = false;

        public MultiValueFacetDataCache()
        {
            _nestedArray = new BigNestedIntArray();
        }

        public virtual MultiValueFacetDataCache<T> SetMaxItems(int maxItems)
        {
            _maxItems = Math.Min(maxItems, BigNestedIntArray.MAX_ITEMS);
            _nestedArray.MaxItems = _maxItems;
            return this;
        }

        public override int GetNumItems(int docid)
        {
            return _nestedArray.GetNumItems(docid);
        } 

        public override void Load(string fieldName, IndexReader reader, TermListFactory<T> listFactory)
        {
            this.Load(fieldName, reader, listFactory, new BoboIndexReader.WorkArea());
        }

        ///  
        ///   <summary> * loads multi-value facet data. This method uses a workarea to prepare loading. </summary>
        ///   * <param name="fieldName"> </param>
        ///   * <param name="reader"> </param>
        ///   * <param name="listFactory"> </param>
        ///   * <param name="workArea"> </param>
        ///   * <exception cref="IOException"> </exception>
        ///   
        public virtual void Load(string fieldName, IndexReader reader, TermListFactory<T> listFactory, BoboIndexReader.WorkArea workArea)
        {
            long t0 = Environment.TickCount;
            int maxdoc = reader.MaxDoc;
            BigNestedIntArray.BufferedLoader loader = GetBufferedLoader(maxdoc, workArea);

            TermEnum tenum = null;
            TermDocs tdoc = null;
            TermValueList<T> list = (listFactory == null ? (TermValueList<T>)new TermStringList() : listFactory.CreateTermList());
            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();
            OpenBitSet bitset = new OpenBitSet();
            int negativeValueCount = GetNegativeValueCount(reader, string.Intern(fieldName));
            int t = 0; // current term number
            list.Add(null);
            minIDList.Add(-1);
            maxIDList.Add(-1);
            freqList.Add(0);
            t++;

            _overflow = false;
            try
            {
                tdoc = reader.TermDocs();
                tenum = reader.Terms(new Term(fieldName, ""));
                if (tenum != null)
                {
                    do
                    {
                        Term term = tenum.Term;
                        if (term == null || !fieldName.Equals(term.Field))
                            break;

                        string val = term.Text;

                        if (val != null)
                        {
                            list.Add(val);

                            tdoc.Seek(tenum);
                            //freqList.add(tenum.docFreq()); // removed because the df doesn't take into account the num of deletedDocs
                            int df = 0;
                            int minID = -1;
                            int maxID = -1;
                            int valId = (t - 1 < negativeValueCount) ? (negativeValueCount - t + 1) : t;
                            if (tdoc.Next())
                            {
                                df++;
                                int docid = tdoc.Doc;

                                if (!loader.Add(docid, valId))
                                    LogOverflow(fieldName);
                                minID = docid;
                                bitset.FastSet(docid);
                                while (tdoc.Next())
                                {
                                    df++;
                                    docid = tdoc.Doc;

                                    if (!loader.Add(docid, valId))
                                        LogOverflow(fieldName);
                                    bitset.FastSet(docid);
                                }
                                maxID = docid;
                            }
                            freqList.Add(df);
                            minIDList.Add(minID);
                            maxIDList.Add(maxID);
                        }

                        t++;
                    }
                    while (tenum.Next());
                }
            }
            finally
            {
                try
                {
                    if (tdoc != null)
                    {
                        tdoc.Close();
                    }
                }
                finally
                {
                    if (tenum != null)
                    {
                        tenum.Close();
                    }
                }
            }

            list.Seal();

            try
            {
                _nestedArray.Load(maxdoc + 1, loader);
            }
            catch (System.IO.IOException e)
            {
                throw e;
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
            while (doc <= maxdoc && !_nestedArray.Contains(doc, 0, true))
            {
                ++doc;
            }
            if (doc <= maxdoc)
            {
                this.minIDs[0] = doc;
                doc = maxdoc;
                while (doc > 0 && !_nestedArray.Contains(doc, 0, true))
                {
                    --doc;
                }
                if (doc > 0)
                {
                    this.maxIDs[0] = doc;
                }
            }
            this.freqs[0] = maxdoc + 1 - (int)bitset.Cardinality();
        }

        ///  
        ///   <summary> * loads multi-value facet data. This method uses the count payload to allocate storage before loading data. </summary>
        ///   * <param name="fieldName"> </param>
        ///   * <param name="sizeTerm"> </param>
        ///   * <param name="reader"> </param>
        ///   * <param name="listFactory"> </param>
        ///   * <exception cref="IOException"> </exception>
        ///   
        public virtual void Load(string fieldName, IndexReader reader, TermListFactory<T> listFactory, Term sizeTerm)
        {
            int maxdoc = reader.MaxDoc;
            BigNestedIntArray.Loader loader = new AllocOnlyLoader(_maxItems, sizeTerm, reader);
            int negativeValueCount = GetNegativeValueCount(reader, string.Intern(fieldName));
            try
            {
                _nestedArray.Load(maxdoc + 1, loader);
            }
            catch (System.IO.IOException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new RuntimeException("failed to load due to " + e.ToString(), e);
            }

            TermEnum tenum = null;
            TermDocs tdoc = null;
            TermValueList<T> list = (listFactory == null ? (TermValueList<T>)new TermStringList() : listFactory.CreateTermList());
            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();
            OpenBitSet bitset = new OpenBitSet();

            int t = 0; // current term number
            list.Add(null);
            minIDList.Add(-1);
            maxIDList.Add(-1);
            freqList.Add(0);
            t++;

            _overflow = false;
            try
            {
                tdoc = reader.TermDocs();
                tenum = reader.Terms(new Term(fieldName, ""));
                if (tenum != null)
                {
                    do
                    {
                        Term term = tenum.Term;
                        if (term == null || !fieldName.Equals(term.Field))
                            break;

                        string val = term.Text;

                        if (val != null)
                        {
                            list.Add(val);

                            tdoc.Seek(tenum);
                            //freqList.add(tenum.docFreq()); // removed because the df doesn't take into account the num of deletedDocs
                            int df = 0;
                            int minID = -1;
                            int maxID = -1;
                            if (tdoc.Next())
                            {
                                df++;
                                int docid = tdoc.Doc;
                                if (!_nestedArray.AddData(docid, t))
                                    LogOverflow(fieldName);
                                minID = docid;
                                bitset.FastSet(docid);
                                int valId = (t - 1 < negativeValueCount) ? (negativeValueCount - t + 1) : t;
                                while (tdoc.Next())
                                {
                                    df++;
                                    docid = tdoc.Doc;
                                    if (!_nestedArray.AddData(docid, valId))
                                        LogOverflow(fieldName);
                                    bitset.FastSet(docid);
                                }
                                maxID = docid;
                            }
                            freqList.Add(df);
                            minIDList.Add(minID);
                            maxIDList.Add(maxID);
                        }

                        t++;
                    }
                    while (tenum.Next());
                }
            }
            finally
            {
                try
                {
                    if (tdoc != null)
                    {
                        tdoc.Close();
                    }
                }
                finally
                {
                    if (tenum != null)
                    {
                        tenum.Close();
                    }
                }
            }

            list.Seal();

            this.valArray = list;
            this.freqs = freqList.ToArray();
            this.minIDs = minIDList.ToArray();
            this.maxIDs = maxIDList.ToArray();

            int doc = 0;
            while (doc <= maxdoc && !_nestedArray.Contains(doc, 0, true))
            {
                ++doc;
            }
            if (doc <= maxdoc)
            {
                this.minIDs[0] = doc;
                doc = maxdoc;
                while (doc > 0 && !_nestedArray.Contains(doc, 0, true))
                {
                    --doc;
                }
                if (doc > 0)
                {
                    this.maxIDs[0] = doc;
                }
            }
            this.freqs[0] = maxdoc + 1 - (int)bitset.Cardinality();
        }

        protected void LogOverflow(string fieldName)
        {
            if (!_overflow)
            {
                logger.Error("Maximum value per document: " + _maxItems + " exceeded, fieldName=" + fieldName);
                _overflow = true;
            }
        }

        protected BigNestedIntArray.BufferedLoader GetBufferedLoader(int maxdoc, BoboIndexReader.WorkArea workArea)
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
            private IndexReader _reader;
            private Term _sizeTerm;
            private int _maxItems;

            public AllocOnlyLoader(int maxItems, Term sizeTerm, IndexReader reader)
            {
                _maxItems = Math.Min(maxItems, BigNestedIntArray.MAX_ITEMS);
                _sizeTerm = sizeTerm;
                _reader = reader;
            }

            public override void Load()
            {
                TermPositions tp = null;
                byte[] payloadBuffer = new byte[4]; // four bytes for an int
                try
                {
                    tp = _reader.TermPositions(_sizeTerm);

                    if (tp == null)
                        return;

                    while (tp.Next())
                    {
                        if (tp.Freq > 0)
                        {
                            tp.NextPosition();
                            tp.GetPayload(payloadBuffer, 0);
                            int len = BytesToInt(payloadBuffer);
                            Allocate(tp.Doc, Math.Min(len, _maxItems), true);
                        }
                    }
                }
                finally
                {
                    if (tp != null)
                        tp.Close();
                }
            }

            private static int BytesToInt(byte[] bytes)
            {
                return ((bytes[3] & 0xFF) << 24) | ((bytes[2] & 0xFF) << 16) | ((bytes[1] & 0xFF) << 8) | (bytes[0] & 0xFF);
            }
        }

        public sealed class MultiFacetDocCaomparatorSource : DocComparatorSource
        {
            private MultiDataCacheBuilder cacheBuilder;
		    public MultiFacetDocComparatorSource(MultiDataCacheBuilder multiDataCacheBuilder){
		      cacheBuilder = multiDataCacheBuilder;
		    }

            public override GetComparator(IndexReader reader, int docbase)
            {
                if (!reader.GetType().Equals(typeof(BoboIndexReader))) 
                    throw new ArgumentException("reader must be instance of BoboIndexReader");
                BoboIndexReader boboReader = (BoboIndexReader)reader;
                MultiValueFacetDataCache<T> datacache = cacheBuilder.Build(boboReader);
                return new MyComparator(dataCache);
            }

            public sealed class MyComparator : DocComparator
            {
                private readonly MultiValueFacetDataCache<T> _dataCache;

                public MyComparator(MultiValueFacetDataCache<T> dataCache)
                {
                    _dataCache = dataCache;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    return _dataCache._nestedArray.Compare(doc1.Doc, doc2.Doc);
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    string[] vals = _dataCache._nestedArray.GetTranslatedData(doc.Doc, _dataCache.valArray);
                    return new StringArrayComparator(vals);
                }
            }
        }
    }
}
