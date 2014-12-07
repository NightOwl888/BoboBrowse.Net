// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Range
{
    using BoboBrowse.Net.Facets.Data;
    using System;

    public class MultiDataCacheBuilder<T> : FacetDataCacheBuilder
    {
        private string name;
        private string indexFieldName;

        public MultiDataCacheBuilder(string name, string indexFieldName)
        {
            this.name = name;
            this.indexFieldName = indexFieldName;
        }

        public MultiValueFacetDataCache<T> build(BoboIndexReader reader)
        {
            return (MultiValueFacetDataCache<T>)reader.GetFacetData(name);
        }

        public String GetName()
        {
            return name;
        }

        public String GetIndexFieldName()
        {
            return indexFieldName;
        }
    }
}
