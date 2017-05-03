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
        protected readonly IDictionary<string, IDictionary<string, float>> m_boostMaps;
        protected readonly IFacetTermScoringFunctionFactory m_scoringFunctionFactory;

        public FacetBasedBoostScorerBuilder(IDictionary<string, IDictionary<string, float>> boostMaps)
            : this(boostMaps, new MultiplicativeFacetTermScoringFunctionFactory())
        {
        }

        protected FacetBasedBoostScorerBuilder(IDictionary<string, IDictionary<string, float>> boostMaps, IFacetTermScoringFunctionFactory scoringFunctionFactory)
        {
            m_boostMaps = boostMaps;
            m_scoringFunctionFactory = scoringFunctionFactory;
        }

        // NOTE: The Weight.Scorer method lost the scoreDocsInOrder and topScorer parameters between
        // Lucene 4.3.0 and 4.8.0. They are not used by BoboBrowse anyway, so the code here diverges 
        // from the original Java source to remove these two parameters.

        // public virtual Scorer CreateScorer(Scorer innerScorer, AtomicReader reader, bool scoreDocsInOrder, bool topScorer)
        public virtual Scorer CreateScorer(Scorer innerScorer, AtomicReader reader)
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
            foreach (var boostEntry in m_boostMaps)
            {
                string facetName = boostEntry.Key;
                IFacetHandler handler = reader.GetFacetHandler(facetName);
                if (!(handler is IFacetScoreable))
                    throw new ArgumentException(facetName + " does not implement IFacetScoreable");

                IFacetScoreable facetScoreable = (IFacetScoreable)handler;
                BoboDocScorer scorer = facetScoreable.GetDocScorer(reader, m_scoringFunctionFactory, boostEntry.Value);
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
            private readonly Scorer m_innerScorer;
            private readonly BoboDocScorer[] m_facetScorers;
    
            private int m_docid;

            public FacetBasedBoostingScorer(FacetBasedBoostScorerBuilder parent, BoboSegmentReader reader, Scorer innerScorer)
                : base(innerScorer.Weight)
            {
                m_innerScorer = innerScorer;

                List<BoboDocScorer> list = new List<BoboDocScorer>();

                foreach (var boostEntry in parent.m_boostMaps)
                {
                    string facetName = boostEntry.Key;
                    IFacetHandler handler = reader.GetFacetHandler(facetName);
                    if (!(handler is IFacetScoreable))
                        throw new ArgumentException(facetName + " does not implement IFacetScoreable");
                    IFacetScoreable facetScoreable = (IFacetScoreable)handler;
                    BoboDocScorer scorer = facetScoreable.GetDocScorer(reader, parent.m_scoringFunctionFactory, boostEntry.Value);
                    if (scorer != null) list.Add(scorer);
                }
                m_facetScorers = list.ToArray();
                m_docid = -1;
            }

            public override float GetScore()
            {
                float score = m_innerScorer.GetScore();
                foreach (BoboDocScorer facetScorer in m_facetScorers)
                {
                    float fscore = facetScorer.Score(m_docid);
                    if (fscore > 0.0)
                    {
                        score *= fscore;
                    }
                }
                return score;
            }

            public override int DocID
            {
                get { return m_docid; }
            }

            public override int NextDoc()
            {
                return (m_docid = m_innerScorer.NextDoc());
            }

            public override int Advance(int target)
            {
                return (m_docid = m_innerScorer.Advance(target));
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
    }
}
