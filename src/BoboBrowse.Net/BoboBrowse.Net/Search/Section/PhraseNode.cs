// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Index;

    /// <summary>
    /// Phrase operator node for SectionSearchQUeryPlan
    /// </summary>
    public class PhraseNode : AndNode
    {
        private TermNode[] _termNodes;
        private int _curPos;

        public PhraseNode(TermNode[] termNodes, IndexReader reader)
            : base(termNodes)
        {
            _termNodes = termNodes;
        }

        public override int FetchDoc(int targetDoc)
        {
            _curPos = -1;
            return base.FetchDoc(targetDoc);
        }

        public override int FetchSec(int targetSec)
        {
            TermNode firstNode = _termNodes[0];

            while (FetchPos() < SectionSearchQueryPlan.NO_MORE_POSITIONS)
            {
                int secId = firstNode.ReadSecId();
                if (secId >= targetSec)
                {
                    targetSec = secId;
                    bool matched = true;
                    for (int i = 1; i < _termNodes.Length; i++)
                    {
                        matched = (targetSec == _termNodes[i].ReadSecId());
                        if (!matched) break;
                    }
                    if (matched)
                    {
                        _curSec = targetSec;
                        return _curSec;
                    }
                }
            }
            _curSec = SectionSearchQueryPlan.NO_MORE_SECTIONS;
            return _curSec;
        }

        protected override int FetchPos()
        {
            int targetPhrasePos = _curPos + 1;

            int i = 0;
            while (i < _termNodes.Length)
            {
                TermNode node = _termNodes[i];
                int targetTermPos = (targetPhrasePos + node.PositionInPhrase);
                while (node.CurPos < targetTermPos)
                {
                    if (node.FetchPosInternal() == SectionSearchQueryPlan.NO_MORE_POSITIONS)
                    {
                        _curPos = SectionSearchQueryPlan.NO_MORE_POSITIONS;
                        return _curPos;
                    }
                }
                if (node.CurPos == targetTermPos)
                {
                    i++;
                }
                else
                {
                    targetPhrasePos = node.CurPos - i;
                    i = 0;
                }
            }
            _curPos = targetPhrasePos;
            return _curPos;
        }
    }
}
