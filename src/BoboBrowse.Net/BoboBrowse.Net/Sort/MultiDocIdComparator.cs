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

﻿// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Sort
{
    using Lucene.Net.Search;
    using System;

    public class MultiDocIdComparer : DocComparer
    {
        private readonly DocComparer[] m_comparers;

        public MultiDocIdComparer(DocComparer[] comparers)
        {
            m_comparers = comparers;
        }

        public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
        {
            for (int i = 0; i < m_comparers.Length; ++i)
            {
                int v = m_comparers[i].Compare(doc1, doc2);
                if (v != 0) return v;
            }
            return 0;
        }

        public override void SetScorer(Scorer scorer)
        {
            foreach (DocComparer comparer in m_comparers)
            {
                comparer.SetScorer(scorer);
            }
        }

        public override IComparable Value(ScoreDoc doc)
        {
            return new MultiDocIdComparable(doc, m_comparers);
        }

        public class MultiDocIdComparable : IComparable
        {
            private readonly ScoreDoc m_doc;
            private readonly DocComparer[] m_comparers;

            public MultiDocIdComparable(ScoreDoc doc, DocComparer[] comparers)
            {
                m_doc = doc;
                m_comparers = comparers;
            }

            public virtual int CompareTo(object o)
            {
                MultiDocIdComparable other = (MultiDocIdComparable)o;
                IComparable c1, c2;
                for (int i = 0; i < m_comparers.Length; ++i)
                {
                    c1 = m_comparers[i].Value(m_doc);
                    c2 = other.m_comparers[i].Value(other.m_doc);
                    int v = c1.CompareTo(c2);
                    if (v != 0)
                    {
                        return v;
                    }
                }
                return 0;
            }
        }
    }
}
