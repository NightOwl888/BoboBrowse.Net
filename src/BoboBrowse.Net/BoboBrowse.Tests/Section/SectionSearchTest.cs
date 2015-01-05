// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Search.Section
{
    using BoboBrowse.Net.Analysis.Section;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Analysis;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class SectionSearchTest // Original name was TestSectionSearch
    {
        private static Term intMetaTerm = new Term("metafield", "intmeta");
        private RAMDirectory directory;
        private Analyzer analyzer;
        private IndexWriter writer;
        private IndexSearcher searcher;
        private IndexSearcher searcherWithCache;

        [SetUp]
        public void Init()
        {
            directory = new RAMDirectory();
            analyzer = new WhitespaceAnalyzer();
            writer = new IndexWriter(directory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
            AddDoc("1", new String[] { "aa", "bb" }, new String[] { "aaa", "aaa" }, new int[] { 100, 200 });
            AddDoc("2", new String[] { "aa", "bb" }, new String[] { "aaa", "bbb" }, new int[] { 200, 200 });
            AddDoc("3", new String[] { "aa", "bb" }, new String[] { "bbb", "aaa" }, new int[] { 300, 300 });
            AddDoc("3", new String[] { "bb", "aa" }, new String[] { "bbb", "bbb" }, new int[] { 300, 400 });
            AddDoc("3", new String[] { "bb", "aa" }, new String[] { "aaa", "ccc" }, new int[] { 300, 500 });
            writer.Commit();
            IndexReader reader = IndexReader.Open(directory, true);
            searcher = new IndexSearcher(reader);
            IndexReader readerWithCache = new IndexReaderWithMetaDataCache(reader);
            searcherWithCache = new IndexSearcher(readerWithCache);
        }

        [TearDown]
        public void Dispose()
        {
            searcher.Dispose();
            writer.Dispose();
            directory.Dispose();
            analyzer = null;
        }

        private void AddDoc(string key, string[] f1, string[] f2, int[] meta)
        {
            Document doc = new Document();
            AddStoredField(doc, "key", key);
            AddTextField(doc, "f1", f1);
            AddTextField(doc, "f2", f2);
            AddMetaDataField(doc, intMetaTerm, meta);
            writer.AddDocument(doc);
        }

        private void AddStoredField(Document doc, string fieldName, string value)
        {
            Field field = new Field(fieldName, value, Field.Store.YES, Field.Index.NO);
            doc.Add(field);
        }

        private void AddTextField(Document doc, string fieldName, string[] sections)
        {
            for (int i = 0; i < sections.Length; i++)
            {
                Field field = new Field(fieldName, new SectionTokenStream(analyzer.TokenStream(fieldName, new System.IO.StringReader(sections[i])), i));
                doc.Add(field);
            }
        }

        private void AddMetaDataField(Document doc, Term term, int[] meta)
        {
            IntMetaDataTokenStream tokenStream = new IntMetaDataTokenStream(term.Text);
            tokenStream.SetMetaData(meta);
            Field field = new Field(term.Field, tokenStream);
            doc.Add(field);
        }

        static int GetNumHits(Query q, IndexSearcher searcher)
        {
            TopDocs hits = searcher.Search(q, 10);
            return hits.TotalHits;
        }

        [Test]
        public void TestSimpleSearch()
        {
            BooleanQuery bquery;
            SectionSearchQuery squery;
            int count;

            // 1. (+f1:aa +f2:aaa)
            bquery = new BooleanQuery();
            bquery.Add(new TermQuery(new Term("f1", "aa")), Occur.MUST);
            bquery.Add(new TermQuery(new Term("f2", "aaa")), Occur.MUST);

            count = GetNumHits(bquery, searcher);
            Assert.AreEqual(4, count, "non-section count mismatch");

            squery = new SectionSearchQuery(bquery);
            count = GetNumHits(squery, searcher);
            Assert.AreEqual(2, count, "seciton count mismatch");

            // 2. (+f1:bb + f2:aaa)
            bquery = new BooleanQuery();
            bquery.Add(new TermQuery(new Term("f1", "bb")), Occur.MUST);
            bquery.Add(new TermQuery(new Term("f2", "aaa")), Occur.MUST);

            count = GetNumHits(bquery, searcher);
            Assert.AreEqual(4, count, "non-section count mismatch");

            squery = new SectionSearchQuery(bquery);
            count = GetNumHits(squery, searcher);
            Assert.AreEqual(3, count, "seciton count mismatch");

            // 3. (+f1:aa +f2:bbb)
            bquery = new BooleanQuery();
            bquery.Add(new TermQuery(new Term("f1", "aa")), Occur.MUST);
            bquery.Add(new TermQuery(new Term("f2", "bbb")), Occur.MUST);

            count = GetNumHits(bquery, searcher);
            Assert.AreEqual(3, count, "non-section count mismatch");

            squery = new SectionSearchQuery(bquery);
            count = GetNumHits(squery, searcher);
            Assert.AreEqual(2, count, "seciton count mismatch");

            // 4. (+f1:aa +(f2:bbb f2:ccc))
            BooleanQuery bquery2 = new BooleanQuery();
            bquery2.Add(new TermQuery(new Term("f2", "bbb")), Occur.SHOULD);
            bquery2.Add(new TermQuery(new Term("f2", "ccc")), Occur.SHOULD);
            bquery = new BooleanQuery();
            bquery.Add(new TermQuery(new Term("f1", "aa")), Occur.MUST);
            bquery.Add(bquery2, Occur.MUST);

            count = GetNumHits(bquery, searcher);
            Assert.AreEqual(4, count, "non-section count mismatch");

            squery = new SectionSearchQuery(bquery);
            count = GetNumHits(squery, searcher);
            Assert.AreEqual(3, count, "section count mismatch");
        }

        [Test]
        public void TestMetaData()
        {
            MetaDataSearch(searcher);
        }

        [Test]
        public void TestMetaDataWithCache()
        {
            MetaDataSearch(searcherWithCache);    
        }

        private void MetaDataSearch(IndexSearcher searcher)
        {
            IndexReader reader = searcher.IndexReader;

            BooleanQuery bquery;
            SectionSearchQuery squery;
            Scorer scorer;
            int count;

            // 1.
            bquery = new BooleanQuery();
            bquery.Add(new TermQuery(new Term("f1", "aa")), Occur.MUST);
            bquery.Add(new IntMetaDataQuery(intMetaTerm, new IntMetaDataQuery.SimpleValueValidator(100)), Occur.MUST);
            squery = new SectionSearchQuery(bquery);
            scorer = squery.CreateWeight(searcher).Scorer(reader, true, true);
            count = 0;
            while (scorer.NextDoc() != Scorer.NO_MORE_DOCS) count++;
            Assert.AreEqual(1, count, "section count mismatch");

            // 2.
            bquery = new BooleanQuery();
            bquery.Add(new TermQuery(new Term("f1", "aa")), Occur.MUST);
            bquery.Add(new IntMetaDataQuery(intMetaTerm, new IntMetaDataQuery.SimpleValueValidator(200)), Occur.MUST);
            squery = new SectionSearchQuery(bquery);
            scorer = squery.CreateWeight(searcher).Scorer(reader, true, true);
            count = 0;
            while (scorer.NextDoc() != Scorer.NO_MORE_DOCS) count++;
            Assert.AreEqual(1, count, "section count mismatch");

            // 3.
            bquery = new BooleanQuery();
            bquery.Add(new TermQuery(new Term("f1", "bb")), Occur.MUST);
            bquery.Add(new IntMetaDataQuery(intMetaTerm, new IntMetaDataQuery.SimpleValueValidator(200)), Occur.MUST);
            squery = new SectionSearchQuery(bquery);
            scorer = squery.CreateWeight(searcher).Scorer(reader, true, true);
            count = 0;
            while (scorer.NextDoc() != Scorer.NO_MORE_DOCS) count++;
            Assert.AreEqual(2, count, "section count mismatch");

            // 4.
            bquery = new BooleanQuery();
            bquery.Add(new TermQuery(new Term("f1", "aa")), Occur.MUST);
            bquery.Add(new IntMetaDataQuery(intMetaTerm, new IntMetaDataQuery.SimpleValueValidator(300)), Occur.MUST);
            squery = new SectionSearchQuery(bquery);
            scorer = squery.CreateWeight(searcher).Scorer(reader, true, true);
            count = 0;
            while (scorer.NextDoc() != Scorer.NO_MORE_DOCS) count++;
            Assert.AreEqual(1, count, "section count mismatch");

            // 5.
            bquery = new BooleanQuery();
            bquery.Add(new TermQuery(new Term("f1", "bb")), Occur.MUST);
            bquery.Add(new IntMetaDataQuery(intMetaTerm, new IntMetaDataQuery.SimpleValueValidator(300)), Occur.MUST);
            squery = new SectionSearchQuery(bquery);
            scorer = squery.CreateWeight(searcher).Scorer(reader, true, true);
            count = 0;
            while (scorer.NextDoc() != Scorer.NO_MORE_DOCS) count++;
            Assert.AreEqual(3, count, "section count mismatch");
        }
    }
}
