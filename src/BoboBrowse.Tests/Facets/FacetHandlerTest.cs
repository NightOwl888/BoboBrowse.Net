namespace BoboBrowse.Net.Facets
{
    using BoboBrowse.Net.Facets.Filters;
    using BoboBrowse.Net.Search;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class FacetHandlerTest
    {
        private Directory _ramDir;
        private class NoopFacetHandler : FacetHandler
        {
            public NoopFacetHandler(string name)
                : base(name)
            {
            }

            public NoopFacetHandler(string name, IEnumerable<string> dependsOn)
                : base(name, dependsOn)
            {
            }

            public override RandomAccessFilter BuildRandomAccessFilter(string value, Properties selectionProperty)
            {
                return null;
            }

            public override IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec fspec)
            {
                return null;
            }

            public override string[] GetFieldValues(int id)
            {
                return null;
            }

            // TODO: Not implemented
            //public override ScoreDocComparator GetStoreDocComparator()
            //{
            //    return null;
            //}

            public override FieldComparator GetComparator(int numDocs, SortField field)
            {
                return null;
            }

            public override void Load(BoboIndexReader reader)
            {
            }

            public override object[] GetRawFieldValues(int id)
            {
                // TODO Auto-generated method stub
                return null;
            }
        }

        public FacetHandlerTest()
        {
            _ramDir = new RAMDirectory();
            try
            {
                using (var writer = new IndexWriter(_ramDir, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_CURRENT), IndexWriter.MaxFieldLength.UNLIMITED))
                {
                }
            }
            catch
            {
                Assert.Fail("unable to load test");
            }
        }

        [Test]
        public void TestFacetHandlerLoad()
        {
            var reader = IndexReader.Open(_ramDir, true);
            
            var list = new List<FacetHandler>();
            var h1 = new NoopFacetHandler("A");
            list.Add(h1);

            var s2 = new HashSet<string>();
            s2.Add("A");
            s2.Add("C");
            s2.Add("D");
            var h2 = new NoopFacetHandler("B", s2);
            list.Add(h2);

            var s3 = new HashSet<string>();
            s3.Add("A");
            s2.Add("D"); // BUG: Should this be s3?
            var h3 = new NoopFacetHandler("C", s3);
            list.Add(h3);

            var s4 = new HashSet<string>();
            s4.Add("A");
            var h4 = new NoopFacetHandler("D", s4);
            list.Add(h4);

            var s5 = new HashSet<string>();
            s5.Add("E");
            var h5 = new NoopFacetHandler("E", s5);
            list.Add(h5);


            using (var boboReader = BoboIndexReader.GetInstance(reader, list))
            {

                using (var browser = new BoboBrowser(boboReader))
                {
                    var s6 = new HashSet<string>();
                    s6.Add("A");
                    s6.Add("B");
                    s6.Add("C");
                    s6.Add("D");
                    browser.SetFacetHandler(new NoopFacetHandler("runtime", s6));

                    var expected = new HashSet<string>();
                    expected.Add("A");
                    expected.Add("B");
                    expected.Add("C");
                    expected.Add("D");
                    expected.Add("E");
                    expected.Add("runtime");

                    var facetsLoaded = browser.GetFacetNames();

                    foreach (var name in facetsLoaded)
                    {
                        if (expected.Contains(name))
                        {
                            expected.Remove(name);
                        }
                        else
                        {
                            Assert.Fail(name + " is not in expected set.");
                        }
                    }

                    if (expected.Count > 0)
                    {
                        Assert.Fail("some facets not loaded: " + string.Join(", ", expected.ToArray()));
                    }
                }
            }
        }

        [Test]
        public void TestNegativeLoad()
        {
            var reader = IndexReader.Open(_ramDir, true);

            var list = new List<FacetHandler>();
            var s1 = new HashSet<string>();
            s1.Add("C");
            var h1 = new NoopFacetHandler("A", s1);
            list.Add(h1);

            var s2 = new HashSet<string>();
            s2.Add("A");
            s2.Add("C");
            s2.Add("D");
            var h2 = new NoopFacetHandler("B", s2);
            list.Add(h2);

            var s3 = new HashSet<string>();
            s3.Add("A");
            s2.Add("D"); // BUG: Should this be s3?
            var h3 = new NoopFacetHandler("C", s3);
            list.Add(h3);

            var s4 = new HashSet<string>();
            s4.Add("A");
            var h4 = new NoopFacetHandler("D", s4);
            list.Add(h4);

            var s5 = new HashSet<string>();
            s5.Add("E");
            var h5 = new NoopFacetHandler("E", s5);
            list.Add(h5);

            using (var boboReader = BoboIndexReader.GetInstance(reader, list))
            {

                using (var browser = new BoboBrowser(boboReader))
                {
                    var expected = new HashSet<string>();
                    expected.Add("A");
                    expected.Add("B");
                    expected.Add("C");
                    expected.Add("D");
                    expected.Add("E");

                    var facetsLoaded = browser.GetFacetNames();

                    foreach (var name in facetsLoaded)
                    {
                        if (expected.Contains(name))
                        {
                            expected.Remove(name);
                        }
                        else
                        {
                            Assert.Fail(name + " is not in expected set.");
                        }
                    }

                    if (expected.Count > 0)
                    {
                        if (expected.Count == 4)
                        {
                            expected.Remove("A");
                            expected.Remove("B");
                            expected.Remove("C");
                            expected.Remove("D");
                            if (expected.Count > 0)
                            {
                                Assert.Fail("some facets not loaded: " + string.Join(", ", expected.ToArray()));
                            }
                        }
                        else
                        {
                            Assert.Fail("incorrect number of left over facets: " + string.Join(", ", expected.ToArray()));
                        }
                    }
                    else
                    {
                        Assert.Fail("some facets should not have been loaded.");
                    }
                }
            }
        }
    }
}
