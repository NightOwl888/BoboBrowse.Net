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
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;

    public class RecencyBoostScorerBuilder : IScorerBuilder
    {
        private readonly float m_maxFactor;
	    private readonly TimeUnit m_timeunit;
	    private readonly float m_min;
	    private readonly float m_max;
	    private readonly long m_cutoffInMillis;
	    private readonly float m_A;
	    private readonly string m_timeFacetName;
        private readonly long m_now;

        public RecencyBoostScorerBuilder(string timeFacetName, float maxFactor, long cutoff, TimeUnit timeunit)
            : this(timeFacetName, maxFactor, timeunit.Convert(System.Environment.TickCount, TimeUnit.MILLISECONDS), cutoff, timeunit)
        {  
        }

        public RecencyBoostScorerBuilder(string timeFacetName, float maxFactor, long from, long cutoff, TimeUnit timeunit)
        {
            m_timeFacetName = timeFacetName;
            m_maxFactor = maxFactor;
            m_min = 1.0f;
            m_max = m_maxFactor + m_min;
            m_timeunit = timeunit;
            m_cutoffInMillis = m_timeunit.ToMillis(cutoff);
            m_A = (m_min - m_max) / (((float)m_cutoffInMillis) * ((float)m_cutoffInMillis));
            m_now = timeunit.ToMillis(from);
        }

        public virtual Explanation Explain(AtomicReader reader, int doc, Explanation innerExplanation)
        {
            if (reader is BoboSegmentReader)
            {
                BoboSegmentReader boboReader = (BoboSegmentReader)reader;
                object dataObj = boboReader.GetFacetData(m_timeFacetName);
                if (dataObj is FacetDataCache)
                {
                    FacetDataCache facetDataCache = (FacetDataCache)(boboReader.GetFacetData(m_timeFacetName));
                    BigSegmentedArray orderArray = facetDataCache.OrderArray;
                    TermLongList termList = (TermLongList)facetDataCache.ValArray;
                    Explanation finalExpl = new Explanation();
                    finalExpl.AddDetail(innerExplanation);
                    float rawScore = innerExplanation.Value;
                    long timeVal = termList.GetPrimitiveValue(orderArray.Get(doc));
                    float timeScore = ComputeTimeFactor(timeVal);
                    float finalScore = CombineScores(timeScore, rawScore);
                    finalExpl.Value = finalScore;
                    finalExpl.Description = "final score = (time score: " + timeScore + ") * (raw score: " + rawScore + "), timeVal: " + timeVal;
                    return finalExpl;
                }
                else
                {
                    throw new InvalidOperationException("underlying facet data must be of type FacetDataCache<long>");
                }
            }
            else
            {
                throw new ArgumentException("reader not instance of " + typeof(BoboSegmentReader));
            }
        }

        // NOTE: The Weight.Scorer method lost the scoreDocsInOrder and topScorer parameters between
        // Lucene 4.3.0 and 4.8.0. They are not used by BoboBrowse anyway, so the code here diverges 
        // from the original Java source to remove these two parameters.

        // public virtual Scorer CreateScorer(Scorer innerScorer, AtomicReader reader, bool scoreDocsInOrder, bool topScorer)
        public virtual Scorer CreateScorer(Scorer innerScorer, AtomicReader reader)
        {
            if (reader is BoboSegmentReader)
            {
                BoboSegmentReader boboReader = (BoboSegmentReader)reader;
                object dataObj = boboReader.GetFacetData(m_timeFacetName);
                if (dataObj is FacetDataCache)
                {
                    FacetDataCache facetDataCache = (FacetDataCache)(boboReader.GetFacetData(m_timeFacetName));
                    BigSegmentedArray orderArray = facetDataCache.OrderArray;
                    TermLongList termList = (TermLongList)facetDataCache.ValArray;
                    return new RecencyBoostScorer(this, innerScorer, orderArray, termList);
                }
                else
                {
                    throw new InvalidOperationException("underlying facet data must be of type FacetDataCache<long>");
                }
            }
            else
            {
                throw new ArgumentException("reader not instance of " + typeof(BoboSegmentReader));
            }
        }

        private class RecencyBoostScorer : Scorer
        {
            private readonly RecencyBoostScorerBuilder m_parent;
            private readonly Scorer m_innerScorer;
            private readonly BigSegmentedArray m_orderArray;
            private readonly TermLongList m_termList;

            public RecencyBoostScorer(RecencyBoostScorerBuilder parent, Scorer innerScorer, BigSegmentedArray orderArray, TermLongList termList)
                : base(innerScorer.Weight)
            {
                m_parent = parent;
                m_innerScorer = innerScorer;
                m_orderArray = orderArray;
                m_termList = termList;
            }

            public override float GetScore()
            {
                float rawScore = m_innerScorer.GetScore();
                long timeVal = (long)m_termList.GetRawValue(m_orderArray.Get(m_innerScorer.DocID));
                float timeScore = m_parent.ComputeTimeFactor(timeVal);
                return RecencyBoostScorerBuilder.CombineScores(timeScore, rawScore);
            }

            public override int Advance(int target)
            {
                return m_innerScorer.Advance(target);
            }

            public override int DocID
            {
                get { return m_innerScorer.DocID; }
            }

            public override int NextDoc()
            {
                return m_innerScorer.NextDoc();
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


        protected virtual float ComputeTimeFactor(long timeVal)
        {
            long xVal = m_now - timeVal;
            if (xVal > m_cutoffInMillis)
            {
                return m_min;
            }
            else
            {
                float xValFloat = xVal;
                return m_A * xValFloat * xValFloat + m_max;
            }
        }
	
	    private static float CombineScores(float timeScore, float rawScore)
        {
		    return timeScore * rawScore;
	    }
    }
}
