// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class MultiValuedPathFacetCountCollector : PathFacetCountCollector
    {
        private readonly BigNestedIntArray _array;

        public MultiValuedPathFacetCountCollector(string name, string sep,
            BrowseSelection sel, FacetSpec ospec, IFacetDataCache dataCache)
            : base(name, sep, sel, ospec, dataCache)
        {
            _array = ((MultiValueFacetDataCache)(dataCache))._nestedArray;
        }

        public override sealed void Collect(int docid) 
        {
            _array.CountNoReturn(docid, _count);
        }

        public override sealed void CollectAll()
        {
            _count = _dataCache.Freqs;
        }
    }
}
