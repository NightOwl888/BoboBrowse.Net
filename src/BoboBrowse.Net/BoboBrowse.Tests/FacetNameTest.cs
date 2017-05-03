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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Tests
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Index;
    using BoboBrowse.Net.Index.Digest;
    using log4net;
    using log4net.Config;
    using log4net.Appender;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This class is to test the case when facetName is different from the underlying indexingFieldName for simpleFacetHandler
    /// 
    /// author hyan
    /// </summary>
    [TestFixture]
    public class FacetNameTest
    {
        //private static readonly BoboBrowse.Net.Support.Logging.ILog logger = LogProvider.For<FacetNameTest>();  // NOT USED
        private IList<IFacetHandler> _facetHandlers;
        private int _documentSize;

        private class TestDataDigester : DataDigester
        {
            private readonly Document[] _data;

            public TestDataDigester(IList<IFacetHandler> facetHandlers, Document[] data)
            {
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
                d.Add(new StringField("id", ID, Field.Store.YES));
                d.Add(new StringField("color", color, Field.Store.YES));
                d.Add(new StringField("make", make, Field.Store.YES));
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
            using (var r = DirectoryReader.Open(dir))
            {
            }

            return dir;
        }

        public static IList<IFacetHandler> CreateFacetHandlers()
        {
            var facetHandlers = new List<IFacetHandler>();
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

            int expectedHitNum = 3;

            Directory ramIndexDir = CreateIndex();
            using (DirectoryReader srcReader = DirectoryReader.Open(ramIndexDir))
            {
                using (BoboMultiReader boboReader = BoboMultiReader.GetInstance(srcReader, _facetHandlers))
                {
                    using (BoboBrowser boboBrowser = new BoboBrowser(boboReader))
                    {
                        using (BrowseResult result = boboBrowser.Browse(br))
                        {

                            Assert.AreEqual(expectedHitNum, result.NumHits);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Verifies that if a logger (such as log4net) is added to the project that
        /// the logging will automatically be sent to it.
        /// </summary>
        [Test]
        public void TestLogging()
        {
            // Set up a simple Log4Net configuration that logs in memory.
            var memAppend = new log4net.Appender.MemoryAppender();
            BasicConfigurator.Configure(memAppend);

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

            Directory ramIndexDir = CreateIndex();
            using (DirectoryReader srcReader = DirectoryReader.Open(ramIndexDir))
            {
                using (BoboMultiReader boboReader = BoboMultiReader.GetInstance(srcReader, _facetHandlers))
                {
                    using (BoboBrowser boboBrowser = new BoboBrowser(boboReader))
                    {
                        using (BrowseResult result = boboBrowser.Browse(br))
                        {
                        }
                    }
                }
            }

            var events = memAppend.GetEvents();

            Assert.AreEqual(3, events.Length);
            StringAssert.StartsWith("facetHandler loaded: id, took:", events[0].RenderedMessage);
            StringAssert.StartsWith("facetHandler loaded: make, took:", events[1].RenderedMessage);
            StringAssert.StartsWith("facetHandler loaded: mycolor, took:", events[2].RenderedMessage);
        }
    }
}
