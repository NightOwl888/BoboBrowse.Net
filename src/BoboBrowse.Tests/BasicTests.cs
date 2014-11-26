namespace BoboBrowse.Tests
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Search;
    using Lucene.Net.Analysis;
    using Lucene.Net.Index;
    using Lucene.Net.QueryParsers;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    [TestFixture]
    public class BasicTests
    {
        private const string fieldName = "File Type";
        private Directory _indexDir;

        [TestFixtureSetUp]
        public void Init()
        {
            var indexPath = @"..\..\..\BoboBrowse.Tests\Index";
            _indexDir = FSDirectory.Open(indexPath);
        }

        [Test]
        public void TestPathFacet()
        {
            string fieldName = "Food and Beverage";

            FacetHandler facetHandler = new SimpleFacetHandler(fieldName);

            ICollection<FacetHandler> handlerList = new FacetHandler[] { facetHandler };

            // opening a lucene index

            IndexReader reader = IndexReader.Open(_indexDir, true);

            // decorate it with a bobo index reader
            BoboIndexReader boboReader = BoboIndexReader.GetInstance(reader, handlerList);

            // creating a browse request
            BrowseRequest browseRequest = new BrowseRequest();
            browseRequest.Count = 10;
            browseRequest.Offset = 0;
            browseRequest.FetchStoredFields = false;

            browseRequest.Query = new MatchAllDocsQuery();

            // add the facet output specs
            FacetSpec spec = new FacetSpec();
            spec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;

            browseRequest.SetFacetSpec(fieldName, spec);

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
        }

        [Test]
        public void AutoCompleteTest()
        {
            FacetHandler handler = new MultiValueFacetHandler("Body");

            IndexReader reader = IndexReader.Open(_indexDir, true);

            // decorate it with a bobo index reader
            BoboIndexReader boboReader = BoboIndexReader.GetInstance(reader, new FacetHandler[] { handler });

            BrowseRequest browseRequest = new BrowseRequest();
            browseRequest.Count = 10;
            browseRequest.Offset = 0;
            browseRequest.FetchStoredFields = true;

            // add a selection
            BrowseSelection sel = new BrowseSelection("Body");
            //sel.AddValue("alexey");
            browseRequest.AddSelection(sel);

            // parse a query
            QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "Entity", new KeywordAnalyzer());
            Query q = parser.Parse("SPListItem");
            browseRequest.Query = q;

            // add the facet output specs
            FacetSpec colorSpec = new FacetSpec();
            colorSpec.Prefix = "al";
            colorSpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;

            browseRequest.SetFacetSpec("Body", colorSpec);

            // perform browse
            IBrowsable browser = new BoboBrowser(boboReader);

            BrowseResult result = browser.Browse(browseRequest);

            // Showing results now
            Dictionary<String, IFacetAccessible> facetMap = result.FacetMap;

            IFacetAccessible colorFacets = facetMap["Body"];

            IEnumerable<BrowseFacet> facetVals = colorFacets.GetFacets();

            Debug.WriteLine("Facets:");

            int count = 0;
            foreach (BrowseFacet facet in facetVals)
            {
                count++;
                Debug.WriteLine(facet.ToString());
            }
            Debug.WriteLine("Total = " + count);

        }

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
    }
}

