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
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Search.Similarities;
    using System.Text;

    public class SectionSearchQuery : Query
    { 
        private Query _query;

        private class SectionSearchWeight : Weight
        {
            Weight _weight;
            private readonly SectionSearchQuery _parent;

            public SectionSearchWeight(SectionSearchQuery parent, IndexSearcher searcher, Query query)
            {
                _parent = parent;
                _weight = searcher.CreateNormalizedWeight(query);
            }

            public override string ToString()
            {
                return "weight(" + _parent.ToString() + ")";
            }

            public override Query Query
            {
                get { return _parent; }
            }

            public override float Value
            {
                get { return _parent.Boost; }
            }

            // NOTE: The Weight.Scorer method lost the scoreDocsInOrder and topScorer parameters between
            // Lucene 4.3.0 and 4.8.0. They are not used by BoboBrowse anyway, so the code here diverges 
            // from the original Java source to remove these two parameters.

            public override Scorer Scorer(AtomicReaderContext context, Lucene.Net.Util.Bits acceptDocs)
            {
                SectionSearchScorer scorer = new SectionSearchScorer(this.Query, _weight, this.Value, context.AtomicReader);
                return scorer;
            }

            public override float ValueForNormalization
            {
                get { return _weight.ValueForNormalization; }
            }


            public override void Normalize(float norm, float topLevelBoost)
            {
                _weight.Normalize(norm, topLevelBoost);
            }
        }

        public class SectionSearchScorer : Scorer
        {
            private int _curDoc = -1;
            private float _curScr;
            //private bool _more = true; // more hits // NOT USED
            private SectionSearchQueryPlan _plan;

            public SectionSearchScorer(Query query, Weight weight, float score, AtomicReader reader)
                : base(weight)
            {
                _curScr = score;

                SectionSearchQueryPlanBuilder builer = new SectionSearchQueryPlanBuilder(reader);
                _plan = builer.GetPlan(query);
                if (_plan != null)
                {
                    _curDoc = -1;
                }
                else
                {
                    _curDoc = DocIdSetIterator.NO_MORE_DOCS;
                }
            }

            public override int DocID()
            {
                return _curDoc;
            }

            public override int NextDoc()
            {
                return Advance(0);
            }

            public override float Score()
            {
                return _curScr;
            }

            public override int Advance(int target)
            {
                if (_curDoc < DocIdSetIterator.NO_MORE_DOCS)
                {
                    if (target <= _curDoc) target = _curDoc + 1;

                    return _plan.Fetch(target);
                }
                return _curDoc;
            }

            public override int Freq()
            {
                return 0;
            }

            public override long Cost()
            {
                return 0;
            }
        }

        /// <summary>
        /// constructs SectionSearchQuery
        /// </summary>
        /// <param name="query"></param>
        public SectionSearchQuery(Query query)
        {
            _query = query;
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("SECTION(" + _query.ToString() + ")");
            return buffer.ToString();
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new SectionSearchWeight(this, searcher, _query);
        }

        public override Query Rewrite(IndexReader reader)
        {
            _query.Rewrite(reader);
            return this;
        }
    }
}
