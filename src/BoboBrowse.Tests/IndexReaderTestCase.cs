namespace BoboBrowse.Tests
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Search;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
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
    public class IndexReaderTestCase
    {
        private IEnumerable<FacetHandler> _fconf;

        [SetUp]
        public void Init()
        {
            _fconf = BoboTestCase.BuildFieldConf();
        }

        [Test]
        public void TestIndexReload()
        {
            try
            {
                RAMDirectory idxDir = new RAMDirectory();
                Document[] docs = BoboTestCase.BuildData();
                BoboIndexReader.WorkArea workArea = new BoboIndexReader.WorkArea();
                BrowseRequest req;
                BrowseSelection sel;
                BoboBrowser browser;
                BrowseResult result;

                IndexWriter writer = new IndexWriter(idxDir, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_CURRENT), true, IndexWriter.MaxFieldLength.UNLIMITED);
                writer.Close();

                int dup = 0;
                for (int j = 0; j < 50; j++)
                {
                    IndexReader idxReader = IndexReader.Open(idxDir, true);
                    BoboIndexReader reader = BoboIndexReader.GetInstance(idxReader, _fconf, workArea);

                    req = new BrowseRequest();
                    req.Offset = 0;
                    req.Count = 10;
                    sel = new BrowseSelection("color");
                    sel.AddValue("red");
                    req.AddSelection(sel);
                    browser = new BoboBrowser(reader);
                    result = browser.Browse(req);

                    Assert.AreEqual(3 * dup, result.NumHits);

                    req = new BrowseRequest();
                    req.Offset = 0;
                    req.Count = 10;
                    sel = new BrowseSelection("tag");
                    sel.AddValue("dog");
                    req.AddSelection(sel);
                    browser = new BoboBrowser(reader);
                    result = browser.Browse(req);

                    Assert.AreEqual(2 * dup, result.NumHits);

                    req = new BrowseRequest();
                    req.Offset = 0;
                    req.Count = 10;
                    sel = new BrowseSelection("tag");
                    sel.AddValue("funny");
                    req.AddSelection(sel);
                    browser = new BoboBrowser(reader);
                    result = browser.Browse(req);

                    Assert.AreEqual(3 * dup, result.NumHits);

                    writer = new IndexWriter(idxDir, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_CURRENT), false, IndexWriter.MaxFieldLength.UNLIMITED);
                    for (int k = 0; k <= j; k++)
                    {
                        for (int i = 0; i < docs.Length; i++)
                        {
                            writer.AddDocument(docs[i]);
                        }
                        dup++;
                    }
                    writer.Close();
                }
                idxDir.Close();
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        private static byte[] lookup = new byte[2500000];
        private static byte[] lookup2 = new byte[5000000];

        public static void Test3()
        {
            Arrays.Fill<byte>(lookup2, (byte)0x0);

            for (int i = 0; i < 250; i += 3)
            {
                lookup2[i] |= (0x1);
            }
            for (int i = 1; i < 10000; i += 3)
            {
                lookup2[i] |= (0x2);
            }

            long start = System.Environment.TickCount;
            for (int i = 0; i < 5000000; ++i)
            {
                int degree;
                if ((lookup2[i] & (0x1)) == 0)
                {
                    degree = 1;
                }
                else if ((lookup2[i] & (0x2)) == 0)
                {
                    degree = 2;
                }
                else
                {
                    degree = 3;
                }
            }

            long end = System.Environment.TickCount;
            Console.WriteLine("test 3 took: " + (end - start));
        }

        public static void Test2()
        {
            Arrays.Fill(lookup, (byte)0x0);

            for (int i = 0; i < 250; i += 3)
            {
                lookup[i >> 1] |= (byte)((0x1) << ((i & 0x1) << 2));
            }
            for (int i = 1; i < 10000; i += 3)
            {
                lookup[i >> 1] |= (byte)((0x2) << ((i & 0x1) << 2));
            }

            long start = System.Environment.TickCount;
            for (int i = 0; i < 5000000; ++i)
            {
                int degree;
                if ((lookup[i >> 1] & (0x1) << ((i & 0x1) << 2)) == 0)
                {
                    degree = 1;
                }
                else if ((lookup[i >> 1] & (0x2) << ((i & 0x1) << 2)) == 0)
                {
                    degree = 2;
                }
                else
                {
                    degree = 3;
                }
            }

            long end = System.Environment.TickCount;
            Console.WriteLine("test 2 took: " + (end - start));
        }

        public static void Test1()
        {
            var firstDegree = new HashSet<int>();
            var secondDegree = new HashSet<int>();

            for (int i = 0; i < 250; i += 3)
            {
                firstDegree.Add(i);
            }
            for (int i = 1; i < 10000; i += 3)
            {
                secondDegree.Add(i);
            }

            long start = System.Environment.TickCount;
            for (int i = 0; i < 5000000; ++i)
            {
                int degree;
                if (secondDegree.Contains(i))
                {
                    degree = 2;
                }
                else if (firstDegree.Contains(i))
                {
                    degree = 1;
                }
                else
                {
                    degree = 3;
                }
            }

            long end = System.Environment.TickCount;
            Console.WriteLine("test 1 took: " + (end - start));
        }

        [Test]
        public void TestFastMatchAllDocs()
        {
            System.IO.DirectoryInfo idxFile = new System.IO.DirectoryInfo("/Users/jwang/dataset/idx");
            Directory idxDir = FSDirectory.Open(idxFile);

            BoboIndexReader reader = BoboIndexReader.GetInstance(IndexReader.Open(idxDir, true));
            IndexSearcher searcher = new IndexSearcher(reader);

            //Query q = reader.getFastMatchAllDocsQuery();
            //Query q = new MatchAllDocsQuery();

            QueryParser qp = new QueryParser(Lucene.Net.Util.Version.LUCENE_CURRENT, "contents", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_CURRENT));
            Query q = qp.Parse("*:*");
            TopDocs topDocs = searcher.Search(q, 100);

            Assert.AreEqual(reader.NumDocs(), topDocs.TotalHits);

            reader.Close();
        }

        [Test]
        public void Test1_Test2_Test3()
        {
            for (int i = 0; i < 20; i++)
            {
                Test1();
                Test2();
                Test3();
            }
        }
    }
}
