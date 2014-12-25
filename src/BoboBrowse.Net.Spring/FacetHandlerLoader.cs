
namespace BoboBrowse.Net.Spring
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets;
    using global::Spring.Context.Support;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class FacetHandlerLoader
    {
        public IEnumerable<IFacetHandler> LoadFacetHandlers(string springConfigFile, BoboIndexReader.WorkArea workArea)
        {
            if (File.Exists(springConfigFile))
            {
                XmlApplicationContext appCtx = new XmlApplicationContext(springConfigFile);
                return appCtx.GetObjectsOfType(typeof(IFacetHandler)).Values.OfType<IFacetHandler>().ToList();
            }
            else
            {
                return new List<IFacetHandler>();
            }
        }
    }
}
