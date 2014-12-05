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

namespace BoboBrowse.Tests
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Index;
    using BoboBrowse.Net.Index.Digest;
    using BoboBrowse.Net.Search;
    using Common.Logging;
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

    /// <summary>
    /// This class is to test the case when facetName is different from the underlying indexingFieldName for simpleFacetHandler
    /// 
    /// author hyan
    /// 
    /// Ported from BoboBrowse version 3.1.0
    /// </summary>
    [TestFixture]
    public class FacetNameTest
    {
        private static readonly ILog logger = LogManager.GetLogger<FacetNameTest>();
        private IEnumerable<FacetHandler> _facetHandlers;
        private int _documentSize;

        private class TestDataDigester : DataDigester
        {
            private IEnumerable<FacetHandler> _facetHandlers;
            private Document[] _data;

            public TestDataDigester(IEnumerable<FacetHandler> facetHandlers, Document[] data)
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
            _documentSize = 10;
        }

        [TearDown]
        public void Dispose()
        {
            _facetHandlers = null;
            _documentSize = 0;
        }

        public Document[] CreateData()
        {
            var dataList = new List<Document>();
            for (int i = 0; i < _documentSize; ++i)
            {
                String color = null;
                if (i == 0) color = "red";
                else if (i == 1) color = "green";
                else if (i == 2) color = "blue";
                else if (i % 2 == 0) color = "yellow";
                else color = "white";

                String make = null;
                if (i == 0) make = "camry";
                else if (i == 1) make = "accord";
                else if (i == 2) make = "4runner";
                else if (i % 2 == 0) make = "rav4";
                else make = "prius";

                String ID = i.ToString();
                Document d = new Document();
                d.Add(new Field("id", ID, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
                d.Add(new Field("color", color, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
                d.Add(new Field("make", make, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
                dataList.Add(d);
            }
            return dataList.ToArray();
        }

        private Directory CreateIndex()
        {
            Directory dir = new RAMDirectory();

            Document[] data = CreateData();

            TestDataDigester testDigester = new TestDataDigester(_facetHandlers, data);
            BoboIndexer indexer = new BoboIndexer(testDigester, dir);
            indexer.Index();
            IndexReader r = IndexReader.Open(dir, false);
            r.Close();

            return dir;
        }

        public static IEnumerable<FacetHandler> CreateFacetHandlers()
        {
            var facetHandlers = new List<FacetHandler>();
            facetHandlers.Add(new SimpleFacetHandler("id"));
            facetHandlers.Add(new SimpleFacetHandler("make"));
            facetHandlers.Add(new SimpleFacetHandler("mycolor", "color"));

            return facetHandlers;
        }

        [Test]
        public void TestFacetNameForSimpleFacetHandler()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 20;
            br.Offset = 0;

            BrowseSelection colorSel = new BrowseSelection("mycolor");
            colorSel.AddValue("yellow");
            br.AddSelection(colorSel);

            BrowseSelection makeSel = new BrowseSelection("make");
            makeSel.AddValue("rav4");
            br.AddSelection(makeSel);

            FacetSpec spec = new FacetSpec();
            spec.ExpandSelection = true;
            spec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            spec.MaxCount = 15;

            br.SetFacetSpec("mycolor", spec);
            br.SetFacetSpec("id", spec);
            br.SetFacetSpec("make", spec);

            BrowseResult result = null;
            BoboBrowser boboBrowser = null;
            int expectedHitNum = 3;
            try
            {
                Directory ramIndexDir = CreateIndex();
                IndexReader srcReader = IndexReader.Open(ramIndexDir, true);
                boboBrowser = new BoboBrowser(BoboIndexReader.GetInstance(srcReader, _facetHandlers, null));
                result = boboBrowser.Browse(br);

                Assert.AreEqual(expectedHitNum, result.NumHits);
            }
            catch (BrowseException e)
            {
                Assert.Fail(e.Message);
            }
            catch (System.IO.IOException ioe)
            {
                Assert.Fail(ioe.Message);
            }
            finally
            {
                if (boboBrowser != null)
                {
                    try
                    {
                        boboBrowser.Close();
                    }
                    catch (System.IO.IOException e)
                    {
                        Assert.Fail(e.Message);
                    }
                }
            }
        }
    }
}
