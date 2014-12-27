// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;

    /// <summary>
    /// An abstract class for terminal nodes of SectionSearchQueryPlan
    /// </summary>
    public abstract class AbstractTerminalNode : SectionSearchQueryPlan
    {
        protected TermPositions _tp;
        protected int _posLeft;
        protected int _curPos;

        public AbstractTerminalNode(Term term, IndexReader reader)
        {
            _tp = reader.TermPositions();
            _tp.Seek(term);
            _posLeft = 0;
        }

        public virtual int CurPos
        {
            get { return _curPos; }
        }

        public override int FetchDoc(int targetDoc)
        {
            if (targetDoc <= _curDoc) targetDoc = _curDoc + 1;

            if (_tp.SkipTo(targetDoc))
            {
                _curDoc = _tp.Doc;
                _posLeft = _tp.Freq;
                _curSec = -1;
                _curPos = -1;
                return _curDoc;
            }
            else
            {
                _curDoc = DocIdSetIterator.NO_MORE_DOCS;
                _tp.Close();
                return _curDoc;
            }
        }
    }
}
