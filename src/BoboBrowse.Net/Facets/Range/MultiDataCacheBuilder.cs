// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Range
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using System;

    public class MultiDataCacheBuilder : IFacetDataCacheBuilder
    {
        private string name;
        private string indexFieldName;

        public MultiDataCacheBuilder(string name, string indexFieldName)
        {
            this.name = name;
            this.indexFieldName = indexFieldName;
        }

        public IMultiValueFacetDataCache Build(BoboIndexReader reader)
        {
            return (IMultiValueFacetDataCache)reader.GetFacetData(name);
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
