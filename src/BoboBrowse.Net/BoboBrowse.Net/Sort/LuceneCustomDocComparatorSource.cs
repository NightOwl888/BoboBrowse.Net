﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
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
namespace BoboBrowse.Net.Sort
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;

    public class LuceneCustomDocComparerSource : DocComparerSource
    {
        private readonly FieldComparer _luceneComparer;
        private readonly string _fieldname;
        
        public LuceneCustomDocComparerSource(string fieldname, FieldComparer luceneComparer)
        {
            _fieldname = fieldname;
            _luceneComparer = luceneComparer;
        }

        public override DocComparer GetComparer(AtomicReader reader, int docbase)
        {
            _luceneComparer.SetNextReader(reader.AtomicContext);
            return new LuceneCustomDocComparer(_luceneComparer);
        }

        private class LuceneCustomDocComparer : DocComparer
        {
            private readonly FieldComparer _luceneComparer;

            public LuceneCustomDocComparer(FieldComparer luceneComparer)
            {
                this._luceneComparer = luceneComparer;
            }

            public override IComparable Value(ScoreDoc doc)
            {
                return _luceneComparer[doc.Doc];
            }

            public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
            {
                return _luceneComparer.Compare(doc1.Doc, doc2.Doc);
            }

            public override void SetScorer(Scorer scorer)
            {
                _luceneComparer.SetScorer(scorer);
            }
        }
    }
}
