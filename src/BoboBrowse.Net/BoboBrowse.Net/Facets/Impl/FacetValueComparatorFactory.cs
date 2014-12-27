// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections.Generic;

    public class FacetValueComparatorFactory : IComparatorFactory
    {
        public IComparer<int> NewComparator(IFieldValueAccessor fieldValueAccessor, int[] counts)
        {
            return new FacetValueComparatorFactoryComparator();
        }

        private class FacetValueComparatorFactoryComparator : IComparer<int>
        {
            public int Compare(int o1, int o2)
            {
                return o2 - o1;
            }
        }

        public IComparer<BrowseFacet> NewComparator()
        {
            return new FacetValueComparatorFactoryBrowseFacetComparator();
        }

        private class FacetValueComparatorFactoryBrowseFacetComparator : IComparer<BrowseFacet>
        {
            public int Compare(BrowseFacet o1, BrowseFacet o2)
            {
                return string.CompareOrdinal(o1.Value, o2.Value);
            }
        }
    }
}
