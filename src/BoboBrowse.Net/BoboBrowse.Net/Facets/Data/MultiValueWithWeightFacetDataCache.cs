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
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Index;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;

    public class MultiValueWithWeightFacetDataCache : MultiValueFacetDataCache
    {
        //private static long serialVersionUID = 1L; // NOT USED

        protected readonly BigNestedIntArray _weightArray;

        public MultiValueWithWeightFacetDataCache()
        {
            _weightArray = new BigNestedIntArray();
        }

        /// <summary>
        /// Added in .NET version as an accessor to the _weightArray field.
        /// </summary>
        /// <returns></returns>
        public virtual BigNestedIntArray WeightArray
        {
            get { return _weightArray; }
        }

        public override void Load(string fieldName, AtomicReader reader, TermListFactory listFactory, BoboSegmentReader.WorkArea workArea)
        {
            string field = string.Intern(fieldName);
            int maxdoc = reader.MaxDoc;
            BigNestedIntArray.BufferedLoader loader = GetBufferedLoader(maxdoc, workArea);
            BigNestedIntArray.BufferedLoader weightLoader = GetBufferedLoader(maxdoc, null);

            var list = (listFactory == null ? new TermStringList() : listFactory.CreateTermList());
            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();
            OpenBitSet bitset = new OpenBitSet(maxdoc + 1);
            int negativeValueCount = GetNegativeValueCount(reader, string.Intern(fieldName));
            int t = 1; // valid term id starts from 1
            list.Add(null);
            minIDList.Add(-1);
            maxIDList.Add(-1);
            freqList.Add(0);
            t++;

            _overflow = false;

            string pre = null;

            int df = 0;
            int minID = -1;
            int maxID = -1;
            int docID = -1;
            int valId = 0;

            Terms terms = reader.Terms(field);
            if (terms != null)
            {
                TermsEnum termsEnum = terms.Iterator(null);
                BytesRef text;
                while ((text = termsEnum.Next()) != null)
                {
                    string strText = text.Utf8ToString();
                    string val = null;
                    int weight = 0;
                    string[] split = strText.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length > 1)
                    {
                        val = split[0];
                        weight = int.Parse(split[split.Length - 1]);
                    }
                    else
                    {
                        continue;
                    }

                    if (pre == null || !val.Equals(pre))
                    {
                        if (pre != null)
                        {
                            freqList.Add(df);
                            minIDList.Add(minID);
                            maxIDList.Add(maxID);
                        }
                        list.Add(val);
                        df = 0;
                        minID = -1;
                        maxID = -1;
                        valId = (t - 1 < negativeValueCount) ? (negativeValueCount - t + 1) : t;
                        t++;
                    }

                    Term term = new Term(field, strText);
                    DocsEnum docsEnum = reader.TermDocsEnum(term);
                    if (docsEnum != null)
                    {
                        while ((docID = docsEnum.NextDoc()) != DocsEnum.NO_MORE_DOCS)
                        {
                            df++;

                            if (!loader.Add(docID, valId))
                            {
                                LogOverflow(fieldName);
                            }
                            else
                            {
                                weightLoader.Add(docID, weight);
                            }

                            if (docID < minID) minID = docID;
                            bitset.FastSet(docID);
                            while (docsEnum.NextDoc() != DocsEnum.NO_MORE_DOCS)
                            {
                                docID = docsEnum.DocID();
                                df++;
                                if (!loader.Add(docID, valId))
                                {
                                    LogOverflow(fieldName);
                                }
                                else
                                {
                                    weightLoader.Add(docID, weight);
                                }
                                bitset.FastSet(docID);
                            }
                            if (docID > maxID) maxID = docID;
                        }
                    }
                    pre = val;
                }
                if (pre != null)
                {
                    freqList.Add(df);
                    minIDList.Add(minID);
                    maxIDList.Add(maxID);
                }
            }

            list.Seal();

            try
            {
                _nestedArray.Load(maxdoc + 1, loader);
                _weightArray.Load(maxdoc + 1, weightLoader);
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
    }
}
