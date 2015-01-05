// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Attribute
{
    using System;
    using System.Collections.Generic;

    public class AttributesFacetIterator : FacetIterator
    {
        private readonly IEnumerator<BrowseFacet> iterator;
        private readonly IEnumerable<BrowseFacet> facets;

        public AttributesFacetIterator(IEnumerable<BrowseFacet> facets)
        {
            iterator = facets.GetEnumerator();
            this.facets = facets;
        }

        public override bool HasNext()
        {
            return iterator.MoveNext();
        }

        public override void Remove()
        {
            throw new NotSupportedException();
        }

        public override string Next()
        {
            count = 0;
            BrowseFacet next = iterator.Current;
            if (next == null)
            {
                return null;
            }
            count = next.FacetValueHitCount;
            facet = next.Value;
            return next.Value;
        }

        public override string Next(int minHits)
        {
            while (iterator.MoveNext())
            {
                BrowseFacet next = iterator.Current;
                base.count = next.FacetValueHitCount;
                base.facet = next.Value;
                if (next.FacetValueHitCount >= minHits)
                {
                    return next.Value;
                }
            }
            return null;
        }

        public override string Format(object val)
        {
            return val != null ? val.ToString() : null;
        }
    }
}
