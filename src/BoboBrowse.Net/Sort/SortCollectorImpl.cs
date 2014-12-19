// Version compatibility level: 3.1.0
// EXCEPTION: MemoryCache
namespace BoboBrowse.Net.Sort
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    public class SortCollectorImpl : SortCollector
    {
        private static IComparer<MyScoreDoc> MERGE_COMPATATOR = new MyScoreDocComparator();

        private class MyScoreDocComparator : IComparer<MyScoreDoc>
        {
            public int Compare(MyScoreDoc o1, MyScoreDoc o2)
            {
                var s1 = o1.Value;
                var s2 = o2.Value;

                int r;
                if (s1 == null)
                {
                    if (s2 == null)
                    {
                        r = 0;
                    }
                    else
                    {
                        r = -1;
                    }
                }
                else if (s2 == null)
                {
                    r = 1;
                }
                else
                {
                    int v = s1.CompareTo(s2);
                    if (v == 0)
                    {
                        r = o1.Doc + o1.queue.@base - o2.Doc - o2.queue.@base;
                    }
                    else
                    {
                        r = v;
                    }
                }

                return r;
            }
        }

        private readonly List<DocIDPriorityQueue> _pqList;
        private readonly int _numHits;
        private int _totalHits;
        private int _totalGroups;
        private ScoreDoc _bottom;
        private ScoreDoc _tmpScoreDoc;
        private bool _queueFull;
        private DocComparator _currentComparator;
        private DocComparatorSource _compSource;
        private DocIDPriorityQueue _currentQueue;
        private BoboIndexReader _currentReader = null;
        private IFacetCountCollector _facetCountCollector;
        private IFacetCountCollector[] _facetCountCollectorMulti = null;

        private readonly bool _doScoring;
        private Scorer _scorer;
        private readonly int _offset;
        private readonly int _count;

        private readonly IBrowsable _boboBrowser;

        // NightOwl888: The _collectDocIdCache setting seems to put arrays into
        // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.
        //private readonly bool _collectDocIdCache;
        private CombinedFacetAccessible[] _groupAccessibles;
        private readonly List<IFacetAccessible>[] _facetAccessibleLists;
        //private readonly Int2ObjectOpenHashMap<ScoreDoc> _currentValueDocMaps;
        private readonly IDictionary<int, ScoreDoc> _currentValueDocMaps;

        protected class MyScoreDoc : ScoreDoc
        {
            private static long serialVersionUID = 1L;

            public DocIDPriorityQueue queue;
            public BoboIndexReader reader;
            public IComparable sortValue;

            public MyScoreDoc()
                : this(0, 0.0f, null, null)
            {
            }

            public MyScoreDoc(int docid, float score, DocIDPriorityQueue queue, BoboIndexReader reader)
                : base(docid, score)
            {
                this.queue = queue;
                this.reader = reader;
                this.sortValue = null;
            }

            public IComparable Value
            {
                get
                {
                    if (sortValue == null)
                        sortValue = queue.SortValue(this);
                    return sortValue;
                }
            }
        }

        // NightOwl888: The _collectDocIdCache setting seems to put arrays into
        // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.
        //private CollectorContext _currentContext;
        private int[] _currentDocIdArray;
        private float[] _currentScoreArray;
        private int _docIdArrayCursor = 0;
        private int _docIdCacheCapacity = 0;
        private IEnumerable<string> _termVectorsToFetch;

        public SortCollectorImpl(
            DocComparatorSource compSource,
            SortField[] sortFields,
            IBrowsable boboBrowser,
            int offset,
            int count,
            bool doScoring,
            bool fetchStoredFields,
            IEnumerable<string> termVectorsToFetch,
            string[] groupBy,
            int maxPerGroup,
            bool collectDocIdCache)
            : base(sortFields, fetchStoredFields)
        {
            // TODO: Make this a guard clause?
            Debug.Assert(offset >= 0 && count >= 0);
            _boboBrowser = boboBrowser;
            _compSource = compSource;
            _pqList = new List<DocIDPriorityQueue>();
            _numHits = offset + count;
            _offset = offset;
            _count = count;
            _totalHits = 0;
            _totalGroups = 0;
            _queueFull = false;
            _doScoring = doScoring;
            _tmpScoreDoc = new MyScoreDoc();
            _termVectorsToFetch = termVectorsToFetch;

            // NightOwl888: The _collectDocIdCache setting seems to put arrays into
            // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.
            //_collectDocIdCache = collectDocIdCache || groupBy != null;

            if (groupBy != null && groupBy.Length != 0)
            {
                var groupByList = new List<IFacetHandler>(groupBy.Length);
                foreach (string field in groupBy)
                {
                    IFacetHandler handler = boboBrowser.GetFacetHandler(field);
                    if (handler != null)
                        groupByList.Add(handler);
                }
                if (groupByList.Count > 0)
                {
                    this.groupByMulti = groupByList.ToArray();
                    this.groupBy = groupByMulti[0];
                }
                if (this.groupBy != null && _count > 0)
                {
                    if (groupByMulti.Length == 1)
                    {
                        //_currentValueDocMaps = new Int2ObjectOpenHashMap<ScoreDoc>(_count);
                        _currentValueDocMaps = new Dictionary<int, ScoreDoc>(_count);
                        _facetAccessibleLists = null;
                    }
                    else
                    {
                        _currentValueDocMaps = null;
                        _facetCountCollectorMulti = new IFacetCountCollector[groupByList.Count];
                        _facetAccessibleLists = new List<IFacetAccessible>[groupByMulti.Length];
                        for (int i = 0; i < groupByMulti.Length; ++i)
                        {
                            _facetAccessibleLists[i] = new List<IFacetAccessible>();
                        }
                    }

                    // NightOwl888: The _collectDocIdCache setting seems to put arrays into
                    // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.
                    //if (_collectDocIdCache)
                    //{
                    //    contextList = new List<CollectorContext>();
                    //    docidarraylist = new List<int[]>();
                    //    if (doScoring)
                    //        scorearraylist = new List<float[]>();
                    //}
                }
                else
                {
                    _currentValueDocMaps = null;
                    _facetAccessibleLists = null;
                }
            }
            else
            {
                _currentValueDocMaps = null;
                _facetAccessibleLists = null;
            }
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get { return _collector == null ? true : _collector.AcceptsDocsOutOfOrder; }
        }

        public override void Collect(int doc)
        {
            ++_totalHits;

            if (groupBy != null)
            {
                if (_facetCountCollectorMulti != null)
                {
                    for (int i = 0; i < _facetCountCollectorMulti.Length; ++i)
                    {
                        if (_facetCountCollectorMulti[i] != null)
                            _facetCountCollectorMulti[i].Collect(doc);
                    }

                    if (_count > 0)
                    {
                        float score = (_doScoring ? _scorer.Score() : 0.0f);

                        // NightOwl888: The _collectDocIdCache setting seems to put arrays into
                        // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.
                        //if (_collectDocIdCache)
                        //{
                        //    if (_totalHits > _docIdCacheCapacity)
                        //    {
                        //        _currentDocIdArray = intarraymgr.Get(BLOCK_SIZE);
                        //        docidarraylist.Add(_currentDocIdArray);
                        //        if (_doScoring)
                        //        {
                        //            _currentScoreArray = floatarraymgr.Get(BLOCK_SIZE);
                        //            scorearraylist.Add(_currentScoreArray);
                        //        }
                        //        _docIdCacheCapacity += BLOCK_SIZE;
                        //        _docIdArrayCursor = 0;
                        //    }
                        //    _currentDocIdArray[_docIdArrayCursor] = doc;
                        //    if (_doScoring)
                        //        _currentScoreArray[_docIdArrayCursor] = score;
                        //    ++_docIdArrayCursor;
                        //    ++_currentContext.length;
                        //}
                    }
                    return;
                }
                else
                {
                    //if (_facetCountCollector != null)
                    //_facetCountCollector.collect(doc);

                    if (_count > 0)
                    {
                        float score = (_doScoring ? _scorer.Score() : 0.0f);

                        // NightOwl888: The _collectDocIdCache setting seems to put arrays into
                        // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.
                        //if (_collectDocIdCache)
                        //{
                        //    if (_totalHits > _docIdCacheCapacity)
                        //    {
                        //        _currentDocIdArray = intarraymgr.Get(BLOCK_SIZE);
                        //        docidarraylist.Add(_currentDocIdArray);
                        //        if (_doScoring)
                        //        {
                        //            _currentScoreArray = floatarraymgr.Get(BLOCK_SIZE);
                        //            scorearraylist.Add(_currentScoreArray);
                        //        }
                        //        _docIdCacheCapacity += BLOCK_SIZE;
                        //        _docIdArrayCursor = 0;
                        //    }
                        //    _currentDocIdArray[_docIdArrayCursor] = doc;
                        //    if (_doScoring)
                        //        _currentScoreArray[_docIdArrayCursor] = score;
                        //    ++_docIdArrayCursor;
                        //    ++_currentContext.length;
                        //}

                        _tmpScoreDoc.Doc = doc;
                        _tmpScoreDoc.Score = score;
                        if (!_queueFull || _currentComparator.Compare(_bottom, _tmpScoreDoc) > 0)
                        {
                            int order = groupBy.GetFacetData<IFacetDataCache>(_currentReader).OrderArray.Get(doc);
                            ScoreDoc pre = _currentValueDocMaps.Get(order);
                            if (pre != null)
                            {
                                if (_currentComparator.Compare(pre, _tmpScoreDoc) > 0)
                                {
                                    ScoreDoc tmp = pre;
                                    _bottom = _currentQueue.Replace(_tmpScoreDoc, pre);
                                    _currentValueDocMaps.Put(order, _tmpScoreDoc);
                                    _tmpScoreDoc = tmp;
                                }
                            }
                            else
                            {
                                if (_queueFull)
                                {
                                    MyScoreDoc tmp = (MyScoreDoc)_bottom;
                                    _currentValueDocMaps.Remove(groupBy.GetFacetData<IFacetDataCache>(tmp.reader).OrderArray.Get(tmp.Doc));
                                    _bottom = _currentQueue.Replace(_tmpScoreDoc);
                                    _currentValueDocMaps.Put(order, _tmpScoreDoc);
                                    _tmpScoreDoc = tmp;
                                }
                                else
                                {
                                    ScoreDoc tmp = new MyScoreDoc(doc, score, _currentQueue, _currentReader);
                                    _bottom = _currentQueue.Add(tmp);
                                    _currentValueDocMaps.Put(order, tmp);
                                    _queueFull = (_currentQueue.size >= _numHits);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (_count > 0)
                {
                    float score = (_doScoring ? _scorer.Score() : 0.0f);

                    if (_queueFull)
                    {
                        _tmpScoreDoc.Doc = doc;
                        _tmpScoreDoc.Score = score;

                        if (_currentComparator.Compare(_bottom, _tmpScoreDoc) > 0)
                        {
                            ScoreDoc tmp = _bottom;
                            _bottom = _currentQueue.Replace(_tmpScoreDoc);
                            _tmpScoreDoc = tmp;
                        }
                    }
                    else
                    {
                        _bottom = _currentQueue.Add(new MyScoreDoc(doc, score, _currentQueue, _currentReader));
                        _queueFull = (_currentQueue.size >= _numHits);
                    }
                }
            }

            if (_collector != null) _collector.Collect(doc);
        }

        private void CollectTotalGroups()
        {
            if (_facetCountCollector is GroupByFacetCountCollector)
            {
                _totalGroups += ((GroupByFacetCountCollector)_facetCountCollector).GetTotalGroups();
                return;
            }

            int[] count = _facetCountCollector.GetCountDistribution();
            foreach (int c in count)
            {
                if (c > 0)
                    ++_totalGroups;
            }
        }

        public override void SetNextReader(IndexReader reader, int docBase)
        {
            if (!(reader is BoboIndexReader))
                throw new ArgumentException("reader must be a BoboIndexReader");
            _currentReader = (BoboIndexReader)reader;
            _currentComparator = _compSource.GetComparator(reader, docBase);
            _currentQueue = new DocIDPriorityQueue(_currentComparator, _numHits, docBase);
            if (groupBy != null)
            {
                if (_facetCountCollectorMulti != null)
                {
                    for (int i = 0; i < groupByMulti.Length; ++i)
                    {
                        _facetCountCollectorMulti[i] = groupByMulti[i].GetFacetCountCollectorSource(null, null, true).GetFacetCountCollector(_currentReader, docBase);
                    }
                    //if (_facetCountCollector != null)
                    //    collectTotalGroups();
                    _facetCountCollector = _facetCountCollectorMulti[0];
                    if (_facetAccessibleLists != null)
                    {
                        for (int i = 0; i < groupByMulti.Length; ++i)
                        {
                            _facetAccessibleLists[i].Add(_facetCountCollectorMulti[i]);
                        }
                    }
                }
                if (_currentValueDocMaps != null)
                    _currentValueDocMaps.Clear();

                // NightOwl888: The _collectDocIdCache setting seems to put arrays into
                // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.
                //if (contextList != null)
                //{
                //    _currentContext = new CollectorContext(_currentReader, docBase, _currentComparator);
                //    contextList.Add(_currentContext);
                //}
            }
            MyScoreDoc myScoreDoc = (MyScoreDoc)_tmpScoreDoc;
            myScoreDoc.queue = _currentQueue;
            myScoreDoc.reader = _currentReader;
            myScoreDoc.sortValue = null;
            _pqList.Add(_currentQueue);
            _queueFull = false;
        }

        public override void SetScorer(Scorer scorer)
        {
            _scorer = scorer;
            _currentComparator.SetScorer(scorer);
        }

        public override int TotalHits
        {
            get { return _totalHits; }
        }

        public override int TotalGroups
        {
            get { return _totalGroups; }
        }

        public override BrowseHit[] TopDocs
        {
            get
            {
                var iterList = new List<IEnumerator<MyScoreDoc>>(_pqList.Count);
                foreach (DocIDPriorityQueue pq in _pqList)
                {
                    int count = pq.Size;
                    MyScoreDoc[] resList = new MyScoreDoc[count];
                    for (int i = count - 1; i >= 0; i--)
                    {
                        resList[i] = (MyScoreDoc)pq.Pop();
                    }
                    iterList.Add(((IEnumerable<MyScoreDoc>)resList).GetEnumerator());
                }

                {
                    List<MyScoreDoc> resList;
                    if (_count > 0)
                    {
                        if (groupBy == null)
                        {
                            resList = ListMerger.MergeLists(_offset, _count, iterList, MERGE_COMPATATOR);
                        }
                        else
                        {
                            int rawGroupValueType = 0;  // 0: unknown, 1: normal, 2: long[]

                            PrimitiveLongArrayWrapper primitiveLongArrayWrapperTmp = new PrimitiveLongArrayWrapper(null);

                            object rawGroupValue = null;

                            //if (_facetCountCollector != null)
                            //{
                            //collectTotalGroups();
                            //_facetCountCollector = null;
                            //}
                            if (_facetAccessibleLists != null)
                            {
                                _groupAccessibles = new CombinedFacetAccessible[_facetAccessibleLists.Length];
                                for (int i = 0; i < _facetAccessibleLists.Length; ++i)
                                    _groupAccessibles[i] = new CombinedFacetAccessible(new FacetSpec(), _facetAccessibleLists[i]);
                            }
                            resList = new List<MyScoreDoc>(_count);
                            IEnumerator<MyScoreDoc> mergedIter = ListMerger.MergeLists(iterList, MERGE_COMPATATOR);
                            IList<object> groupSet = new List<object>(_offset + _count);
                            int offsetLeft = _offset;
                            while (mergedIter.MoveNext())
                            {
                                MyScoreDoc scoreDoc = mergedIter.Current;
                                object[] vals = groupBy.GetRawFieldValues(scoreDoc.reader, scoreDoc.Doc);
                                rawGroupValue = null;
                                if (vals != null && vals.Length > 0)
                                    rawGroupValue = vals[0];

                                if (rawGroupValueType == 0)
                                {
                                    if (rawGroupValue != null)
                                    {
                                        if (rawGroupValue is long[])
                                            rawGroupValueType = 2;
                                        else
                                            rawGroupValueType = 1;
                                    }
                                }
                                if (rawGroupValueType == 2)
                                {
                                    primitiveLongArrayWrapperTmp.data = (long[])rawGroupValue;
                                    rawGroupValue = primitiveLongArrayWrapperTmp;
                                }

                                if (!groupSet.Contains(rawGroupValue))
                                {
                                    if (offsetLeft > 0)
                                        --offsetLeft;
                                    else
                                    {
                                        resList.Add(scoreDoc);
                                        if (resList.Count >= _count)
                                            break;
                                    }
                                    groupSet.Add(new PrimitiveLongArrayWrapper(primitiveLongArrayWrapperTmp.data));
                                }
                            }
                        }
                    }
                    else
                        resList = new List<MyScoreDoc>();

                    var facetHandlerMap = _boboBrowser.FacetHandlerMap;
                    return BuildHits(resList.ToArray(), _sortFields, facetHandlerMap, _fetchStoredFields, _termVectorsToFetch, groupBy, _groupAccessibles);
                }
            }
        }

        protected static BrowseHit[] BuildHits(MyScoreDoc[] scoreDocs, SortField[] sortFields,
            IDictionary<string, IFacetHandler> facetHandlerMap, bool fetchStoredFields,
            IEnumerable<string> termVectorsToFetch, IFacetHandler groupBy, CombinedFacetAccessible[] groupAccessibles)
        {
            BrowseHit[] hits = new BrowseHit[scoreDocs.Length];
            IEnumerable<IFacetHandler> facetHandlers = facetHandlerMap.Values;
            for (int i = scoreDocs.Length - 1; i >= 0; i--)
            {
                MyScoreDoc fdoc = scoreDocs[i];
                BoboIndexReader reader = fdoc.reader;
                BrowseHit hit = new BrowseHit();
                if (fetchStoredFields)
                {
                    hit.StoredFields = reader.Document(fdoc.Doc);
                }
                if (termVectorsToFetch != null && termVectorsToFetch.Count() > 0)
                {
                    var tvMap = new Dictionary<string, BrowseHit.TermFrequencyVector>();
                    hit.TermFreqMap = tvMap;
                    foreach (string field in termVectorsToFetch)
                    {
                        ITermFreqVector tv = reader.GetTermFreqVector(fdoc.Doc, field);
                        if (tv != null)
                        {
                            int[] freqs = tv.GetTermFrequencies();
                            string[] terms = tv.GetTerms();
                            tvMap[field] = new BrowseHit.TermFrequencyVector(terms, freqs);
                        }
                    }
                }
                var map = new Dictionary<string, string[]>();
                var rawMap = new Dictionary<string, object[]>();
                foreach (var facetHandler in facetHandlers)
                {
                    map[facetHandler.Name] = facetHandler.GetFieldValues(reader, fdoc.Doc);
                    rawMap[facetHandler.Name] = facetHandler.GetRawFieldValues(reader, fdoc.Doc);
                }
                hit.FieldValues = map;
                hit.RawFieldValues = rawMap;
                hit.DocId = fdoc.Doc + fdoc.queue.@base;
                hit.Score = fdoc.Score;
                hit.Comparable = fdoc.Value;
                if (groupBy != null)
                {
                    hit.GroupValue = hit.GetField(groupBy.Name);
                    hit.RawGroupValue = hit.GetRawField(groupBy.Name);
                    if (groupAccessibles != null &&
                        hit.GroupValue != null &&
                        groupAccessibles != null &&
                        groupAccessibles.Length > 0)
                    {
                        BrowseFacet facet = groupAccessibles[0].GetFacet(hit.GroupValue);
                        hit.GroupHitsCount = facet.FacetValueHitCount;
                    }
                }
                hits[i] = hit;
            }
            return hits;
        }
    }
}
