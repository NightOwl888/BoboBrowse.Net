// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections.Generic;

    public class FacetValueComparatorFactory : IComparatorFactory
    {
        public virtual IComparer<int> NewComparator(IFieldValueAccessor fieldValueAccessor, int[] counts)
        {
            return new FacetValueComparatorFactoryComparator();
        }

        private class FacetValueComparatorFactoryComparator : IComparer<int>
        {
            public virtual int Compare(int o1, int o2)
            {
                return o2 - o1;
            }
        }

        public virtual IComparer<BrowseFacet> NewComparator()
        {
            return new FacetValueComparatorFactoryBrowseFacetComparator();
        }

        private class FacetValueComparatorFactoryBrowseFacetComparator : IComparer<BrowseFacet>
        {
            public virtual int Compare(BrowseFacet o1, BrowseFacet o2)
            {
                return string.CompareOrdinal(o1.Value, o2.Value);
            }
        }
    }
}
