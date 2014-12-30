// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Search;
    using Lucene.Net.Util;

    public abstract class SectionSearchQueryPlan
    {
        public const int NO_MORE_POSITIONS = int.MaxValue;
        public const int NO_MORE_SECTIONS = int.MaxValue;

        protected int _curDoc;
        protected int _curSec;

        /// <summary>
        /// Priority queue of Nodes.
        /// </summary>
        public class NodeQueue : PriorityQueue<SectionSearchQueryPlan>
        {
            public NodeQueue(int size)
            {
                Initialize(size);
            }

            public override bool LessThan(SectionSearchQueryPlan nodeA, SectionSearchQueryPlan nodeB)
            {
                if (nodeA._curDoc == nodeB._curDoc)
                {
                    return (nodeA._curSec < nodeB._curSec);
                }
                return (nodeA._curDoc < nodeB._curDoc);
            }
        }

        public SectionSearchQueryPlan()
        {
            _curDoc = -1;
            _curSec = -1;
        }

        public virtual int DocId
        {
            get { return _curDoc; }
        }

        public virtual int SecId
        {
            get { return _curSec; }
        }

        public virtual int Fetch(int targetDoc)
        {
            while (FetchDoc(targetDoc) < DocIdSetIterator.NO_MORE_DOCS)
            {
                if (FetchSec(0) < SectionSearchQueryPlan.NO_MORE_SECTIONS) return _curDoc;
            }
            return _curDoc;
        }

        public abstract int FetchDoc(int targetDoc);

        public abstract int FetchSec(int targetSec);

        protected virtual int FetchPos()
        {
            return NO_MORE_POSITIONS;
        }
    }
}
