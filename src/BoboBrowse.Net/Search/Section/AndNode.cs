// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Search;

    /// <summary>
    /// AND operator node for SectionSearchQueryPlan
    /// </summary>
    public class AndNode : SectionSearchQueryPlan
    {
        protected SectionSearchQueryPlan[] _subqueries;

        public AndNode(SectionSearchQueryPlan[] subqueries)
        {
            _subqueries = subqueries;
            _curDoc = (subqueries.Length > 0 ? -1 : DocIdSetIterator.NO_MORE_DOCS);
        }

        public override int FetchDoc(int targetDoc)
        {
            if (_curDoc == DocIdSetIterator.NO_MORE_DOCS)
            {
                return _curDoc;
            }

            SectionSearchQueryPlan node = _subqueries[0];
            _curDoc = node.FetchDoc(targetDoc);
            targetDoc = _curDoc;

            int i = 1;
            while (i < _subqueries.Length)
            {
                node = _subqueries[i];
                if (node.DocId < targetDoc)
                {
                    _curDoc = node.FetchDoc(targetDoc);
                    if (_curDoc == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        return _curDoc;
                    }

                    if (_curDoc > targetDoc)
                    {
                        targetDoc = _curDoc;
                        i = 0;
                        continue;
                    }
                }
                i++;
            }
            _curSec = -1;
            return _curDoc;
        }

        public override int FetchSec(int targetSec)
        {
            SectionSearchQueryPlan node = _subqueries[0];
            targetSec = node.FetchSec(targetSec);
            if (targetSec == SectionSearchQueryPlan.NO_MORE_SECTIONS)
            {
                _curSec = SectionSearchQueryPlan.NO_MORE_SECTIONS;
                return targetSec;
            }

            int i = 1;
            while (i < _subqueries.Length)
            {
                node = _subqueries[i];
                if (node.SecId < targetSec)
                {
                    _curSec = node.FetchSec(targetSec);
                    if (_curSec == SectionSearchQueryPlan.NO_MORE_SECTIONS)
                    {
                        return _curSec;
                    }

                    if (_curSec > targetSec)
                    {
                        targetSec = _curSec;
                        i = 0;
                        continue;
                    }
                }
                i++;
            }
            return _curSec;
        }
    }
}
