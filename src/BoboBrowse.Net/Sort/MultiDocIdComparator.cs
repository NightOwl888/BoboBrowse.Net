﻿// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Sort
{
    using Lucene.Net.Search;
    using System;

    public class MultiDocIdComparator : DocComparator
    {
        private readonly DocComparator[] _comparators;

        public MultiDocIdComparator(DocComparator[] comparators)
        {
            _comparators = comparators;
        }

        public int Compare(ScoreDoc doc1, ScoreDoc doc2)
        {
            for (int i = 0; i < _comparators.Length; ++i)
            {
                int v = _comparators[i].Compare(doc1, doc2);
                if (v != 0) return v;
            }
            return 0;
        }

        public void SetScorer(Scorer scorer)
        {
            foreach (DocComparator comparator in _comparators)
            {
                comparator.SetScorer(scorer);
            }
        }

        public override IComparable Value(ScoreDoc doc)
        {
            return new MultiDocIdComparable(doc, _comparators);
        }

        public class MultiDocIdComparable : IComparable
        {
            private ScoreDoc _doc;
            private DocComparator[] _comparators;

            public MultiDocIdComparable(ScoreDoc doc, DocComparator[] comparators)
            {
                _doc = doc;
                _comparators = comparators;
            }

            public int CompareTo(object o)
            {
                MultiDocIdComparable other = (MultiDocIdComparable)o;
                IComparable c1, c2;
                for (int i = 0; i < _comparators.Length; ++i)
                {
                    c1 = _comparators[i].Value(_doc);
                    c2 = other._comparators[i].Value(other._doc);
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
