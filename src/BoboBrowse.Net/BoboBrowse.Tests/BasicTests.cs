//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
//*
//* Copyright (C) 2005-2015  John Wang
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//*   http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Index.Digest;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using Lucene.Net.Util;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class BasicTests
    {
        private Directory _indexDir;
        private IEnumerable<IFacetHandler> _facetConfig;
        //private IDictionary<string, int> _categories;

        //private class TestDataDigester : DataDigester
        //{
        //    private readonly Document[] _data;

        //    public TestDataDigester(IEnumerable<IFacetHandler> facetHandlers, Document[] data)
        //    {
        //        _data = data;
        //    }

        //    public override void Digest(DataDigester.IDataHandler handler)
        //    {
        //        for (int i = 0; i < _data.Length; ++i)
        //        {
        //            handler.HandleDocument(_data[i]);
        //        }
        //    }
        //}

        private Directory CreateIndex()
        {
            RAMDirectory idxDir = new RAMDirectory();

            //build a test index file.            
            var dataSet = new[] {
                new {Name="Head First Java, 2nd Edition",Year=2000,Category="JAVA",CategoryID=1,Price=28.95,Author="Kathy Sierra",Path="Book/Programming/Language"},
                new {Name="Head First Java: Your Brain on Java - A Learner's Guide",Year=2010,Category="JAVA",CategoryID=1,Price=47.42,Author="Bert Bates,Kathy Sierra",Path="Book/Programming/Language"},
                new {Name="Java: A Beginner's Guide",Year=2010,Category="JAVA",CategoryID=1,Price=25.94,Author="Herbert Schildt",Path="Book/Programming/Language"},
                new {Name="Head First Design Patterns",Year=2014,Category="JAVA",CategoryID=1,Price=37.15,Author="Eric Freeman",Path="Book/Programming/Language"},
                new {Name="Effective Java (2nd Edition)",Year=2011,Category="JAVA",CategoryID=1,Price=38.82,Author="Joshua Bloch",Path="Book/Programming/Language"},
                new {Name="Hadoop: The Definitive Guide",Year=2010,Category="JAVA",CategoryID=1,Price=28.99,Author="Tom White",Path="Book/Programming/Language"},
                new {Name="Secrets of the JavaScript Ninja",Year=2014,Category="JAVA",CategoryID=1,Price=24.46,Author="Bear Bibeault",Path="Book/Programming/Language"},
                new {Name="Spring in Action",Year=2008,Category="JAVA",CategoryID=1,Price=40.74,Author="Craig Walls",Path="Book/Programming/Language"},

                new {Name="C# 5.0 in a Nutshell: The Definitive Reference",Year=2010,Category="C#",CategoryID=2,Price=34.07,Author="Joseph Albahari,Ben Albahari",Path="Book/Programming/Language"},
                new {Name="C# in Depth, 3rd Edition",Year=2012,Category="C#",CategoryID=2,Price=32.76,Author="Jon Skeet",Path="Book/Programming/Language"},
                new {Name="C# 5.0 Pocket Reference: Instant Help for C# 5.0 Programmers",Year=2012,Category="C#",CategoryID=2,Price=11.43,Author="Joseph Albahari,Ben Albahari",Path="Book/Programming/Language"},
                new {Name="LINQ Pocket Reference",Year=2013,Category="C#",CategoryID=2,Price=14.99,Author="Joseph Albahari,Ben Albahari",Path="Book/Programming/Language"},
                new {Name="Pro ASP.NET MVC 5",Year=2014,Category="C#",CategoryID=2,Price=27.49,Author="Adam Freeman",Path="Book/Programming/Language"},
                new {Name="ASP.NET Web API 2: Building a REST Service from Start to Finish",Year=2008,Category="C#",CategoryID=2,Price=23.45,Author="Jamie Kurtz , Brian Wortman",Path="Book/Programming/Language"},
                new {Name="Designing Evolvable Web APIs with ASP.NET",Year=2013,Category="C#",CategoryID=2,Price=33.34,Author="Pablo Cibraro",Path="Book/Programming/Language"},

                new {Name="Professional ASP.NET 3.5 AJAX",Year=1998,Category="AJAX",CategoryID=3,Price=33.29,Author="Bill Evjen",Path="Book/Programming/Web"},
                new {Name="Head First Ajax",Year=2007,Category="AJAX",CategoryID=3,Price=31.57,Author="Rebecca Riordan",Path="Book/Programming/Web"},
                new {Name="JavaScript and AJAX For Dummies",Year=2005,Category="AJAX",CategoryID=3,Price=20.18,Author="Richard Wagner",Path="Book/Programming/Web"},
                new {Name="AJAX and PHP: Building Responsive Web",Year=2004,Category="AJAX",CategoryID=3,Price=28.5,Author="Cristian Darie",Path="Book/Programming/Web"},
                new {Name="Web 2.0 Fundamentals: With AJAX, Development Tools, And Mobile Platforms",Year=2007,Category="AJAX",CategoryID=3,Price=28.54,Author="Oswald Campesato",Path="Book/Programming/Web"},

                new {Name="Learning Python, 5th Edition",Year=1998,Category="PYTHON",CategoryID=4,Price=41.35,Author="Mark Lutz",Path="Book/Programming/Language"},
                new {Name="Mining the Social Web: Data Mining Facebook, Twitter, LinkedIn, Google+, GitHub, and More",Year=2000,Category="PYTHON",CategoryID=4,Price=26.41,Author="Matthew A. Russell",Path="Book/Programming/Language"},
                new {Name="Python Programming for the Absolute Beginner, 3rd Edition",Year=2005,Category="PYTHON",CategoryID=4,Price=22.70,Author="Michael Dawson",Path="Book/Programming/Language"},
                new {Name="Python Cookbook",Year=2010,Category="PYTHON",CategoryID=4,Price=30.37,Author="David M. Beazley",Path="Book/Programming/Language"},

                new {Name="Web Design with HTML, CSS, JavaScript and jQuery Set",Year=2014,Category="WEB",CategoryID=5,Price=38.69,Author="Mark Myers",Path="Book/Programming/Design"},
                new {Name="Pro AngularJS (Expert's Voice in Web Development)",Year=2008,Category="WEB",CategoryID=5,Price=36.27,Author="Adam Freeman",Path="Book/Programming/Design"},
                new {Name="HTML and CSS: Design and Build Websites",Year=2011,Category="WEB",CategoryID=5,Price=17.39,Author="Jon Duckett",Path="Book/Programming/Design"},
                new {Name="Web Design All-in-One For Dummies",Year=2014,Category="WEB",CategoryID=5,Price=27.2,Author="Sue Jenkins",Path="Book/Programming/Design"},
                new {Name="The Principles of Beautiful Web Design",Year=2014,Category="WEB",CategoryID=5,Price=27.28,Author="Jason Beaird",Path="Book/Programming/Design"},
                new {Name="Bootstrap for ASP.NET MVC",Year=2014,Category="WEB",CategoryID=5,Price=11.49,Author="Pieter van der Westhuizen",Path="Book/Programming/Design"},
                new {Name="Mobile ASP.NET MVC 5",Year=2012,Category="WEB",CategoryID=5,Price=19.79,Author="Eric Sowell",Path="Book/Programming/Design"},

                new {Name="JavaScript and JQuery: Interactive Front-End Web Development",Year=2013,Category="JAVASCRIPT",CategoryID=6,Price=26.93,Author="Jon Duckett",Path="Book/Programming/Web"},
                new {Name="JavaScript: The Definitive Guide: Activate Your Web Pages",Year=2013,Category="JAVASCRIPT",CategoryID=6,Price=30.69,Author="David Flanagan",Path="Book/Programming/Web"},
                new {Name="JavaScript and HTML5 Now",Year=2013,Category="JAVASCRIPT",CategoryID=6,Price=12.1,Author=" Kyle Simpson",Path="Book/Programming/Web"},
                new {Name="Head First JavaScript Programming",Year=2012,Category="JAVASCRIPT",CategoryID=6,Price=31.92,Author=" Eric T. Freeman , Elisabeth Robson",Path="Book/Programming/Web"},
            };

            var conf = new IndexWriterConfig(LuceneVersion.LUCENE_48, new StandardAnalyzer(LuceneVersion.LUCENE_48));
            using (var indexWriter = new IndexWriter(idxDir, conf))
            {
                foreach (var book in dataSet)
                {
                    var doc = new Document();
                    doc.Add(new StringField("name", book.Name, Field.Store.YES));
                    doc.Add(new StringField("category", book.Category, Field.Store.YES));
                    doc.Add(new IntField("categoryId", book.CategoryID, Field.Store.YES));
                    doc.Add(new DoubleField("price", book.Price, Field.Store.YES));

                    // Multi-valued field
                    var authors = book.Author.Split(',').Select(a => a.Trim()).ToList();
                    foreach (var author in authors)
                    {
                        doc.Add(new StringField("author", author, Field.Store.YES));
                    }

                    doc.Add(new StringField("path", book.Path, Field.Store.YES));
                    doc.Add(new StringField("year", book.Year.ToString(), Field.Store.YES));
                    indexWriter.AddDocument(doc);
                }
            }

            //using (DirectoryReader r = DirectoryReader.Open(idxDir))
            //{
            //}

            return idxDir;
        }

        private IEnumerable<IFacetHandler> GetFacetConfiguration()
        {
            return new IFacetHandler[] { 
                new SimpleFacetHandler("category"), 
                new MultiValueFacetHandler("author"),
                new RangeFacetHandler("year", new string[] { "[* TO 2000]", "[2000 TO 2005]", "[2006 TO 2010]", "[2011 TO *]" })
            };
        }

        [TestFixtureSetUp]
        public void Init()
        {
            //_categories = new Dictionary<string, int>() { { "JAVA", 1 }, { "C#", 2 }, { "AJAX", 3 }, { "PYTHON", 4 }, { "WEB", 5 }, { "JAVASCRIPT", 6 } };
            _indexDir = CreateIndex();
            _facetConfig = GetFacetConfiguration();
        }

        //[Test]
        //public void TestAutoComplete()
        //{
        //    // NOTE: Typically facet handlers are setup once per application start, not at runtime.
        //    // If you want to dynamically add them at runtime, you should use BoboBroser.SetFacetHandler().
        //    var facetHandlers = new IFacetHandler[] { new SimpleFacetHandler("category") };

        //    // Runtime setup
        //    var query = new MatchAllDocsQuery();
        //    Console.WriteLine(string.Format("query: <{0}>", query.ToString()));
        //    var request = new BrowseRequest()
        //    {
        //        Count = 10,
        //        Offset = 0,
        //        Query = query
        //    };
        //    var factSpec = new FacetSpec() { OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc };
        //    request.SetFacetSpec("category", factSpec);

        //    //var prefix = "java"; // NightOwl888: Prefix is no longer a feature
        //    //Console.WriteLine(string.Format("prefix:{0}", prefix));
        //    //Console.WriteLine("=============================");

        //    using (var srcReader = DirectoryReader.Open(_indexDir))
        //    {
        //        using (var indexReader = BoboMultiReader.GetInstance(srcReader, facetHandlers))
        //        {
        //            using (var browser = new BoboBrowser(indexReader))
        //            {
        //                using (var result = browser.Browse(request))
        //                {
        //                    var facets = result.FacetMap["category"].GetFacets();

        //                    foreach (var facet1 in facets)
        //                    {
        //                        Console.WriteLine(facet1.ToString());
                                
        //                    }

        //                    //BrowseFacet facet;

        //                    //facet = facets.ElementAt(0);
        //                    //Assert.AreEqual(1, facet.FacetValueHitCount);
        //                    //Assert.AreEqual("AJAX and PHP: Building Responsive Web", facet.Value);

        //                    //facet = facets.ElementAt(1);
        //                    //Assert.AreEqual(1, facet.FacetValueHitCount);
        //                    //Assert.AreEqual("ASP.NET Web API 2: Building a REST Service from Start to Finish", facet.Value);

        //                    //facet = facets.ElementAt(2);
        //                    //Assert.AreEqual(1, facet.FacetValueHitCount);
        //                    //Assert.AreEqual("Bootstrap for ASP.NET MVC", facet.Value);

        //                    //facet = facets.ElementAt(3);
        //                    //Assert.AreEqual(1, facet.FacetValueHitCount);
        //                    //Assert.AreEqual("AJAX and PHP: Building Responsive Web", facet.Value);

        //                    //facet = facets.ElementAt(4);
        //                    //Assert.AreEqual(1, facet.FacetValueHitCount);
        //                    //Assert.AreEqual("AJAX and PHP: Building Responsive Web", facet.Value);

        //                    //facet = facets.ElementAt(5);
        //                    //Assert.AreEqual(1, facet.FacetValueHitCount);
        //                    //Assert.AreEqual("AJAX and PHP: Building Responsive Web", facet.Value);

        //                    //facet = facets.ElementAt(6);
        //                    //Assert.AreEqual(1, facet.FacetValueHitCount);
        //                    //Assert.AreEqual("AJAX and PHP: Building Responsive Web", facet.Value);

        //                    //facet = facets.ElementAt(7);
        //                    //Assert.AreEqual(1, facet.FacetValueHitCount);
        //                    //Assert.AreEqual("AJAX and PHP: Building Responsive Web", facet.Value);
        //                }
        //            }
        //        }
        //    }
        //}

        //[Test]
        //public void TestSimpleFacetHandler()
        //{
        //    // NOTE: Typically facet handlers are setup once per application start, not at runtime.
        //    // If you want to dynamically add them at runtime, you should use BoboBroser.SetFacetHandler().
        //    var facetHandlers = new IFacetHandler[] { new SimpleFacetHandler("category") };

        //    // Runtime setup
        //    var query = new TermQuery(new Term("name", "asp.net"));
        //    Console.WriteLine(string.Format("query: <{0}>", query.ToString()));
        //    var request = new BrowseRequest()
        //    {
        //        Count = 10,
        //        Offset = 0,
        //        Query = query,
        //        Sort = new Lucene.Net.Search.Sort(new SortField("year", SortField.Type_e.INT, false)).GetSort()
        //    };
        //    var factSpec = new FacetSpec() { OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc, MinHitCount = 1 };
        //    request.SetFacetSpec("category", factSpec);

            

        //    using (var srcReader = DirectoryReader.Open(_indexDir))
        //    {
        //        using (var indexReader = BoboMultiReader.GetInstance(srcReader, facetHandlers))
        //        {
        //            using (var browser = new BoboBrowser(indexReader))
        //            {
        //                using (var result = browser.Browse(request))
        //                {
        //                    Console.WriteLine(string.Format("total hits:{0}", result.NumHits));
        //                    Console.WriteLine("===========================");
        //                    foreach (var facet in result.FacetMap["category"].GetFacets())
        //                    {
        //                        var category = _categories.First(k => k.Value == int.Parse(facet.Value.ToString()));
        //                        Console.WriteLine("{0}:({1})", category.Key, facet.FacetValueHitCount);
        //                    }
        //                    Console.WriteLine("===========================");
        //                    for (var i = 0; i < result.Hits.Length; i++)
        //                    {
        //                        var doc = browser.Document(result.Hits[i].DocId);
        //                        var category = _categories.First(k => k.Value == int.Parse(doc.GetField("category").StringValue)).Key;
        //                        Console.WriteLine(string.Format("{2} - {0}({4}) ${1} by {3}", doc.GetField("name").StringValue, doc.GetField("price").StringValue, category, doc.GetField("author").StringValue, doc.GetField("year").StringValue));
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //[Test]
        //public void TestFacetSelectionFilter()
        //{
        //    // NOTE: Typically facet handlers are setup once per application start, not at runtime.
        //    // If you want to dynamically add them at runtime, you should use BoboBroser.SetFacetHandler().
        //    var facetHandlers = new IFacetHandler[] { new MultiValueFacetHandler("author") };

        //    // Runtime setup
        //    var query = new MatchAllDocsQuery();
        //    Console.WriteLine(string.Format("query: <{0}>", query.ToString()));
        //    var request = new BrowseRequest()
        //    {
        //        Count = 10,
        //        Offset = 0,
        //        Query = query
        //    };
        //    var authors = new string[] { "kathy", "sierra" };//kathy&sierra
        //    var sectionFilter = new BrowseSelection("author");
        //    sectionFilter.Values = authors;
        //    sectionFilter.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationAnd;
        //    request.AddSelection(sectionFilter);

        //    using (var srcReader = DirectoryReader.Open(_indexDir))
        //    {
        //        using (var indexReader = BoboMultiReader.GetInstance(srcReader, facetHandlers))
        //        {
        //            using (var browser = new BoboBrowser(indexReader))
        //            {
        //                using (var result = browser.Browse(request))
        //                {
        //                    Console.WriteLine("===========================");
        //                    for (var i = 0; i < result.Hits.Length; i++)
        //                    {
        //                        var doc = browser.Document(result.Hits[i].DocId);
        //                        var category = _categories.First(k => k.Value == int.Parse(doc.GetField("category").StringValue)).Key;
        //                        Console.WriteLine(string.Format("{2} - {0} ${1} by {3}", doc.GetField("name").StringValue, doc.GetField("price").StringValue, category, doc.GetField("author").StringValue));
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //[Test]
        //public void TestMultiValueFacetHandler()
        //{
        //    // NOTE: Typically facet handlers are setup once per application start, not at runtime.
        //    // If you want to dynamically add them at runtime, you should use BoboBroser.SetFacetHandler().
        //    var facetHandlers = new IFacetHandler[] { new MultiValueFacetHandler("path") };

        //    // Runtime setup
        //    var query = new MatchAllDocsQuery();
        //    Console.WriteLine(string.Format("query: <{0}>", query.ToString()));
        //    var request = new BrowseRequest()
        //    {
        //        Count = 100,
        //        Offset = 0,
        //        Query = query,
        //        Sort = new Lucene.Net.Search.Sort(new SortField("path", SortField.Type_e.STRING, false)).GetSort()
        //    };
        //    var factSpec = new FacetSpec() { OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc };
        //    request.SetFacetSpec("path", factSpec);

        //    using (var srcReader = DirectoryReader.Open(_indexDir))
        //    {
        //        using (var indexReader = BoboMultiReader.GetInstance(srcReader, facetHandlers))
        //        {
        //            using (var browser = new BoboBrowser(indexReader))
        //            {
        //                using (var result = browser.Browse(request))
        //                {
        //                    Console.WriteLine(string.Format("total hits:{0}", result.NumHits));
        //                    Console.WriteLine("===========================");
        //                    foreach (var facet in result.FacetMap["path"].GetFacets())
        //                    {
        //                        Console.WriteLine(facet.ToString());
        //                    }
        //                    Console.WriteLine("===========================");
        //                    for (var i = 0; i < result.Hits.Length; i++)
        //                    {
        //                        var doc = browser.Document(result.Hits[i].DocId);
        //                        var category = _categories.First(k => k.Value == int.Parse(doc.GetField("category").StringValue)).Key;
        //                        Console.WriteLine(string.Format("{2} - {0} ${1} by {3}", doc.GetField("name").StringValue, doc.GetField("price").StringValue, category, doc.GetField("author").StringValue));
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //[Test]
        //public void TestRangeFacetHandler()
        //{
        //    // NOTE: Typically facet handlers are setup once per application start, not at runtime.
        //    // If you want to dynamically add them at runtime, you should use BoboBroser.SetFacetHandler().
        //    var facetHandlers = new IFacetHandler[] { 
        //        new RangeFacetHandler("year", new string[] { "[* TO 2000]", "[2000 TO 2005]", "[2006 TO 2010]", "[2011 TO *]" }), 
        //        new SimpleFacetHandler("category") 
        //    };


        //    // Runtime setup
        //    var query = new MatchAllDocsQuery();
        //    Console.WriteLine(string.Format("query: <{0}>", query.ToString()));

        //    var request = new BrowseRequest()
        //    {
        //        Count = 100,
        //        Offset = 0,
        //        Query = query,
        //        Sort = new Lucene.Net.Search.Sort(new SortField("price", SortField.Type_e.DOUBLE, false)).GetSort()
        //    };

        //    var sectionFilter = new BrowseSelection("category");
        //    sectionFilter.NotValues = new string[] { "5" };
        //    sectionFilter.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationAnd;
        //    request.AddSelection(sectionFilter);

        //    var factSpec = new FacetSpec() { OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc };
        //    request.SetFacetSpec("year", factSpec);


        //    using (var srcReader = DirectoryReader.Open(_indexDir))
        //    {
        //        using (var indexReader = BoboMultiReader.GetInstance(srcReader, facetHandlers))
        //        {
        //            using (var browser = new BoboBrowser(indexReader))
        //            {
        //                using (var result = browser.Browse(request))
        //                {
        //                    Console.WriteLine(string.Format("total hits:{0}", result.NumHits));
        //                    foreach (var facetName in result.FacetMap.Keys)
        //                    {
        //                        foreach (var facet in result.FacetMap[facetName].GetFacets())
        //                        {
        //                            Console.WriteLine(facet.ToString());
        //                        }
        //                        Console.WriteLine("");
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //[Test]
        //public void TestSimpleBrowserOld()
        //{
        //    // NOTE: Typically facet handlers are setup once per application start, not at runtime.
        //    // If you want to dynamically add them at runtime, you should use BoboBroser.SetFacetHandler().
        //    var facetHandlers = new IFacetHandler[] { new SimpleFacetHandler("category") };


        //    // Runtime setup
        //    //var query = new TermQuery(new Term("name", "asp.net"));
        //    var query = new TermQuery(new Term("name", "AJAX"));
        //    Console.WriteLine(string.Format("query: <{0}>", query.ToString()));
        //    var request = new BrowseRequest()
        //    {
        //        Count = 10,
        //        Offset = 0,
        //        //Query = query,
        //        Sort = new Lucene.Net.Search.Sort(new SortField("price", SortField.Type_e.DOUBLE, false)).GetSort()
        //    };

        //    var facetSpec = new FacetSpec() { OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc, MinHitCount = 1 };
        //    request.SetFacetSpec("category", facetSpec);


        //    using (var srcReader = DirectoryReader.Open(_indexDir))
        //    {
        //        using (var indexReader = BoboMultiReader.GetInstance(srcReader, facetHandlers))
        //        {
        //            using (var browser = new BoboBrowser(indexReader))
        //            {
        //                using (var result = browser.Browse(request))
        //                {
        //                    Console.WriteLine(string.Format("total hits:{0}", result.NumHits));
        //                    Console.WriteLine("===========================");
        //                    foreach (var facet in result.FacetMap["category"].GetFacets())
        //                    {
        //                        var category = _categories.First(k => k.Value == int.Parse(facet.Value.ToString()));
        //                        Console.WriteLine("{0}:({1})", category.Key, facet.FacetValueHitCount);
        //                    }
        //                    Console.WriteLine("===========================");
        //                    for (var i = 0; i < result.Hits.Length; i++)
        //                    {
        //                        var doc = browser.Document(result.Hits[i].DocId);
        //                        var category = _categories.First(k => k.Value == int.Parse(doc.GetField("category").StringValue)).Key;
        //                        Console.WriteLine(string.Format("{2} - {0} ${1} by {3}", 
        //                            doc.GetField("name").StringValue, 
        //                            doc.GetField("price").StringValue, 
        //                            category, 
        //                            doc.GetField("author").StringValue));
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}


        [Test]
        public void TestSimpleBrowser()
        {
            // The Lucene.Net index is generally created out of band from the browse operations.
            // The idea is that we index once and then use the index for dozens/hundreds/thousands
            // of searches.
            var indexDir = CreateIndex();


            // Typically facet handlers are setup once per application start, not at runtime.
            // You would create all of the facet handlers you need over several use cases and use
            // FacetSpec to utilize only the ones you need at any given time.
            // If you want to dynamically add them at runtime, you should use BoboBroser.SetFacetHandler().
            var facetHandlers = GetFacetConfiguration();


            // Runtime setup
            var request = new BrowseRequest()
            {
                // 10 rows per page
                Count = 10,
                // 0 based page index
                Offset = 0,
                // Fetch the stored values from the index so they are available in the output
                FetchStoredFields = true,
                // Sort from lowest to highest price
                Sort = new Lucene.Net.Search.Sort(new SortField("price", SortField.Type_e.DOUBLE, false)).GetSort()
            };

            // Create the facets for the current request
            var categorySpec = new FacetSpec() { OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc, MinHitCount = 1 };
            request.SetFacetSpec("category", categorySpec);

            var authorSpec = new FacetSpec();
            request.SetFacetSpec("author", authorSpec);

            var yearSpec = new FacetSpec() { OrderBy = FacetSpec.FacetSortSpec.OrderValueAsc };
            request.SetFacetSpec("year", yearSpec);
            

            // Add user selections (this part would normally change for each request).
            var categorySelections = new BrowseSelection("category");
            categorySelections.Values = new string[] { "C#", "JAVA", "JAVASCRIPT" };
            request.AddSelection(categorySelections);

            //var authorSelections = new BrowseSelection("author");
            //authorSelections.Values = new string[] { "Joseph Albahari", "Adam Freeman", "Jon Skeet", "Eric T. Freeman" };
            //request.AddSelection(authorSelections);

            var yearSelections = new BrowseSelection("year");
            yearSelections.Values = new string[] { "[2006 TO 2010]" };
            request.AddSelection(yearSelections);

            // Perform the browse
            using (var directoryReader = DirectoryReader.Open(_indexDir))
            {
                using (var multiReader = BoboMultiReader.GetInstance(directoryReader, facetHandlers))
                {
                    using (var browser = new BoboBrowser(multiReader))
                    {
                        using (var result = browser.Browse(request))
                        {
                            // Output the results

                            Console.WriteLine(string.Format("total hits:{0}", result.NumHits));
                            Console.WriteLine("===========================");
                            foreach (var facet in result.FacetMap["category"].GetFacets())
                            {
                                Console.WriteLine(facet.ToString());
                            }
                            Console.WriteLine("===========================");
                            foreach (var facet in result.FacetMap["author"].GetFacets())
                            {
                                Console.WriteLine(facet.ToString());
                            }
                            Console.WriteLine("===========================");
                            foreach (var facet in result.FacetMap["year"].GetFacets())
                            {
                                Console.WriteLine(facet.ToString());
                            }
                            Console.WriteLine("===========================");
                            foreach (var hit in result.Hits)
                            {
                                Console.WriteLine(string.Format("{2} - {0} ${1} by {3} ({4})",
                                    hit.GetFieldStringValue("name"),
                                    hit.GetFieldStringValue("price"),
                                    hit.GetFieldStringValue("category"),
                                    hit.GetFieldStringValue("author"),
                                    hit.GetFieldStringValue("year")));
                            }
                        }
                    }
                }
            }
        }

    }
}