// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Index;

    public interface IMetaDataCacheProvider
    {
        IMetaDataCache Get(Term term);
    }
}
