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
    using Lucene.Net.Util;
    using System.Text;

    public class SectionSearchQuery : Query
    { 
        private Query m_query;

        private class SectionSearchWeight : Weight
        {
            Weight m_weight;
            private readonly SectionSearchQuery m_parent;

            public SectionSearchWeight(SectionSearchQuery parent, IndexSearcher searcher, Query query)
            {
                m_parent = parent;
                m_weight = searcher.CreateNormalizedWeight(query);
            }

            public override string ToString()
            {
                return "weight(" + m_parent.ToString() + ")";
            }

            public override Query Query
            {
                get { return m_parent; }
            }

            public float Value
            {
                get { return m_parent.Boost; }
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                Explanation result = new Explanation();
                result.Value = m_parent.Boost;
                result.Description = m_parent.ToString();
                return result;
            }

            // NOTE: The Weight.Scorer method lost the scoreDocsInOrder and topScorer parameters between
            // Lucene 4.3.0 and 4.8.0. They are not used by BoboBrowse anyway, so the code here diverges 
            // from the original Java source to remove these two parameters.

            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                SectionSearchScorer scorer = new SectionSearchScorer(this.Query, m_weight, this.Value, context.AtomicReader);
                return scorer;
            }

            public override float GetValueForNormalization()
            {
                return m_weight.GetValueForNormalization();
            }


            public override void Normalize(float norm, float topLevelBoost)
            {
                m_weight.Normalize(norm, topLevelBoost);
            }
        }

        public class SectionSearchScorer : Scorer
        {
            private int m_curDoc = -1;
            private float m_curScr;
            //private bool _more = true; // more hits // NOT USED
            private SectionSearchQueryPlan m_plan;

            public SectionSearchScorer(Query query, Weight weight, float score, AtomicReader reader)
                : base(weight)
            {
                m_curScr = score;

                SectionSearchQueryPlanBuilder builer = new SectionSearchQueryPlanBuilder(reader);
                m_plan = builer.GetPlan(query);
                if (m_plan != null)
                {
                    m_curDoc = -1;
                }
                else
                {
                    m_curDoc = DocIdSetIterator.NO_MORE_DOCS;
                }
            }

            public override int DocID
            {
                get { return m_curDoc; }
            }

            public override int NextDoc()
            {
                return Advance(0);
            }

            public override float GetScore()
            {
                return m_curScr;
            }

            public override int Advance(int target)
            {
                if (m_curDoc < DocIdSetIterator.NO_MORE_DOCS)
                {
                    if (target <= m_curDoc) target = m_curDoc + 1;

                    return m_plan.Fetch(target);
                }
                return m_curDoc;
            }

            public override int Freq
            {
                get { return 0; }
            }

            public override long GetCost()
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
            m_query = query;
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("SECTION(" + m_query.ToString() + ")");
            return buffer.ToString();
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return new SectionSearchWeight(this, searcher, m_query);
        }

        public override Query Rewrite(IndexReader reader)
        {
            m_query.Rewrite(reader);
            return this;
        }
    }
}
