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
        private IEnumerable<IFacetHandler> _facetHandlers;

        private class TestDataDigester : DataDigester
        {
            private IEnumerable<IFacetHandler> _fconf;
            private Document[] _data;

            public TestDataDigester(IEnumerable<IFacetHandler> fConf, Document[] data)
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
            
            Document d2 = new Document();
            d2.Add(BuildField("price", "000099.222"));

            Document d3 = new Document();
            d3.Add(BuildField("price", "000050.200"));

            Document d4 = new Document();
            d4.Add(BuildField("price", "000007.340"));

            Document d5 = new Document();
            d5.Add(BuildField("price", "002345.100"));

            Document d6 = new Document();
            d6.Add(BuildField("price", "000051.000"));

            Document d7 = new Document();
            d7.Add(BuildField("price", "001000.500"));

            Document d8 = new Document();
            d8.Add(BuildField("price", "000999.220"));

            Document d9 = new Document();
            d9.Add(BuildField("price", "000898.334"));

            Document d10 = new Document();
            d10.Add(BuildField("price", "091100.500"));

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
            using (var r = IndexReader.Open(dir, false))
            {
            }

            return dir;
        }

        public static Field BuildField(string name, string val)
        {
            Field f = new Field(name, val, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS);
            f.OmitTermFreqAndPositions = true;
            return f;
        }

        public static IEnumerable<IFacetHandler> CreateFacetHandlers()
        {
            var facetHandlers = new List<IFacetHandler>();
            facetHandlers.Add(new RangeFacetHandler("pricedouble", "price", new PredefinedTermListFactory<double>("c"), new string[] { "[000000.000 TO 000999.220]", "[000999.230 TO 100000.000]" } ));
            facetHandlers.Add(new RangeFacetHandler("pricefloat", "price", new PredefinedTermListFactory<float>("c"), new string[] { "[000000.000 TO 000999.220]", "[000999.230 TO 100000.000]" }));
            return facetHandlers;
        }

        [Test]
        public void TestPriceRangeWithDouble()
        {
            // Field.
            string field = "pricedouble";

            // Lucene index.
            using (IndexReader reader = IndexReader.Open(this._indexDir, true))
            {
                // Bobo reader.
                using (BoboIndexReader boboReader = BoboIndexReader.GetInstance(reader, this._facetHandlers))
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
                        Assert.AreEqual(2, facetVals.Count());
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
            using (IndexReader reader = IndexReader.Open(this._indexDir, true))
            {
                // Bobo reader.
                using (BoboIndexReader boboReader = BoboIndexReader.GetInstance(reader, this._facetHandlers))
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
                        Assert.AreEqual(1, facetVals.Count());
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
            using (IndexReader reader = IndexReader.Open(this._indexDir, true))
            {
                // Bobo reader.
                using (BoboIndexReader boboReader = BoboIndexReader.GetInstance(reader, this._facetHandlers))
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
                        Assert.AreEqual(2, facetVals.Count());
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
            using (IndexReader reader = IndexReader.Open(this._indexDir, true))
            {
                // Bobo reader.
                using (BoboIndexReader boboReader = BoboIndexReader.GetInstance(reader, this._facetHandlers))
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
                        Assert.AreEqual(1, facetVals.Count());
                        Assert.AreEqual("[000000.000 TO 000999.220](7)", facetVals[0].ToString());
                    }
                }
            }
        }
    }
}
