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
namespace BoboBrowse.Net.Search
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.MapRed;
    using BoboBrowse.Net.Support;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class BoboSearcher : IndexSearcher
    {
        protected ICollection<FacetHitCollector> m_facetCollectors;
        protected BoboSegmentReader[] m_subReaders;

        public BoboSearcher(BoboSegmentReader reader)
            : base(reader)
        {
            m_facetCollectors = new List<FacetHitCollector>();
            var readerList = new List<BoboSegmentReader>();
            readerList.Add(reader);
            m_subReaders = readerList.ToArray();
        }

        public BoboSearcher(BoboMultiReader reader)
            : base(reader)
        {
            m_facetCollectors = new List<FacetHitCollector>();
            IEnumerable<BoboSegmentReader> subReaders = reader.GetSubReaders();
            m_subReaders = subReaders.ToArray();
        }

        public virtual void SetFacetHitCollectorList(ICollection<FacetHitCollector> facetHitCollectors)
        {
            if (facetHitCollectors != null)
            {
                m_facetCollectors = facetHitCollectors;
            }
        }

        public abstract class FacetValidator
        {
            protected readonly FacetHitCollector[] m_collectors;
            protected readonly int m_numPostFilters;
            protected IFacetCountCollector[] m_countCollectors;
            public int m_nextTarget;

            private void SortPostCollectors(BoboSegmentReader reader)
            {
                var comparer = new SortPostCollectorsComparer(reader);
                System.Array.Sort(m_collectors, 0, m_numPostFilters, comparer);
            }

            private class SortPostCollectorsComparer : IComparer<FacetHitCollector>
            {
                private readonly BoboSegmentReader m_reader;

                public SortPostCollectorsComparer(BoboSegmentReader reader)
                {
                    this.m_reader = reader;
                }

                public virtual int Compare(FacetHitCollector fhc1, FacetHitCollector fhc2)
                {
                    double selectivity1 = fhc1.Filter.GetFacetSelectivity(m_reader);
                    double selectivity2 = fhc2.Filter.GetFacetSelectivity(m_reader);

                    if (selectivity1 < selectivity2)
                    {
                        return -1;
                    }
                    else if (selectivity1 > selectivity2)
                    {
                        return 1;
                    }
                    return 0;
                }
            }
    
            public FacetValidator(FacetHitCollector[] collectors, int numPostFilters)
            {
                m_collectors = collectors;
                m_numPostFilters = numPostFilters;
                m_countCollectors = new IFacetCountCollector[collectors.Length];
            }

            ///<summary>This method validates the doc against any multi-select enabled fields. </summary>
            ///<param name="docid"> </param>
            ///<returns> true if all fields matched </returns>
            public abstract bool Validate(int docid);

            public virtual void SetNextReader(BoboSegmentReader reader, int docBase)
            {
                List<IFacetCountCollector> collectorList = new List<IFacetCountCollector>();
                SortPostCollectors(reader);
                for (int i = 0; i < m_collectors.Length; ++i)
                {
                    m_collectors[i].SetNextReader(reader, docBase);
                    IFacetCountCollector collector = m_collectors[i].CurrentPointers.FacetCountCollector;
                    if (collector != null)
                    {
                        collectorList.Add(collector);
                    }
                }
                m_countCollectors = collectorList.ToArray();
            }

            public virtual IFacetCountCollector[] GetCountCollectors()
            {
                List<IFacetCountCollector> collectors = new List<IFacetCountCollector>();
                collectors.AddRange(m_countCollectors);
                foreach (FacetHitCollector facetHitCollector in m_collectors)
                {
                    collectors.AddRange(facetHitCollector.CollectAllCollectorList);
                    collectors.AddRange(facetHitCollector.CountCollectorList);
                }
                return collectors.ToArray();
            }
        }

        private sealed class DefaultFacetValidator : FacetValidator
        {
            public DefaultFacetValidator(FacetHitCollector[] collectors, int numPostFilters)
                : base(collectors, numPostFilters)
            {
            }

            ///<summary>This method validates the doc against any multi-select enabled fields. </summary>
            ///<param name="docid"> </param>
            ///<returns>true if all fields matched </returns>
            public override sealed bool Validate(int docid)
            {
                CurrentPointers miss = null;

                for (int i = 0; i < m_numPostFilters; i++)
                {
                    CurrentPointers cur = m_collectors[i].CurrentPointers;
                    int sid = cur.Doc;

                    if (sid < docid)
                    {
                        sid = cur.PostDocIDSetIterator.Advance(docid);
                        cur.Doc = sid;
                        if (sid == DocIdSetIterator.NO_MORE_DOCS)
                        {
                            // move this to front so that the call can find the failure faster
                            FacetHitCollector tmp = m_collectors[0];
                            m_collectors[0] = m_collectors[i];
                            m_collectors[i] = tmp;
                        }
                    }

                    if (sid > docid) //mismatch
                    {
                        if (miss != null)
                        {
                            // failed because we already have a mismatch
                            m_nextTarget = (miss.Doc < cur.Doc ? miss.Doc : cur.Doc);
                            return false;
                        }
                        miss = cur;
                    }
                }

                m_nextTarget = docid + 1;

                if (miss != null)
                {
                    miss.FacetCountCollector.Collect(docid);
                    return false;
                }
                else
                {
                    foreach (IFacetCountCollector collector in m_countCollectors)
                    {
                      collector.Collect(docid);
                    }
                    return true;
                }
            }
        }

        private sealed class OnePostFilterFacetValidator : FacetValidator
        {
            private readonly FacetHitCollector m_firsttime;

            public OnePostFilterFacetValidator(FacetHitCollector[] collectors)
                : base(collectors, 1)
            {
                m_firsttime = m_collectors[0];
            }

            public override sealed bool Validate(int docid)
            {
                CurrentPointers miss = null;

                RandomAccessDocIdSet @set = m_firsttime.CurrentPointers.DocIdSet;
                if (@set != null && !@set.Get(docid))
                {
                    miss = m_firsttime.CurrentPointers;
                }
                m_nextTarget = docid + 1;

                if (miss != null)
                {
                    miss.FacetCountCollector.Collect(docid);
                    return false;
                }
                else
                {
                    foreach (IFacetCountCollector collector in m_countCollectors)
                    {
                        collector.Collect(docid);
                    }
                    return true;
                }
            }
        }

        private sealed class NoNeedFacetValidator : FacetValidator
        {
            public NoNeedFacetValidator(FacetHitCollector[] collectors)
                : base(collectors, 0)
            {
            }

            public override sealed bool Validate(int docid)
            {
                foreach (IFacetCountCollector collector in m_countCollectors)
                {
                    collector.Collect(docid);
                }
                return true;
            }
        }

        protected virtual FacetValidator CreateFacetValidator()
        {
            FacetHitCollector[] collectors = new FacetHitCollector[m_facetCollectors.Count];
            FacetCountCollectorSource[] countCollectors = new FacetCountCollectorSource[collectors.Length];
            int numPostFilters;
            int i = 0;
            int j = collectors.Length;

            foreach (FacetHitCollector facetCollector in m_facetCollectors)
            {
                if (facetCollector.Filter != null)
                {
                    collectors[i] = facetCollector;
                    countCollectors[i] = facetCollector.FacetCountCollectorSource;
                    i++;
                }
                else
                {
                    j--;
                    collectors[j] = facetCollector;
                    countCollectors[j] = facetCollector.FacetCountCollectorSource;
                }
            }
            numPostFilters = i;

            if (numPostFilters == 0)
            {
                return new NoNeedFacetValidator(collectors);
            }
            else if (numPostFilters == 1)
            {
                return new OnePostFilterFacetValidator(collectors);
            }
            else
            {
                return new DefaultFacetValidator(collectors, numPostFilters);
            }
        }

        public override void Search(Query query, Filter filter, ICollector collector)
        {
            Weight weight = CreateNormalizedWeight(query);
            this.Search(weight, filter, collector, 0, null);
        }

        public virtual void Search(Weight weight, Filter filter, ICollector collector, int start, IBoboMapFunctionWrapper mapReduceWrapper)
        {
            FacetValidator validator = CreateFacetValidator();
            int target = 0;

            IndexReader reader = this.IndexReader;
            IndexReaderContext indexReaderContext = reader.Context;
            if (filter == null)
            {
                for (int i = 0; i < m_subReaders.Length; i++)
                {
                    AtomicReaderContext atomicContext = indexReaderContext.Children == null 
                        ? (AtomicReaderContext)indexReaderContext
                        : (AtomicReaderContext)(indexReaderContext.Children.Get(i));
                    int docStart = start;

                    // NOTE: This code calls an internal constructor. Apparently, this was in the same namespace as Lucene,
                    // but was added to this project, which presumably allows you to call internal constructors in Java.
                    // In .NET, we can just use Activator.CreateInstance. Not great, but this code will be removed
                    // when applying commit https://github.com/senseidb/bobo/commit/924c8579d90dbb5d56103976d39b47daa2242ef3
                    // which includes several major changes after the 4.0.2 release.

                    // atomicContext = AtomicReaderContextUtil.UpdateDocBase(atomicContext, docStart);
                    BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                    object[] args = new object[] { (CompositeReaderContext)null, atomicContext.AtomicReader, 0, 0, 0, docStart };
                    atomicContext = (AtomicReaderContext)Activator.CreateInstance(typeof(AtomicReaderContext), flags, null, args, null);

                    if (reader is BoboMultiReader) 
                    {
                        docStart = start + ((BoboMultiReader) reader).SubReaderBase(i);
                    }
                    collector.SetNextReader(atomicContext);
                    validator.SetNextReader(m_subReaders[i], docStart);

                    // NOTE: The Weight.Scorer method lost the scoreDocsInOrder and topScorer parameters between
                    // Lucene 4.3.0 and 4.8.0. They are not used by BoboBrowse anyway, so the code here diverges 
                    // from the original Java source to remove these two parameters.

                    // Scorer scorer = weight.Scorer(atomicContext, true, true, _subReaders[i].LiveDocs);
                    Scorer scorer = weight.GetScorer(atomicContext, m_subReaders[i].LiveDocs);
                    if (scorer != null)
                    {

                        collector.SetScorer(scorer);
                        target = scorer.NextDoc();
                        while (target != DocIdSetIterator.NO_MORE_DOCS)
                        {
                            if (validator.Validate(target))
                            {
                                collector.Collect(target);
                                target = scorer.NextDoc();
                            }
                            else
                            {
                                target = validator.m_nextTarget;
                                target = scorer.Advance(target);
                            }
                        }
                    }
                    if (mapReduceWrapper != null)
                    {
                        mapReduceWrapper.MapFullIndexReader(m_subReaders[i], validator.GetCountCollectors());
                    }
                }
                return;
            }

            for (int i = 0; i < m_subReaders.Length; i++)
            {
                AtomicReaderContext atomicContext = indexReaderContext.Children == null
                        ? (AtomicReaderContext)indexReaderContext
                        : (AtomicReaderContext)(indexReaderContext.Children.Get(i));

                DocIdSet filterDocIdSet = filter.GetDocIdSet(atomicContext, m_subReaders[i].LiveDocs);
                if (filterDocIdSet == null) return;  //shall we use return or continue here ??
                int docStart = start;
                if (reader is BoboMultiReader)
                {
                    docStart = start + ((BoboMultiReader)reader).SubReaderBase(i);
                }
                collector.SetNextReader(atomicContext);
                validator.SetNextReader(m_subReaders[i], docStart);

                // NOTE: The Weight.Scorer method lost the scoreDocsInOrder and topScorer parameters between
                // Lucene 4.3.0 and 4.8.0. They are not used by BoboBrowse anyway, so the code here diverges 
                // from the original Java source to remove these two parameters.

                // Scorer scorer = weight.Scorer(atomicContext, true, false, _subReaders[i].LiveDocs);
                Scorer scorer = weight.GetScorer(atomicContext, m_subReaders[i].LiveDocs);
                if (scorer != null)
                {
                    collector.SetScorer(scorer);
                    DocIdSetIterator filterDocIdIterator = filterDocIdSet.GetIterator(); // CHECKME: use ConjunctionScorer here?

                    if (filterDocIdIterator == null)
                        continue;

                    int doc = -1;
                    target = filterDocIdIterator.NextDoc();
                    if (mapReduceWrapper == null)
                    {
                        while (target < DocIdSetIterator.NO_MORE_DOCS)
                        {
                            if (doc < target)
                            {
                                doc = scorer.Advance(target);
                            }

                            if (doc == target) // permitted by filter
                            {
                                if (validator.Validate(doc))
                                {
                                    collector.Collect(doc);

                                    target = filterDocIdIterator.NextDoc();
                                }
                                else
                                {
                                    // skip to the next possible docid
                                    target = filterDocIdIterator.Advance(validator.m_nextTarget);
                                }
                            }
                            else // doc > target
                            {
                                if (doc == DocIdSetIterator.NO_MORE_DOCS)
                                    break;
                                target = filterDocIdIterator.Advance(doc);
                            }
                        }
                    }
                    else
                    {
                        //MapReduce wrapper is not null
                        while (target < DocIdSetIterator.NO_MORE_DOCS)
                        {
                            if (doc < target)
                            {
                                doc = scorer.Advance(target);
                            }

                            if (doc == target) // permitted by filter
                            {
                                if (validator.Validate(doc))
                                {
                                    mapReduceWrapper.MapSingleDocument(doc, m_subReaders[i]);
                                    collector.Collect(doc);

                                    target = filterDocIdIterator.NextDoc();
                                }
                                else
                                {
                                    // skip to the next possible docid
                                    target = filterDocIdIterator.Advance(validator.m_nextTarget);
                                }
                            }
                            else // doc > target
                            {
                                if (doc == DocIdSetIterator.NO_MORE_DOCS)
                                    break;
                                target = filterDocIdIterator.Advance(doc);
                            }
                        }
                        mapReduceWrapper.FinalizeSegment(m_subReaders[i], validator.GetCountCollectors());
                    }
                }
            }     
        }
    }
}
