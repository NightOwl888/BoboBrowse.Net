// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Range
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using Lucene.Net.Util;
    using System;

    public class ValueConverterBitSetBuilder : IBitSetBuilder
    {
        private readonly IFacetValueConverter facetValueConverter;
        private readonly string[] vals;
        private readonly bool takeCompliment;

        public ValueConverterBitSetBuilder(IFacetValueConverter facetValueConverter, string[] vals, bool takeCompliment) 
        {
            this.facetValueConverter = facetValueConverter;
            this.vals = vals;
            this.takeCompliment = takeCompliment;    
        }

        public virtual OpenBitSet BitSet(FacetDataCache dataCache)
        {
            int[] index = facetValueConverter.Convert(dataCache, vals);

            OpenBitSet bitset = new OpenBitSet(dataCache.ValArray.Count);
            foreach (int i in index)
            {
                bitset.FastSet(i);
            }
            if (takeCompliment)
            {
                // flip the bits
                for (int i = 0; i < index.Length; ++i)
                {
                    bitset.FastFlip(i);
                }
            }
            return bitset;
        }
    }
}
