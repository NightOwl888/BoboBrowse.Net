//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Written in Java.
//* 
//* Copyright (C) 2005-2006  John Wang
//*
//* This library is free software; you can redistribute it and/or
//* modify it under the terms of the GNU Lesser General Public
//* License as published by the Free Software Foundation; either
//* version 2.1 of the License, or (at your option) any later version.
//*
//* This library is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

namespace BoboBrowse.Net.Query
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Query.Scoring;
    using BoboBrowse.Net.Search;
    using Lucene.Net.Index;
    using Lucene.Net.Util;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Text;

    ///<summary>A query that matches all documents.</summary>
    public sealed class FastMatchAllDocsQuery : Query
    {
        private readonly int[] deletedDocs;

        public FastMatchAllDocsQuery(int[] deletedDocs, int maxDoc)
        {
            this.deletedDocs = deletedDocs;
        }

        public sealed class FastMatchAllScorer : Scorer
        {
            private int deletedIndex;
            private bool moreDeletions;
            internal int doc;
            internal readonly float score;
            internal readonly int[] deletedDocs;
            private readonly int maxDoc;
            private readonly int delLen;

            public FastMatchAllScorer(int maxdoc, int[] delDocs, float score)
                : this(maxdoc, delDocs, new DefaultSimilarity(), score)
            {
            }

            public FastMatchAllScorer(int maxdoc, int[] delDocs, Similarity similarity, float score)
                : base(similarity)
            {
                doc = -1;
                deletedDocs = delDocs;
                deletedIndex = 0;
                moreDeletions = deletedDocs != null && deletedDocs.Length > 0;
                delLen = deletedDocs != null ? deletedDocs.Length : 0;
                this.score = score;
                maxDoc = maxdoc;
            }
           
            public override int DocID()
            {
                return doc;
            }

            public override int NextDoc()
            {
                while (++doc < maxDoc)
                {
                    if (!moreDeletions || doc < deletedDocs[deletedIndex])
                    {
                        return doc;
                    }
                    else // _moreDeletions == true && _doc >= _deletedDocs[_deletedIndex]
                    {
                        while (moreDeletions && doc > deletedDocs[deletedIndex]) // catch up _deletedIndex to _doc
                        {
                            deletedIndex++;
                            moreDeletions = deletedIndex < delLen;
                        }
                        if (!moreDeletions || doc < deletedDocs[deletedIndex])
                        {
                            return doc;
                        }
                    }
                }
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override int Advance(int target)
            {
                if (target > doc)
                {
                    doc = target - 1;
                    return NextDoc();
                }

                return (target == doc) ? NextDoc() : DocIdSetIterator.NO_MORE_DOCS;
            }

            public override float Score()
            {
                return score;
            }
        }

        private class FastMatchAllDocsWeight : Weight
        {
            private readonly FastMatchAllDocsQuery parent;
            private readonly Similarity similarity;
            private float queryWeight;
            private float queryNorm;

            public FastMatchAllDocsWeight(FastMatchAllDocsQuery parent, Searcher searcher)
            {
                this.parent = parent;
                similarity = null;// searcher.GetSimilarity();
            }

            public override string ToString()
            {
                return "weight(" + parent + ")";
            }

            public override Query Query
            {
                get
                {
                    return parent;
                }
            }

            public override float Value
            {
                get
                {
                    return queryWeight;
                }
            }

            public override float GetSumOfSquaredWeights()
            {
                queryWeight = parent.Boost;
                return queryWeight * queryWeight;
            }

            public override void Normalize(float queryNorm)
            {
                this.queryNorm = queryNorm;
                queryWeight *= this.queryNorm;
            }

            public override Scorer Scorer(IndexReader reader, bool scoreDocsInOrder, bool topScorer)
            {
                return new FastMatchAllScorer(reader.MaxDoc, parent.deletedDocs, similarity, this.Value);
            }

            public override Explanation Explain(IndexReader reader, int doc)
            {
                // explain query weight
                Explanation queryExpl = new Explanation(this.Value, "FastMatchAllDocsQuery");
                if (parent.Boost != 1.0f)
                {
                    queryExpl.AddDetail(new Explanation(parent.Boost, "boost"));
                }
                queryExpl.AddDetail(new Explanation(queryNorm, "queryNorm"));

                return queryExpl;
            }
        }

        public override Weight CreateWeight(Searcher searcher)
        {
            return new FastMatchAllDocsWeight(this, searcher);
        }       

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("MatchAllDocsQuery");
            buffer.Append(ToStringUtils.Boost(this.Boost));
            return buffer.ToString();
        }

        public override bool Equals(object o)
        {
            if (!(o is FastMatchAllDocsQuery))
            {
                return false;
            }
            FastMatchAllDocsQuery other = (FastMatchAllDocsQuery)o;
            return this.Boost == other.Boost;
        }

        public override int GetHashCode()
        {
            return this.Boost.floatToIntBits() ^ 0x1AA71190;
        }

        private class TestDocIdSetIterator : FilteredDocSetIterator
        {
            private readonly List<int> dupDocs;
            private readonly int min;
            private readonly int max;

            public TestDocIdSetIterator(List<int> dupDocs, DocIdSetIterator innerIter)
                : base(innerIter)
            {
                this.dupDocs = dupDocs;
                if (this.dupDocs != null && this.dupDocs.Count > 0)
                {
                    int[] arr = this.dupDocs.ToArray();
                    min = arr[0];
                    max = arr[arr.Length - 1];
                }
                else
                {
                    min = int.MaxValue;
                    max = -1;
                }
            }

            protected internal override sealed bool Match(int docid)
            {
                return !(dupDocs != null && docid >= min && docid <= max && dupDocs.Contains(docid));
                //	 return !(_dupDocs != null && _dupDocs.contains(docid));
                //	return true;
            }
        }       
    }
}
