//// -----------------------------------------------------------------------
//// <sourcefile name="BrowseTests.cs" Date="2014/02/28">
//// <copyright company="YAMOOL!" url="http://www.yamool.com">
////      Copyright (c) yamool.com. All rights reserved.
//// </copyright>
//// <author name="zhengchun" email="zc@yamool.com"/>
//// -----------------------------------------------------------------------

namespace BoboBrowse.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using NUnit.Framework;
    using Lucene.Net.Store;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.QueryParsers;
    using Lucene.Net.Analysis;
    using Lucene.Net.Documents;

    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net;
    using BoboBrowse.Net.Search;
   
    public class BrowseInstanceTest
    {
        private static BoboBrowser _instance = null;             
        static BrowseInstanceTest()
        {

            _instance = CreateNewInstance();
            var createdTime = DateTime.Now;
            new System.Threading.Thread(() =>
            {
                while (true)
                {
                    if ((DateTime.Now - createdTime).TotalHours >= 1)
                    {
                        _instance=CreateNewInstance();
                        createdTime = DateTime.Now;
                    }
                    System.Threading.Thread.Sleep(1);
                }               
            }).Start();
        }
        
        public BrowseResult Request(BrowseRequest request)
        {
            return _instance.Browse(request);
        }

        private static BoboBrowser CreateNewInstance()
        {
            var boboReader = BoboIndexReader.GetInstance(IndexReader.Open(FSDirectory.Open(new System.IO.DirectoryInfo("")), true), new FacetHandler[] { });
            return new BoboBrowser(boboReader);
        }
    }

    [TestFixture]
    public class BrowseTests
    {
        private string IndexPath = "";

        [TestFixtureSetUp]
        public void Init()
        {
            IndexPath = @"..\..\..\BoboBrowse.Tests\Index";
        }

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
    }
}
