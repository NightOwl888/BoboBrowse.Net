/**
 * Bobo Browse Engine - High performance faceted/parametric search implementation 
 * that handles various types of semi-structured data.  Written in Java.
 * 
 * Copyright (C) 2005-2006  John Wang
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * 
 * To contact the project administrators for the bobo-browse project, 
 * please go to https://sourceforge.net/projects/bobo-browse/, or 
 * send mail to owner@browseengine.com.
 */

// Version compatibility level: 3.2.0
namespace BoboBrowse.Tests
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Index;
    using BoboBrowse.Net.Index.Digest;
    using Common.Logging;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Text;

    [TestFixture]
    public class FacetNotValuesTest
    {
        private static ILog log = LogManager.GetLogger<FacetNotValuesTest>();
        private List<IFacetHandler> _facetHandlers;
        private int _documentSize;
        private static string[] _idRanges = new string[] { "[10 TO 10]" };

        private class TestDataDigester : DataDigester
        {
            private List<IFacetHandler> _facetHandlers;
            private Document[] _data;

            public TestDataDigester(List<IFacetHandler> facetHandlers, Document[] data)
            {
                _facetHandlers = facetHandlers;
                _data = data;
            }

            public override void Digest(DataDigester.IDataHandler handler)
            {
                for (int i = 0; i < _data.Length; ++i)
                {
                    handler.HandleDocument(_data[i]);
                }
            }
        }

        [SetUp]
        public void Init()
        {
            _facetHandlers = CreateFacetHandlers();

            _documentSize = 2;
            //string confdir = System.getProperty("conf.dir");
            //if (confdir == null) confdir = "./resource";
            //org.apache.log4j.PropertyConfigurator.configure(confdir + "/log4j.properties");
        }

        [TearDown]
        public void Dispose()
        {
            _facetHandlers = null;
            _documentSize = 0;
        }

        public Document[] CreateDataTwo()
        {
            List<Document> dataList = new List<Document>();
            string color = "red";
            string ID = "10";
            Document d = new Document();
            d.Add(new Field("id", ID, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            d.Add(new Field("color", color, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            d.Add(new NumericField("NUM").SetIntValue(10));
            dataList.Add(d);

            color = "green";
            ID = "11";
            d = new Document();
            d.Add(new Field("id", ID, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            d.Add(new Field("color", color, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
            d.Add(new NumericField("NUM").SetIntValue(11));
            dataList.Add(d);


            return dataList.ToArray();
        }

        public Document[] CreateData()
        {
            List<Document> dataList = new List<Document>();
            for (int i = 0; i < _documentSize; i++)
            {
                string color = (i % 2 == 0) ? "red" : "green";
                string ID = Convert.ToString(i);
                Document d = new Document();
                d.Add(new Field("id", ID, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
                d.Add(new Field("color", color, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
                dataList.Add(d);
            }

            return dataList.ToArray();
        }

        private Directory CreateIndexTwo()
        {
            Directory dir = new RAMDirectory();

            Document[] data = CreateDataTwo();

            TestDataDigester testDigester = new TestDataDigester(_facetHandlers, data);
            BoboIndexer indexer = new BoboIndexer(testDigester, dir);
            indexer.Index();
            using (IndexReader r = IndexReader.Open(dir, false))
            { }

            return dir;
        }

        private Directory CreateIndex()
        {
            Directory dir = new RAMDirectory();

            Document[] data = CreateData();

            TestDataDigester testDigester = new TestDataDigester(_facetHandlers, data);
            BoboIndexer indexer = new BoboIndexer(testDigester, dir);
            indexer.Index();
            using (IndexReader r = IndexReader.Open(dir, false))
            { }

            return dir;
        }

        public static List<IFacetHandler> CreateFacetHandlers()
        {
            List<IFacetHandler> facetHandlers = new List<IFacetHandler>();
            facetHandlers.Add(new SimpleFacetHandler("id"));
            facetHandlers.Add(new SimpleFacetHandler("color"));
            IFacetHandler rangeFacetHandler = new RangeFacetHandler("idRange", "id", null); //, Arrays.asList(_idRanges));
            facetHandlers.Add(rangeFacetHandler);

            return facetHandlers;
        }

        [Test]
        public void TestNotValuesForSimpleFacetHandler()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 20;
            br.Offset = 0;

            BrowseSelection colorSel = new BrowseSelection("color");
            colorSel.AddValue("red");
            br.AddSelection(colorSel);

            BrowseSelection idSel = new BrowseSelection("id");
            idSel.AddNotValue("0");
            br.AddSelection(idSel);

            BrowseResult result = null;
            BoboBrowser boboBrowser = null;
            int expectedHitNum = (_documentSize / 2) - 1;

            using (Directory ramIndexDir = CreateIndex())
            {
                using (IndexReader srcReader = IndexReader.Open(ramIndexDir, true))
                {
                    using (boboBrowser = new BoboBrowser(BoboIndexReader.GetInstance(srcReader, _facetHandlers, null)))
                    {
                        result = boboBrowser.Browse(br);

                        Assert.AreEqual(expectedHitNum, result.NumHits);

                        StringBuilder buffer = new StringBuilder();
                        BrowseHit[] hits = result.Hits;

                        for (int i = 0; i < hits.Length; ++i)
                        {
                            int expectedID = (i + 1) * 2;
                            Assert.AreEqual(expectedID, int.Parse(hits[i].GetField("id")));
                            if (i != 0)
                            {
                                buffer.Append('\n');
                            }
                            buffer.Append("id=" + hits[i].GetField("id") + "," + "color=" + hits[i].GetField("color"));
                        }
                        log.Info(buffer.ToString());
                    }
                }
            }
        }

        [Test]
        public void TestNotValuesForRangeFacetHandler()
        {
            Console.WriteLine("TestNotValuesForRangeFacetHandler");
            BrowseResult result = null;
            BoboBrowser boboBrowser=null;

            using (Directory ramIndexDir = CreateIndexTwo())
            {

                using (IndexReader srcReader = IndexReader.Open(ramIndexDir, true))
                {

                    using (boboBrowser = new BoboBrowser(BoboIndexReader.GetInstance(srcReader, _facetHandlers, null)))
                    {

                        BrowseRequest br = new BrowseRequest();
                        br.Count = (20);
                        br.Offset = (0);

                        if (_idRanges == null)
                        {
                            log.Error("_idRanges cannot be null in order to test NOT on RangeFacetHandler");
                        }
                        BrowseSelection idSel = new BrowseSelection("idRange");
                        //int rangeIndex = 2; // Not used
                        idSel.AddNotValue(_idRanges[0]);
                        int expectedHitNum = 1;
                        br.AddSelection(idSel);
                        BooleanQuery q = new BooleanQuery();
                        q.Add(NumericRangeQuery.NewIntRange("NUM", 10, 10, true, true), Occur.MUST_NOT);
                        q.Add(new MatchAllDocsQuery(), Occur.MUST);
                        br.Query = q;

                        result = boboBrowser.Browse(br);

                        Assert.AreEqual(expectedHitNum, result.NumHits);
                        for (int i = 0; i < result.NumHits; i++)
                        {
                            Console.WriteLine(result.Hits[i]);
                        }
                    }
                }
            }
        }
    }
}
