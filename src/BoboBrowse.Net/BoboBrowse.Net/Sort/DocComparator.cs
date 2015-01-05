// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Sort
{
    using Lucene.Net.Search;
    using System;

    public abstract class DocComparator
    {
        public abstract int Compare(ScoreDoc doc1, ScoreDoc doc2);

        public abstract IComparable Value(ScoreDoc doc);

        public virtual void SetScorer(Scorer scorer)
        {
        }
    }
}
