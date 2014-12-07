// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Range
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using System;

    public class SimpleDataCacheBuilder<T> : AdaptiveFacetFilter.FacetDataCacheBuilder
    {
        private String name;
        private String indexFieldName;

        public SimpleDataCacheBuilder(String name, String indexFieldName)
        {
            this.name = name;
            this.indexFieldName = indexFieldName;
        }

        public FacetDataCache<T> build(BoboIndexReader reader)
        {
            return (FacetDataCache<T>)reader.GetFacetData(name);
        }

        public String getName()
        {
            return name;
        }

        public String getIndexFieldName()
        {
            return indexFieldName;
        }
    }
}
