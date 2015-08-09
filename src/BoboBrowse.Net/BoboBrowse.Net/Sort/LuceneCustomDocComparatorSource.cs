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
namespace BoboBrowse.Net.Sort
{
    using Lucene.Net.Search;
    using System;

    public class LuceneCustomDocComparatorSource : DocComparatorSource
    {
        private readonly FieldComparator _luceneComparator;
        private readonly string _fieldname;
        
        public LuceneCustomDocComparatorSource(string fieldname, FieldComparator luceneComparator)
        {
            _fieldname = fieldname;
            _luceneComparator = luceneComparator;
        }

        public override DocComparator GetComparator(Lucene.Net.Index.IndexReader reader, int docbase)
        {
            _luceneComparator.SetNextReader(reader, docbase);
            return new LuceneCustomDocComparator(_luceneComparator);
        }

        private class LuceneCustomDocComparator : DocComparator
        {
            private readonly FieldComparator _luceneComparator;

            public LuceneCustomDocComparator(FieldComparator luceneComparator)
            {
                this._luceneComparator = luceneComparator;
            }

            public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
            {
                return _luceneComparator.Compare(doc1.Doc, doc2.Doc);
            }

            public override IComparable Value(ScoreDoc doc)
            {
                return _luceneComparator[doc.Doc];
            }

            public override void SetScorer(Scorer scorer)
            {
                _luceneComparator.SetScorer(scorer);
            }
        }
    }
}
