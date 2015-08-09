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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Sort
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;

    public class ReverseDocComparatorSource : DocComparatorSource
    {
        private readonly DocComparatorSource _inner;

        public ReverseDocComparatorSource(DocComparatorSource inner)
        {
            _inner = inner;
        }

        public override DocComparator GetComparator(IndexReader reader, int docbase)
        {
            return new ReverseDocComparator(_inner.GetComparator(reader, docbase));
        }

        public class ReverseDocComparator : DocComparator
        {
            private readonly DocComparator _comparator;

            public ReverseDocComparator(DocComparator comparator)
            {
                _comparator = comparator;
            }

            public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
            {
                return -_comparator.Compare(doc1, doc2);
            }

            public override IComparable Value(ScoreDoc doc)
            {
                return new ReverseComparable(_comparator.Value(doc));
            }

            [Serializable]
            public class ReverseComparable : IComparable
            {
                //private static long serialVersionUID = 1L; // NOT USED

                private readonly IComparable _inner;

                public ReverseComparable(IComparable inner)
                {
                    _inner = inner;
                }

                public virtual int CompareTo(object obj)
                {
                    if (obj is ReverseComparable)
                    {
                        IComparable inner = ((ReverseComparable)obj)._inner;
                        return -_inner.CompareTo(inner);
                    }
                    else
                    {
                        throw new ArgumentException("expected instance of " + typeof(ReverseComparable));
                    }
                }

                public override string ToString()
                {
                    return string.Concat("!", _inner);
                }
            }
        }
    }
}
