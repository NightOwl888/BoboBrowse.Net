// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Filter
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using LuceneExt.Impl;

    public class NotFilter : Filter
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private readonly Filter _innerFilter;

        public NotFilter(Filter innerFilter)
        {
            _innerFilter = innerFilter;
        }

        public override DocIdSet GetDocIdSet(IndexReader reader)
        {
            return new NotDocIdSet(_innerFilter.GetDocIdSet(reader), reader.MaxDoc);
        }
    }
}