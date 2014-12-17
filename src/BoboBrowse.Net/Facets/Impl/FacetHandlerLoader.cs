// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    // NOTE: This type doesn't appear to be used anywhere (or complete).

    public class FacetHandlerLoader
    {
        private FacetHandlerLoader()
        {

        }
        public static void Load(IEnumerable<IFacetHandler> tobeLoaded)
        {
            Load(tobeLoaded, null);
        }

        public static void Load(IEnumerable<IFacetHandler> tobeLoaded, IDictionary<string, IFacetHandler> preloaded)
        {

        }

        private static void Load(BoboIndexReader reader, IEnumerable<IFacetHandler> tobeLoaded, IDictionary<string, IFacetHandler> preloaded, IEnumerable<string> visited)
        {
            IDictionary<string, IFacetHandler> loaded = new Dictionary<string, IFacetHandler>();
            if (preloaded != null)
            {
                loaded.PutAll(preloaded);
            }

            IEnumerator<IFacetHandler> iter = tobeLoaded.GetEnumerator();

            while (iter.MoveNext())
            {
                IFacetHandler handler = iter.Current;
                if (!loaded.ContainsKey(handler.Name))
                {
                    IEnumerable<string> depends = handler.DependsOn;
                    if (depends.Count() > 0)
                    {
                    }
                    handler.Load(reader);
                }
            }
        }
    }
}
