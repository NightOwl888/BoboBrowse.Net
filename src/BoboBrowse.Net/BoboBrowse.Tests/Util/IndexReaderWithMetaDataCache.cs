// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.Search.Section;
    using BoboBrowse.Net.Support;
    using Lucene.Net.Index;
    using System.Collections.Generic;

    public class IndexReaderWithMetaDataCache : FilterIndexReader, IMetaDataCacheProvider
    {
        private static Term intMetaTerm = new Term("metafield", "intmeta");
        private IDictionary<Term, IMetaDataCache> map = new Dictionary<Term, IMetaDataCache>();

        public IndexReaderWithMetaDataCache(IndexReader @in)
            : base(@in)
        {
            map.Put(intMetaTerm, new IntMetaDataCache(intMetaTerm, @in));
        }

        public IMetaDataCache Get(Term term)
        {
            return map.Get(term);
        }
    }
}
