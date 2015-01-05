// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Query
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;

    public class MatchAllDocIdSetIterator : DocIdSetIterator
    {
        private readonly TermDocs _termDocs;
        private int _docid;
        public MatchAllDocIdSetIterator(IndexReader reader)
        {
            _termDocs = reader.TermDocs(null);
            _docid = -1;
        }

        public override int Advance(int target)
        {
            return _docid = _termDocs.SkipTo(target) ? _termDocs.Doc : NO_MORE_DOCS;
        }

        public override int DocID()
        {
            return _docid;
        }

        public override int NextDoc()
        {
            return _docid = _termDocs.Next() ? _termDocs.Doc : NO_MORE_DOCS;
        }
    }
}
