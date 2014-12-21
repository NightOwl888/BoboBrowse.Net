// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System.Text;

    public class SectionSearchQuery : Query
    {
        private static long serialVersionUID = 1L;
  
        private Query _query;

        private class SectionSearchWeight : Weight
        {
            private static long serialVersionUID = 1L;

            float _weight;
            Similarity _similarity;
            private readonly SectionSearchQuery _parent;

            public SectionSearchWeight(SectionSearchQuery parent, Searcher searcher)
            {
                _parent = parent;
                _similarity = _parent.GetSimilarity(searcher);
            }

            public override string ToString()
            {
                return "weight(" + _parent.ToString() + ")";
            }

            public override Query Query
            {
                get { return _parent._query; }
            }

            public override float Value
            {
                get { return _parent.Boost; }
            }

            public override float GetSumOfSquaredWeights()
            {
                _weight = _parent.Boost;
                return _weight * _weight;
            }

            public override void Normalize(float queryNorm)
            {
                _weight *= queryNorm;
            }

            public virtual Scorer Scorer(IndexReader reader)
            {
                SectionSearchScorer scorer = new SectionSearchScorer(this.Query, _similarity, Value, reader);

                return scorer;
            }

            public override Explanation Explain(IndexReader reader, int doc)
            {
                Explanation result = new Explanation();
                result.Value = _weight;
                result.Description = _parent.ToString();

                return result;
            }

            public override Scorer Scorer(IndexReader reader, bool scoreDocsInOrder, bool topScorer)
            {
                return Scorer(reader);
            }
        }

        public class SectionSearchScorer : Scorer
        {
            private int _curDoc = -1;
            private float _curScr;
            private bool _more = true; // more hits
            private SectionSearchQueryPlan _plan;

            public SectionSearchScorer(Query query, Similarity similarity, float score, IndexReader reader)
                : base(similarity)
            {
                _curScr = score;

                SectionSearchQueryPlanBuilder builer = new SectionSearchQueryPlanBuilder(reader);
                _plan = builer.GetPlan(query);
                if (_plan != null)
                {
                    _curDoc = -1;
                    _more = true;
                }
                else
                {
                    _curDoc = DocIdSetIterator.NO_MORE_DOCS;
                    _more = false; ;
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

        public override Weight CreateWeight(Searcher searcher)
        {
            return new SectionSearchWeight(this, searcher);
        }

        public override Query Rewrite(IndexReader reader)
        {
            _query.Rewrite(reader);
            return this;
        }
    }
}
