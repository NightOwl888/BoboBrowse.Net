//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
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
namespace BoboBrowse.Net.Query.Scoring
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Support;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;

    public class FacetTermQuery : Query
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private static ILog logger = LogManager.GetLogger(typeof(FacetTermQuery));

        private readonly string _name;
        private readonly BrowseSelection _sel;
        private readonly IFacetTermScoringFunctionFactory _scoringFactory;
        private readonly IDictionary<string, float> _boostMap;

        public FacetTermQuery(BrowseSelection sel, IDictionary<string, float> boostMap)
            : this(sel, boostMap, new DefaultFacetTermScoringFunctionFactory())
        {
        }

        public FacetTermQuery(BrowseSelection sel, IDictionary<string, float> boostMap, IFacetTermScoringFunctionFactory scoringFactory)
        {
            _name = sel.FieldName;
            _sel = sel;
            _scoringFactory = scoringFactory;
            _boostMap = boostMap;
        }

        public virtual string Name
        {
            get { return _name; }
        }

        public virtual IDictionary<string, float> BoostMap
        {
            get { return _boostMap; }
        }

        public override string ToString(string fieldname)
        {
            return _sel.ToString();
        }

        public override Weight CreateWeight(Searcher searcher)
        {
            return new FacetTermWeight(this, searcher.Similarity);
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            foreach (string val in _sel.Values)
            {
                terms.Add(new Term(_name, val));
            }
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (this.GetType() != obj.GetType())
                return false;
            if (!(obj is FacetTermQuery))
                return false;

            FacetTermQuery other = (FacetTermQuery)obj;
            if (!this.ToString().Equals(other.ToString()))
                return false;
            if (!_name.Equals(other.Name))
                return false;

            IDictionary<string, float> _boostMap_1 = this._boostMap;
            IDictionary<string, float> _boostMap_2 = other.BoostMap;

            if (_boostMap_1.Count != _boostMap_2.Count)
                return false;
            var it_map = _boostMap_1.Keys.GetEnumerator();
            while (it_map.MoveNext())
            {
                string key_1 = it_map.Current;
                if (!_boostMap_2.ContainsKey(key_1))
                    return false;
                else
                {
                    float boost_1 = _boostMap_1.Get(key_1);
                    float boost_2 = _boostMap_2.Get(key_1);

                    if (Lucene.Net.Support.Single.FloatToIntBits(boost_1) != Lucene.Net.Support.Single.FloatToIntBits(boost_2))
                        return false;
                }
            }

            return true;
        }

        // Required by .NET because Equals() was overridden.
        // Source: http://stackoverflow.com/questions/70303/how-do-you-implement-gethashcode-for-structure-with-two-string#21604191
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;

                // String properties
                hashCode = (hashCode * 397) ^ (_name != null ? _name.GetHashCode() : string.Empty.GetHashCode());

                // Since any of the dictionary values could change at any time, we need to
                // rely on the default implementation of GetHashCode for Contains.
                hashCode = (hashCode * 397) ^ base.GetHashCode();

                return hashCode;
            }


            //unchecked
            //{
            //    int hashCode = 0;

            //    // String properties
            //    hashCode = (hashCode * 397) ^ (_name != null ? _name.GetHashCode() : string.Empty.GetHashCode());

            //    // dictionary properties
            //    var it_map = this._boostMap.GetEnumerator();
            //    while (it_map.MoveNext())
            //    {
            //        var element = it_map.Current;
            //        var key = element.Key;
            //        var value = element.Value;

            //        hashCode = (hashCode * 397) ^ (key != null ? key.GetHashCode() : string.Empty.GetHashCode());
            //        hashCode = (hashCode * 397) ^ value.GetHashCode();
            //    }

            //    return hashCode;
            //}
        }

        private class FacetTermWeight : Weight
        {
            //private static long serialVersionUID = 1L; // NOT USED
            internal Similarity _similarity;
            private float value;
            private FacetTermQuery parent;

            public FacetTermWeight(FacetTermQuery parent, Similarity sim)
            {
                this.parent = parent;
                _similarity = sim;
            }

            public override Explanation Explain(IndexReader reader, int docid)
            {
                BoboSegmentReader boboReader = (BoboSegmentReader)reader;
                IFacetHandler fhandler = boboReader.GetFacetHandler(parent._name);
                if (fhandler != null)
                {
                    BoboDocScorer scorer = null;
                    if (fhandler is IFacetScoreable)
                    {
                        scorer = ((IFacetScoreable)fhandler).GetDocScorer(boboReader, parent._scoringFactory, parent._boostMap);
                        Explanation exp1 = scorer.Explain(docid);
                        Explanation exp2 = new Explanation(parent.Boost, "boost");
					    Explanation expl = new Explanation();
					    expl.Description = "product of:";
					    expl.Value = (exp1.Value * exp2.Value);
					    expl.AddDetail(exp1);
					    expl.AddDetail(exp2);
					    return expl;
                    }
                    else
                    {
                        return null;
                    }
                }
                return null;
            }

            public override Query Query
            {
                get { return parent; }
            }

            public override float Value
            {
                get { return value; }
            }

            public override void Normalize(float score)
            {
                value = parent.Boost;
            }

            private DocIdSetIterator BuildIterator(RandomAccessDocIdSet docset, TermDocs td)
            {
                return new FacetTermQueryDocIdSetIterator(docset, td);
            }

            private class FacetTermQueryDocIdSetIterator : DocIdSetIterator
            {
                private int doc = DocIdSetIterator.NO_MORE_DOCS;
                private readonly RandomAccessDocIdSet _docset;
                private readonly TermDocs _td;

                public FacetTermQueryDocIdSetIterator(RandomAccessDocIdSet docset, TermDocs td)
                {
                    _docset = docset;
                    _td = td;
                }

                public override int Advance(int target)
                {
                    if (_td.SkipTo(target))
                    {
                        doc = _td.Doc;
                        while (!_docset.Get(doc))
                        {
                            if (_td.Next())
                            {
                                doc = _td.Doc;
                            }
                            else
                            {
                                doc = DocIdSetIterator.NO_MORE_DOCS;
                                break;
                            }
                        }
                        return doc;
                    }
                    else
                    {
                        doc = DocIdSetIterator.NO_MORE_DOCS;
                        return doc;
                    }
                }

                public override int DocID()
                {
                    return doc;
                }

                public override int NextDoc()
                {
                    if (_td.Next())
                    {
                        doc = _td.Doc;
                        while (!_docset.Get(doc))
                        {
                            if (_td.Next())
                            {
                                doc = _td.Doc;
                            }
                            else
                            {
                                doc = DocIdSetIterator.NO_MORE_DOCS;
                                break;
                            }
                        }
                        return doc;
                    }
                    else
                    {
                        doc = DocIdSetIterator.NO_MORE_DOCS;
                        return doc;
                    }
                }
            }


            public override Scorer Scorer(IndexReader reader, bool scoreDocsInOrder, bool topScorer)
            {
                if (reader is BoboSegmentReader)
                {
                    BoboSegmentReader boboReader = (BoboSegmentReader)reader;
                    TermDocs termDocs = boboReader.TermDocs(null);
                    IFacetHandler fhandler = boboReader.GetFacetHandler(parent._name);
                    if (fhandler != null)
                    {
                        DocIdSetIterator dociter = null;
                        RandomAccessFilter filter = fhandler.BuildFilter(parent._sel);
                        if (filter != null)
                        {
                            RandomAccessDocIdSet docset = filter.GetRandomAccessDocIdSet(boboReader);
                            if (docset != null)
                            {
                                dociter = BuildIterator(docset, termDocs);
                            }
                        }
                        if (dociter == null)
                        {
                            dociter = new MatchAllDocIdSetIterator(reader);
                        }
                        BoboDocScorer scorer = null;
                        if (fhandler is IFacetScoreable)
                        {
                            scorer = ((IFacetScoreable)fhandler).GetDocScorer(boboReader, parent._scoringFactory, parent._boostMap);
                        }
                        return new FacetTermScorer(parent, _similarity, dociter, scorer);
                    }
                    else
                    {
                        logger.Error("FacetHandler is not defined for the field: " + parent._name);
                    }
                    return null;
                }
                else
                {
                    throw new IOException("index reader not instance of " + typeof(BoboSegmentReader));
                }
            }

            public override float GetSumOfSquaredWeights()
            {
                return 0;
            }
        }

        private class FacetTermScorer : Scorer
        {
            private readonly DocIdSetIterator _docSetIter;
            private readonly BoboDocScorer _scorer;
            private readonly FacetTermQuery _parent;

            public FacetTermScorer(FacetTermQuery parent, Similarity similarity, DocIdSetIterator docidsetIter, BoboDocScorer scorer)
                : base(similarity)
            {
                _parent = parent;
                _docSetIter = docidsetIter;
                _scorer = scorer;
            }

            public override float Score()
            {
                return _scorer == null ? 1.0f : _scorer.Score(_docSetIter.DocID()) * _parent.Boost;
            }

            public override int DocID()
            {
                return _docSetIter.DocID();
            }

            public override int NextDoc()
            {
                return _docSetIter.NextDoc();
            }

            public override int Advance(int target)
            {
                return _docSetIter.Advance(target);
            }   
        }
    }
}