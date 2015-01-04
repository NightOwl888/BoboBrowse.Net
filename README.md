BoboBrowse.Net
===============

Bobo-Browse is a powerful and extensible faceted search engine library built on top of Lucene.Net. It is a C# port of the [original Bobo-Browse project](https://github.com/senseidb/bobo) written in Java by John Wang.

This project is based on earlier work from [here](https://bobo.codeplex.com/) and [here](https://github.com/zhengchun/Bobo-Browse.Net), but both of those versions are based on Bobo-Browse.Net 2.x. This is an (almost) complete port of [Bobo-Browse 3.1.0](https://github.com/senseidb/bobo/releases), which is fully compatible with [Lucene.Net 3.0.3](https://www.nuget.org/packages/Lucene.Net/).

Features Not Implemented:

1. bobo-contrib
2. bobo-solr (doesn't make sense to port because this feature is a plugin for solr, a Java-based application).
3. CollectDocIdCache feature of BoboBrowser (uses memory management in a way that is incompatible with .NET).
4. Util.PrimitiveMatrix, Util.FloatMatrix, and Util.IntMatrix (not used by the rest of the framework and are essentially just 2-dimensional arrays, which don't exist in Java but already exist in .NET).
5. Util.MemoryManager and Util.MemoryManagerAdminMBean (uses memory management in a way that is incompatible with .NET).
6. Index.MakeBobo (a console application that can be used for writing Lucene.Net indexes, but isn't required)

###Status###

All tests are now passing. We now in an incubation period to evaluate stability.

###Documentation###

Read the documentation on the wiki: https://github.com/NightOwl888/BoboBrowse.Net/wiki

###License###

[GNU General Public License](https://github.com/NightOwl888/BoboBrowse.Net/blob/master/LICENSE.md)

###Install Via NuGet###

    PM> Install-Package BoboBrowse.Net -Pre

See [our page on the NuGet gallery](https://www.nuget.org/packages/BoboBrowse.Net/).

###Building the Source###

You only need to build the source if you plan to customize the source, but these instructions are provided in case you need to do so.

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
build.bat -v:3.1.0 -pv:3.1.0-beta00003
```

The -v parameter is the file version number, and the -pv parameter is the NuGet package version number. The package version number can contain a pre-release tag as shown in the example, but is not required.

Once the source has been built, you can install Bobo-Browse.Net into your project using NuGet. In Visual Studio, open the Options dialog from the Tools menu. In the left pane, choose NuGet Package Manager > Package Sources. Click the "+" button. Name the package source "Local Bobo-Browse.Net Feed", and add a Windows file path to `[Bobo-Browse Project Directory]\packages\packagesource\`. You can then use either the UI or Package Manager Console to install Bobo-Browse.Net into your project by selecting the "Local Bobo-Browse.Net Feed" as the package source.

###Sample Usage###

Here is a quick demonstration showing how easy it is to create a faceted search:

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
		Sort = new Sort(new SortField("price", SortField.DOUBLE, false)).GetSort()
	};

	var facetHandlers = new IFacetHandler[] { new SimpleFacetHandler("category") };
	var browser = new BoboBrowser(BoboIndexReader.GetInstance(IndexReader.Open(_indexDir, true), facetHandlers));
	var facetSpec = new FacetSpec() { OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc, MinHitCount = 1 };
	request.SetFacetSpec("category", facetSpec);

	var result = browser.Browse(request);
	Console.WriteLine(string.Format("total hits:{0}", result.NumHits));
	Console.WriteLine("===========================");
	foreach (var facet in result.FacetMap["category"].GetFacets())
	{
		var category = _categories.First(k => k.Value == int.Parse(facet.Value.ToString()));
		Console.WriteLine("{0}:({1})", category.Key, facet.FacetValueHitCount);
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

###Demo###

See the Car Demo in the source code for an ASP.NET MVC based example how Bobo-Browse.Net can be used to provide faceted drill-down search capability.
