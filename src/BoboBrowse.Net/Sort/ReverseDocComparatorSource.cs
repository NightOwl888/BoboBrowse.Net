﻿// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Sort
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

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
                return _comparator.Compare(doc1, doc2);
            }

            public override IComparable Value(ScoreDoc doc)
            {
                return new ReverseComparable(_comparator.Value(doc));
            }

            [Serializable]
            public class ReverseComparable : IComparable
            {
                private static long serialVersionUID = 1L;

                private readonly IComparable _inner;

                public ReverseComparable(IComparable inner)
                {
                    _inner = inner;
                }

                public int CompareTo(object obj)
                {
                    if (obj is ReverseComparable)
                    {
                        IComparable inner = ((ReverseComparable)obj)._inner;
                        return -_inner.CompareTo(inner);
                    }
                    else
                    {
                        throw new ArgumentException("expected instance of ReverseComparable");
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
