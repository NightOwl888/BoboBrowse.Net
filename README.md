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

```cs
public void TestSimpleBrowser()
{
	var query = new TermQuery(new Term("name", "asp.net"));
	Console.WriteLine(string.Format("query: <{0}>", query.ToString()));
	var request = new BrowseRequest()
	{
		Count = 10,
		Offset = 0,
		Query = query,
		Sort = new Sort(new SortField("price", SortField.DOUBLE,false)).GetSort()
	};

	var faceHandlers = new FacetHandler[] { new SimpleFacetHandler("category") };
	var browser = new BoboBrowser(BoboIndexReader.GetInstance(IndexReader.Open(_indexDir, true), faceHandlers));
	var factSpec = new FacetSpec() { OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc, MinHitCount = 1 };
	request.SetFacetSpec("category", factSpec);

	var result = browser.Browse(request);
	Console.WriteLine(string.Format("total hits:{0}", result.NumHits));
	Console.WriteLine("===========================");
	foreach (var facet in result.FacetMap["category"].GetFacets())
	{
		var category = _categories.First(k => k.Value == int.Parse(facet.Value.ToString()));
		Console.WriteLine("{0}:({1})", category.Key, facet.HitCount);
	}
	Console.WriteLine("===========================");
	for (var i = 0; i < result.Hits.Length; i++)
	{
		var doc = browser.Doc(result.Hits[i].DocId);
		var category = _categories.First(k => k.Value == int.Parse(doc.GetField("category").StringValue)).Key;
		Console.WriteLine(string.Format("{2} - {0} ${1} by {3}", doc.GetField("name").StringValue, doc.GetField("price").StringValue, category, doc.GetField("author").StringValue));
	}
}
```

output:
```
total hits:6
===========================
C#:(3)
WEB:(2)
AJAX:(1)
===========================
WEB - Bootstrap for ASP.NET MVC $11.49 by Pieter van der Westhuizen
WEB - Mobile ASP.NET MVC 5 $19.79 by Eric Sowell
C# - ASP.NET Web API 2: Building a REST Service from Start to Finish $23.45 by Jamie Kurtz , Brian Wortman
C# - Pro ASP.NET MVC 5 $27.49 by Adam Freeman
AJAX - Professional ASP.NET 3.5 AJAX $33.29 by Bill Evjen
C# - Designing Evolvable Web APIs with ASP.NET $33.34 by Pablo Cibraro
```