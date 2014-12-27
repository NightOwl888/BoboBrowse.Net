// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets
{
    using BoboBrowse.Net.Facets.Data;
    using Lucene.Net.Util;
    using System;

    public interface IBitSetBuilder
    {
        OpenBitSet BitSet(FacetDataCache dataCache);
    }
}
