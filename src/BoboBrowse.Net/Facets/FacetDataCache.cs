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

namespace BoboBrowse.Net.Facets
{
    using System;
    using System.Collections.Generic;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using BoboBrowse.Net.Utils;

    [Serializable]
    public class FacetDataCache
    {
        public BigSegmentedArray orderArray;
        public ITermValueList valArray;
        public int[] freqs;
        public int[] minIDs;
        public int[] maxIDs;
        private readonly FacetHandler.TermCountSize termCountSize;

        public FacetDataCache(BigSegmentedArray orderArray, ITermValueList valArray, int[] freqs, int[] minIDs, int[] maxIDs, FacetHandler.TermCountSize termCountSize)
        {
            this.orderArray = orderArray;
            this.valArray = valArray;
            this.freqs = freqs;
            this.minIDs = minIDs;
            this.maxIDs = maxIDs;
            this.termCountSize = termCountSize;
        }

        public FacetDataCache()
        {
            this.orderArray = null;
            this.valArray = null;
            this.maxIDs = null;
            this.minIDs = null;
            this.freqs = null;
            termCountSize = FacetHandler.TermCountSize.Large;
        }

        private static BigSegmentedArray NewInstance(FacetHandler.TermCountSize termCountSize, int maxDoc)
        {
            if (termCountSize == FacetHandler.TermCountSize.Small)
            {
                return new BigByteArray(maxDoc);
            }
            else if (termCountSize == FacetHandler.TermCountSize.Medium)
            {
                return new BigShortArray(maxDoc);
            }
            else
                return new BigIntArray(maxDoc);
        }

        public virtual void Load(string fieldName, IndexReader reader, TermListFactory listFactory)
        {
            string field = string.Intern(fieldName);
            int maxDoc = reader.MaxDoc;

            if (orderArray == null) // we want to reuse the memory
            {
                orderArray = NewInstance(termCountSize, maxDoc);
            }
            else
            {
                orderArray.EnsureCapacity(maxDoc); // no need to fill to 0, we are reseting the data anyway
            }

            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();

            int length = maxDoc + 1;
            ITermValueList list = listFactory == null ? new TermStringList() : listFactory.CreateTermList();
            TermDocs termDocs = reader.TermDocs();
            TermEnum termEnum = reader.Terms(new Term(field));
            int t = 0; // current term number

            list.Add(null);
            minIDList.Add(-1);
            maxIDList.Add(-1);
            freqList.Add(0);
            //int df = 0;
            t++;
            try
            {
                do
                {
                    Term term = termEnum.Term;
                    if (term == null || string.CompareOrdinal(term.Field, field) != 0)
                        break;

                    if (t >= orderArray.MaxValue())
                    {
                        throw new System.IO.IOException("maximum number of value cannot exceed: " + orderArray.MaxValue());
                    }
                    // Alexey: well, we could get now more than one term per document. Effectively, we could build facet againsts tokenized field
                    /*// we expect that there is at most one term per document
                    if (t >= length)
                    {
                        throw new RuntimeException("there are more terms than " + "documents in field \"" + field + "\", but it's impossible to sort on " + "tokenized fields");
                    }*/
                    // store term text
                    list.Add(term.Text);
                    termDocs.Seek(termEnum);
                    // freqList.add(termEnum.docFreq()); // doesn't take into account deldocs
                    int minID = -1;
                    int maxID = -1;
                    int df = 0;
                    if (termDocs.Next())
                    {
                        df++;
                        int docid = termDocs.Doc;
                        orderArray.Add(docid, t);
                        minID = docid;
                        while (termDocs.Next())
                        {
                            df++;
                            docid = termDocs.Doc;
                            orderArray.Add(docid, t);
                        }
                        maxID = docid;
                    }
                    freqList.Add(df);
                    minIDList.Add(minID);
                    maxIDList.Add(maxID);

                    t++;
                } while (termEnum.Next());
            }
            finally
            {
                termDocs.Dispose();
                termEnum.Dispose();
            }
            list.Seal();

            this.valArray = list;
            this.freqs = freqList.ToArray();
            this.minIDs = minIDList.ToArray();
            this.maxIDs = maxIDList.ToArray();
        }

        public static int[] Convert(FacetDataCache dataCache, string[] vals)
        {
            List<int> list = new List<int>(vals.Length);
            for (int i = 0; i < vals.Length; ++i)
            {
                int index = dataCache.valArray.IndexOf(vals[i]);
                if (index >= 0)
                {
                    list.Add(index);
                }
            }
            return list.ToArray();
        }

        public virtual FieldComparator GetScoreDocComparator()
        {
            return new FacetScoreDocComparator(this);
        }

        public class FacetScoreDocComparator : FieldComparator
        {
            private FacetDataCache _dataCache;
            private BigSegmentedArray orderArray;
            public FacetScoreDocComparator(FacetDataCache dataCache)
            {
                _dataCache = dataCache;
                orderArray = _dataCache.orderArray;
            }

            public override int Compare(int slot1, int slot2)
            {
                return orderArray.Get(slot1) - orderArray.Get(slot2);
            }

            public override int CompareBottom(int doc)
            {
                throw new NotImplementedException();
            }

            public override void Copy(int slot, int doc)
            {
                throw new NotImplementedException();
            }

            public override void SetBottom(int slot)
            {
                throw new NotImplementedException();
            }

            public override void SetNextReader(IndexReader reader, int docBase)
            {
                throw new NotImplementedException();
            }

            public override IComparable this[int slot]
            {
                get {
                    int index = orderArray.Get(slot);
                    return _dataCache.valArray.Get(index);
                }
            }
        }

        static void Main3(string[] args)
        {
            Console.WriteLine("byte: " + sbyte.MaxValue);
            Console.WriteLine("short: " + short.MaxValue);
            Console.WriteLine("int: " + int.MaxValue);
        }
    }
}
