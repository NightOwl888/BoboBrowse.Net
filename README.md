Bobo-Browse.Net
===============

This Bobo-Browse.Net is build for Lucene.Net 3.x.

if you still use Lucene.Net 2.x,you can see this project BoboBrowse.Net(https://bobo.codeplex.com/).

###Building the Source###

Prerequisites:

- [.NET Framework 3.5 SP1](http://www.microsoft.com/en-us/download/details.aspx?id=25150)
- [.NET Framework 4.0.x](http://www.microsoft.com/en-us/download/details.aspx?id=17851)
- [.NET Framework 4.5.x](http://www.microsoft.com/en-us/download/details.aspx?id=42643)
- [Windows PowerShell](http://technet.microsoft.com/en-us/library/hh847837.aspx)

> NOTE: If you have not yet run Windows Powershell on your computer, you will need to run the following command from an elevated command prompt before you can build the source.

```
Set-ExecutionPolicy RemoteSigned
```

To build the source, run the following command from the root directory of the Git project (the same directory that contains the .git folder):

```
build.bat -v:1.1.1 -pv:1.1.1-alpha00006
```

The -v parameter is the file version number, and the -pv parameter is the NuGet package version number. The package version number can contain a pre-release tag as shown in the example, but is not required.

Once the source has been built, you can install Bobo-Browse.Net into your project using NuGet. In Visual Studio, open the Options dialog from the Tools menu. In the left pane, choose NuGet Package Manager > Package Sources. Click the "+" button. Name the package source "Local Bobo-Browse.Net Feed", and add a Windows file path to `[Bobo-Browse Project Directory]\packages\packagesource\`. You can then use either the UI or Package Manager Console to install Bobo-Browse.Net into your project by selecting the "Local Bobo-Browse.Net Feed" as the package source.

###Performance###

we recommend use a  **singleton pattern** to create one instance of BoboBrowser in the application and interval update  object in the background-thread.that can avoid frequency reload entity index file and reduce a memory usage.

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
