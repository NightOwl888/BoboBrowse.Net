namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Query.Scoring;
    using BoboBrowse.Net.Util;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.IO;
    
    public class CompactMultiValueFacetHandler : FacetHandler, IFacetHandlerFactory, IFacetScoreable
    {
        private static ILog logger = LogManager.GetLogger(typeof(CompactMultiValueFacetHandler));

        private const int MAX_VAL_COUNT = 32;
        private readonly TermListFactory _termListFactory;
        private FacetDataCache _dataCache;
        private readonly string _indexFieldName;

        public CompactMultiValueFacetHandler(string name, string indexFieldName, TermListFactory termListFactory)
            : base(name)
        {
            _indexFieldName = indexFieldName;
            _termListFactory = termListFactory;
        }

        public CompactMultiValueFacetHandler(string name, TermListFactory termListFactory)
            : this(name, name, termListFactory)
        {
        }

        public CompactMultiValueFacetHandler(string name, string indexFieldName)
            : this(name, indexFieldName, null)
        {
        }

        public CompactMultiValueFacetHandler(string name)
            : this(name, name, null)
        {
        }

        public virtual FacetHandler NewInstance()
        {
            return new CompactMultiValueFacetHandler(Name, _indexFieldName, _termListFactory);
        }

        public override FieldComparator GetComparator(int numDocs, SortField field)
        {
            return _dataCache.GeFieldComparator(numDocs, field.Type);
        }

        public FacetDataCache GetDataCache()
        {
            return _dataCache;
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string @value, Properties prop)
        {
            int index = _dataCache.valArray.IndexOf(@value);
            if (index >= 0)
                return new CompactMultiValueFacetFilter(_dataCache, index);
            else
                return null;
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties prop)
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
                    return EmptyFilter.GetInstance();
                }
            }
            if (filterList.Count == 1)
                return filterList[0];
            return new RandomAccessAndFilter(filterList);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot)
        {
            RandomAccessFilter filter = null;

            int[] indexes = FacetDataCache.Convert(_dataCache, vals);
            if (indexes.Length > 0)
            {
                filter = new CompactMultiValueFacetFilter(_dataCache, indexes);
            }
            else
            {
                filter = EmptyFilter.GetInstance();
            }
            if (isNot)
            {
                filter = new RandomAccessNotFilter(filter);
            }
            return filter;
        }

        public override string[] GetFieldValues(int id)
        {
            int encoded = _dataCache.orderArray.Get(id);
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
                        valList.Add(_dataCache.valArray.Get(count));
                    }
                    count++;
                    encoded >>= 1;
                }
                return valList.ToArray();
            }
        }

        public override object[] GetRawFieldValues(int id)
        {
            int encoded = _dataCache.orderArray.Get(id);
            if (encoded == 0)
            {
                return new object[0];
            }
            else
            {
                int count = 1;
                List<object> valList = new List<object>(MAX_VAL_COUNT);

                while (encoded != 0)
                {
                    if ((encoded & 0x00000001) != 0x0)
                    {
                        valList.Add(_dataCache.valArray.GetRawValue(count));
                    }
                    count++;
                    encoded >>= 1;
                }
                return valList.ToArray();
            }
        }

        public override IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec ospec)
        {
            return new CompactMultiValueFacetCountCollector(sel, _dataCache, Name, ospec);
        }

        public override void Load(BoboIndexReader reader)
        {
            int maxDoc = reader.MaxDoc;

            BigIntArray order = new BigIntArray(maxDoc);

            ITermValueList mterms = _termListFactory == null ? new TermStringList() : _termListFactory.CreateTermList();

            List<int> minIDList = new List<int>();
            List<int> maxIDList = new List<int>();
            List<int> freqList = new List<int>();

            TermDocs termDocs = null;
            TermEnum termEnum = null;
            int t = 0; // current term number
            mterms.Add(null);
            minIDList.Add(-1);
            maxIDList.Add(-1);
            freqList.Add(0);
            t++;
            try
            {
                termDocs = reader.TermDocs();
                termEnum = reader.Terms(new Term(_indexFieldName, ""));
                do
                {
                    if (termEnum == null)
                        break;
                    Term term = termEnum.Term;
                    if (term == null || !_indexFieldName.Equals(term.Field))
                        break;

                    // store term text
                    // we expect that there is at most one term per document
                    if (t > MAX_VAL_COUNT)
                    {
                        throw new IOException("maximum number of value cannot exceed: " + MAX_VAL_COUNT);
                    }
                    string val = term.Text;
                    mterms.Add(val);
                    int bit = (0x00000001 << (t - 1));
                    termDocs.Seek(termEnum);
                    //freqList.add(termEnum.docFreq());  // removed because the df doesn't take into account the num of deletedDocs
                    int df = 0;
                    int minID = -1;
                    int maxID = -1;
                    if (termDocs.Next())
                    {
                        df++;
                        int docid = termDocs.Doc;
                        order.Add(docid, order.Get(docid) | bit);
                        minID = docid;
                        while (termDocs.Next())
                        {
                            df++;
                            docid = termDocs.Doc;
                            order.Add(docid, order.Get(docid) | bit);
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
                try
                {
                    if (termDocs != null)
                    {
                        termDocs.Dispose();
                    }
                }
                finally
                {
                    if (termEnum != null)
                    {
                        termEnum.Dispose();
                    }
                }
            }

            mterms.Seal();

            _dataCache = new FacetDataCache(order, mterms, freqList.ToArray(), minIDList.ToArray(), maxIDList.ToArray(), TermCountSize.Large);
        }

        public virtual BoboDocScorer GetDocScorer(IFacetTermScoringFunctionFactory scoringFunctionFactory, Dictionary<string, float> boostMap)
        {
            float[] boostList = BoboDocScorer.BuildBoostList(_dataCache.valArray.GetInnerList(), boostMap);
            return new CompactMultiValueDocScorer(_dataCache, scoringFunctionFactory, boostList);
        }

        private sealed class CompactMultiValueDocScorer : BoboDocScorer
        {
            private readonly FacetDataCache _dataCache;
            internal CompactMultiValueDocScorer(FacetDataCache dataCache, IFacetTermScoringFunctionFactory scoreFunctionFactory, float[] boostList)
                : base(scoreFunctionFactory.GetFacetTermScoringFunction(dataCache.valArray.Count, dataCache.orderArray.Size()), boostList)
            {
                _dataCache = dataCache;
            }

            public override Explanation Explain(int doc)
            {
                int encoded = _dataCache.orderArray.Get(doc);

                int count = 1;
                List<float> scoreList = new List<float>(_dataCache.valArray.Count);
                List<Explanation> explList = new List<Explanation>(scoreList.Count);
                while (encoded != 0)
                {
                    if ((encoded & 0x00000001) != 0x0)
                    {
                        int idx = count - 1;
                        scoreList.Add(Function.Score(_dataCache.freqs[idx], BoostList[idx]));
                        explList.Add(Function.Explain(_dataCache.freqs[idx], BoostList[idx]));
                    }
                    count++;
                    encoded >>= 1;
                }
                Explanation topLevel = Function.Explain(scoreList.ToArray());
                foreach (Explanation sub in explList)
                {
                    topLevel.AddDetail(sub);
                }
                return topLevel;
            }

            public override sealed float Score(int docid)
            {
                Function.ClearScores();
                int encoded = _dataCache.orderArray.Get(docid);

                int count = 1;

                while (encoded != 0)
                {
                    int idx = count - 1;
                    if ((encoded & 0x00000001) != 0x0)
                    {
                        Function.ScoreAndCollect(_dataCache.freqs[idx], BoostList[idx]);
                    }
                    count++;
                    encoded >>= 1;
                }
                return Function.GetCurrentScore();
            }

        }

        private sealed class CompactMultiValueFacetCountCollector : DefaultFacetCountCollector
        {
            private readonly new BigSegmentedArray _array;
            private readonly int[] _combinationCount = new int[16 * 8];
            private int _noValCount = 0;
            private bool _aggregated = false;


            internal CompactMultiValueFacetCountCollector(BrowseSelection sel, FacetDataCache dataCache, string name, FacetSpec ospec)
                : base(sel, dataCache, name, ospec)
            {
                _array = _dataCache.orderArray;
            }


            public override sealed void CollectAll()
            {
                _count = _dataCache.freqs;
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
                        encoded = (int)((uint)encoded >> 4);
                        if (encoded == 0)
                            break;
                        offset += 16;
                    }
                }
            }

            public override BrowseFacet GetFacet(string @value)
            {
                if (!_aggregated)
                    aggregateCounts();
                return base.GetFacet(@value);
            }

            public override int[] GetCountDistribution()
            {
                if (!_aggregated)
                    aggregateCounts();
                return _count;
            }

            public override IEnumerable<BrowseFacet> GetFacets()
            {
                if (!_aggregated)
                    aggregateCounts();
                return base.GetFacets();
            }

            private void aggregateCounts()
            {
                _count[0] = _noValCount;

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
                                _count[index + offset] += count;
                            }
                            index++;
                            encoded >>= 1;
                        }
                    }
                }
                _aggregated = true;
            }
        }
    }
}