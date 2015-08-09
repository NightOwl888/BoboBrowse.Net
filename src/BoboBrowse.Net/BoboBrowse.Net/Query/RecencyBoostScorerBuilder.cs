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

// Version compatibility level: 3.2.0
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
        private readonly float _maxFactor;
	    private readonly TimeUnit _timeunit;
	    private readonly float _min;
	    private readonly float _max;
	    private readonly long _cutoffInMillis;
	    private readonly float _A;
	    private readonly string _timeFacetName;
        private readonly long _now;

        public RecencyBoostScorerBuilder(string timeFacetName, float maxFactor, long cutoff, TimeUnit timeunit)
            : this(timeFacetName, maxFactor, timeunit.Convert(System.Environment.TickCount, TimeUnit.MILLISECONDS), cutoff, timeunit)
        {  
        }

        public RecencyBoostScorerBuilder(string timeFacetName, float maxFactor, long from, long cutoff, TimeUnit timeunit)
        {
            _timeFacetName = timeFacetName;
            _maxFactor = maxFactor;
            _min = 1.0f;
            _max = _maxFactor + _min;
            _timeunit = timeunit;
            _cutoffInMillis = _timeunit.ToMillis(cutoff);
            _A = (_min - _max) / (((float)_cutoffInMillis) * ((float)_cutoffInMillis));
            _now = timeunit.ToMillis(from);
        }

        public virtual Explanation Explain(IndexReader reader, int doc, Explanation innerExplanation)
        {
            if (reader is BoboIndexReader)
            {
                BoboIndexReader boboReader = (BoboIndexReader)reader;
                object dataObj = boboReader.GetFacetData(_timeFacetName);
                if (dataObj is FacetDataCache)
                {
                    FacetDataCache facetDataCache = (FacetDataCache)(boboReader.GetFacetData(_timeFacetName));
                    BigSegmentedArray orderArray = facetDataCache.OrderArray;
                    TermLongList termList = (TermLongList)facetDataCache.ValArray;
                    long now = System.Environment.TickCount;
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
                throw new ArgumentException("reader not instance of " + typeof(BoboIndexReader));
            }
        }

        public virtual Scorer CreateScorer(Scorer innerScorer, IndexReader reader, bool scoreDocsInOrder, bool topScorer)
        {
            if (reader is BoboIndexReader)
            {
                BoboIndexReader boboReader = (BoboIndexReader)reader;
                object dataObj = boboReader.GetFacetData(_timeFacetName);
                if (dataObj is FacetDataCache)
                {
                    FacetDataCache facetDataCache = (FacetDataCache)(boboReader.GetFacetData(_timeFacetName));
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
                throw new ArgumentException("reader not instance of " + typeof(BoboIndexReader));
            }
        }

        private class RecencyBoostScorer : Scorer
        {
            private readonly RecencyBoostScorerBuilder _parent;
            private readonly Scorer _innerScorer;
            private readonly BigSegmentedArray _orderArray;
            private readonly TermLongList _termList;

            public RecencyBoostScorer(RecencyBoostScorerBuilder parent, Scorer innerScorer, BigSegmentedArray orderArray, TermLongList termList)
                : base(innerScorer.Similarity)
            {
                _parent = parent;
                _innerScorer = innerScorer;
                _orderArray = orderArray;
                _termList = termList;
            }

            public override float Score()
            {
                float rawScore = _innerScorer.Score();
                long timeVal = (long)_termList.GetRawValue(_orderArray.Get(_innerScorer.DocID()));
                float timeScore = _parent.ComputeTimeFactor(timeVal);
                return RecencyBoostScorerBuilder.CombineScores(timeScore, rawScore);
            }

            public override int Advance(int target)
            {
                return _innerScorer.Advance(target);
            }

            public override int DocID()
            {
                return _innerScorer.DocID();
            }

            public override int NextDoc()
            {
                return _innerScorer.NextDoc();
            }
        }


        protected virtual float ComputeTimeFactor(long timeVal)
        {
            long xVal = _now - timeVal;
            if (xVal > _cutoffInMillis)
            {
                return _min;
            }
            else
            {
                float xValFloat = (float)xVal;
                return _A * xValFloat * xValFloat + _max;
            }
        }
	
	    private static float CombineScores(float timeScore, float rawScore)
        {
		    return timeScore * rawScore;
	    }
    }
}
