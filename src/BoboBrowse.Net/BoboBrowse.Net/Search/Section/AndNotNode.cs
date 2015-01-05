// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Search;

    /// <summary>
    /// AND-NOT operator node for SectionSearchQueryPlan
    /// </summary>
    public class AndNotNode : SectionSearchQueryPlan
    {
        SectionSearchQueryPlan _positiveNode;
        SectionSearchQueryPlan _negativeNode;

        public AndNotNode(SectionSearchQueryPlan positiveNode, SectionSearchQueryPlan negativeNode)
        {
            _positiveNode = positiveNode;
            _negativeNode = negativeNode;
        }

        public override int FetchDoc(int targetDoc)
        {
            _curDoc = _positiveNode.FetchDoc(targetDoc);
            _curSec = -1;
            return _curDoc;
        }

        public override int FetchSec(int targetSec)
        {
            while (_curSec < SectionSearchQueryPlan.NO_MORE_SECTIONS)
            {
                _curSec = _positiveNode.FetchSec(targetSec);
                if (_curSec == SectionSearchQueryPlan.NO_MORE_SECTIONS) break;

                targetSec = _curSec;

                if (_negativeNode.DocId < _curDoc)
                {
                    if (_negativeNode.FetchDoc(_curDoc) == DocIdSetIterator.NO_MORE_DOCS) break;
                }

                if (_negativeNode.DocId == _curDoc &&
                    (_negativeNode.SecId == SectionSearchQueryPlan.NO_MORE_SECTIONS ||
                     _negativeNode.FetchSec(targetSec) > _curSec))
                {
                    break;
                }
            }
            return _curSec;
        }
    }
}
