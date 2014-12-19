// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Range
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using System;

    public class SimpleDataCacheBuilder : IFacetDataCacheBuilder
    {
        private string name;
        private string indexFieldName;

        public SimpleDataCacheBuilder(string name, string indexFieldName)
        {
            this.name = name;
            this.indexFieldName = indexFieldName;
        }

        public IFacetDataCache Build(BoboIndexReader reader)
        {
            return (IFacetDataCache)reader.GetFacetData(name);
        }

        public string Name
        {
            get { return name; }
        }

        public string IndexFieldName
        {
            get { return indexFieldName; }
        }
    }
}
