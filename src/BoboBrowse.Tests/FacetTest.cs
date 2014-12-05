namespace BoboBrowse.Tests
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Search;
    using Lucene.Net.Analysis;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Index;
    using Lucene.Net.QueryParsers;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using Lucene.Net.Documents;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    [TestFixture]
    public class FacetTest
    {
        [Test]
        public void TestDoBrowse()
        {
            System.IO.DirectoryInfo idx = new System.IO.DirectoryInfo("/Users/jwang/dataset/people-search-index-norm/beef");

            Directory idxDir = FSDirectory.Open(idx);
            IndexReader reader = IndexReader.Open(idxDir, true);

            BoboIndexReader boboReader = BoboIndexReader.GetInstance(reader);
            BoboBrowser browser = new BoboBrowser(boboReader);
            int iter = 1000000;
            for (int i = 0; i < iter; ++i)
            {
                DoBrowse(browser);
            }
        }

        private static void DoBrowse(BoboBrowser browser)
        {
            String q = "java";
            QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_CURRENT, "b", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_CURRENT));
            Query query = parser.Parse(q);
            BrowseRequest br = new BrowseRequest();
            //br.setQuery(query);
            br.Offset = 0;
            br.Count = 0;

            BrowseSelection geoSel = new BrowseSelection("geo_region");
            geoSel.AddValue("5227");
            BrowseSelection industrySel = new BrowseSelection("industry_norm");
            industrySel.AddValue("1");

            //br.AddSelection(geoSel);
            br.AddSelection(industrySel);

            FacetSpec regionSpec = new FacetSpec();
            regionSpec.ExpandSelection = true;
            regionSpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            regionSpec.MaxCount = 5;

            FacetSpec industrySpec=new FacetSpec();
            industrySpec.ExpandSelection = true;
            industrySpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            industrySpec.MaxCount = 5;
        

            FacetSpec numEndorserSpec=new FacetSpec();
            numEndorserSpec.ExpandSelection = true;
    
		    br.SetFacetSpec("industry_norm", industrySpec);
            br.SetFacetSpec("geo_region", regionSpec);
            br.SetFacetSpec("num_endorsers_norm", numEndorserSpec);

		    long start = System.Environment.TickCount;
		    BrowseResult res = browser.Browse(br);
		    long end = System.Environment.TickCount;

            Console.WriteLine("result: " + res);
            Console.WriteLine("took: " + (end-start));
        }
    }
}
