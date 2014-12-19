// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Facets.Range;
    using BoboBrowse.Net.Query.Scoring;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class MultiValueWithWeightFacetHandler : MultiValueFacetHandler
    {
        public MultiValueWithWeightFacetHandler(string name, string indexFieldName, TermListFactory termListFactory)
            : base(name, indexFieldName, termListFactory, null, null)
        {
        }

        public MultiValueWithWeightFacetHandler(string name, string indexFieldName)
            : base(name, indexFieldName, null, null, null)
        {
        }

        public MultiValueWithWeightFacetHandler(string name)
            : base(name, name, null, null, null)
        {
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, Properties prop)
        {
            MultiValueFacetFilter f = new MultiValueFacetFilter(new MultiDataCacheBuilder(Name, _indexFieldName), value);
            return f;
        }

        public override IMultiValueFacetDataCache Load(BoboIndexReader reader, BoboIndexReader.WorkArea workArea)
        {
            MultiValueWithWeightFacetDataCache dataCache = new MultiValueWithWeightFacetDataCache();

            dataCache.SetMaxItems(_maxItems);

            if (_sizePayloadTerm == null)
            {
                dataCache.Load(_indexFieldName, reader, _termListFactory, workArea);
            }
            else
            {
                dataCache.Load(_indexFieldName, reader, _termListFactory, _sizePayloadTerm);
            }
            return dataCache;
        }
    }
}
