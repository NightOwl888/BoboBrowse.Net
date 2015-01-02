// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Query
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System.Collections.Generic;

    public class ScoreAdjusterQuery : Query
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private class ScoreAdjusterWeight : Weight
        {
            //private static long serialVersionUID = 1L; // NOT USED

            Weight _innerWeight;
            private readonly ScoreAdjusterQuery _parent;

            public ScoreAdjusterWeight(ScoreAdjusterQuery parent, Weight innerWeight)
            {
                _parent = parent;
                _innerWeight = innerWeight;
            }

            public override string ToString()
            {
                return "weight(" + _parent.ToString() + ")";
            }

            public override Query Query
            {
                get { return _innerWeight.Query; }
            }

            public override float Value
            {
                get { return _innerWeight.Value; }
            }

            public override float GetSumOfSquaredWeights()
            {
                return _innerWeight.GetSumOfSquaredWeights();
            }

            public override void Normalize(float queryNorm)
            {
                _innerWeight.Normalize(queryNorm);
            }

            public override Scorer Scorer(IndexReader reader, bool scoreDocsInOrder, bool topScorer)
            {
                Scorer innerScorer = _innerWeight.Scorer(reader, scoreDocsInOrder, topScorer);
                return _parent._scorerBuilder.CreateScorer(innerScorer, reader, scoreDocsInOrder, topScorer);
            }

            public override Explanation Explain(IndexReader reader, int doc)
            {
                Explanation innerExplain = _innerWeight.Explain(reader, doc);
                return _parent._scorerBuilder.Explain(reader, doc, innerExplain);
            }
        }

        protected readonly Query _query;
        protected readonly IScorerBuilder _scorerBuilder;

        public ScoreAdjusterQuery(Query query, IScorerBuilder scorerBuilder)
        {
            _query = query;
            _scorerBuilder = scorerBuilder;
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            _query.ExtractTerms(terms);
        }

        public override Weight CreateWeight(Searcher searcher)
        {
            return new ScoreAdjusterWeight(this, _query.CreateWeight(searcher));
        }

        public override Query Rewrite(IndexReader reader)
        {
            _query.Rewrite(reader);
            return this;
        }

        public override string ToString(string field)
        {
            return _query.ToString(field);
        }
    }
}
