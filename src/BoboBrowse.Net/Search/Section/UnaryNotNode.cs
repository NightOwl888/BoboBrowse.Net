// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Search.Section
{
    using System;

    /// <summary>
    /// UNARY-NOT operator node
    /// (this node is not supported by SectionSearchQueryPlan)
    /// </summary>
    public class UnaryNotNode : SectionSearchQueryPlan
    {
        private SectionSearchQueryPlan _subquery;

        public UnaryNotNode(SectionSearchQueryPlan subquery)
        {
            _subquery = subquery;
        }

        public virtual SectionSearchQueryPlan GetSubquery()
        {
            return _subquery;
        }

        public override int FetchDoc(int targetDoc)
        {
            throw new NotSupportedException("UnaryNotNode does not support fetchDoc");
        }

        public override int FetchSec(int targetSec)
        {
            throw new NotSupportedException("UnaryNotNode does not support fetchSec");
        }
    }
}
