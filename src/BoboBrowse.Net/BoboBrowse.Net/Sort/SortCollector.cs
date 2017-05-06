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
    using BoboBrowse.Net.Support.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;

    public abstract class SortCollector : ICollector, IDisposable
    {
        private static readonly ILog logger = LogProvider.For<SortCollector>();

        // NightOwl888: The _collectDocIdCache setting seems to put arrays into
        // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.

        //public class CollectorContext
        //{
        //    public BoboIndexReader reader;
        //    public int @base;
        //    public DocComparer comparer;
        //    public int length;

        //    private IDictionary<string, IRuntimeFacetHandler> _runtimeFacetMap;
        //    private IDictionary<string, object> _runtimeFacetDataMap;

        //    public CollectorContext(BoboSegmentReader reader, int @base, DocComparer comparer)
        //    {
        //        this.reader = reader;
        //        this.@base = @base;
        //        this.comparer = comparer;
        //        _runtimeFacetMap = reader.RuntimeFacetHandlerMap;
        //        _runtimeFacetDataMap = reader.RuntimeFacetDataMap;
        //    }

        //    public virtual void RestoreRuntimeFacets()
        //    {
        //        reader.RuntimeFacetHandlerMap = _runtimeFacetMap;
        //        reader.RuntimeFacetDataMap = _runtimeFacetDataMap;
        //    }

        //    public virtual void ClearRuntimeFacetData()
        //    {
        //        reader.ClearRuntimeFacetData();
        //        reader.ClearRuntimeFacetHandler();
        //        _runtimeFacetDataMap = null;
        //        _runtimeFacetMap = null;
        //    }
        //}

        public IFacetHandler m_groupBy = null; // Point to the first element of groupByMulti to avoid array lookups.
        public IFacetHandler[] m_groupByMulti = null;

        // NightOwl888: The _collectDocIdCache setting seems to put arrays into
        // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.

        //public List<CollectorContext> contextList;
        //public List<int[]> docidarraylist;
        //public List<float[]> scorearraylist;

        //public static int BLOCK_SIZE = 4096;

        protected readonly SortField[] m_sortFields;
        protected readonly bool m_fetchStoredFields;
        protected bool m_closed = false;

        protected SortCollector(SortField[] sortFields, bool fetchStoredFields)
        {
            m_sortFields = sortFields;
            m_fetchStoredFields = fetchStoredFields;
        }


        public abstract void SetScorer(Scorer scorer); // BoboBrowse.Net specific - needed to implement interface
        public abstract void Collect(int doc); // BoboBrowse.Net specific - needed to implement interface
        public abstract void SetNextReader(AtomicReaderContext context); // BoboBrowse.Net specific - needed to implement interface
        public abstract bool AcceptsDocsOutOfOrder { get; } // BoboBrowse.Net specific - needed to implement interface


        abstract public BrowseHit[] TopDocs { get; }

        abstract public int TotalHits { get; }
        abstract public int TotalGroups { get; }
        abstract public IFacetAccessible[] GroupAccessibles { get; }

        private static DocComparerSource GetNonFacetComparerSource(SortField sf)
        {
            string fieldname = sf.Field;
            SortFieldType type = sf.Type;

            switch (type)
            {
                case SortFieldType.INT32:
                    return new DocComparerSource.Int32DocComparerSource(fieldname);

                case SortFieldType.SINGLE:
                    return new DocComparerSource.SingleDocComparerSource(fieldname);

                case SortFieldType.INT64:
                    return new DocComparerSource.Int64DocComparerSource(fieldname);

                case SortFieldType.DOUBLE:
                    return new DocComparerSource.Int64DocComparerSource(fieldname);

#pragma warning disable 612, 618
                case SortFieldType.BYTE:
                    return new DocComparerSource.ByteDocComparerSource(fieldname);

                case SortFieldType.INT16:
                    return new DocComparerSource.Int16DocComparerSource(fieldname);
#pragma warning restore 612, 618

                case SortFieldType.STRING:
                    return new DocComparerSource.StringOrdComparerSource(fieldname);

                case SortFieldType.STRING_VAL:
                    return new DocComparerSource.StringValComparerSource(fieldname);

                case SortFieldType.CUSTOM:
                    throw new InvalidOperationException("lucene custom sort no longer supported: " + fieldname);

                default:
                    throw new InvalidOperationException("Illegal sort type: " + type + ", for field: " + fieldname);
            }
        }

        private static DocComparerSource GetComparerSource(IBrowsable browser, SortField sf)
        {
            DocComparerSource compSource = null;
            if (SortField.FIELD_DOC.Equals(sf))
            {
                compSource = new DocComparerSource.DocIdDocComparerSource();
            }
            else if (SortField.FIELD_SCORE.Equals(sf) || sf.Type == SortFieldType.SCORE)
            {
                // we want to do reverse sorting regardless for relevance
                compSource = new ReverseDocComparerSource(new DocComparerSource.RelevanceDocComparerSource());
            }
            else if (sf is BoboCustomSortField)
            {
                BoboCustomSortField custField = (BoboCustomSortField)sf;
                DocComparerSource src = custField.GetCustomComparerSource();
                Debug.Assert(src != null);
                compSource = src;
            }
            else
            {
                IEnumerable<string> facetNames = browser.FacetNames;
                string sortName = sf.Field;
                if (facetNames.Contains(sortName))
                {
                    var handler = browser.GetFacetHandler(sortName);
                    Debug.Assert(handler != null);
                    compSource = handler.GetDocComparerSource();
                }
                else
                {
                    // default lucene field
                    logger.Info("doing default lucene sort for: " + sf);
                    compSource = GetNonFacetComparerSource(sf);
                }
            }
            bool reverse = sf.IsReverse;
            if (reverse)
            {
                compSource = new ReverseDocComparerSource(compSource);
            }
            compSource.IsReverse = reverse;
            return compSource;
        }

        private static SortField Convert(IBrowsable browser, SortField sort)
        {
            string field = sort.Field;
            var facetHandler = browser.GetFacetHandler(field);
            if (facetHandler != null)
            {
                //browser.GetFacetHandler(field); // BUG? this does nothing with the result.
                BoboCustomSortField sortField = new BoboCustomSortField(field, sort.IsReverse, facetHandler.GetDocComparerSource());
                return sortField;
            }
            else
            {
                return sort;
            }
        }

        public static SortCollector BuildSortCollector(IBrowsable browser, Query q, SortField[] sort,
            int offset, int count, bool fetchStoredFields, ICollection<string> termVectorsToFetch,
            string[] groupBy, int maxPerGroup, bool collectDocIdCache)
        {
            
            if (sort == null || sort.Length == 0)
            {
                if (q != null && !(q is MatchAllDocsQuery))
                {
                    sort = new SortField[] { SortField.FIELD_SCORE };
                }
                else
                {
                    sort = new SortField[] { SortField.FIELD_DOC };
                }
            }

            bool doScoring = false;
            foreach (SortField sf in sort)
            {
                if (sf.Type == SortFieldType.SCORE)
                {
                    doScoring = true;
                    break;
                }
            }

            DocComparerSource compSource;
            if (sort.Length == 1)
            {
                SortField sf = Convert(browser, sort[0]);
                compSource = GetComparerSource(browser, sf);
            }
            else
            {
                DocComparerSource[] compSources = new DocComparerSource[sort.Length];
                for (int i = 0; i < sort.Length; ++i)
                {
                    compSources[i] = GetComparerSource(browser, Convert(browser, sort[i]));
                }
                compSource = new MultiDocIdComparerSource(compSources);
            }
            return new SortCollectorImpl(compSource, sort, browser, offset, count, doScoring, 
                fetchStoredFields, termVectorsToFetch, groupBy, maxPerGroup, collectDocIdCache);
        }

        public virtual ICollector Collector { get; set; }

        public virtual void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!m_closed)
                {
                    m_closed = true;

                    // NightOwl888: The _collectDocIdCache setting seems to put arrays into
                    // memory, but then do nothing with the arrays. Seems wasteful and unnecessary.
                    //if (contextList != null)
                    //{
                    //    foreach (CollectorContext context in contextList)
                    //    {
                    //        context.ClearRuntimeFacetData();
                    //    }
                    //}
                    //if (docidarraylist != null)
                    //{
                    //    while (!(docidarraylist.Count == 0))
                    //    {
                    //        intarraymgr.Release(docidarraylist.Poll());
                    //    }
                    //}
                    //if (scorearraylist != null)
                    //{
                    //    while (!(scorearraylist.Count == 0))
                    //    {
                    //        floatarraymgr.Release(scorearraylist.Poll());
                    //    }
                    //}
                }
            }
        }
    }
}
