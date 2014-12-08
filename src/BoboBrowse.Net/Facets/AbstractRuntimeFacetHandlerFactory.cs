// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets
{
    using System;

    public abstract class AbstractRuntimeFacetHandlerFactory<P, F, T> : IRuntimeFacetHandlerFactory<P, F, T>
    {
        public abstract string Name { get; }

        /// <summary>
        /// if this facet support empty params or not. By default it returns false.
        /// </summary>
        public virtual bool IsLoadLazily
        {
            get { return false; }
        }

        public abstract F Get(P @params);
    }
}
