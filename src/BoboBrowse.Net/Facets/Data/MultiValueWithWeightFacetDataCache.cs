// Version compatibility level: 3.1.0
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
    using System.Linq;
    using System.Text;

    public class MultiValueWithWeightFacetDataCache<T> : MultiValueFacetDataCache<T>
    {
        private static long serialVersionUID = 1L;

        public readonly BigNestedIntArray _weightArray;

        public MultiValueWithWeightFacetDataCache()
        {
            _weightArray = new BigNestedIntArray();
        }

        public override void Load(string fieldName, IndexReader reader, TermListFactory listFactory, BoboIndexReader.WorkArea workArea)
        {
            long t0 = System.Environment.TickCount;
            int maxdoc = reader.MaxDoc;
            BigNestedIntArray.BufferedLoader loader = GetBufferedLoader(maxdoc, workArea);
            BigNestedIntArray.BufferedLoader weightLoader = GetBufferedLoader(maxdoc, null);

            TermEnum tenum = null;
            TermDocs tdoc = null;
            var list = (listFactory == null ? new TermStringList() : listFactory.CreateTermList());
            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();
            OpenBitSet bitset = new OpenBitSet(maxdoc + 1);
            int negativeValueCount = GetNegativeValueCount(reader, string.Intern(fieldName));
            int t = 0; // current term number
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
            int valId = 0;

            try
            {
                tdoc = reader.TermDocs();
                tenum = reader.Terms(new Term(fieldName, ""));
                if (tenum != null)
                {
                    do
                    {
                        Term term = tenum.Term;
                        if (term == null || !fieldName.Equals(term.field()))
                            break;

                        string val = term.text();

                        if (val != null)
                        {
                            int weight = 0;
                            //string[] split = val.Split("\u0000");
                            string[] split = val.Split('\0'); // TODO: Verify this
                            if (split.Length > 1)
                            {
                                val = split[0];
                                weight = int.Parse(split[split.Length - 1]);
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

                            tdoc.Seek(tenum);
                            if (tdoc.Next())
                            {
                                df++;
                                int docid = tdoc.Doc;

                                if (!loader.Add(docid, valId)) LogOverflow(fieldName);
                                else weightLoader.Add(docid, weight);

                                if (docid < minID) minID = docid;
                                bitset.FastSet(docid);
                                while (tdoc.Next())
                                {
                                    df++;
                                    docid = tdoc.Doc;

                                    if (!loader.Add(docid, valId)) LogOverflow(fieldName);
                                    else weightLoader.Add(docid, weight);

                                    bitset.FastSet(docid);
                                }
                                if (docid > maxID) maxID = docid;
                            }
                            pre = val;
                        }

                    }
                    while (tenum.Next());
                    if (pre != null)
                    {
                        freqList.Add(df);
                        minIDList.Add(minID);
                        maxIDList.Add(maxID);
                    }
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
                _weightArray.Load(maxdoc + 1, weightLoader);
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
    }
}
