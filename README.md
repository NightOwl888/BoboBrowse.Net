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

```c#
[Test]
public void BrowseTest()
{
	FacetHandler facetHandler = new MultiValueFacetHandler(fieldName);

	ICollection<FacetHandler> handlerList = new FacetHandler[] { facetHandler };

	// opening a lucene index
	IndexReader reader = IndexReader.Open(_indexDir, true);

	// decorate it with a bobo index reader
	BoboIndexReader boboReader = BoboIndexReader.GetInstance(reader, handlerList);

	// creating a browse request
	BrowseRequest browseRequest = new BrowseRequest();
	browseRequest.Count = 10;
	browseRequest.Offset = 0;
	browseRequest.Sort = new SortField[] { new SortField("LeafName", SortField.STRING) };
	browseRequest.FetchStoredFields = true;

	// add a selection
	BrowseSelection sel = new BrowseSelection(fieldName);
	//sel.addValue("21");
	browseRequest.AddSelection(sel);

	// parse a query
	QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "Entity", new KeywordAnalyzer());
	Query q = parser.Parse("SPListItem");
	browseRequest.Query = q;

	// add the facet output specs
	FacetSpec colorSpec = new FacetSpec();
	colorSpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;

	FacetSpec categorySpec = new FacetSpec();
	categorySpec.MinHitCount = 2;
	categorySpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;

	browseRequest.SetFacetSpec(fieldName, colorSpec);

	// perform browse
	IBrowsable browser = new BoboBrowser(boboReader);

	BrowseResult result = browser.Browse(browseRequest);

	// Showing results now
	int totalHits = result.NumHits;
	BrowseHit[] hits = result.Hits;

	Dictionary<String, IFacetAccessible> facetMap = result.FacetMap;

	IFacetAccessible colorFacets = facetMap[fieldName];

	IEnumerable<BrowseFacet> facetVals = colorFacets.GetFacets();

	Debug.WriteLine("Facets:");

	foreach (BrowseFacet facet in facetVals)
	{
		Debug.WriteLine(facet.ToString());
	}

	Debug.WriteLine("Actual items:");

	for (int i = 0; i < hits.Length; ++i)
	{
		BrowseHit browseHit = hits[i];
		Debug.WriteLine(browseHit.StoredFields.Get("LeafName"));
	}
}
```