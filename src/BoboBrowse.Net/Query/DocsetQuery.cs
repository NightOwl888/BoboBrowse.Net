namespace BoboBrowse.Net.Query
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System.Text;

    public class DocsetQuery : Query
    {
        private readonly DocIdSetIterator _iter;

        public DocsetQuery(DocIdSet docSet)
            : this(docSet.Iterator())
        {
        }

        public DocsetQuery(DocIdSetIterator iter)
        {
            _iter = iter;
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("docset query:");
            buffer.Append(ToStringUtils.Boost(base.Boost));
            return buffer.ToString();
        }

        public override Weight CreateWeight(Searcher searcher)
        {
            return new DocSetIteratorWeight(this, searcher.Similarity, _iter);
        }

        private class DocSetIteratorWeight : Weight
        {
            private readonly Query _query;
            private readonly DocIdSetIterator _iter;
            private readonly Similarity _similarity;

            private float _queryWeight;
            private float _queryNorm;

            internal DocSetIteratorWeight(Query query, Similarity similarity, DocIdSetIterator iter)
            {
                _query = query;
                _similarity = similarity;
                _iter = iter;
                _queryNorm = 1.0f;
                _queryWeight = _query.Boost;
            }

            public override Explanation Explain(IndexReader reader, int doc)
            {
                // explain query weight
                Explanation queryExpl = new ComplexExplanation(true, this.Value, "docset query, product of:");
                float boost = _query.Boost;
                if (boost != 1.0f)
                {
                    queryExpl.AddDetail(new Explanation(boost, "boost"));
                }
                queryExpl.AddDetail(new Explanation(_queryNorm, "queryNorm"));

                return queryExpl;
            }

            public override Query Query
            {
                get { return _query; }
            }

            public override float Value
            {
                get { return _queryWeight; }
            }

            public override void Normalize(float norm)
            {
                // we just take the boost, not going to normalize the score

                //_queryNorm = norm;
                //_queryWeight *= _queryNorm;
            }

            public override Scorer Scorer(IndexReader reader, bool scoreDocsInOrder, bool topScorer)
            {
                return new DocSetIteratorScorer(_similarity, _iter, this, reader);
            }

            public override float GetSumOfSquaredWeights()
            {
                return _queryWeight * _queryWeight;
            }

            private class DocSetIteratorScorer : Scorer
            {
                private readonly DocIdSetIterator _iter;
                private readonly float _score;
                private readonly IndexReader _reader;

                internal DocSetIteratorScorer(Similarity similarity, DocIdSetIterator iter, Weight weight, IndexReader reader)
                    : base(similarity)
                {
                    _iter = iter;
                    _score = weight.Value;
                    _reader = reader;
                }

                public override int DocID()
                {
                    return _iter.DocID();
                }

                public override int NextDoc()
                {
                    while (true)
                    {
                        var doc = _iter.NextDoc();
                        if (doc == DocIdSetIterator.NO_MORE_DOCS)
                        {
                            return doc;
                        }
                        else
                        {
                            if (!_reader.IsDeleted(doc))
                            {
                                return doc;
                            }
                        }
                    }
                }

                public override float Score()
                {
                    return _score;
                }

                public override int Advance(int target)
                {
                    var doc = _iter.Advance(target);
                    if (doc != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        if (_reader.IsDeleted(doc))
                        {
                            return this.NextDoc();
                        }
                        else
                        {
                            return doc;
                        }
                    }
                    else
                    {
                        return DocIdSetIterator.NO_MORE_DOCS;
                    }
                }
            }
        }
    }
}