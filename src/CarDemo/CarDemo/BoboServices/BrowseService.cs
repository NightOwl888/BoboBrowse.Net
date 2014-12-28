using BoboBrowse.Net;
using BoboBrowse.Net.Facets;
using BoboBrowse.Net.Facets.Data;
using BoboBrowse.Net.Facets.Impl;
using Lucene.Net.Index;
using Lucene.Net.Store;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Hosting;

namespace CarDemo.BoboServices
{
    public class BrowseService
    {
        public BrowseResult Browse(BrowseRequest browseRequest)
        {
            string indexDir = HostingEnvironment.MapPath(ConfigurationManager.AppSettings["LuceneIndexDirectory"]);

            // This is the equivalent code to what is specified in the "/LuceneIndex/bobo.spring" file. You could just as well
            // specify this configuration here and pass it to another overload of BoboIndexReader.GetInstance that accepts 
            // an IEnumerable<IFacetHandler> as an argument.

            //var facetHandlers = new List<IFacetHandler>();
            //facetHandlers.Add(new SimpleFacetHandler("color") { TermCountSize = BoboBrowse.Net.Facets.TermCountSize.Small });
            //facetHandlers.Add(new SimpleFacetHandler("category") { TermCountSize = BoboBrowse.Net.Facets.TermCountSize.Medium });
            //facetHandlers.Add(new PathFacetHandler("city") { Separator = "/" });
            //facetHandlers.Add(new PathFacetHandler("makemodel") { Separator = "/" });
            //facetHandlers.Add(new RangeFacetHandler("year", new PredefinedTermListFactory<int>("00000000000000000000"), new string[] { "[1993 TO 1994]", "[1995 TO 1996]", "[1997 TO 1998]", "[1999 TO 2000]", "[2001 TO 2002]" }));
            //facetHandlers.Add(new RangeFacetHandler("price", new PredefinedTermListFactory<float>("00000000000000000000"), new string[] { "[2001 TO 6700]", "[6800 TO 9900]", "[10000 TO 13100]", "[13200 TO 17300]", "[17400 TO 19500]" }));
            //facetHandlers.Add(new RangeFacetHandler("mileage", new PredefinedTermListFactory<int>("00000000000000000000"), new string[] { "[* TO 12500]", "[12501 TO 15000]", "[15001 TO 17500]", "[17501 TO *]" }));
            //facetHandlers.Add(new MultiValueFacetHandler("tags"));


            System.IO.DirectoryInfo idxDir = new System.IO.DirectoryInfo(indexDir);
            using (IndexReader reader = IndexReader.Open(FSDirectory.Open(idxDir), true))
            {
                using (BoboIndexReader boboReader = BoboIndexReader.GetInstance(reader))
                {
                    using (BoboBrowser browser = new BoboBrowser(boboReader))
                    {
                        return browser.Browse(browseRequest);
                    }
                }
            }
        }
    }
}