// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;

    public abstract class MetaDataQuery : Query
    {
        private static long serialVersionUID = 1L;

        protected Term _term;

        public MetaDataQuery(Term term)
        {
            _term = term;
        }

        public virtual Term Term
        {
            get { return _term; }
        }

        public abstract SectionSearchQueryPlan GetPlan(IndexReader reader);
        public abstract SectionSearchQueryPlan GetPlan(IMetaDataCache cache);
    }
}
