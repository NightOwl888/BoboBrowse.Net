// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Query.Scoring;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.IO;
    
    public class CompactMultiValueFacetHandler : FacetHandler<IFacetDataCache>, IFacetScoreable
    {
        private static ILog logger = LogManager.GetLogger<CompactMultiValueFacetHandler>();

        private const int MAX_VAL_COUNT = 32;
        private readonly TermListFactory _termListFactory;
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

        public override DocComparatorSource GetDocComparatorSource()
        {
            return new CompactMultiFacetDocComparatorSource(this);
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, Properties prop)
        {
            return new CompactMultiValueFacetFilter(this, value);
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

            if (vals.Length > 0)
            {
                filter = new CompactMultiValueFacetFilter(this, vals);
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

        private static int CountBits(int val)
        {
            int c = 0;
            for (c = 0; val > 0; c++)
            {
                val &= val - 1;
            }
            return c;
        }

        public override int GetNumItems(BoboIndexReader reader, int id)
        {
            IFacetDataCache dataCache = GetFacetData<IFacetDataCache>(reader);
            if (dataCache == null) return 0;
            int encoded = dataCache.OrderArray.Get(id);
            return CountBits(encoded);
        }

        public override string[] GetFieldValues(BoboIndexReader reader, int id)
        {
            IFacetDataCache dataCache = GetFacetData<IFacetDataCache>(reader);
            if (dataCache == null) return new string[0];
            int encoded = dataCache.OrderArray.Get(id);
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
                        valList.Add(dataCache.ValArray.Get(count));
                    }
                    count++;
                    encoded = (int)(((uint)encoded) >> 1);
                }
                return valList.ToArray();
            }
        }

        public override object[] GetRawFieldValues(BoboIndexReader reader, int id)
        {
            IFacetDataCache dataCache = GetFacetData<IFacetDataCache>(reader);
            if (dataCache == null) return new string[0];
            int encoded = dataCache.OrderArray.Get(id);
            if (encoded == 0)
            {
                return new object[0];
            }
            else
            {
                int count = 1;
                List<Object> valList = new List<Object>(MAX_VAL_COUNT);

                while (encoded != 0)
                {
                    if ((encoded & 0x00000001) != 0x0)
                    {
                        valList.Add(dataCache.ValArray.GetRawValue(count));
                    }
                    count++;
                    encoded = (int)(((uint)encoded) >> 1);
                }
                return valList.ToArray();
            }
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec)
        {
            return new CompactMultiValueFacetCountCollectorSource(this.GetFacetData<IFacetDataCache>, _name, sel, fspec);
        }

        public class CompactMultiValueFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly Func<BoboIndexReader, IFacetDataCache> getFacetData;
            private readonly string _name;
            private readonly BrowseSelection _sel;
            private readonly FacetSpec _ospec;
            
            public CompactMultiValueFacetCountCollectorSource(Func<BoboIndexReader, IFacetDataCache> getFacetData, string name, BrowseSelection sel, FacetSpec ospec)
            {
                this.getFacetData = getFacetData;
                _name = name;
                _ospec = ospec;
                _sel = sel;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase)
            {
                IFacetDataCache dataCache = getFacetData(reader);
                return new CompactMultiValueFacetCountCollector(_name, _sel, dataCache, docBase, _ospec);
            }
        }

        public override IFacetDataCache Load(BoboIndexReader reader)
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
                        termDocs.Close();
                    }
                }
                finally
                {
                    if (termEnum != null)
                    {
                        termEnum.Close();
                    }
                }
            }

            mterms.Seal();

            return new FacetDataCache(order, mterms, freqList.ToArray(), minIDList.ToArray(), maxIDList.ToArray(), TermCountSize.Large);
        }

        private class CompactMultiFacetDocComparatorSource : DocComparatorSource
        {
            private readonly CompactMultiValueFacetHandler _facetHandler;
            public CompactMultiFacetDocComparatorSource(CompactMultiValueFacetHandler facetHandler)
            {
                _facetHandler = facetHandler;
            }

            public override DocComparator GetComparator(IndexReader reader, int docbase)
            {
                if (!(reader is BoboIndexReader))
                    throw new InvalidOperationException("reader must be instance of BoboIndexReader");
                var boboReader = (BoboIndexReader)reader;
                IFacetDataCache dataCache = _facetHandler.GetFacetData<IFacetDataCache>(boboReader);
                return new CompactMultiValueDocComparator(dataCache, _facetHandler, boboReader);
            }

            public class CompactMultiValueDocComparator : DocComparator
            {
                private readonly IFacetDataCache _dataCache;
                private readonly IFacetHandler _facetHandler;
                private readonly BoboIndexReader _reader;

                public CompactMultiValueDocComparator(IFacetDataCache dataCache, IFacetHandler facetHandler, BoboIndexReader reader)
                {
                    _dataCache = dataCache;
                    _facetHandler = facetHandler;
                    _reader = reader;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    int encoded1 = _dataCache.OrderArray.Get(doc1.Doc);
                    int encoded2 = _dataCache.OrderArray.Get(doc2.Doc);
                    return encoded1 - encoded2;
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    return new StringArrayComparator(_facetHandler.GetFieldValues(_reader, doc.Doc));
                }
            }
        }

        public virtual BoboDocScorer GetDocScorer(BoboIndexReader reader, IFacetTermScoringFunctionFactory scoringFunctionFactory, IDictionary<string, float> boostMap)
        {
            IFacetDataCache dataCache = GetFacetData<IFacetDataCache>(reader);
            float[] boostList = BoboDocScorer.BuildBoostList(dataCache.ValArray, boostMap);
            return new CompactMultiValueDocScorer(dataCache, scoringFunctionFactory, boostList);
        }

        private sealed class CompactMultiValueDocScorer : BoboDocScorer
        {
            private readonly IFacetDataCache _dataCache;
            internal CompactMultiValueDocScorer(IFacetDataCache dataCache, IFacetTermScoringFunctionFactory scoreFunctionFactory, float[] boostList)
                : base(scoreFunctionFactory.GetFacetTermScoringFunction(dataCache.ValArray.Count, dataCache.OrderArray.Size()), boostList)
            {
                _dataCache = dataCache;
            }

            public override Explanation Explain(int doc)
            {
                int encoded = _dataCache.OrderArray.Get(doc);

                int count = 1;
                List<float> scoreList = new List<float>(_dataCache.ValArray.Count);
                List<Explanation> explList = new List<Explanation>(scoreList.Count);
                while (encoded != 0)
                {
                    if ((encoded & 0x00000001) != 0x0)
                    {
                        int idx = count - 1;
                        scoreList.Add(_function.Score(_dataCache.Freqs[idx], _boostList[idx]));
                        explList.Add(_function.Explain(_dataCache.Freqs[idx], _boostList[idx]));
                    }
                    count++;
                    encoded = (int)(((uint)encoded) >> 1);
                }
                Explanation topLevel = _function.Explain(scoreList.ToArray());
                foreach (Explanation sub in explList)
                {
                    topLevel.AddDetail(sub);
                }
                return topLevel;
            }

            public override sealed float Score(int docid)
            {
                _function.ClearScores();
                int encoded = _dataCache.OrderArray.Get(docid);

                int count = 1;

                while (encoded != 0)
                {
                    int idx = count - 1;
                    if ((encoded & 0x00000001) != 0x0)
                    {
                        _function.ScoreAndCollect(_dataCache.Freqs[idx], _boostList[idx]);
                    }
                    count++;
                    encoded = (int)(((uint)encoded) >> 1);
                }
                return _function.GetCurrentScore();
            }
        }

        private sealed class CompactMultiValueFacetCountCollector : DefaultFacetCountCollector
        {
            private readonly new BigSegmentedArray _array;
            private readonly int[] _combinationCount = new int[16 * 8];
            private int _noValCount = 0;
            private bool _aggregated = false;


            internal CompactMultiValueFacetCountCollector(string name, BrowseSelection sel, IFacetDataCache dataCache, int docBase, FacetSpec ospec)
                : base(name, dataCache, docBase, sel, ospec)
            {
                _array = _dataCache.OrderArray;
            }


            public override sealed void CollectAll()
            {
                _count = _dataCache.Freqs;
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
                        encoded = (int)(((uint)encoded) >> 4);
                        if (encoded == 0)
                            break;
                        offset += 16;
                    }
                }
            }

            public override BrowseFacet GetFacet(string value)
            {
                if (!_aggregated)
                    AggregateCounts();
                return base.GetFacet(value);
            }

            public override int GetFacetHitsCount(object value)
            {
                if (!_aggregated)
                    AggregateCounts();
                return base.GetFacetHitsCount(value);
            }

            public override int[] GetCountDistribution()
            {
                if (!_aggregated)
                    AggregateCounts();
                return _count;
            }

            public override IEnumerable<BrowseFacet> GetFacets()
            {
                if (!_aggregated)
                    AggregateCounts();
                return base.GetFacets();
            }

            private void AggregateCounts()
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
                            encoded = (int)(((uint)encoded) >> 1);
                        }
                    }
                }
                _aggregated = true;
            }

            public override FacetIterator Iterator()
            {
                if (!_aggregated) AggregateCounts();
                return base.Iterator();
            }
        }
    }
}