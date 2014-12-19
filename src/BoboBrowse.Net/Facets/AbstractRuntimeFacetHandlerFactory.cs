// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets
{
    using System;

    public abstract class AbstractRuntimeFacetHandlerFactory : IRuntimeFacetHandlerFactory
    {
        public abstract string Name { get; }

        /// <summary>
        /// if this facet support empty params or not. By default it returns false.
        /// </summary>
        public virtual bool IsLoadLazily
        {
            get { return false; }
        }

        public abstract IRuntimeFacetHandler Get(FacetHandlerInitializerParam @params);
    }
}
