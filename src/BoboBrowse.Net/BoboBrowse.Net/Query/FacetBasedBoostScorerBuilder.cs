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
namespace BoboBrowse.Net.Query
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Query.Scoring;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Search.Similarities;
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

        public virtual Scorer CreateScorer(Scorer innerScorer, AtomicReader reader, bool scoreDocsInOrder, bool topScorer)
        {
            if(!(reader is BoboSegmentReader))
                throw new ArgumentException("IndexReader is not BoboSegmentReader");
    
            return new FacetBasedBoostingScorer(this, (BoboSegmentReader)reader, innerScorer);
        }

        public virtual Explanation Explain(AtomicReader indexReader, int docid, Explanation innerExplaination)
        {
            if (!(indexReader is BoboSegmentReader)) throw new ArgumentException("IndexReader is not BoboSegmentReader");
            BoboSegmentReader reader = (BoboSegmentReader)indexReader;

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

            public FacetBasedBoostingScorer(FacetBasedBoostScorerBuilder parent, BoboSegmentReader reader, Scorer innerScorer)
                : base(innerScorer.Weight)
            {
                _innerScorer = innerScorer;

                List<BoboDocScorer> list = new List<BoboDocScorer>();

                foreach (var boostEntry in parent._boostMaps)
                {
                    string facetName = boostEntry.Key;
                    IFacetHandler handler = reader.GetFacetHandler(facetName);
                    if (!(handler is IFacetScoreable))
                        throw new ArgumentException(facetName + " does not implement IFacetScoreable");
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

            public override int Freq()
            {
                return 0;
            }

            public override long Cost()
            {
                return 0;
            }
        }
    }
}
