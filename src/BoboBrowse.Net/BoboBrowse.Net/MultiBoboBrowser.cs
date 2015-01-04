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
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using Common.Logging;
    using Lucene.Net.Search;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides implementation of Browser for multiple Browser instances.
    /// </summary>
    public class MultiBoboBrowser : MultiSearcher, IBrowsable
    {
        private static ILog logger = LogManager.GetLogger<MultiBoboBrowser>();

        protected readonly IBrowsable[] _subBrowsers;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="browsers">Browsers to search on</param>
        public MultiBoboBrowser(IBrowsable[] browsers)
            : base(browsers)
        {
            _subBrowsers = browsers;
        }


        /// <summary>
        /// Generates a merged BrowseResult from the supplied <see cref="T:BrowseRequest"/>.
        /// The results are put into a Lucene.Net <see cref="T:Lucene.Net.Search.Collector"/> and a <see cref="T:System.Collections.Generic.IDictionary{System.String, IFacetAccessible}"/>.
        /// </summary>
        /// <param name="req"><see cref="T:BrowseRequest"/> for generating the facets.</param>
        /// <param name="hitCollector">A <see cref="T:Lucene.Net.Search.Collector"/> for the hits generated during a search.</param>
        /// <param name="facetMap">A dictionary of all of the facet collections (output).</param>
        public virtual void Browse(BrowseRequest req, Collector hitCollector, IDictionary<string, IFacetAccessible> facetMap)
        {
            Browse(req, hitCollector, facetMap, 0);
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
            Collector hitCollector, 
            IDictionary<string, IFacetAccessible> facetMap,
            int start)
        {
            Weight w = null;
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
                w = CreateWeight(q);
            }
            catch (Exception ioe)
            {
                throw new BrowseException(ioe.Message, ioe);
            }
    
            Browse(req, w, hitCollector, facetMap, start);
        }

        /// <summary>
        /// Generates a merged BrowseResult from the supplied <see cref="T:BrowseRequest"/> and a <see cref="T:Lucene.Net.Search.Weight"/>.
        /// The results are put into a Lucene.Net <see cref="T:Lucene.Net.Search.Collector"/> and a <see cref="T:System.Collections.Generic.IDictionary{System.String, IFacetAccessible}"/>.
        /// </summary>
        /// <param name="req"><see cref="T:BrowseRequest"/> for generating the facets.</param>
        /// <param name="weight">A <see cref="T:Lucene.Net.Search.Weight"/> instance to alter the score of the queries in a multiple index scenario.</param>
        /// <param name="hitCollector">A <see cref="T:Lucene.Net.Search.Collector"/> for the hits generated during a search.</param>
        /// <param name="facetMap">A dictionary of all of the facet collections (output).</param>
        /// <param name="start">The offset value for the document number.</param>
        public virtual void Browse(
            BrowseRequest req, 
            Weight weight, 
            Collector hitCollector, 
            IDictionary<string, IFacetAccessible> facetMap, 
            int start)
        {
            IBrowsable[] browsers = this.GetSubBrowsers();
            // index empty
            if (browsers==null || browsers.Length ==0 ) return;
            int[] starts = GetStarts();

            var mergedMap = new Dictionary<string, IList<IFacetAccessible>>();
            try
            {
                var facetColMap = new Dictionary<string, IFacetAccessible>();
                for (int i = 0; i < browsers.Length; i++)
                {
                    try
                    {
                        browsers[i].Browse(req, weight, hitCollector, facetColMap, (start + starts[i]));
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
                                list = new List<IFacetAccessible>(browsers.Length);
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
                        logger.Error(e.Message, e);
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
            SortCollector collector = GetSortCollector(req.Sort, req.Query, offset, count, req.FetchStoredFields, req.TermVectorsToFetch, false, req.GroupBy, req.MaxPerGroup, req.CollectDocIdCache);

            var facetCollectors = new Dictionary<string, IFacetAccessible>();
            Browse(req, collector, facetCollectors);

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
                logger.Error(e.Message, e);
                result.AddError(e.Message);
                hits = new BrowseHit[0];
            }

            var q = req.Query;
            if (q == null)
            {
                q = new MatchAllDocsQuery();
            }
            if (req.ShowExplanation)
            {
                foreach (BrowseHit hit in hits)
                {
                    try
                    {
                        Explanation expl = Explain(q, hit.DocId);
                        hit.Explanation = expl;
                    }
                    catch (Exception e)
                    {
                        logger.Error(e.Message, e);
                        result.AddError(e.Message);
                    }
                }
            }

            result.Hits = hits;
            result.NumHits = collector.TotalHits;
            result.NumGroups = collector.TotalGroups;
            result.GroupAccessibles = collector.GroupAccessibles;
            result.SortCollector = collector;
            result.TotalDocs = this.NumDocs();
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
            int i = SubSearcher(docid);
            IBrowsable browser = GetSubBrowsers()[i];
            return browser.GetFieldVal(SubDoc(docid), fieldname);
        }

        /// <summary>
        /// Return the raw (primitive) field values for the given doc.
        /// </summary>
        /// <param name="docid">The document id.</param>
        /// <param name="fieldname">The field name.</param>
        /// <returns>An object array of raw field values.</returns>
        public virtual object[] GetRawFieldVal(int docid, string fieldname)
        {
            int i = SubSearcher(docid);
            IBrowsable browser = GetSubBrowsers()[i];
            return browser.GetRawFieldVal(SubDoc(docid), fieldname);
        }
       
        /// <summary>
        /// Gets the array of sub-browsers.
        /// </summary>
        /// <returns>sub-browsers</returns>
        public virtual IBrowsable[] GetSubBrowsers()
        {
            return _subBrowsers;
        }


        protected override int[] GetStarts()
        {
            return base.GetStarts();
        }

        /// <summary>
        /// Compare BrowseFacets by their value
        /// </summary>
        public class BrowseFacetValueComparator : IComparer<BrowseFacet>
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
            return (GetSubBrowsers()[SubSearcher(docid)]);
        }

        public override Similarity Similarity
        {
            get
            {
                return base.Similarity;
            }
            set
            {
                base.Similarity = value;
                foreach (IBrowsable subBrowser in GetSubBrowsers())
                {
                    subBrowser.Similarity = value;
                }
            }
        }

        /// <summary>
        /// Gets the total number of documents in all sub browser instances.
        /// </summary>
        /// <returns>The total number of documents.</returns>
        public virtual int NumDocs()
        {
            int count = 0;
            IBrowsable[] subBrowsers = GetSubBrowsers();
            foreach (IBrowsable subBrowser in subBrowsers)
            {
                count += subBrowser.NumDocs();
            }
            return count;
        }

        public virtual IEnumerable<string> FacetNames
        {
            get
            {
                var names = new List<string>();
                IBrowsable[] subBrowsers = GetSubBrowsers();
                foreach (IBrowsable subBrowser in subBrowsers)
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
            IBrowsable[] subBrowsers = GetSubBrowsers();
            foreach (IBrowsable subBrowser in subBrowsers)
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
                IBrowsable[] subBrowsers = GetSubBrowsers();
                foreach (IBrowsable subBrowser in subBrowsers)
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
            IBrowsable[] subBrowsers = GetSubBrowsers();
            foreach (IBrowsable subBrowser in subBrowsers)
            {
                subBrowser.SetFacetHandler(facetHandler);
            }
        }

        public virtual SortCollector GetSortCollector(SortField[] sort, Lucene.Net.Search.Query q,
            int offset, int count, bool fetchStoredFields, IEnumerable<string> termVectorsToFetch,
            bool forceScoring, string[] groupBy, int maxPerGroup, bool collectDocIdCache)
        {
            if (_subBrowsers.Length == 1)
            {
                return _subBrowsers[0].GetSortCollector(sort, q, offset, count, fetchStoredFields, termVectorsToFetch, forceScoring, groupBy, maxPerGroup, collectDocIdCache);
            }
            return SortCollector.BuildSortCollector(this, q, sort, offset, count, forceScoring, fetchStoredFields, termVectorsToFetch, groupBy, maxPerGroup, collectDocIdCache);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IBrowsable[] subBrowsers = GetSubBrowsers();
                foreach (IBrowsable subBrowser in subBrowsers)
                {
                    subBrowser.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
