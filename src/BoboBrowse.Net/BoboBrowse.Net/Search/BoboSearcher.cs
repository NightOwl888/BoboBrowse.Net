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
    using System.Collections.Generic;
    using System.Linq;

    public class BoboSearcher : IndexSearcher
    {
        protected IEnumerable<FacetHitCollector> _facetCollectors;
        protected BoboSegmentReader[] _subReaders;

        public BoboSearcher(BoboSegmentReader reader)
            : base(reader)
        {
            _facetCollectors = new List<FacetHitCollector>();
            var readerList = new List<BoboSegmentReader>();
            readerList.Add(reader);
            _subReaders = readerList.ToArray();
        }

        public BoboSearcher(BoboMultiReader reader)
            : base(reader)
        {
            _facetCollectors = new List<FacetHitCollector>();
            IEnumerable<BoboSegmentReader> subReaders = reader.GetSubReaders();
            _subReaders = subReaders.ToArray();
        }

        public virtual IEnumerable<FacetHitCollector> FacetHitCollectorList
        {
            set
            {
                if (value != null)
                {
                    _facetCollectors = value;
                }
            }
        }

        public abstract class FacetValidator
        {
            protected readonly FacetHitCollector[] _collectors;
            protected readonly int _numPostFilters;
            protected IFacetCountCollector[] _countCollectors;
            public int _nextTarget;

            private void SortPostCollectors(BoboSegmentReader reader)
            {
                var comparator = new SortPostCollectorsComparator(reader);
                System.Array.Sort(_collectors, 0, _numPostFilters, comparator);
            }

            private class SortPostCollectorsComparator : IComparer<FacetHitCollector>
            {
                private readonly BoboSegmentReader reader;

                public SortPostCollectorsComparator(BoboSegmentReader reader)
                {
                    this.reader = reader;
                }

                public virtual int Compare(FacetHitCollector fhc1, FacetHitCollector fhc2)
                {
                    double selectivity1 = fhc1._filter.GetFacetSelectivity(reader);
                    double selectivity2 = fhc2._filter.GetFacetSelectivity(reader);

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
                _collectors = collectors;
                _numPostFilters = numPostFilters;
                _countCollectors = new IFacetCountCollector[collectors.Length];
            }

            ///<summary>This method validates the doc against any multi-select enabled fields. </summary>
            ///<param name="docid"> </param>
            ///<returns> true if all fields matched </returns>
            public abstract bool Validate(int docid);

            public virtual void SetNextReader(BoboSegmentReader reader, int docBase)
            {
                List<IFacetCountCollector> collectorList = new List<IFacetCountCollector>();
                SortPostCollectors(reader);
                for (int i = 0; i < _collectors.Length; ++i)
                {
                    _collectors[i].SetNextReader(reader, docBase);
                    IFacetCountCollector collector = _collectors[i]._currentPointers.FacetCountCollector;
                    if (collector != null)
                    {
                        collectorList.Add(collector);
                    }
                }
                _countCollectors = collectorList.ToArray();
            }

            public virtual IFacetCountCollector[] GetCountCollectors()
            {
                List<IFacetCountCollector> collectors = new List<IFacetCountCollector>();
                collectors.AddRange(_countCollectors);
                foreach (FacetHitCollector facetHitCollector in _collectors)
                {
                    collectors.AddRange(facetHitCollector._collectAllCollectorList);
                    collectors.AddRange(facetHitCollector._countCollectorList);
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
                FacetHitCollector.CurrentPointers miss = null;

                for (int i = 0; i < _numPostFilters; i++)
                {
                    FacetHitCollector.CurrentPointers cur = _collectors[i]._currentPointers;
                    int sid = cur.Doc;

                    if (sid < docid)
                    {
                        sid = cur.PostDocIDSetIterator.Advance(docid);
                        cur.Doc = sid;
                        if (sid == DocIdSetIterator.NO_MORE_DOCS)
                        {
                            // move this to front so that the call can find the failure faster
                            FacetHitCollector tmp = _collectors[0];
                            _collectors[0] = _collectors[i];
                            _collectors[i] = tmp;
                        }
                    }

                    if (sid > docid) //mismatch
                    {
                        if (miss != null)
                        {
                            // failed because we already have a mismatch
                            _nextTarget = (miss.Doc < cur.Doc ? miss.Doc : cur.Doc);
                            return false;
                        }
                        miss = cur;
                    }
                }

                _nextTarget = docid + 1;

                if (miss != null)
                {
                    miss.FacetCountCollector.Collect(docid);
                    return false;
                }
                else
                {
                    foreach (IFacetCountCollector collector in _countCollectors)
                    {
                      collector.Collect(docid);
                    }
                    return true;
                }
            }
        }

        private sealed class OnePostFilterFacetValidator : FacetValidator
        {
            private readonly FacetHitCollector _firsttime;

            public OnePostFilterFacetValidator(FacetHitCollector[] collectors)
                : base(collectors, 1)
            {
                _firsttime = _collectors[0];
            }

            public override sealed bool Validate(int docid)
            {
                FacetHitCollector.CurrentPointers miss = null;

                RandomAccessDocIdSet @set = _firsttime._currentPointers.DocIdSet;
                if (@set != null && !@set.Get(docid))
                {
                    miss = _firsttime._currentPointers;
                }
                _nextTarget = docid + 1;

                if (miss != null)
                {
                    miss.FacetCountCollector.Collect(docid);
                    return false;
                }
                else
                {
                    foreach (IFacetCountCollector collector in _countCollectors)
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
                foreach (IFacetCountCollector collector in _countCollectors)
                {
                    collector.Collect(docid);
                }
                return true;
            }
        }

        protected virtual FacetValidator CreateFacetValidator()
        {
            FacetHitCollector[] collectors = new FacetHitCollector[_facetCollectors.Count()];
            FacetCountCollectorSource[] countCollectors = new FacetCountCollectorSource[collectors.Length];
            int numPostFilters;
            int i = 0;
            int j = collectors.Length;

            foreach (FacetHitCollector facetCollector in _facetCollectors)
            {
                if (facetCollector._filter != null)
                {
                    collectors[i] = facetCollector;
                    countCollectors[i] = facetCollector._facetCountCollectorSource;
                    i++;
                }
                else
                {
                    j--;
                    collectors[j] = facetCollector;
                    countCollectors[j] = facetCollector._facetCountCollectorSource;
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

        public override void Search(Query query, Filter filter, Collector collector)
        {
            Weight weight = CreateNormalizedWeight(query);
            this.Search(weight, filter, collector, 0, null);
        }

        public virtual void Search(Weight weight, Filter filter, Collector collector, int start, IBoboMapFunctionWrapper mapReduceWrapper)
        {
            FacetValidator validator = CreateFacetValidator();
            int target = 0;

            IndexReader reader = this.IndexReader;
            IndexReaderContext indexReaderContext = reader.Context;
            if (filter == null)
            {
                for (int i = 0; i < _subReaders.Length; i++)
                {
                    AtomicReaderContext atomicContext = indexReaderContext.Children == null 
                        ? (AtomicReaderContext)indexReaderContext
                        : (AtomicReaderContext)(indexReaderContext.Children.Get(i));
                    int docStart = start;

                    // TODO: Need to contribute this to Lucene.net
                    atomicContext = Lucene.Net.Index.AtomicReaderContextUtil.UpdateDocBase(atomicContext, docStart);


                    if (reader is BoboMultiReader) 
                    {
                        docStart = start + ((BoboMultiReader) reader).SubReaderBase(i);
                    }
                    // TODO: Make these consistenly properties or methods
                    collector.NextReader = atomicContext;
                    validator.SetNextReader(_subReaders[i], docStart);

                    //// TODO: Check whether this overload is supposed to be supported by Lucene 4.8.0
                    //Scorer scorer = weight.Scorer(atomicContext, true, true, _subReaders[i].LiveDocs);
                    // TODO: Check whether this overload is supposed to be supported by Lucene 4.8.0
                    Scorer scorer = weight.Scorer(atomicContext, _subReaders[i].LiveDocs);
                    if (scorer != null)
                    {

                        collector.Scorer = scorer;
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
                                target = validator._nextTarget;
                                target = scorer.Advance(target);
                            }
                        }
                    }
                    if (mapReduceWrapper != null)
                    {
                        mapReduceWrapper.MapFullIndexReader(_subReaders[i], validator.GetCountCollectors());
                    }
                }
                return;
            }

            for (int i = 0; i < _subReaders.Length; i++)
            {
                AtomicReaderContext atomicContext = indexReaderContext.Children == null
                        ? (AtomicReaderContext)indexReaderContext
                        : (AtomicReaderContext)(indexReaderContext.Children.Get(i));

                DocIdSet filterDocIdSet = filter.GetDocIdSet(atomicContext, _subReaders[i].LiveDocs);
                if (filterDocIdSet == null) return;  //shall we use return or continue here ??
                int docStart = start;
                if (reader is BoboMultiReader)
                {
                    docStart = start + ((BoboMultiReader)reader).SubReaderBase(i);
                }
                collector.NextReader = atomicContext;
                validator.SetNextReader(_subReaders[i], docStart);
                //// TODO: Check whether this overload is supposed to be supported by Lucene 4.8.0
                //Scorer scorer = weight.Scorer(atomicContext, true, false, _subReaders[i].LiveDocs);
                Scorer scorer = weight.Scorer(atomicContext, _subReaders[i].LiveDocs);
                if (scorer != null)
                {
                    collector.Scorer = scorer;
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
                                    target = filterDocIdIterator.Advance(validator._nextTarget);
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
                                    mapReduceWrapper.MapSingleDocument(doc, _subReaders[i]);
                                    collector.Collect(doc);

                                    target = filterDocIdIterator.NextDoc();
                                }
                                else
                                {
                                    // skip to the next possible docid
                                    target = filterDocIdIterator.Advance(validator._nextTarget);
                                }
                            }
                            else // doc > target
                            {
                                if (doc == DocIdSetIterator.NO_MORE_DOCS)
                                    break;
                                target = filterDocIdIterator.Advance(doc);
                            }
                        }
                        mapReduceWrapper.FinalizeSegment(_subReaders[i], validator.GetCountCollectors());
                    }
                }
            }     
        }
    }
}
