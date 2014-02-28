Bobo-Browse.Net
===============

__NOTES__:This version of Bobo-Browse.Net is build for Lucene.Net 3.0 that based on BoboBrowse.Net(https://bobo.codeplex.com/) source code.

If you want re-build,you will need a some of third party library that can by Nuget tool get it.

see an example can see this: http://www.yamool.com/catalog/2585

###Performance###

we recommend use a  **singleton pattern** to create one instance of BoboBrowser in the application and interval update  object in the background-thread.that can avoid frequency reload entity index file and reduce a memory usage.see more at http://www.yamool.org/post/2014/02/28/bobobrowse-net-lucene-net-303

usage:

        [Test]
        public void SimpleYamoolDemo()
        {
            //open a lucene index file.                 
            var idx = FSDirectory.Open(new System.IO.DirectoryInfo(IndexPath));
            var reader = IndexReader.Open(idx, true);
            //declare a Body field by faceted handler.
            var facetHandler = new MultiValueFacetHandler("Body");
            var boboReader = BoboIndexReader.GetInstance(reader, new FacetHandler[] { facetHandler });
            //create a new search request of browse that similare to lucene search(etc.skip,count,sort)
            var browseRequest = new BrowseRequest()
            {
                Count = 10,
                Offset = 0,
                Sort = new SortField[] { new SortField("LeafName",SortField.STRING) },
                FetchStoredFields = true
            };
            //create a new query for search
            var parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_CURRENT, "Entity", new KeywordAnalyzer());
            var q = parser.Parse("SPListItem");
            //TODO:setting query for browse request.
            browseRequest.Query = q;

            // declare a facete option for by handler by bobo-browse
            var facetOption = new FacetSpec();
            //declare a filter for facet result that only return facet with 'al' begin.            
            facetOption.Prefix = "al";//if we not filter for facet we can remove it.
            facetOption.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;            
            browseRequest.SetFacetSpec("Body", facetOption);

            // perform browse
            var browser = new BoboBrowser(boboReader);
            var result = browser.Browse(browseRequest);

            // Showing results of now          
            //get a specified facet field
            var facetResult = result.FacetMap["Body"];
            var facetVals = facetResult.GetFacets();

            Console.WriteLine("Facets:");
            int count = 0;
            foreach (BrowseFacet facet in facetVals)
            {
                count++;
                Console.WriteLine(facet.ToString());
            }
            Console.WriteLine("Total = " + count);

            //show items
            Console.WriteLine(string.Empty);
            Console.WriteLine("Actual items:");
            BrowseHit[] hits = result.Hits;
            for (int i = 0; i < hits.Length; i++)
            {
                BrowseHit browseHit = hits[i];
                Console.WriteLine(browseHit.StoredFields.Get("LeafName"));
            }
        }    
