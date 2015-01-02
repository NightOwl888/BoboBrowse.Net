// Version compatibility level: 3.1.0
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
