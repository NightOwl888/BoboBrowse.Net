// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Query
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Query.Scoring;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;

    public class FacetBasedBoostScorerBuilder : IScorerBuilder
    {
        protected readonly IDictionary<string, IDictionary<string, float>> _boostMaps;
        protected readonly IFacetTermScoringFunctionFactory _scoringFunctionFactory;

        public FacetBasedBoostScorerBuilder(IDictionary<string, IDictionary<string, float>> boostMaps)
            : this(boostMaps, new MultiplicativeFacetTermScoringFunctionFactory())
        {
        }

        protected FacetBasedBoostScorerBuilder(IDictionary<string, IDictionary<string, float>> boostMaps, IFacetTermScoringFunctionFactory scoringFunctionFactory)
        {
            _boostMaps = boostMaps;
            _scoringFunctionFactory = scoringFunctionFactory;
        }

        public virtual Scorer CreateScorer(Scorer innerScorer, IndexReader reader, bool scoreDocsInOrder, bool topScorer)
        {
            if(!(reader is BoboIndexReader)) 
                throw new ArgumentException("IndexReader is not BoboIndexReader");
    
            return new FacetBasedBoostingScorer(this, (BoboIndexReader)reader, innerScorer.Similarity, innerScorer);
        }

        public virtual Explanation Explain(IndexReader indexReader, int docid, Explanation innerExplaination)
        {
            if (!(indexReader is BoboIndexReader)) throw new ArgumentException("IndexReader is not BoboIndexReader");
            BoboIndexReader reader = (BoboIndexReader)indexReader;

            Explanation exp = new Explanation();
            exp.Description = "FacetBasedBoost";

            float boost = 1.0f;
            foreach (var boostEntry in _boostMaps)
            {
                string facetName = boostEntry.Key;
                IFacetHandler handler = reader.GetFacetHandler(facetName);
                if (!(handler is IFacetScoreable))
                    throw new ArgumentException(facetName + " does not implement IFacetScoreable");

                IFacetScoreable facetScoreable = (IFacetScoreable)handler;
                BoboDocScorer scorer = facetScoreable.GetDocScorer(reader, _scoringFunctionFactory, boostEntry.Value);
                float facetBoost = scorer.Score(docid);

                Explanation facetExp = new Explanation();
                facetExp.Description = facetName;
                facetExp.Value = facetBoost;
                facetExp.AddDetail(scorer.Explain(docid));
                boost *= facetBoost;
                exp.AddDetail(facetExp);
            }
            exp.Value = boost;
            exp.AddDetail(innerExplaination);
            return exp;
        }

        private class FacetBasedBoostingScorer : Scorer
        {
            private readonly Scorer _innerScorer;
            private readonly BoboDocScorer[] _facetScorers;
    
            private int _docid;

            public FacetBasedBoostingScorer(FacetBasedBoostScorerBuilder parent, BoboIndexReader reader, Similarity similarity, Scorer innerScorer)
                : base(similarity)
            {
                _innerScorer = innerScorer;

                List<BoboDocScorer> list = new List<BoboDocScorer>();

                foreach (var boostEntry in parent._boostMaps)
                {
                    string facetName = boostEntry.Key;
                    IFacetHandler handler = reader.GetFacetHandler(facetName);
                    if (!(handler is IFacetScoreable))
                        throw new ArgumentException(facetName + " does not implement FacetScoreable");
                    IFacetScoreable facetScoreable = (IFacetScoreable)handler;
                    BoboDocScorer scorer = facetScoreable.GetDocScorer(reader, parent._scoringFunctionFactory, boostEntry.Value);
                    if (scorer != null) list.Add(scorer);
                }
                _facetScorers = list.ToArray();
                _docid = -1;
            }

            public override float Score()
            {
                float score = _innerScorer.Score();
                foreach (BoboDocScorer facetScorer in _facetScorers)
                {
                    float fscore = facetScorer.Score(_docid);
                    if (fscore > 0.0)
                    {
                        score *= fscore;
                    }
                }
                return score;
            }

            public override int DocID()
            {
                return _docid;
            }

            public override int NextDoc()
            {
                return (_docid = _innerScorer.NextDoc());
            }

            public override int Advance(int target)
            {
                return (_docid = _innerScorer.Advance(target));
            }
        }
    }
}
