// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets
{
    using System;

    public abstract class FacetCountCollectorSource
    {
        public abstract IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase);
    }
}
