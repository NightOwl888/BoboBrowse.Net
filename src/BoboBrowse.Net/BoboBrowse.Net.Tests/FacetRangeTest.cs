//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
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

namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Index;
    using BoboBrowse.Net.Index.Digest;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class FacetRangeTest
    {
        private Directory _indexDir;
        private IList<IFacetHandler> _facetHandlers;

        private class TestDataDigester : DataDigester
        {
            private IEnumerable<IFacetHandler> _fconf;
            private Document[] _data;

            public TestDataDigester(IList<IFacetHandler> fConf, Document[] data)
                : base()
            {
                _fconf = fConf;
                _data = data;
            }

            public override void Digest(IDataHandler handler)
            {
                foreach (var dataItem in this._data)
                {
                    handler.HandleDocument(dataItem);
                }
            }
        }

        [SetUp]
        public void Init()
        {
            _indexDir = CreateIndex();
            _facetHandlers = CreateFacetHandlers();
        }

        [TearDown]
        public void Dispose()
        {
            _indexDir = null;
            _facetHandlers = null;
        }

        public static Document[] CreateData()
        {
            var dataList = new List<Document>();

            Document d1 = new Document();
            d1.Add(BuildField("price", "000010.789"));
            d1.Add(BuildField("number", "000000100"));
            
            Document d2 = new Document();
            d2.Add(BuildField("price", "000099.222"));
            d2.Add(BuildField("number", "000000101"));

            Document d3 = new Document();
            d3.Add(BuildField("price", "000050.200"));
            d3.Add(BuildField("number", "000001000"));
            
            Document d4 = new Document();
            d4.Add(BuildField("price", "000007.340"));
            d4.Add(BuildField("number", "000001006"));

            Document d5 = new Document();
            d5.Add(BuildField("price", "002345.100"));
            d5.Add(BuildField("number", "000010000"));

            Document d6 = new Document();
            d6.Add(BuildField("price", "000051.000"));
            d6.Add(BuildField("number", "000100000"));

            Document d7 = new Document();
            d7.Add(BuildField("price", "001000.500"));
            d7.Add(BuildField("number", "000100020"));

            Document d8 = new Document();
            d8.Add(BuildField("price", "000999.220"));
            d8.Add(BuildField("number", "001000000"));
            
            Document d9 = new Document();
            d9.Add(BuildField("price", "000898.334"));
            d9.Add(BuildField("number", "001000003"));

            Document d10 = new Document();
            d10.Add(BuildField("price", "091100.500"));
            d9.Add(BuildField("number", "001000048"));

            dataList.Add(d1);
            dataList.Add(d2);
            dataList.Add(d3);
            dataList.Add(d4);
            dataList.Add(d5);
            dataList.Add(d6);
            dataList.Add(d7);
            dataList.Add(d8);
            dataList.Add(d9);
            dataList.Add(d10);

            return dataList.ToArray();
        }

        private Directory CreateIndex()
        {
            Directory dir = new RAMDirectory();

            Document[] data = CreateData();

            TestDataDigester testDigester = new TestDataDigester(_facetHandlers, data);
            BoboIndexer indexer = new BoboIndexer(testDigester, dir);
            indexer.Index();
            using (var r = DirectoryReader.Open(dir))
            {
            }

            return dir;
        }

        public static Field BuildField(string name, string val)
        {
            return new StringField(name, val, Field.Store.YES);
        }

        public static IList<IFacetHandler> CreateFacetHandlers()
        {
            var facetHandlers = new List<IFacetHandler>();
            facetHandlers.Add(new RangeFacetHandler("pricedouble", "price", new PredefinedTermListFactory<double>("c"), new string[] { "[000000.000 TO 000999.220]", "[000999.230 TO 100000.000]" } ));
            facetHandlers.Add(new RangeFacetHandler("pricefloat", "price", new PredefinedTermListFactory<float>("c"), new string[] { "[000000.000 TO 000999.220]", "[000999.230 TO 100000.000]" }));
            facetHandlers.Add(new RangeFacetHandler("number", new PredefinedTermListFactory<int>(), new string[] { "[0000000000 TO 0000001000]", "[0000001000 TO 0000010000]", "[0000010000 TO 0000100000]", "[0000100000 TO 0001000000]", "[0001000000 TO *]" }));
            return facetHandlers;
        }

        [Test]
        public void TestPriceRangeWithDouble()
        {
            // Field.
            string field = "pricedouble";

            // Lucene index.
            using (DirectoryReader reader = DirectoryReader.Open(this._indexDir))
            {
                // Bobo reader.
                using (BoboMultiReader boboReader = BoboMultiReader.GetInstance(reader, this._facetHandlers))
                {
                    // Request.
                    BrowseRequest browseRequest = new BrowseRequest();
                    browseRequest.Count = 10;
                    browseRequest.Offset = 0;
                    browseRequest.FetchStoredFields = true;

                    // Selection.
                    BrowseSelection sel = new BrowseSelection(field);
                    browseRequest.AddSelection(sel);

                    // Query.
                    MatchAllDocsQuery query = new MatchAllDocsQuery();

                    // Output.
                    FacetSpec spec = new FacetSpec();
                    spec.OrderBy = FacetSpec.FacetSortSpec.OrderValueAsc;
                    spec.MaxCount = 10;
                    browseRequest.SetFacetSpec(field, spec);

                    // Browse.
                    IBrowsable browser = new BoboBrowser(boboReader);
                    using (BrowseResult result = browser.Browse(browseRequest))
                    {
                        // Results.
                        int totalHits = result.NumHits;
                        BrowseHit[] hits = result.Hits;
                        IDictionary<String, IFacetAccessible> facetMap = result.FacetMap;
                        IFacetAccessible facets = facetMap[field];
                        List<BrowseFacet> facetVals = facets.GetFacets().ToList();

                        // Check.
                        Assert.AreEqual(10, totalHits);
                        Assert.AreEqual(2, facetVals.Count);
                        Assert.AreEqual("[000000.000 TO 000999.220](7)", facetVals[0].ToString());
                        Assert.AreEqual("[000999.230 TO 100000.000](3)", facetVals[1].ToString());
                    }
                }
            }
        }

        [Test]
        public void TestPriceRangeWithDoubleAndSelection()
        {
            // Field.
            string field = "pricedouble";

            // Lucene index.
            using (DirectoryReader reader = DirectoryReader.Open(this._indexDir))
            {
                // Bobo reader.
                using (BoboMultiReader boboReader = BoboMultiReader.GetInstance(reader, this._facetHandlers))
                {
                    // Request.
                    BrowseRequest browseRequest = new BrowseRequest();
                    browseRequest.Count = 10;
                    browseRequest.Offset = 0;
                    browseRequest.FetchStoredFields = true;

                    // Selection.
                    BrowseSelection sel = new BrowseSelection(field);
                    sel.AddValue("[000000.000 TO 000999.220]");
                    browseRequest.AddSelection(sel);

                    // Query.
                    MatchAllDocsQuery query = new MatchAllDocsQuery();

                    // Output.
                    FacetSpec spec = new FacetSpec();
                    spec.OrderBy = FacetSpec.FacetSortSpec.OrderValueAsc;
                    spec.MaxCount = 10;
                    browseRequest.SetFacetSpec(field, spec);

                    // Browse.
                    IBrowsable browser = new BoboBrowser(boboReader);
                    using (BrowseResult result = browser.Browse(browseRequest))
                    {
                        // Results.
                        int totalHits = result.NumHits;
                        BrowseHit[] hits = result.Hits;
                        IDictionary<String, IFacetAccessible> facetMap = result.FacetMap;
                        IFacetAccessible facets = facetMap[field];
                        List<BrowseFacet> facetVals = facets.GetFacets().ToList();

                        // Check.
                        Assert.AreEqual(7, totalHits);
                        Assert.AreEqual(1, facetVals.Count);
                        Assert.AreEqual("[000000.000 TO 000999.220](7)", facetVals[0].ToString());
                    }
                }
            }
        }

        [Test]
        public void TestPriceRangeWithFloat()
        {
            // Field.
            string field = "pricefloat";

            // Lucene index.
            using (DirectoryReader reader = DirectoryReader.Open(this._indexDir))
            {
                // Bobo reader.
                using (BoboMultiReader boboReader = BoboMultiReader.GetInstance(reader, this._facetHandlers))
                {
                    // Request.
                    BrowseRequest browseRequest = new BrowseRequest();
                    browseRequest.Count = 10;
                    browseRequest.Offset = 0;
                    browseRequest.FetchStoredFields = true;

                    // Selection.
                    BrowseSelection sel = new BrowseSelection(field);
                    browseRequest.AddSelection(sel);

                    // Query.
                    MatchAllDocsQuery query = new MatchAllDocsQuery();

                    // Output.
                    FacetSpec spec = new FacetSpec();
                    spec.OrderBy = FacetSpec.FacetSortSpec.OrderValueAsc;
                    spec.MaxCount = 10;
                    browseRequest.SetFacetSpec(field, spec);

                    // Browse.
                    IBrowsable browser = new BoboBrowser(boboReader);
                    using (BrowseResult result = browser.Browse(browseRequest))
                    {
                        // Results.
                        int totalHits = result.NumHits;
                        BrowseHit[] hits = result.Hits;
                        IDictionary<String, IFacetAccessible> facetMap = result.FacetMap;
                        IFacetAccessible facets = facetMap[field];
                        List<BrowseFacet> facetVals = facets.GetFacets().ToList();

                        // Check.
                        Assert.AreEqual(10, totalHits);
                        Assert.AreEqual(2, facetVals.Count);
                        Assert.AreEqual("[000000.000 TO 000999.220](7)", facetVals[0].ToString());
                        Assert.AreEqual("[000999.230 TO 100000.000](3)", facetVals[1].ToString());
                    }
                }
            }
        }

        [Test]
        public void TestPriceRangeWithFloatAndSelection()
        {
            // Field.
            string field = "pricefloat";

            // Lucene index.
            using (DirectoryReader reader = DirectoryReader.Open(this._indexDir))
            {
                // Bobo reader.
                using (BoboMultiReader boboReader = BoboMultiReader.GetInstance(reader, this._facetHandlers))
                {
                    // Request.
                    BrowseRequest browseRequest = new BrowseRequest();
                    browseRequest.Count = 10;
                    browseRequest.Offset = 0;
                    browseRequest.FetchStoredFields = true;

                    // Selection.
                    BrowseSelection sel = new BrowseSelection(field);
                    sel.AddValue("[000000.000 TO 000999.220]");
                    browseRequest.AddSelection(sel);

                    // Query.
                    MatchAllDocsQuery query = new MatchAllDocsQuery();

                    // Output.
                    FacetSpec spec = new FacetSpec();
                    spec.OrderBy = FacetSpec.FacetSortSpec.OrderValueAsc;
                    spec.MaxCount = 10;
                    browseRequest.SetFacetSpec(field, spec);

                    // Browse.
                    IBrowsable browser = new BoboBrowser(boboReader);
                    using (BrowseResult result = browser.Browse(browseRequest))
                    {
                        // Results.
                        int totalHits = result.NumHits;
                        BrowseHit[] hits = result.Hits;
                        IDictionary<String, IFacetAccessible> facetMap = result.FacetMap;
                        IFacetAccessible facets = facetMap[field];
                        List<BrowseFacet> facetVals = facets.GetFacets().ToList();

                        // Check.
                        Assert.AreEqual(7, totalHits);
                        Assert.AreEqual(1, facetVals.Count);
                        Assert.AreEqual("[000000.000 TO 000999.220](7)", facetVals[0].ToString());
                    }
                }
            }
        }

        [Test]
        public void TestNumberRangeWithInt()
        {
            // Field.
            string field = "number";

            // Lucene index.
            using (DirectoryReader reader = DirectoryReader.Open(this._indexDir))
            {
                // Bobo reader.
                using (BoboMultiReader boboReader = BoboMultiReader.GetInstance(reader, this._facetHandlers))
                {
                    // Request.
                    BrowseRequest browseRequest = new BrowseRequest();
                    browseRequest.Count = 10;
                    browseRequest.Offset = 0;
                    browseRequest.FetchStoredFields = true;

                    // Selection.
                    BrowseSelection sel = new BrowseSelection(field);
                    browseRequest.AddSelection(sel);

                    // Query.
                    MatchAllDocsQuery query = new MatchAllDocsQuery();

                    // Output.
                    FacetSpec spec = new FacetSpec();
                    spec.OrderBy = FacetSpec.FacetSortSpec.OrderValueAsc;
                    spec.MaxCount = 10;
                    browseRequest.SetFacetSpec(field, spec);

                    // Browse.
                    IBrowsable browser = new BoboBrowser(boboReader);
                    using (BrowseResult result = browser.Browse(browseRequest))
                    {
                        // Results.
                        int totalHits = result.NumHits;
                        BrowseHit[] hits = result.Hits;
                        IDictionary<String, IFacetAccessible> facetMap = result.FacetMap;
                        IFacetAccessible facets = facetMap[field];
                        List<BrowseFacet> facetVals = facets.GetFacets().ToList();

                        // Check.
                        Assert.AreEqual(10, totalHits);
                        Assert.AreEqual(5, facetVals.Count);
                        Assert.AreEqual("[0000000000 TO 0000001000](3)", facetVals[0].ToString());
                        Assert.AreEqual("[0000001000 TO 0000010000](3)", facetVals[1].ToString());
                        Assert.AreEqual("[0000010000 TO 0000100000](2)", facetVals[2].ToString());
                        Assert.AreEqual("[0000100000 TO 0001000000](3)", facetVals[3].ToString());
                        Assert.AreEqual("[0001000000 TO *](3)", facetVals[4].ToString());
                    }
                }
            }
        }
    }
}
