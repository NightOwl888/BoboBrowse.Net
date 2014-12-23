// Version 3.1.0 compatibility
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using System;

    public abstract class GroupByFacetCountCollector : DefaultFacetCountCollector
    {
        private int _totalGroups;

        public GroupByFacetCountCollector(string name,
                                    FacetDataCache dataCache,
                                    int docBase,
                                    BrowseSelection sel,
                                    FacetSpec ospec)
            : base(name, dataCache, docBase, sel, ospec)
        {
        }

        public abstract int GetTotalGroups();
    }
}
