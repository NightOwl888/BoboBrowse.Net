//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Written in Java.
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
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public interface IFacetDataCache
    {
        BigSegmentedArray OrderArray { get; }
        ITermValueList ValArray { get; }
        int[] Freqs { get; }
        int[] MinIDs { get; }
        int[] MaxIDs { get; }
        int GetNumItems(int docid);
        void Load(string fieldName, IndexReader reader, TermListFactory listFactory);
    }

    public class FacetDataCache_Static
    {
        private static int[] ConvertString(IFacetDataCache dataCache, string[] vals)
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
        /// Same as ConvertString(FacetDataCache dataCache,String[] vals) except that the
        /// values are supplied in raw form so that we can take advantage of the type
        /// information to find index faster.
        /// </summary>
        /// <param name="dataCache"></param>
        /// <param name="vals"></param>
        /// <returns>the array of order indices of the values.</returns>
        public static int[] Convert<T>(IFacetDataCache dataCache, T[] vals)
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

    [Serializable]
    public class FacetDataCache<T> : IFacetDataCache
    {
        private static ILog logger = LogManager.GetLogger<FacetDataCache<T>>();

        private readonly static long serialVersionUID = 1L;

        protected BigSegmentedArray orderArray;
        protected ITermValueList valArray;
        protected int[] freqs;
        protected int[] minIDs;
        protected int[] maxIDs;
        private readonly TermCountSize _termCountSize;

        public FacetDataCache(BigSegmentedArray orderArray, ITermValueList valArray, int[] freqs, int[] minIDs, 
            int[] maxIDs, TermCountSize termCountSize)
        {
            this.orderArray = orderArray;
            this.valArray = valArray;
            this.freqs = freqs;
            this.minIDs = minIDs;
            this.maxIDs = maxIDs;
            _termCountSize = termCountSize;
        }

        public FacetDataCache()
        {
            this.orderArray = null;
            this.valArray = null;
            this.maxIDs = null;
            this.minIDs = null;
            this.freqs = null;
            _termCountSize = TermCountSize.Large;
        }

        public virtual ITermValueList ValArray
        {
            get { return this.valArray; }
        }

        public virtual BigSegmentedArray OrderArray
        {
            get { return this.orderArray; }
        }

        public virtual int[] Freqs
        {
            get { return freqs; }
        }

        public virtual int[] MinIDs
        {
            get { return minIDs; }
        }

        public virtual int[] MaxIDs
        {
            get { return maxIDs; }
        }

        

        public virtual int GetNumItems(int docid)
        {
            int valIdx = orderArray.Get(docid);
            return valIdx <= 0 ? 0 : 1;
        }

        private static BigSegmentedArray NewInstance(TermCountSize termCountSize, int maxDoc)
        {
            if (termCountSize == TermCountSize.Small)
            {
                return new BigByteArray(maxDoc);
            }
            else if (termCountSize == TermCountSize.Medium)
            {
                return new BigShortArray(maxDoc);
            }
            else
                return new BigIntArray(maxDoc);
        }

        protected int GetNegativeValueCount(IndexReader reader, string field)
        {
            int ret = 0;
            TermEnum termEnum = null;
            try
            {
                termEnum = reader.Terms(new Term(field, ""));
                do
                {
                    Term term = termEnum.Term;
                    if (term == null || string.CompareOrdinal(term.Field, field) != 0)
                        break;
                    if (!term.Text.StartsWith("-"))
                    {
                        break;
                    }
                    ret++;
                } while (termEnum.Next());
            }
            finally
            {
                termEnum.Close();
            }
            return ret;
        }

        public virtual void Load(string fieldName, IndexReader reader, TermListFactory listFactory)
        {
            string field = string.Intern(fieldName);
            int maxDoc = reader.MaxDoc;

            BigSegmentedArray order = this.orderArray;
            if (order == null) // we want to reuse the memory
            {
                order = NewInstance(_termCountSize, maxDoc);
            }
            else
            {
                order.EnsureCapacity(maxDoc); // no need to fill to 0, we are reseting the 
                                              // data anyway
            }
            this.orderArray = order;

            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();

            int length = maxDoc + 1;
            ITermValueList list = listFactory == null ? (ITermValueList)new TermStringList() : listFactory.CreateTermList();
            int negativeValueCount = GetNegativeValueCount(reader, field);

            TermDocs termDocs = reader.TermDocs();
            TermEnum termEnum = reader.Terms(new Term(field, ""));
            int t = 0; // current term number

            list.Add(null);
            minIDList.Add(-1);
            maxIDList.Add(-1);
            freqList.Add(0);
            int totalFreq = 0;
            //int df = 0;
            t++;
            try
            {
                do
                {
                    Term term = termEnum.Term;
                    if (term == null || string.CompareOrdinal(term.Field, field) != 0)
                        break;

                    if (t > orderArray.MaxValue)
                    {
                        throw new System.IO.IOException("maximum number of value cannot exceed: " + orderArray.MaxValue);
                    }
                    // store term text
                    // we expect that there is at most one term per document

                    // Alexey: well, we could get now more than one term per document. Effectively, we could build facet againsts tokenized field
                    // NightOwl888: This check was commented by Alexey, but was replaced to align with the 3.1.0 source.
                    if (t >= length)
                    {
                        throw new RuntimeException("there are more terms than " + "documents in field \"" + field 
                            + "\", but it's impossible to sort on " + "tokenized fields");
                    }
                    list.Add(term.Text);
                    termDocs.Seek(termEnum);
                    // freqList.add(termEnum.docFreq()); // doesn't take into account deldocs
                    int minID = -1;
                    int maxID = -1;
                    int df = 0;
                    int valId = (t - 1 < negativeValueCount) ? (negativeValueCount - t + 1) : t;
                    if (termDocs.Next())
                    {
                        df++;
                        int docid = termDocs.Doc;
                        order.Add(docid, valId);
                        minID = docid;
                        while (termDocs.Next())
                        {
                            df++;
                            docid = termDocs.Doc;
                            order.Add(docid, valId);
                        }
                        maxID = docid;
                    }
                    freqList.Add(df);
                    totalFreq += df;
                    minIDList.Add(minID);
                    maxIDList.Add(maxID);

                    t++;
                } while (termEnum.Next());
            }
            finally
            {
                termDocs.Close();
                termEnum.Close();
            }
            list.Seal();
            this.valArray = list;
            this.freqs = freqList.ToArray();
            this.minIDs = minIDList.ToArray();
            this.maxIDs = maxIDList.ToArray();

            int doc = 0;
            while (doc <= maxDoc && order.Get(doc) != 0)
            {
                ++doc;
            }
            if (doc <= maxDoc)
            {
                this.minIDs[0] = doc;
                // Try to get the max
                doc = maxDoc;
                while (doc > 0 && order.Get(doc) != 0)
                {
                    --doc;
                }
                if (doc > 0)
                {
                    this.maxIDs[0] = doc;
                }
            }
            this.freqs[0] = maxDoc + 1 - totalFreq;
        }

        //private static int[] ConvertString(IFacetDataCache dataCache, string[] vals)
        //{
        //    var list = new List<int>(vals.Length);
        //    for (int i = 0; i < vals.Length; ++i)
        //    {
        //        int index = dataCache.ValueArray.IndexOf(vals[i]);
        //        if (index >= 0)
        //        {
        //            list.Add(index);
        //        }
        //    }
        //    return list.ToArray();
        //}

        ///// <summary>
        ///// Same as ConvertString(FacetDataCache dataCache,String[] vals) except that the
        ///// values are supplied in raw form so that we can take advantage of the type
        ///// information to find index faster.
        ///// </summary>
        ///// <param name="dataCache"></param>
        ///// <param name="vals"></param>
        ///// <returns>the array of order indices of the values.</returns>
        //public static int[] Convert(IFacetDataCache dataCache, T[] vals) 
        //{
        //    if (vals != null && (typeof(T) == typeof(string)))
        //    {
        //        var valsString = vals.Cast<string>().ToArray();
        //        return ConvertString(dataCache, valsString);
        //    }
        //    var list = new List<int>(vals.Length);
        //    for (int i = 0; i < vals.Length; ++i) 
        //    {
        //        int index = -1;
        //        var valArrayTyped = dataCache.ValueArray as TermValueList<T>;
        //        if (valArrayTyped != null)
        //        {
        //            index = valArrayTyped.IndexOfWithType(vals[i]);
        //        }
        //        else
        //        {
        //            index = dataCache.ValueArray.IndexOf(vals[i]);
        //        }
        //        if (index >= 0)
        //        {
        //            list.Add(index);
        //        }
                
        //    }
        //    return list.ToArray();
        //}

        public class FacetDocComparatorSource : DocComparatorSource
        {
            private FacetHandler<FacetDataCache<T>> _facetHandler;

            public FacetDocComparatorSource(FacetHandler<FacetDataCache<T>> facetHandler)
            {
                _facetHandler = facetHandler;
            }

            public override DocComparator GetComparator(IndexReader reader, int docbase)
            {
                if (!(reader.GetType().Equals(typeof(BoboIndexReader))))
                    throw new ArgumentException("reader not instance of BoboIndexReader");
                BoboIndexReader boboReader = (BoboIndexReader)reader;
                var dataCache = _facetHandler.GetFacetData(boboReader);
                var orderArray = dataCache.orderArray;
                return new MyDocComparator(dataCache, orderArray);
            }

            public class MyDocComparator : DocComparator
            {
                private readonly FacetDataCache<T> _dataCache;
                private readonly BigSegmentedArray _orderArray;

                public MyDocComparator(FacetDataCache<T> dataCache, BigSegmentedArray orderArray)
                {
                    _dataCache = dataCache;
                    _orderArray = orderArray;
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    int index = _orderArray.Get(doc.Doc);
                    return _dataCache.ValueArray.GetComparableValue(index);
                }
            }
        }
    }
}
