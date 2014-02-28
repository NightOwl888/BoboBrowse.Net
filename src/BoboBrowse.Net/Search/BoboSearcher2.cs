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

namespace BoboBrowse.Net.Search
{
    using System;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using BoboBrowse.Net.Facets;

    public class BoboSearcher2 : BoboSearcher
    {
        public BoboSearcher2(BoboIndexReader reader)
            : base(reader)
        {
        }

        public abstract class FacetValidator
        {
            protected internal readonly FacetHitCollector[] Collectors;
            protected internal readonly IFacetCountCollector[] CountCollectors;
            protected internal readonly int NumPostFilters;
            public int NextTarget;

            protected FacetValidator(FacetHitCollector[] collectors, IFacetCountCollector[] countCollectors, int numPostFilters)
            {
                Collectors = collectors;
                CountCollectors = countCollectors;
                NumPostFilters = numPostFilters;
            }

            ///<summary>This method validates the doc against any multi-select enabled fields. </summary>
            ///<param name="docid"> </param>
            ///<returns> true if all fields matched </returns>
            public abstract bool Validate(int docid);
        }

        private sealed class DefaultFacetValidator : FacetValidator
        {
            public DefaultFacetValidator(FacetHitCollector[] collectors, IFacetCountCollector[] countCollectors, int numPostFilters)
                : base(collectors, countCollectors, numPostFilters)
            {
            }

            ///<summary>This method validates the doc against any multi-select enabled fields. </summary>
            ///<param name="docid"> </param>
            ///<returns>true if all fields matched </returns>
            public override bool Validate(int docid)
            {
                FacetHitCollector miss = null;

                for (int i = 0; i < NumPostFilters; i++)
                {
                    FacetHitCollector facetCollector = Collectors[i];
                    if (facetCollector.More)
                    {
                        int sid = facetCollector.Doc;
                        if (sid == docid) // matched
                        {
                            continue;
                        }

                        if (sid < docid)
                        {
                            DocIdSetIterator iterator = facetCollector.PostDocIDSetIterator;
                            if (iterator.Advance(docid)!=DocIdSetIterator.NO_MORE_DOCS)
                            {
                                sid = iterator.DocID();
                                facetCollector.Doc = sid;
                                if (sid == docid) // matched
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                facetCollector.More = false;
                                facetCollector.Doc = int.MaxValue;

                                // move this to front so that the call can find the failure faster
                                FacetHitCollector tmp = Collectors[0];
                                Collectors[0] = facetCollector;
                                Collectors[i] = tmp;
                            }
                        }
                    }

                    if (miss != null)
                    {
                        // failed because we already have a mismatch
                        NextTarget = (miss.Doc < facetCollector.Doc ? miss.Doc : facetCollector.Doc);
                        return false;
                    }
                    miss = facetCollector;
                }

                NextTarget = docid + 1;

                if (miss != null)
                {
                    miss.FacetCountCollector.Collect(docid);
                    return false;
                }
                else
                {
                    foreach (IFacetCountCollector collector in CountCollectors)
                    {
                        collector.Collect(docid);
                    }
                    return true;
                }
            }
        }

        private sealed class OnePostFilterFacetValidator : FacetValidator
        {
            private FacetHitCollector firsttime;

            internal OnePostFilterFacetValidator(FacetHitCollector[] collectors, IFacetCountCollector[] countCollectors, int numPostFilters)
                : base(collectors, countCollectors, numPostFilters)
            {
                firsttime = Collectors[0];
            }

            public override bool Validate(int docid)
            {
                FacetHitCollector miss = null;

                RandomAccessDocIdSet @set = firsttime.DocIdSet;
                if (@set != null && !@set.Get(docid))
                {
                    miss = firsttime;
                }
                NextTarget = docid + 1;

                if (miss != null)
                {
                    miss.FacetCountCollector.Collect(docid);
                    return false;
                }
                else
                {
                    foreach (IFacetCountCollector collector in CountCollectors)
                    {
                        collector.Collect(docid);
                    }
                    return true;
                }
            }
        }

        private sealed class NoNeedFacetValidator : FacetValidator
        {
            internal NoNeedFacetValidator(FacetHitCollector[] collectors, IFacetCountCollector[] countCollectors, int numPostFilters)
                : base(collectors, countCollectors, numPostFilters)
            {
            }

            public override bool Validate(int docid)
            {
                foreach (IFacetCountCollector collector in CountCollectors)
                {
                    collector.Collect(docid);
                }
                return true;
            }
        }

        protected FacetValidator CreateFacetValidator()
        {
            FacetHitCollector[] collectors = new FacetHitCollector[facetCollectors.Count];
            IFacetCountCollector[] countCollectors = new IFacetCountCollector[collectors.Length];
            int numPostFilters;
            int i = 0;
            int j = collectors.Length;

            foreach (FacetHitCollector facetCollector in facetCollectors)
            {
                if (facetCollector.PostDocIDSetIterator != null)
                {
                    facetCollector.More = facetCollector.PostDocIDSetIterator.NextDoc()!=DocIdSetIterator.NO_MORE_DOCS;
                    facetCollector.Doc = (facetCollector.More ? facetCollector.PostDocIDSetIterator.DocID() : int.MaxValue);
                    collectors[i] = facetCollector;
                    countCollectors[i] = facetCollector.FacetCountCollector;
                    i++;
                }
                else
                {
                    j--;
                    collectors[j] = facetCollector;
                    countCollectors[j] = facetCollector.FacetCountCollector;
                }
            }
            numPostFilters = i;

            if (numPostFilters == 0)
            {
                return new NoNeedFacetValidator(collectors, countCollectors, numPostFilters);
            }
            else if (numPostFilters == 1)
            {
                return new OnePostFilterFacetValidator(collectors, countCollectors, numPostFilters);
            }
            else
            {
                return new DefaultFacetValidator(collectors, countCollectors, numPostFilters);
            }
        }

        public override void Search(Weight weight, Filter filter, Collector results)
        {
            IndexReader reader = IndexReader;

            Scorer scorer = weight.Scorer(reader, true, false);

            if (scorer == null)
            {
                return;
            }

            results.SetScorer(scorer);
            results.SetNextReader(reader, 0);

            FacetValidator validator = CreateFacetValidator();
            int target = 0;
            bool more;

            if (filter == null)
            {
                more = scorer.NextDoc()!=DocIdSetIterator.NO_MORE_DOCS;
                while (more)
                {
                    target = scorer.DocID();
                    if (validator.Validate(target))
                    {
                        results.Collect(target);
                        more = scorer.NextDoc()!=DocIdSetIterator.NO_MORE_DOCS;
                    }
                    else
                    {
                        target = validator.NextTarget;
                        more = scorer.Advance(target) != DocIdSetIterator.NO_MORE_DOCS;
                    }
                }
                return;
            }

            DocIdSetIterator filterDocIdIterator = filter.GetDocIdSet(reader).Iterator(); // CHECKME: use ConjunctionScorer here?

            target = filterDocIdIterator.NextDoc();
            if (target == DocIdSetIterator.NO_MORE_DOCS)
            {
                return;
            }

            int doc = -1;
            while (true)
            {
                if (doc < target)
                {
                    doc = scorer.Advance(target);
                    if (doc == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        break;
                    }
                }

                if (doc == target) // permitted by filter
                {
                    if (validator.Validate(doc))
                    {
                        results.Collect(doc);

                        target = filterDocIdIterator.NextDoc();
                        if (target == DocIdSetIterator.NO_MORE_DOCS)
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // skip to the next possible docid
                        target = validator.NextTarget;
                    }
                }
                else // doc > target
                {
                    target = doc;
                }

                target = filterDocIdIterator.Advance(target);
                if (target == DocIdSetIterator.NO_MORE_DOCS)
                {
                    break;
                }
            }
        }
    }
}
