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
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Support;
    using Lucene.Net.Analysis;
    using Lucene.Net.Analysis.Core;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using Lucene.Net.Support;
    using Lucene.Net.Util;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class PathMultiValTest // Original name was TestPathMultiVal.java
    {
        private RAMDirectory directory;
	    private Analyzer analyzer;
	    private List<IFacetHandler> facetHandlers;

        const string PathHandlerName = "path";

        [SetUp]
        public void Init()
        {
            facetHandlers = new List<IFacetHandler>();

            directory = new RAMDirectory();
            analyzer = new WhitespaceAnalyzer(LuceneVersion.LUCENE_48);
            IndexWriterConfig config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);
            config.SetOpenMode(OpenMode.CREATE);
            IndexWriter writer = new IndexWriter(directory, config);
            Document doc = new Document();
            AddMetaDataField(doc, PathHandlerName, new String[] { "/a/b/c", "/a/b/d" });
            writer.AddDocument(doc);
            writer.Commit();

            PathFacetHandler pathHandler = new PathFacetHandler("path", true);
            facetHandlers.Add(pathHandler);
        }

        [TearDown]
        public void Dispose()
        {
            facetHandlers = null;
            if (directory != null /*&& directory.IsOpen*/) directory.Dispose();
            directory = null;
            analyzer = null;
        }

        private void AddMetaDataField(Document doc, String name, String[] vals)
        {
            foreach (String val in vals)
            {
                Field field = new StringField(name, val, Field.Store.NO);
                doc.Add(field);
            }
        }

        [Test]
        public void TestMultiValPath()
        {
            DirectoryReader reader = DirectoryReader.Open(directory);
            BoboMultiReader boboReader = BoboMultiReader.GetInstance(reader, facetHandlers);

            BoboBrowser browser = new BoboBrowser(boboReader);
            BrowseRequest req = new BrowseRequest();

            BrowseSelection sel = new BrowseSelection(PathHandlerName);
            sel.AddValue("/a");
            var propMap = new Dictionary<String, String>();
            propMap.Put(PathFacetHandler.SEL_PROP_NAME_DEPTH, "0");
            propMap.Put(PathFacetHandler.SEL_PROP_NAME_STRICT, "false");
            sel.SetSelectionProperties(propMap);

            req.AddSelection(sel);

            FacetSpec fs = new FacetSpec();
            fs.MinHitCount = (1);
            req.SetFacetSpec(PathHandlerName, fs);

            BrowseResult res = browser.Browse(req);
            Assert.AreEqual(res.NumHits, 1);
            IFacetAccessible fa = res.GetFacetAccessor(PathHandlerName);
            IEnumerable<BrowseFacet> facets = fa.GetFacets();
            Console.WriteLine(facets);
            Assert.AreEqual(1, facets.Count());
            BrowseFacet facet = facets.Get(0);
            Assert.AreEqual(2, facet.FacetValueHitCount);
        }
    }
}
