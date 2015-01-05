// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets
{
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Collects facet counts for a given browse request
    /// </summary>
    public interface IFacetCountCollector : IFacetAccessible
    {
        ///<summary>Collect a hit. This is called for every hit, thus the implementation needs to be super-optimized.</summary>
        ///<param name="docid"> doc </param>
        void Collect(int docid);

        ///<summary>Collects all hits. This is called once per request by the facet engine in certain scenarios.</summary>
        void CollectAll();

        ///<summary>Gets the name of the facet </summary>
        ///<returns>facet name </returns>
        string Name { get; }

        ///<summary>Returns an integer array representing the distribution function of a given facet.</summary>
        ///<returns><see cref="T:BoboBrowse.Net.Util.BigSegmentedArray"/> of count values representing distribution of the facet values.</returns>
        BigSegmentedArray GetCountDistribution();
    }

    public static class FacetCountCollector_Fields
    {
        ///<summary>Empty facet list.  </summary>
        public static List<BrowseFacet> EMPTY_FACET_LIST = new List<BrowseFacet>();
    }
}
