// Version compatibility level: 3.2.0
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Impl;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class BasicTests
    {
        private Directory _indexDir;
        private IDictionary<string, int> _categories;

        [TestFixtureSetUp]
        public void Init()
        {
            _categories = new Dictionary<string, int>() { { "JAVA", 1 }, { "C#", 2 }, { "AJAX", 3 }, { "PYTHON", 4 }, { "WEB", 5 }, { "JAVASCRIPT", 6 } };
            _indexDir = new RAMDirectory();
            //build a test index file.            
            var dataSet = new[] {
                new {Name="Head First Java, 2nd Edition",Year=2000,Category=1,Price=28.95,Author="Kathy Sierra",Path="Book/Programming/Language"},
                new {Name="Head First Java: Your Brain on Java - A Learner's Guide",Year=2010,Category=1,Price=47.42,Author="Bert Bates,Kathy Sierra",Path="Book/Programming/Language"},
                new {Name="Java: A Beginner's Guide",Year=2010,Category=1,Price=25.94,Author="Herbert Schildt",Path="Book/Programming/Language"},
                new {Name="Head First Design Patterns",Year=2014,Category=1,Price=37.15,Author="Eric Freeman",Path="Book/Programming/Language"},
                new {Name="Effective Java (2nd Edition)",Year=2011,Category=1,Price=38.82,Author="Joshua Bloch",Path="Book/Programming/Language"},
                new {Name="Hadoop: The Definitive Guide",Year=2010,Category=1,Price=28.99,Author="Tom White",Path="Book/Programming/Language"},
                new {Name="Secrets of the JavaScript Ninja",Year=2014,Category=1,Price=24.46,Author="Bear Bibeault",Path="Book/Programming/Language"},
                new {Name="Spring in Action",Year=2008,Category=1,Price=40.74,Author="Craig Walls",Path="Book/Programming/Language"},

                new {Name="C# 5.0 in a Nutshell: The Definitive Reference",Year=2010,Category=2,Price=34.07,Author="Joseph Albahari,Ben Albahari",Path="Book/Programming/Language"},
                new {Name="C# in Depth, 3rd Edition",Year=2012,Category=2,Price=32.76,Author="Jon Skeet",Path="Book/Programming/Language"},
                new {Name="C# 5.0 Pocket Reference: Instant Help for C# 5.0 Programmers",Year=2012,Category=2,Price=11.43,Author="Joseph Albahari,Ben Albahari",Path="Book/Programming/Language"},
                new {Name="LINQ Pocket Reference",Year=2013,Category=2,Price=14.99,Author="Joseph Albahari,Ben Albahari",Path="Book/Programming/Language"},
                new {Name="Pro ASP.NET MVC 5",Year=2014,Category=2,Price=27.49,Author="Adam Freeman",Path="Book/Programming/Language"},
                new {Name="ASP.NET Web API 2: Building a REST Service from Start to Finish",Year=2008,Category=2,Price=23.45,Author="Jamie Kurtz , Brian Wortman",Path="Book/Programming/Language"},
                new {Name="Designing Evolvable Web APIs with ASP.NET",Year=2013,Category=2,Price=33.34,Author="Pablo Cibraro",Path="Book/Programming/Language"},

                new {Name="Professional ASP.NET 3.5 AJAX",Year=1998,Category=3,Price=33.29,Author="Bill Evjen",Path="Book/Programming/Web"},
                new {Name="Head First Ajax",Year=2007,Category=3,Price=31.57,Author="Rebecca Riordan",Path="Book/Programming/Web"},
                new {Name="JavaScript and AJAX For Dummies",Year=2005,Category=3,Price=20.18,Author="Richard Wagner",Path="Book/Programming/Web"},
                new {Name="AJAX and PHP: Building Responsive Web",Year=2004,Category=3,Price=28.5,Author="Cristian Darie",Path="Book/Programming/Web"},
                new {Name="Web 2.0 Fundamentals: With AJAX, Development Tools, And Mobile Platforms",Year=2007,Category=3,Price=28.54,Author="Oswald Campesato",Path="Book/Programming/Web"},

                new {Name="Learning Python, 5th Edition",Year=1998,Category=4,Price=41.35,Author="Mark Lutz",Path="Book/Programming/Language"},
                new {Name="Mining the Social Web: Data Mining Facebook, Twitter, LinkedIn, Google+, GitHub, and More",Year=2000,Category=4,Price=26.41,Author="Matthew A. Russell",Path="Book/Programming/Language"},
                new {Name="Python Programming for the Absolute Beginner, 3rd Edition",Year=2005,Category=4,Price=22.70,Author="Michael Dawson",Path="Book/Programming/Language"},
                new {Name="Python Cookbook",Year=2010,Category=4,Price=30.37,Author="David M. Beazley",Path="Book/Programming/Language"},

                new {Name="Web Design with HTML, CSS, JavaScript and jQuery Set",Year=2014,Category=5,Price=38.69,Author="Mark Myers",Path="Book/Programming/Design"},
                new {Name="Pro AngularJS (Expert's Voice in Web Development)",Year=2008,Category=5,Price=36.27,Author="Adam Freeman",Path="Book/Programming/Design"},
                new {Name="HTML and CSS: Design and Build Websites",Year=2011,Category=5,Price=17.39,Author="Jon Duckett",Path="Book/Programming/Design"},
                new {Name="Web Design All-in-One For Dummies",Year=2014,Category=5,Price=27.2,Author="Sue Jenkins",Path="Book/Programming/Design"},
                new {Name="The Principles of Beautiful Web Design",Year=2014,Category=5,Price=27.28,Author="Jason Beaird",Path="Book/Programming/Design"},
                new {Name="Bootstrap for ASP.NET MVC",Year=2014,Category=5,Price=11.49,Author="Pieter van der Westhuizen",Path="Book/Programming/Design"},
                new {Name="Mobile ASP.NET MVC 5",Year=2012,Category=5,Price=19.79,Author="Eric Sowell",Path="Book/Programming/Design"},

                new {Name="JavaScript and JQuery: Interactive Front-End Web Development",Year=2013,Category=6,Price=26.93,Author="Jon Duckett",Path="Book/Programming/Web"},
                new {Name="JavaScript: The Definitive Guide: Activate Your Web Pages",Year=2013,Category=6,Price=30.69,Author="David Flanagan",Path="Book/Programming/Web"},
                new {Name="JavaScript and HTML5 Now",Year=2013,Category=6,Price=12.1,Author=" Kyle Simpson",Path="Book/Programming/Web"},
                new {Name="Head First JavaScript Programming",Year=2012,Category=6,Price=31.92,Author=" Eric T. Freeman , Elisabeth Robson",Path="Book/Programming/Web"},
            };
            using (var indexWriter = new IndexWriter(_indexDir, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_CURRENT), IndexWriter.MaxFieldLength.UNLIMITED))
            {
                foreach (var book in dataSet)
                {
                    var doc = new Document();
                    doc.Add(new Field("name", book.Name, Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field("category", book.Category.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                    doc.Add(new NumericField("price", int.MaxValue - 1, Field.Store.YES, true).SetDoubleValue(book.Price));
                    doc.Add(new Field("author", book.Author, Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field("path", book.Path, Field.Store.YES, Field.Index.ANALYZED));
                    doc.Add(new Field("year", book.Year.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                    indexWriter.AddDocument(doc);
                }
                indexWriter.Optimize();
            }
        }

        [Test]
        public void TestAutoComplete()
        {
            var query = new MatchAllDocsQuery();
            Console.WriteLine(string.Format("query: <{0}>", query.ToString()));
            var request = new BrowseRequest()
            {
                Count = 10,
                Offset = 0,
                Query = query
            };

            //var prefix = "java"; // NightOwl888: Prefix is no longer a feature
            //Console.WriteLine(string.Format("prefix:{0}", prefix));
            //Console.WriteLine("=============================");

            var faceHandlers = new IFacetHandler[] { new SimpleFacetHandler("name") };
            var browser = new BoboBrowser(BoboIndexReader.GetInstance(IndexReader.Open(_indexDir, true), faceHandlers));
            var factSpec = new FacetSpec() { OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc };
            request.SetFacetSpec("name", factSpec);

            var result = browser.Browse(request);

            foreach (var facet in result.FacetMap["name"].GetFacets())
            {
                Console.WriteLine(facet.ToString());
            }
        }

        [Test]
        public void TestSimpleFacetHandler()
        {
            var query = new TermQuery(new Term("name", "asp.net"));
            Console.WriteLine(string.Format("query: <{0}>", query.ToString()));
            var request = new BrowseRequest()
            {
                Count = 10,
                Offset = 0,
                Query = query,
                Sort = new Lucene.Net.Search.Sort(new SortField("year", SortField.INT, false)).GetSort()
            };

            var faceHandlers = new IFacetHandler[] { new SimpleFacetHandler("category") };
            var browser = new BoboBrowser(BoboIndexReader.GetInstance(IndexReader.Open(_indexDir, true), faceHandlers));
            var factSpec = new FacetSpec() { OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc, MinHitCount = 1 };
            request.SetFacetSpec("category", factSpec);

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
                Console.WriteLine(string.Format("{2} - {0}({4}) ${1} by {3}", doc.GetField("name").StringValue, doc.GetField("price").StringValue, category, doc.GetField("author").StringValue, doc.GetField("year").StringValue));
            }
        }

        [Test]
        public void TestFacetSelectionFilter()
        {
            var query = new MatchAllDocsQuery();
            Console.WriteLine(string.Format("query: <{0}>", query.ToString()));
            var request = new BrowseRequest()
            {
                Count = 10,
                Offset = 0,
                Query = query
            };
            var authors = new string[] { "kathy", "sierra" };//kathy&sierra
            var sectionFilter = new BrowseSelection("author");
            sectionFilter.Values = authors;
            sectionFilter.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationAnd;
            request.AddSelection(sectionFilter);

            var browser = new BoboBrowser(BoboIndexReader.GetInstance(IndexReader.Open(_indexDir, true), new IFacetHandler[] { new MultiValueFacetHandler("author") }));
            var result = browser.Browse(request);
            Console.WriteLine("===========================");
            for (var i = 0; i < result.Hits.Length; i++)
            {
                var doc = browser.Doc(result.Hits[i].DocId);
                var category = _categories.First(k => k.Value == int.Parse(doc.GetField("category").StringValue)).Key;
                Console.WriteLine(string.Format("{2} - {0} ${1} by {3}", doc.GetField("name").StringValue, doc.GetField("price").StringValue, category, doc.GetField("author").StringValue));
            }
        }

        [Test]
        public void TestMultiValueFacetHandler()
        {
            var query = new MatchAllDocsQuery();
            Console.WriteLine(string.Format("query: <{0}>", query.ToString()));
            var request = new BrowseRequest()
            {
                Count = 100,
                Offset = 0,
                Query = query,
                Sort = new Lucene.Net.Search.Sort(new SortField("path", SortField.STRING, false)).GetSort()
            };

            var faceHandlers = new IFacetHandler[] { new MultiValueFacetHandler("path") };
            var browser = new BoboBrowser(BoboIndexReader.GetInstance(IndexReader.Open(_indexDir, true), faceHandlers));
            var factSpec = new FacetSpec() { OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc };
            request.SetFacetSpec("path", factSpec);

            var result = browser.Browse(request);
            Console.WriteLine(string.Format("total hits:{0}", result.NumHits));
            Console.WriteLine("===========================");
            foreach (var facet in result.FacetMap["path"].GetFacets())
            {
                Console.WriteLine(facet.ToString());
            }
            Console.WriteLine("===========================");
            for (var i = 0; i < result.Hits.Length; i++)
            {
                var doc = browser.Doc(result.Hits[i].DocId);
                var category = _categories.First(k => k.Value == int.Parse(doc.GetField("category").StringValue)).Key;
                Console.WriteLine(string.Format("{2} - {0} ${1} by {3}", doc.GetField("name").StringValue, doc.GetField("price").StringValue, category, doc.GetField("author").StringValue));
            }
        }

        [Test]
        public void TestRangeFacetHandler()
        {
            var query = new MatchAllDocsQuery();
            Console.WriteLine(string.Format("query: <{0}>", query.ToString()));

            var testRangeFacetHandlers = new List<IFacetHandler>();
            //testRangeFacetHandlers.Add(new RangeFacetHandler("year", true));//auto range // NightOwl888 - Auto range is no longer a feature.
            testRangeFacetHandlers.Add(new RangeFacetHandler("year", new List<string>(new string[] { "[* TO 2000]", "[2000 TO 2005]", "[2006 TO 2010]", "[2011 TO *]" })));
            //testRangeFacetHandlers.Add(new RangeFacetHandler("price", "price", new NumberFieldFactory(), true)); // NightOwl888 - Auto range is no longer a feature.

            for (var i = 0; i < testRangeFacetHandlers.Count; i++)
            {
                var request = new BrowseRequest()
                {
                    Count = 100,
                    Offset = 0,
                    Query = query,
                    Sort = new Lucene.Net.Search.Sort(new SortField("price", SortField.DOUBLE, false)).GetSort()
                };

                var sectionFilter = new BrowseSelection("category");
                sectionFilter.NotValues = new string[] { "5" };
                sectionFilter.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationAnd;
                request.AddSelection(sectionFilter);

                var faceHandler = testRangeFacetHandlers[i];
                var faceHandlers = new IFacetHandler[] { faceHandler, new SimpleFacetHandler("category") };
                var browser = new BoboBrowser(BoboIndexReader.GetInstance(IndexReader.Open(_indexDir, true), faceHandlers));
                var factSpec = new FacetSpec() { OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc };
                request.SetFacetSpec(faceHandler.Name, factSpec);
                var result = browser.Browse(request);
                Console.WriteLine(string.Format("total hits:{0}", result.NumHits));
                foreach (var facet in result.FacetMap[faceHandler.Name].GetFacets())
                {
                    Console.WriteLine(facet.ToString());
                }
                Console.WriteLine("");
            }
        }

        [Test]
        public void TestSimpleBrowser()
        {
            var query = new TermQuery(new Term("name", "asp.net"));
            Console.WriteLine(string.Format("query: <{0}>", query.ToString()));
            var request = new BrowseRequest()
            {
                Count = 10,
                Offset = 0,
                Query = query,
                Sort = new Lucene.Net.Search.Sort(new SortField("price", SortField.DOUBLE, false)).GetSort()
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
    }

    public class NumberFieldFactory : TermListFactory
    {

        public override ITermValueList CreateTermList()
        {
            return new PriceValueList();
        }

        public override ITermValueList CreateTermList(int capacity)
        {
            return new PriceValueList();
        }

        private class PriceValueList : TermStringList
        {
            public override void Add(string o)
            {
                if (o == null)
                {
                    base.Add(o);

                }
                else
                {
                    base.Add(Lucene.Net.Util.NumericUtils.PrefixCodedToDouble(o).ToString());
                }
            }
        }
    }
}


