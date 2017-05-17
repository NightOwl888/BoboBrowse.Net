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
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Linq;

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public class FacetDataCache
    {
        //private readonly static long serialVersionUID = 1L; // NOT USED

        protected BigSegmentedArray m_orderArray;
        protected ITermValueList m_valArray;
        protected int[] m_freqs;
        protected int[] m_minIDs;
        protected int[] m_maxIDs;

        public FacetDataCache(BigSegmentedArray orderArray, ITermValueList valArray, int[] freqs, int[] minIDs, 
            int[] maxIDs, TermCountSize termCountSize)
        {
            this.m_orderArray = orderArray;
            this.m_valArray = valArray;
            this.m_freqs = freqs;
            this.m_minIDs = minIDs;
            this.m_maxIDs = maxIDs;
        }

        public FacetDataCache()
        {
            this.m_orderArray = null;
            this.m_valArray = null;
            this.m_maxIDs = null;
            this.m_minIDs = null;
            this.m_freqs = null;
        }

        public virtual ITermValueList ValArray
        {
            get { return this.m_valArray; }
            internal set { this.m_valArray = value; }
        }

        public virtual BigSegmentedArray OrderArray
        {
            get { return this.m_orderArray; }
            internal set { this.m_orderArray = value; }
        }

        public virtual int[] Freqs
        {
            get { return m_freqs; }
            internal set { m_freqs = value; }
        }

        public virtual int[] MinIDs
        {
            get { return m_minIDs; }
            internal set { m_minIDs = value; }
        }

        public virtual int[] MaxIDs
        {
            get { return m_maxIDs; }
            internal set { m_maxIDs = value; }
        }

        

        public virtual int GetNumItems(int docid)
        {
            int valIdx = m_orderArray.Get(docid);
            return valIdx <= 0 ? 0 : 1;
        }

        private static BigSegmentedArray NewInstance(int termCount, int maxDoc)
        {
            // we use < instead of <= to take into consideration "missing" value (zero element in the dictionary)
            if (termCount < sbyte.MaxValue)
            {
                return new BigByteArray(maxDoc);
            }
            else if (termCount < short.MaxValue)
            {
                return new BigInt16Array(maxDoc);
            }
            else
                return new BigInt32Array(maxDoc);
        }

        protected int GetDictValueCount(AtomicReader reader, string field)
        {
            int ret = 0;
            Terms terms = reader.GetTerms(field);
            if (terms == null)
            {
                return ret;
            }
            return (int)terms.Count;
        }

        protected int GetNegativeValueCount(AtomicReader reader, string field)
        {
            int ret = 0;
            Terms terms = reader.GetTerms(field);
            if (terms == null)
            {
                return ret;
            }
            TermsEnum termsEnum = terms.GetIterator(null);
            BytesRef text;
            while ((text = termsEnum.Next()) != null)
            {
                if (!text.Utf8ToString().StartsWith("-"))
                {
                    break;
                }
                ret++;
            }
            return ret;
        }

        public virtual void Load(string fieldName, AtomicReader reader, TermListFactory listFactory)
        {
#if FEATURE_STRING_INTERN
            string field = string.Intern(fieldName);
#else
            string field = fieldName;
#endif
            int maxDoc = reader.MaxDoc;

            int dictValueCount = GetDictValueCount(reader, fieldName);
            BigSegmentedArray order = NewInstance(dictValueCount, maxDoc);

            this.m_orderArray = order;

            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();

            int length = maxDoc + 1;
            ITermValueList list = listFactory == null ? (ITermValueList)new TermStringList() : listFactory.CreateTermList();
            int negativeValueCount = GetNegativeValueCount(reader, field);

            int t = 1; // valid term id starts from 1

            list.Add(null);
            minIDList.Add(-1);
            maxIDList.Add(-1);
            freqList.Add(0);
            int totalFreq = 0;
            Terms terms = reader.GetTerms(field);
            if (terms != null) 
            { 
                TermsEnum termsEnum = terms.GetIterator(null);
                  BytesRef text;
                  while ((text = termsEnum.Next()) != null)
                  {
                      // store term text
                      // we expect that there is at most one term per document
                      if (t >= length) throw new RuntimeException("there are more terms than "
                        + "documents in field \"" + field + "\", but it's impossible to sort on "
                        + "tokenized fields");
                      string strText = text.Utf8ToString();
                      list.Add(strText);
                      Term term = new Term(field, strText);
                      DocsEnum docsEnum = reader.GetTermDocsEnum(term);
                      // freqList.add(termEnum.docFreq()); // doesn't take into account
                      // deldocs
                      int minID = -1;
                      int maxID = -1;
                      int docID = -1;
                      int df = 0;
                      int valId = (t - 1 < negativeValueCount) ? (negativeValueCount - t + 1) : t;
                      while ((docID = docsEnum.NextDoc()) != DocsEnum.NO_MORE_DOCS)
                      {
                          df++;
                          order.Add(docID, valId);
                          minID = docID;
                          while (docsEnum.NextDoc() != DocsEnum.NO_MORE_DOCS)
                          {
                              docID = docsEnum.DocID;
                              df++;
                              order.Add(docID, valId);
                          }
                          maxID = docID;
                      }
                      freqList.Add(df);
                      totalFreq += df;
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
            while (doc < maxDoc && order.Get(doc) != 0)
            {
                ++doc;
            }
            if (doc < maxDoc)
            {
                this.m_minIDs[0] = doc;
                // Try to get the max
                doc = maxDoc - 1;
                while (doc >= 0 && order.Get(doc) != 0)
                {
                    --doc;
                }
                this.m_maxIDs[0] = doc;
            }
            this.m_freqs[0] = reader.NumDocs - totalFreq;
        }

        private static int[] ConvertString(FacetDataCache dataCache, string[] vals)
        {
            var list = new List<int>(vals.Length);
            for (int i = 0; i < vals.Length; ++i)
            {
                int index = dataCache.ValArray.IndexOf(vals[i]);
                if (index >= 0)
                {
                    list.Add(index);
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// Same as ConvertString(FacetDataCache dataCache,string[] vals) except that the
        /// values are supplied in raw form so that we can take advantage of the type
        /// information to find index faster.
        /// </summary>
        /// <param name="dataCache"></param>
        /// <param name="vals"></param>
        /// <returns>the array of order indices of the values.</returns>
        public static int[] Convert<T>(FacetDataCache dataCache, T[] vals)
        {
            if (vals != null && (typeof(T) == typeof(string)))
            {
                var valsString = vals.Cast<string>().ToArray();
                return ConvertString(dataCache, valsString);
            }
            var list = new List<int>(vals.Length);
            for (int i = 0; i < vals.Length; ++i)
            {
                int index = -1;
                var valArrayTyped = dataCache.ValArray as TermValueList<T>;
                if (valArrayTyped != null)
                {
                    index = valArrayTyped.IndexOfWithType(vals[i]);
                }
                else
                {
                    index = dataCache.ValArray.IndexOf(vals[i]);
                }
                if (index >= 0)
                {
                    list.Add(index);
                }

            }
            return list.ToArray();
        }
    }

    public class FacetDocComparerSource : DocComparerSource
    {
        private readonly IFacetHandler m_facetHandler;

        public FacetDocComparerSource(IFacetHandler facetHandler)
        {
            m_facetHandler = facetHandler;
        }

        public override DocComparer GetComparer(AtomicReader reader, int docbase)
        {
            if (!(reader is BoboSegmentReader))
                throw new ArgumentException("reader not instance of BoboSegmentReader");
            BoboSegmentReader boboReader = (BoboSegmentReader)reader;
            FacetDataCache dataCache = m_facetHandler.GetFacetData<FacetDataCache>(boboReader);
            BigSegmentedArray orderArray = dataCache.OrderArray;
            return new FacetDocComparer(dataCache, orderArray);
        }

        public class FacetDocComparer : DocComparer
        {
            private readonly FacetDataCache m_dataCache;
            private readonly BigSegmentedArray m_orderArray;

            public FacetDocComparer(FacetDataCache dataCache, BigSegmentedArray orderArray)
            {
                m_dataCache = dataCache;
                m_orderArray = orderArray;
            }

            public override IComparable Value(ScoreDoc doc)
            {
                int index = m_orderArray.Get(doc.Doc);
                return m_dataCache.ValArray.GetComparableValue(index);
            }

            public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
            {
                return m_orderArray.Get(doc1.Doc) - m_orderArray.Get(doc2.Doc);
            }
        }
    }
}
