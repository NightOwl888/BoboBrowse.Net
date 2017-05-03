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
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.QueryParsers.Classic;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using Lucene.Net.Util;
    using NUnit.Framework;
    using System.Collections.Generic;

    [TestFixture]
    public class BasicIndexingTests
    {
        private IndexWriter m_indexWriter;

        [SetUp]
        public void Init()
        {
            IndexWriterConfig config = new IndexWriterConfig(LuceneVersion.LUCENE_48,
                new StandardAnalyzer(LuceneVersion.LUCENE_48));
            config.MaxBufferedDocs = (1000);
            m_indexWriter = new IndexWriter(new RAMDirectory(), config);
        }

        [TearDown]
        public void Dispose()
        {
            m_indexWriter.Dispose();
            m_indexWriter = null;
        }

        [Test]
        public void TestWithInterleavedCommitsUsingBobo()
        {
            string text = "text";

            Document doc1 = new Document();
            doc1.Add(new TextField(text, "Foo1", Field.Store.YES));
            m_indexWriter.AddDocument(doc1);
            m_indexWriter.Commit();

            Document doc2 = new Document();
            doc2.Add(new TextField(text, "Foo2", Field.Store.YES));
            m_indexWriter.AddDocument(doc2);
            m_indexWriter.Commit();

            Document doc3 = new Document();
            doc3.Add(new TextField(text, "Foo3", Field.Store.YES));
            m_indexWriter.AddDocument(doc3);
            m_indexWriter.Commit();

            List<IFacetHandler> handlerList = new List<IFacetHandler>();


            DirectoryReader reader = BoboMultiReader.Open(m_indexWriter, true);

            BoboMultiReader boboMultiReader = BoboMultiReader.GetInstance(reader,
                handlerList);

            BrowseRequest br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "text",
                new StandardAnalyzer(LuceneVersion.LUCENE_48));
            Lucene.Net.Search.Query q = parser.Parse("Foo*");
            br.Query = (q);

            BoboBrowser browser = new BoboBrowser(boboMultiReader);
            BrowseResult result = browser.Browse(br);

            int totalHits = result.NumHits;
            BrowseHit[] hits = result.Hits;

            Assert.AreEqual(3, totalHits, "should be 3 hits");
            Assert.AreEqual(0, hits[0].DocId, "should be doc 0");
            Assert.AreEqual(1, hits[1].DocId, "should be doc 1"); // <-- This is
            // where the
            // test fails,
            // because all
            // three browser
            // hits are
            // returned with
            // doc id 0
            Assert.AreEqual(2, hits[2].DocId, "should be doc 2");

            result.Dispose();
        }

        [Test]
        public void TestWithSingleCommit()
        {
            string text = "text";

            Document doc1 = new Document();
            doc1.Add(new TextField(text, "Foo1", Field.Store.YES));
            m_indexWriter.AddDocument(doc1);

            Document doc2 = new Document();
            doc2.Add(new TextField(text, "Foo2", Field.Store.YES));
            m_indexWriter.AddDocument(doc2);

            Document doc3 = new Document();
            doc3.Add(new TextField(text, "Foo3", Field.Store.YES));
            m_indexWriter.AddDocument(doc3);

            m_indexWriter.Commit();

            List<IFacetHandler> handlerList = new List<IFacetHandler>();

            DirectoryReader reader = BoboMultiReader.Open(m_indexWriter, true);
            BoboMultiReader boboMultiReader = BoboMultiReader.GetInstance(reader,
                handlerList);

            BrowseRequest br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "text",
                new StandardAnalyzer(LuceneVersion.LUCENE_48));
            Lucene.Net.Search.Query q = parser.Parse("Foo*");
            br.Query = (q);

            BoboBrowser browser = new BoboBrowser(boboMultiReader);
            BrowseResult result = browser.Browse(br);

            int totalHits = result.NumHits;
            BrowseHit[] hits = result.Hits;

            Assert.AreEqual(3, totalHits, "should be 3 hits");
            Assert.AreEqual(0, hits[0].DocId, "should be doc 0");
            Assert.AreEqual(1, hits[1].DocId, "should be doc 1");
            Assert.AreEqual(2, hits[2].DocId, "should be doc 2");

            result.Dispose();
        }

        [Test]
        public void TestWithInterleavedCommitsUsingLuceneQuery()
        {
            string text = "text";

            Document doc1 = new Document();
            doc1.Add(new TextField(text, "Foo1", Field.Store.YES));
            m_indexWriter.AddDocument(doc1);
            m_indexWriter.Commit();

            Document doc2 = new Document();
            doc2.Add(new TextField(text, "Foo2", Field.Store.YES));
            m_indexWriter.AddDocument(doc2);
            m_indexWriter.Commit();

            Document doc3 = new Document();
            doc3.Add(new TextField(text, "Foo3", Field.Store.YES));
            m_indexWriter.AddDocument(doc3);
            m_indexWriter.Commit();

            DirectoryReader reader = DirectoryReader.Open(m_indexWriter, true);
            IndexSearcher searcher = new IndexSearcher(reader);
            TopScoreDocCollector docCollector = TopScoreDocCollector.Create(100,
                true);
            QueryParser queryParser = new QueryParser(LuceneVersion.LUCENE_48, "text",
                new StandardAnalyzer(LuceneVersion.LUCENE_48));
            Lucene.Net.Search.Query query = queryParser.Parse("Foo*");
            searcher.Search(query, docCollector);
            TopDocs docs = docCollector.GetTopDocs();
            ScoreDoc[] scoreDocs = docs.ScoreDocs;

            Assert.AreEqual(0, scoreDocs[0].Doc, "should be doc 0");
            Assert.AreEqual(1, scoreDocs[1].Doc, "should be doc 1");
            Assert.AreEqual(2, scoreDocs[2].Doc, "should be doc 2");

            reader.Dispose();
        }
    }
}
