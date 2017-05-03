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
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Support.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Search.Similarities;
    using Lucene.Net.Support;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides implementation of Browser for multiple Browser instances.
    /// </summary>
    public class MultiBoboBrowser : MultiReader, IBrowsable
    {
        private static readonly ILog logger = LogProvider.For<MultiBoboBrowser>();

        protected readonly IBrowsable[] _subBrowsers;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="browsers">Browsers to search on</param>
        public MultiBoboBrowser(IBrowsable[] browsers)
            : base(GetSegmentReaders(browsers), false)
        {
            _subBrowsers = browsers;
        }

        private static IndexReader[] GetSegmentReaders(IBrowsable[] browsers)
        {
            IndexReader[] readers = new IndexReader[browsers.Length];
            for (int i = 0; i < browsers.Length; ++i)
            {
                readers[i] = browsers[i].IndexReader;
            }
            return readers;
        }

        
        /// <summary>
        /// Generates a merged BrowseResult from the supplied <see cref="T:BrowseRequest"/>.
        /// The results are put into a Lucene.Net <see cref="T:Lucene.Net.Search.Collector"/> and a <see cref="T:System.Collections.Generic.IDictionary{System.String, IFacetAccessible}"/>.
        /// </summary>
        /// <param name="req"><see cref="T:BrowseRequest"/> for generating the facets.</param>
        /// <param name="hitCollector">A <see cref="T:Lucene.Net.Search.Collector"/> for the hits generated during a search.</param>
        /// <param name="facetMap">A dictionary of all of the facet collections (output).</param>
        /// <param name="start">The offset value for the document number.</param>
        public virtual void Browse(
            BrowseRequest req, 
            ICollector hitCollector, 
            IDictionary<string, IFacetAccessible> facetMap,
            int start)
        {
            // index empty
            if (_subBrowsers == null || _subBrowsers.Length == 0)
            {
                return;
            }

            try
            {
                var q = req.Query;
                MatchAllDocsQuery matchAllDocsQuery = new MatchAllDocsQuery();
                if (q == null)
                {
                    q = matchAllDocsQuery;
                } 
                else if (!(q is MatchAllDocsQuery)) 
                {
                    //MatchAllQuery is needed to filter out the deleted docids, that reside in ZoieSegmentReader and are not visible on Bobo level         
                    matchAllDocsQuery.Boost = 0f;
                    q = QueriesSupport.CombineAnd(matchAllDocsQuery, q);        
                }
                req.Query = q;
            }
            catch (Exception ioe)
            {
                throw new BrowseException(ioe.Message, ioe);
            }

            var mergedMap = new Dictionary<string, IList<IFacetAccessible>>();
            try
            {
                var facetColMap = new Dictionary<string, IFacetAccessible>();
                for (int i = 0; i < _subBrowsers.Length; i++)
                {
                    try
                    {
                        _subBrowsers[i].Browse(req, hitCollector, facetColMap, (start + ReaderBase(i)));
                    }
                    finally
                    {
                        foreach (var entry in facetColMap)
                        {
                            string name = entry.Key;
                            IFacetAccessible facetAccessor = entry.Value;
                            var list = mergedMap.Get(name);
                            if (list == null)
                            {
                                list = new List<IFacetAccessible>(_subBrowsers.Length);
                                mergedMap.Put(name, list);
                            }
                            list.Add(facetAccessor);
                        }
                        facetColMap.Clear();
                    }
                }
            }
            finally
            {
                if (req.MapReduceWrapper != null)
                {
                    req.MapReduceWrapper.FinalizePartition();
                }
                foreach (var entry in mergedMap)
                {
                    string name = entry.Key;
                    IFacetHandler handler = GetFacetHandler(name);
                    try
                    {
                        IList<IFacetAccessible> subList = entry.Value;
                        if (subList != null)
                        {
                            IFacetAccessible merged = handler.Merge(req.GetFacetSpec(name), subList);
                            facetMap.Put(name, merged);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.ErrorException(e.Message, e);
                    }
                }
            }
        }


        /// <summary>
        /// Generates a merged BrowseResult from the supplied <see cref="T:BrowseRequest"/>.
        /// </summary>
        /// <param name="req"><see cref="T:BrowseRequest"/> for generating the facets.</param>
        /// <returns><see cref="T:BrowseResult"/> of the results corresponding to the <see cref="T:BrowseRequest"/>.</returns>
        public virtual BrowseResult Browse(BrowseRequest req)
        {
            BrowseResult result = new BrowseResult();

            // index empty
            if (_subBrowsers == null || _subBrowsers.Length == 0)
            {
                return result;
            }
            long start = System.Environment.TickCount;
            int offset = req.Offset;
            int count = req.Count;

            if (offset < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException("both offset and count must be > 0: " + offset + "/" + count);
            }
            SortCollector collector = GetSortCollector(req.Sort, req.Query, offset, count, 
                req.FetchStoredFields, req.TermVectorsToFetch, req.GroupBy, req.MaxPerGroup,
#pragma warning disable 612, 618
                req.CollectDocIdCache);
#pragma warning restore 612, 618

            var facetCollectors = new Dictionary<string, IFacetAccessible>();
            Browse(req, collector, facetCollectors, 0);

            if (req.MapReduceWrapper != null)
            {
                result.MapReduceResult = req.MapReduceWrapper.Result;
            }
            BrowseHit[] hits = null;
            try
            {
                hits = collector.TopDocs;
            }
            catch (Exception e)
            {
                logger.ErrorException(e.Message, e);
                result.AddError(e.Message);
                hits = new BrowseHit[0];
            }

            var q = req.Query;
            if (req.ShowExplanation)
            {
                foreach (BrowseHit hit in hits)
                {
                    try
                    {
                        int doc = hit.DocId;
                        int idx = ReaderIndex(doc);
                        int deBasedDoc = doc - ReaderBase(idx);
                        Explanation expl = _subBrowsers[idx].Explain(q, deBasedDoc);
                        hit.SetExplanation(expl);
                    }
                    catch (Exception e)
                    {
                        logger.ErrorException(e.Message, e);
                        result.AddError(e.Message);
                    }
                }
            }

            result.Hits = hits;
            result.NumHits = collector.TotalHits;
            result.NumGroups = collector.TotalGroups;
            result.GroupAccessibles = collector.GroupAccessibles;
            result.SortCollector = collector;
            result.TotalDocs = this.NumDocs;
            result.AddAll(facetCollectors);
            long end = System.Environment.TickCount;
            result.Time = (end - start);
            // set the transaction ID to trace transactions
            result.Tid = req.Tid;
            return result;
        }

        /// <summary>
        /// Return the string representation of the values of a field for the given doc.
        /// </summary>
        /// <param name="docid">The document id.</param>
        /// <param name="fieldname">The field name.</param>
        /// <returns>A string array of field values.</returns>
        public virtual string[] GetFieldVal(int docid, string fieldname)
        {
            int i = ReaderIndex(docid);
            IBrowsable browser = _subBrowsers[i];
            return browser.GetFieldVal(docid - ReaderBase(i), fieldname);
        }

        /// <summary>
        /// Return the raw (primitive) field values for the given doc.
        /// </summary>
        /// <param name="docid">The document id.</param>
        /// <param name="fieldname">The field name.</param>
        /// <returns>An object array of raw field values.</returns>
        public virtual object[] GetRawFieldVal(int docid, string fieldname)
        {
            int i = ReaderIndex(docid);
            IBrowsable browser = _subBrowsers[i];
            return browser.GetRawFieldVal(docid - ReaderBase(i), fieldname);
        }
       
        /// <summary>
        /// Compare BrowseFacets by their value
        /// </summary>
        public class BrowseFacetValueComparer : IComparer<BrowseFacet>
        {
            public virtual int Compare(BrowseFacet o1, BrowseFacet o2)
            {
                return string.CompareOrdinal(o1.Value, o2.Value);
            }
        }

        /// <summary>
        /// Gets the sub-browser for a given docid
        /// </summary>
        /// <param name="docid">sub-browser instance</param>
        /// <returns></returns>
        public virtual IBrowsable SubBrowser(int docid)
        {
            int i = ReaderIndex(docid);
            return _subBrowsers[i];
        }

        public virtual Similarity Similarity
        {
            set
            {
                foreach (IBrowsable subBrowser in _subBrowsers)
                {
                    subBrowser.Similarity = value;
                }
            }
        }

        public virtual IEnumerable<string> FacetNames
        {
            get
            {
                var names = new List<string>();
                foreach (IBrowsable subBrowser in _subBrowsers)
                {
                    names.AddRange(subBrowser.FacetNames);
                }
                return names.Distinct();
            }
        }

        /// <summary>
        /// Gets a facet handler by facet name.
        /// </summary>
        /// <param name="name">The facet name.</param>
        /// <returns>The facet handler instance.</returns>
        public virtual IFacetHandler GetFacetHandler(string name)
        {
            foreach (IBrowsable subBrowser in _subBrowsers)
            {
                IFacetHandler subHandler = subBrowser.GetFacetHandler(name);
                if (subHandler != null)
                    return subHandler;
            }
            return null;
        }

        public virtual IDictionary<string, IFacetHandler> FacetHandlerMap
        {
            get 
            {
                var map = new Dictionary<string, IFacetHandler>();
                foreach (IBrowsable subBrowser in _subBrowsers)
                {
                    map.PutAll(subBrowser.FacetHandlerMap);
                }

                return map;
            }
        }

        /// <summary>
        /// Sets a facet handler for each sub-browser instance.
        /// </summary>
        /// <param name="facetHandler">A facet handler.</param>
        public virtual void SetFacetHandler(IFacetHandler facetHandler)
        {
            foreach (IBrowsable subBrowser in _subBrowsers)
            {
                subBrowser.SetFacetHandler(facetHandler);
            }
        }

        public virtual SortCollector GetSortCollector(SortField[] sort, Lucene.Net.Search.Query q, int offset, int count, 
            bool fetchStoredFields, IEnumerable<string> termVectorsToFetch, string[] groupBy, int maxPerGroup, 
            bool collectDocIdCache)
        {
            if (_subBrowsers.Length == 1)
            {
                return _subBrowsers[0].GetSortCollector(sort, q, offset, count, fetchStoredFields, 
                    termVectorsToFetch, groupBy, maxPerGroup, collectDocIdCache);
            }
            return SortCollector.BuildSortCollector(this, q, sort, offset, count, fetchStoredFields, 
                termVectorsToFetch, groupBy, maxPerGroup, collectDocIdCache);
        }

        protected override void DoClose()
        {
            base.DoClose();
            lock (this)
            {
                Exception exception = null;
                foreach (IBrowsable subBrowser in _subBrowsers)
                {
                    try
                    {
                        subBrowser.Dispose();
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }
                }
                if (exception != null)
                {
                    throw exception;
                }
            }
        }

        public virtual IndexReader IndexReader
        {
            get { return this; }
        }

        public virtual Explanation Explain(Lucene.Net.Search.Query q, int deBasedDoc)
        {
            throw new NotSupportedException();
        }
    }
}
