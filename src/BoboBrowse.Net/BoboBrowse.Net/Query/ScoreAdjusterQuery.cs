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
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System.Collections.Generic;

    public class ScoreAdjusterQuery : Query
    {
        private class ScoreAdjusterWeight : Weight
        {
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

            // NOTE: The Weight.Scorer method lost the scoreDocsInOrder and topScorer parameters between
            // Lucene 4.3.0 and 4.8.0. They are not used by BoboBrowse anyway, so the code here diverges 
            // from the original Java source to remove these two parameters.

            //public override Scorer Scorer(AtomicReaderContext context, bool scoreDocsInOrder, bool topScorer, Bits acceptDocs)
            //{
            //    Scorer innerScorer = _innerWeight.Scorer(context, scoreDocsInOrder, topScorer, acceptDocs);
            //    return _parent._scorerBuilder.CreateScorer(innerScorer, context.AtomicReader, scoreDocsInOrder, topScorer);
            //}

            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                Scorer innerScorer = _innerWeight.GetScorer(context, acceptDocs);
                return _parent._scorerBuilder.CreateScorer(innerScorer, context.AtomicReader);
            }

            public override Explanation Explain(AtomicReaderContext context, int doc)
            {
                Explanation innerExplain = _innerWeight.Explain(context, doc);
                return _parent._scorerBuilder.Explain(context.AtomicReader, doc, innerExplain);
            }

            public override float GetValueForNormalization()
            {
                return _innerWeight.GetValueForNormalization();
            }


            public override void Normalize(float norm, float topLevelBoost)
            {
                _innerWeight.Normalize(norm, topLevelBoost);
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

        public override Weight CreateWeight(IndexSearcher searcher)
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
