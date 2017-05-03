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
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public class SortCollectorImpl : SortCollector
    {
        private static IComparer<MyScoreDoc> MERGE_COMPATATOR = new MyScoreDocComparer();

        private class MyScoreDocComparer : IComparer<MyScoreDoc>
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
                        r = o1.Doc + o1.m_queue.m_base - o2.Doc - o2.m_queue.m_base;
                    }
                    else
                    {
                        r = v;
                    }
                }

                return r;
            }
        }

        private readonly List<DocIDPriorityQueue> m_pqList;
        private readonly int m_numHits;
        private int m_totalHits;
        private ScoreDoc m_bottom;
        private ScoreDoc m_tmpScoreDoc;
        private bool m_queueFull;
        private DocComparer m_currentComparer;
        private readonly DocComparerSource m_compSource;
        private DocIDPriorityQueue m_currentQueue;
        private BoboSegmentReader m_currentReader = null;
        private IFacetCountCollector m_facetCountCollector;
        private IFacetCountCollector[] m_facetCountCollectorMulti = null;

        private readonly bool m_doScoring;
        private Scorer m_scorer;
        private readonly int m_offset;
        private readonly int m_count;

        private readonly IBrowsable m_boboBrowser;

        // NightOwl888: The _collectDocIdCache setting seems to put arrays into
        // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.
        //private readonly bool _collectDocIdCache;
        private CombinedFacetAccessible[] m_groupAccessibles;
        private readonly List<IFacetAccessible>[] m_facetAccessibleLists;
        private readonly IDictionary<int, ScoreDoc> m_currentValueDocMaps;

        protected class MyScoreDoc : ScoreDoc
        {
            //private static long serialVersionUID = 1L; // NOT USED

            public DocIDPriorityQueue m_queue;
            public BoboSegmentReader m_reader;
            public IComparable m_sortValue;

            public MyScoreDoc()
                : this(0, 0.0f, null, null)
            {
            }

            public MyScoreDoc(int docid, float score, DocIDPriorityQueue queue, BoboSegmentReader reader)
                : base(docid, score)
            {
                this.m_queue = queue;
                this.m_reader = reader;
                this.m_sortValue = null;
            }

            public virtual IComparable Value
            {
                get
                {
                    if (m_sortValue == null)
                        m_sortValue = m_queue.SortValue(this);
                    return m_sortValue;
                }
            }
        }

        // NightOwl888: The _collectDocIdCache setting seems to put arrays into
        // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.
        //private CollectorContext _currentContext;
        //private int[] _currentDocIdArray; // NOT USED
        //private float[] _currentScoreArray; // NOT USED
        //private int _docIdArrayCursor = 0; // NOT USED
        //private int _docIdCacheCapacity = 0; // NOT USED
        private ICollection<string> m_termVectorsToFetch;

        public SortCollectorImpl(
            DocComparerSource compSource,
            SortField[] sortFields,
            IBrowsable boboBrowser,
            int offset,
            int count,
            bool doScoring,
            bool fetchStoredFields,
            ICollection<string> termVectorsToFetch,
            string[] groupBy,
            int maxPerGroup,
            bool collectDocIdCache)
            : base(sortFields, fetchStoredFields)
        {
            Debug.Assert(offset >= 0 && count >= 0);
            m_boboBrowser = boboBrowser;
            m_compSource = compSource;
            m_pqList = new List<DocIDPriorityQueue>();
            m_numHits = offset + count;
            m_offset = offset;
            m_count = count;
            m_totalHits = 0;
            m_queueFull = false;
            m_doScoring = doScoring;
            m_tmpScoreDoc = new MyScoreDoc();
            m_termVectorsToFetch = termVectorsToFetch;

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
                    this.m_groupByMulti = groupByList.ToArray();
                    this.m_groupBy = m_groupByMulti[0];
                }
                if (this.m_groupBy != null && m_count > 0)
                {
                    if (m_groupByMulti.Length == 1)
                    {
                        //_currentValueDocMaps = new Int2ObjectOpenHashMap<ScoreDoc>(_count);
                        m_currentValueDocMaps = new Dictionary<int, ScoreDoc>(m_count);
                        m_facetAccessibleLists = null;
                    }
                    else
                    {
                        m_currentValueDocMaps = null;
                        m_facetCountCollectorMulti = new IFacetCountCollector[groupByList.Count - 1];
                        m_facetAccessibleLists = new List<IFacetAccessible>[m_facetCountCollectorMulti.Length];
                        for (int i = 0; i < m_facetCountCollectorMulti.Length; ++i)
                        {
                            m_facetAccessibleLists[i] = new List<IFacetAccessible>();
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
                    m_currentValueDocMaps = null;
                    m_facetAccessibleLists = null;
                }
            }
            else
            {
                m_currentValueDocMaps = null;
                m_facetAccessibleLists = null;
            }
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get { return this.Collector == null ? true : this.Collector.AcceptsDocsOutOfOrder; }
        }

        public override void Collect(int doc)
        {
            ++m_totalHits;

            if (m_groupBy != null)
            {
                if (m_facetCountCollectorMulti != null)
                {
                    for (int i = 0; i < m_facetCountCollectorMulti.Length; ++i)
                    {
                        if (m_facetCountCollectorMulti[i] != null)
                            m_facetCountCollectorMulti[i].Collect(doc);
                    }

                    if (m_count > 0)
                    {
                        float score = (m_doScoring ? m_scorer.GetScore() : 0.0f);

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
                    if (m_count > 0)
                    {
                        float score = (m_doScoring ? m_scorer.GetScore() : 0.0f);

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

                        m_tmpScoreDoc.Doc = doc;
                        m_tmpScoreDoc.Score = score;
                        if (!m_queueFull || m_currentComparer.Compare(m_bottom, m_tmpScoreDoc) > 0)
                        {
                            int order = m_groupBy.GetFacetData<FacetDataCache>(m_currentReader).OrderArray.Get(doc);
                            ScoreDoc pre = m_currentValueDocMaps.Get(order);
                            if (pre != null)
                            {
                                if (m_currentComparer.Compare(pre, m_tmpScoreDoc) > 0)
                                {
                                    ScoreDoc tmp = pre;
                                    m_bottom = m_currentQueue.Replace(m_tmpScoreDoc, pre);
                                    m_currentValueDocMaps.Put(order, m_tmpScoreDoc);
                                    m_tmpScoreDoc = tmp;
                                }
                            }
                            else
                            {
                                if (m_queueFull)
                                {
                                    MyScoreDoc tmp = (MyScoreDoc)m_bottom;
                                    m_currentValueDocMaps.Remove(m_groupBy.GetFacetData<FacetDataCache>(tmp.m_reader).OrderArray.Get(tmp.Doc));
                                    m_bottom = m_currentQueue.Replace(m_tmpScoreDoc);
                                    m_currentValueDocMaps.Put(order, m_tmpScoreDoc);
                                    m_tmpScoreDoc = tmp;
                                }
                                else
                                {
                                    ScoreDoc tmp = new MyScoreDoc(doc, score, m_currentQueue, m_currentReader);
                                    m_bottom = m_currentQueue.Add(tmp);
                                    m_currentValueDocMaps.Put(order, tmp);
                                    m_queueFull = (m_currentQueue.m_size >= m_numHits);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (m_count > 0)
                {
                    float score = (m_doScoring ? m_scorer.GetScore() : 0.0f);

                    if (m_queueFull)
                    {
                        m_tmpScoreDoc.Doc = doc;
                        m_tmpScoreDoc.Score = score;

                        if (m_currentComparer.Compare(m_bottom, m_tmpScoreDoc) > 0)
                        {
                            ScoreDoc tmp = m_bottom;
                            m_bottom = m_currentQueue.Replace(m_tmpScoreDoc);
                            m_tmpScoreDoc = tmp;
                        }
                    }
                    else
                    {
                        m_bottom = m_currentQueue.Add(new MyScoreDoc(doc, score, m_currentQueue, m_currentReader));
                        m_queueFull = (m_currentQueue.m_size >= m_numHits);
                    }
                }
            }

            if (this.Collector != null) this.Collector.Collect(doc);
        }

        public override void SetNextReader(AtomicReaderContext context)
        {
            AtomicReader reader = context.AtomicReader;
            if (!(reader is BoboSegmentReader))
                throw new ArgumentException("reader must be a BoboIndexReader");
            m_currentReader = (BoboSegmentReader)reader;
            int docBase = context.DocBase;
            m_currentComparer = m_compSource.GetComparer(m_currentReader, docBase);
            m_currentQueue = new DocIDPriorityQueue(m_currentComparer, m_numHits, docBase);
            if (m_groupBy != null)
            {
                if (m_facetCountCollectorMulti != null)  // _facetCountCollectorMulti.Length >= 1
                {
                    for (int i = 0; i < m_facetCountCollectorMulti.Length; ++i)
                    {
                        m_facetCountCollectorMulti[i] = m_groupByMulti[i].GetFacetCountCollectorSource(null, null, true).GetFacetCountCollector(m_currentReader, docBase);
                    }
                    //if (_facetCountCollector != null)
                    //    collectTotalGroups();
                    m_facetCountCollector = m_facetCountCollectorMulti[0];
                    if (m_facetAccessibleLists != null)
                    {
                        for (int i = 0; i < m_facetCountCollectorMulti.Length; ++i)
                        {
                            m_facetAccessibleLists[i].Add(m_facetCountCollectorMulti[i]);
                        }
                    }
                }
                if (m_currentValueDocMaps != null)
                    m_currentValueDocMaps.Clear();

                // NightOwl888: The _collectDocIdCache setting seems to put arrays into
                // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.
                //if (contextList != null)
                //{
                //    _currentContext = new CollectorContext(_currentReader, docBase, _currentComparer);
                //    contextList.Add(_currentContext);
                //}
            }
            MyScoreDoc myScoreDoc = (MyScoreDoc)m_tmpScoreDoc;
            myScoreDoc.m_queue = m_currentQueue;
            myScoreDoc.m_reader = m_currentReader;
            myScoreDoc.m_sortValue = null;
            m_pqList.Add(m_currentQueue);
            m_queueFull = false;
        }

        public override void SetScorer(Scorer scorer)
        {
            m_scorer = scorer;
            m_currentComparer.SetScorer(scorer);
        }

        public override int TotalHits
        {
            get { return m_totalHits; }
        }

        public override int TotalGroups
        {
            get { return m_totalHits; }
        }

        public override IFacetAccessible[] GroupAccessibles
        {
            get { return m_groupAccessibles; }
        }

        public override BrowseHit[] TopDocs
        {
            get
            {
                var iterList = new List<IEnumerator<MyScoreDoc>>(m_pqList.Count);
                foreach (DocIDPriorityQueue pq in m_pqList)
                {
                    int count = pq.Count;
                    MyScoreDoc[] resList = new MyScoreDoc[count];
                    for (int i = count - 1; i >= 0; i--)
                    {
                        resList[i] = (MyScoreDoc)pq.Pop();
                    }
                    iterList.Add(((IEnumerable<MyScoreDoc>)resList).GetEnumerator());
                }

                {
                    IList<MyScoreDoc> resList;
                    if (m_count > 0)
                    {
                        if (m_groupBy == null)
                        {
                            resList = ListMerger.MergeLists(m_offset, m_count, iterList, MERGE_COMPATATOR);
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
                            if (m_facetAccessibleLists != null)
                            {
                                m_groupAccessibles = new CombinedFacetAccessible[m_facetAccessibleLists.Length];
                                for (int i = 0; i < m_facetAccessibleLists.Length; ++i)
                                    m_groupAccessibles[i] = new CombinedFacetAccessible(new FacetSpec(), m_facetAccessibleLists[i]);
                            }
                            resList = new List<MyScoreDoc>(m_count);
                            IEnumerator<MyScoreDoc> mergedIter = ListMerger.MergeLists(iterList, MERGE_COMPATATOR);
                            IList<object> groupSet = new List<object>(m_offset + m_count);
                            int offsetLeft = m_offset;
                            while (mergedIter.MoveNext())
                            {
                                MyScoreDoc scoreDoc = mergedIter.Current;
                                object[] vals = m_groupBy.GetRawFieldValues(scoreDoc.m_reader, scoreDoc.Doc);
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
                                    primitiveLongArrayWrapperTmp.Data = (long[])rawGroupValue;
                                    rawGroupValue = primitiveLongArrayWrapperTmp;
                                }

                                if (!groupSet.Contains(rawGroupValue))
                                {
                                    if (offsetLeft > 0)
                                        --offsetLeft;
                                    else
                                    {
                                        resList.Add(scoreDoc);
                                        if (resList.Count >= m_count)
                                            break;
                                    }
                                    groupSet.Add(new PrimitiveLongArrayWrapper(primitiveLongArrayWrapperTmp.Data));
                                }
                            }
                        }
                    }
                    else
                        resList = new List<MyScoreDoc>();

                    var facetHandlerMap = m_boboBrowser.FacetHandlerMap;
                    return BuildHits(resList.ToArray(), m_sortFields, facetHandlerMap, m_fetchStoredFields, m_termVectorsToFetch, m_groupBy, m_groupAccessibles);
                }
            }
        }

        protected static BrowseHit[] BuildHits(MyScoreDoc[] scoreDocs, SortField[] sortFields,
            IDictionary<string, IFacetHandler> facetHandlerMap, bool fetchStoredFields,
            ICollection<string> termVectorsToFetch, IFacetHandler groupBy, CombinedFacetAccessible[] groupAccessibles)
        {
            BrowseHit[] hits = new BrowseHit[scoreDocs.Length];
            IEnumerable<IFacetHandler> facetHandlers = facetHandlerMap.Values;
            for (int i = scoreDocs.Length - 1; i >= 0; i--)
            {
                MyScoreDoc fdoc = scoreDocs[i];
                BoboSegmentReader reader = fdoc.m_reader;
                BrowseHit hit = new BrowseHit();
                if (fetchStoredFields)
                {
                    hit.SetStoredFields(reader.Document(fdoc.Doc));
                }
                if (termVectorsToFetch != null && termVectorsToFetch.Count > 0)
                {
                    var tvMap = new Dictionary<string, IList<BrowseHit.BoboTerm>>();
                    hit.TermVectorMap = tvMap;
                    Fields fds = reader.GetTermVectors(fdoc.Doc);
                    foreach (string field in termVectorsToFetch)
                    {
                        Terms terms = fds.GetTerms(field);
                        if (terms == null)
                        {
                            continue;
                        }

                        TermsEnum termsEnum = terms.GetIterator(null);
                        BytesRef text;
                        DocsAndPositionsEnum docsAndPositions = null;
                        List<BrowseHit.BoboTerm> boboTermList = new List<BrowseHit.BoboTerm>();

                        while ((text = termsEnum.Next()) != null)
                        {
                            BrowseHit.BoboTerm boboTerm = new BrowseHit.BoboTerm();
                            boboTerm.Term = text.Utf8ToString();
                            boboTerm.Freq = (int)termsEnum.TotalTermFreq;
                            docsAndPositions = termsEnum.DocsAndPositions(null, docsAndPositions);
                            if (docsAndPositions != null)
                            {
                                docsAndPositions.NextDoc();
                                boboTerm.Positions = new List<int>();
                                boboTerm.StartOffsets = new List<int>();
                                boboTerm.EndOffsets = new List<int>();
                                for (int t = 0; t < boboTerm.Freq; ++t)
                                {
                                    boboTerm.Positions.Add(docsAndPositions.NextPosition());
                                    boboTerm.StartOffsets.Add(docsAndPositions.StartOffset);
                                    boboTerm.EndOffsets.Add(docsAndPositions.EndOffset);
                                }
                            }
                            boboTermList.Add(boboTerm);
                        }
                        tvMap.Put(field, boboTermList);
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
                hit.DocId = fdoc.Doc + fdoc.m_queue.m_base;
                hit.Score = fdoc.Score;
                hit.Comparable = fdoc.Value;
                if (groupBy != null)
                {
                    hit.GroupField = groupBy.Name;
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
