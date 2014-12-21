// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Search;

    /// <summary>
    /// OR operator node for SectionSearchQueryPlan
    /// </summary>
    public class OrNode : SectionSearchQueryPlan
    {
        private NodeQueue _pq;

        protected OrNode() { }

        public OrNode(SectionSearchQueryPlan[] subqueries)
        {
            if (subqueries.Length == 0)
            {
                _curDoc = DocIdSetIterator.NO_MORE_DOCS;
            }
            else
            {
                _pq = new NodeQueue(subqueries.Length);
                foreach (SectionSearchQueryPlan q in subqueries)
                {
                    if (q != null) _pq.Add(q);
                }
                _curDoc = -1;
            }
        }

        public override int FetchDoc(int targetDoc)
        {
            if (_curDoc == DocIdSetIterator.NO_MORE_DOCS) return _curDoc;

            if (targetDoc <= _curDoc) targetDoc = _curDoc + 1;

            _curSec = -1;

            SectionSearchQueryPlan node = (SectionSearchQueryPlan)_pq.Top();
            while (true)
            {
                if (node.DocId < targetDoc)
                {
                    if (node.FetchDoc(targetDoc) < DocIdSetIterator.NO_MORE_DOCS)
                    {
                        node = (SectionSearchQueryPlan)_pq.UpdateTop();
                    }
                    else
                    {
                        _pq.Pop();
                        if (_pq.Size() <= 0)
                        {
                            _curDoc = DocIdSetIterator.NO_MORE_DOCS;
                            return _curDoc;
                        }
                        node = (SectionSearchQueryPlan)_pq.Top();
                    }
                }
                else
                {
                    _curDoc = node.DocId;
                    return _curDoc;
                }
            }
        }

        public override int FetchSec(int targetSec)
        {
            if (_curSec == SectionSearchQueryPlan.NO_MORE_SECTIONS) return _curSec;

            if (targetSec <= _curSec) targetSec = _curSec + 1;

            SectionSearchQueryPlan node = (SectionSearchQueryPlan)_pq.Top();
            while (true)
            {
                if (node.DocId == _curDoc && _curSec < SectionSearchQueryPlan.NO_MORE_SECTIONS)
                {
                    if (node.SecId < targetSec)
                    {
                        node.FetchSec(targetSec);
                        node = (SectionSearchQueryPlan)_pq.UpdateTop();
                    }
                    else
                    {
                        _curSec = node.SecId;
                        return _curSec;
                    }
                }
                else
                {
                    _curSec = SectionSearchQueryPlan.NO_MORE_SECTIONS;
                    return _curSec;
                }
            }
        }
    }
}
