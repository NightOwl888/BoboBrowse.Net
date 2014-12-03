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

namespace BoboBrowse.Net.Facets.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common.Logging;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Index;
    using Lucene.Net.Search;

    public class MultiValueFacetDataCache : FacetDataCache
    {
        private static ILog logger = LogManager.GetLogger(typeof(MultiValueFacetDataCache));

        public readonly BigNestedIntArray _nestedArray;
        private int _maxItems = BigNestedIntArray.MAX_ITEMS;
        private bool _overflow = false;

        public MultiValueFacetDataCache()
        {
            _nestedArray = new BigNestedIntArray();
        }

        public virtual void SetMaxItems(int maxItems)
        {
            _maxItems = Math.Min(maxItems, BigNestedIntArray.MAX_ITEMS);
            _nestedArray.setMaxItems(_maxItems);
        }

        public override void Load(string fieldName, IndexReader reader, TermListFactory listFactory)
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
        public virtual void Load(string fieldName, IndexReader reader, TermListFactory listFactory, BoboIndexReader.WorkArea workArea)
        {          
            int maxdoc = reader.MaxDoc;
            BigNestedIntArray.BufferedLoader loader = GetBufferedLoader(maxdoc, workArea);

            TermEnum tenum = null;
            TermDocs tdoc = null;
            ITermValueList list = (listFactory == null ? new TermStringList() : listFactory.CreateTermList());
            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();

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
                tenum = reader.Terms(new Term(fieldName));
                if (tenum != null)
                {
                    do
                    {
                        Term term = tenum.Term;
                        if (term == null || !fieldName.Equals(term.Field))
                            break;

                        string val = term.Text;

                        // if (val!=null && val.length()>0){
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
                                if (!loader.Add(docid, t))
                                    LogOverflow(fieldName);
                                minID = docid;
                                while (tdoc.Next())
                                {
                                    df++;
                                    docid = tdoc.Doc;
                                    if (!loader.Add(docid, t))
                                        LogOverflow(fieldName);
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
                        tdoc.Dispose();
                    }
                }
                finally
                {
                    if (tenum != null)
                    {
                        tenum.Dispose();
                    }
                }
            }

            list.Seal();

            try
            {
                _nestedArray.load(maxdoc, loader);
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
        }

        ///  
        ///   <summary> * loads multi-value facet data. This method uses the count payload to allocate storage before loading data. </summary>
        ///   * <param name="fieldName"> </param>
        ///   * <param name="sizeTerm"> </param>
        ///   * <param name="reader"> </param>
        ///   * <param name="listFactory"> </param>
        ///   * <exception cref="IOException"> </exception>
        ///   
        public virtual void Load(string fieldName, IndexReader reader, TermListFactory listFactory, Term sizeTerm)
        {
            int maxdoc = reader.MaxDoc;
            BigNestedIntArray.Loader loader = new AllocOnlyLoader(_maxItems, sizeTerm, reader);

            try
            {
                _nestedArray.load(maxdoc, loader);
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
            ITermValueList list = (listFactory == null ? new TermStringList() : listFactory.CreateTermList());
            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();

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
                                if (!_nestedArray.addData(docid, t))
                                    LogOverflow(fieldName);
                                minID = docid;
                                while (tdoc.Next())
                                {
                                    df++;
                                    docid = tdoc.Doc;
                                    if (!_nestedArray.addData(docid, t))
                                        LogOverflow(fieldName);
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
                        tdoc.Dispose();
                    }
                }
                finally
                {
                    if (tenum != null)
                    {
                        tenum.Dispose();
                    }
                }
            }

            list.Seal();

            this.valArray = list;
            this.freqs = freqList.ToArray();
            this.minIDs = minIDList.ToArray();
            this.maxIDs = maxIDList.ToArray();
        }

        private void LogOverflow(string fieldName)
        {
            if (!_overflow)
            {
                logger.Error("Maximum value per document: " + _maxItems + " exceeded, fieldName=" + fieldName);
                _overflow = true;
            }
        }

        private BigNestedIntArray.BufferedLoader GetBufferedLoader(int maxdoc, BoboIndexReader.WorkArea workArea)
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
                if (loader == null || loader.capacity() < maxdoc)
                {
                    loader = new BigNestedIntArray.BufferedLoader(maxdoc, _maxItems, buffer);
                    workArea.Put(loader);
                }
                else
                {
                    loader.reset(maxdoc, _maxItems, buffer);
                }
                return loader;
            }
        }

        ///  
        ///   <summary> * A loader that allocate data storage without loading data to BigNestedIntArray.
        ///   * Note that this loader supports only non-negative integer data. </summary>
        ///   
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
                            allocate(tp.Doc, Math.Min(len, _maxItems), true);
                        }
                    }
                }
                finally
                {
                    if (tp != null)
                        tp.Dispose();
                }
            }

            private static int BytesToInt(byte[] bytes)
            {
                return ((bytes[3] & 0xFF) << 24) | ((bytes[2] & 0xFF) << 16) | ((bytes[1] & 0xFF) << 8) | (bytes[0] & 0xFF);
            }
        }

        public override FieldComparator GeFieldComparator(int numDocs, int type)
        {
            return new FacetMultiValueComparator(numDocs, type, this);
        }       

        private class FacetMultiValueComparator : FieldComparator
        {
            private int[] _docs;
            private int _fieldType;
            private MultiValueFacetDataCache _dataCache;
            private string[] _bottom;
            
            public FacetMultiValueComparator(int numHits,int type, MultiValueFacetDataCache dataCache)
            {
                _docs = new int[numHits];
                _fieldType = type;                             
                _dataCache = dataCache;
                              
            }

            public override int Compare(int slot1, int slot2)
            {
                var doc1 = _docs[slot1];
                var doc2 = _docs[slot2];
                return this.CompareValue(_dataCache._nestedArray.getTranslatedData(doc1, _dataCache.valArray),
                    _dataCache._nestedArray.getTranslatedData(doc2, _dataCache.valArray));
            }

            public override int CompareBottom(int doc)
            {
                return this.CompareValue(_bottom, _dataCache._nestedArray.getTranslatedData(doc, _dataCache.valArray));
            }

            public override void Copy(int slot, int doc)
            {
                _docs[slot] = doc;
            }

            public override void SetBottom(int slot)
            {
                _bottom = _dataCache._nestedArray.getTranslatedData(slot, _dataCache.valArray);
            }

            public override void SetNextReader(IndexReader reader, int docBase)
            {
            }

            public override IComparable this[int slot]
            {
                get
                {
                    var doc = _docs[slot];
                    var vals = _dataCache._nestedArray.getTranslatedData(doc, _dataCache.valArray);
                    return new StringArrayComparator(vals);
                }
            }

            private int CompareValue<T>(string[] value1, string[] value2, Func<string, T> parser) where T : IComparable
            {
                //(T)Convert.ChangeType(value, typeof(T))
                var v1 = value1.Select(k => parser(k)).OrderByDescending(k => k).ToArray();
                var v2 = value2.Select(k => parser(k)).OrderByDescending(k => k).ToArray();
                for (var i = 0; i < Math.Min(v1.Length, v2.Length); i++)
                {
                    var compare = v1[i].CompareTo(v2[i]);
                    if (compare != 0)
                    {
                        return compare;
                    }
                }
                return value1.Length.CompareTo(value2.Length);
            }

            private int CompareValue(string[] value1, string[] value2)
            {
                switch (_fieldType)
                {
                    case SortField.BYTE:
                    case SortField.INT:
                        {
                            return this.CompareValue(value1, value2, (k => int.Parse(k)));
                        }
                    case SortField.DOUBLE:
                        {
                            return this.CompareValue(value1, value2, (k => double.Parse(k)));
                        }
                    case SortField.FLOAT:
                        {
                            return this.CompareValue(value1, value2, (k => float.Parse(k)));
                        }
                    case SortField.LONG:
                        {
                            return this.CompareValue(value1, value2, (k => long.Parse(k)));
                        }
                    case SortField.SHORT:
                        {
                            return this.CompareValue(value1, value2, (k => short.Parse(k)));
                        }
                    case SortField.STRING:
                        {
                            return this.CompareValue(value1, value2, (k => k));
                        }                        
                }
                return 0;
            }
        }
    }
}
