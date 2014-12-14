// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets
{
    using System;

    /// <summary>
    /// This interface is intended for using with RuntimeFacetHandler, which typically
    /// have local data that make them not only NOT thread safe but also dependent on
    /// request. So it is necessary to have different instance for different client or
    /// request. Typically, the new instance need to be initialized before use.
    /// 
    /// author xiaoyang
    /// </summary>
    public interface IRuntimeFacetHandlerFactory
    {
        /// <summary>
        /// Gets the facet name of the RuntimeFacetHandler it creates.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets if this facet support empty params or not.
        /// </summary>
        bool IsLoadLazily { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="params">the data used to initialize the RuntimeFacetHandler.</param>
        /// <returns>a new instance of </returns>
        RuntimeFacetHandler Get(FacetHandlerInitializerParam @params);
    }
}
