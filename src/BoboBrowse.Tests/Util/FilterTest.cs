namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Tests;
    using Lucene.Net.Search;
    using LuceneExt;
    using NUnit.Framework;
    using System;
    using System.Collections;

    [TestFixture]
    public class FilterTest
    {
        [Test]
        public void TestFilteredDocSetIterator()
        {
            var set1 = new IntArrayDocIdSet();
            for (int i = 0; i < 100; i++)
            {
                set1.AddDoc(2 * i); // 100 even numbers
            }

            var filteredIter = new MyFilteredDocSetIterator(set1.Iterator());

            var bs = new BitSet(200);
            for (int i = 0; i < 100; ++i)
            {
                int n = 10 * i;
                if (n < 200)
                {
                    bs.Set(n, true);
                }
            }

            try
            {
                while (filteredIter.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                {
                    int doc = filteredIter.DocID();
                    if (!bs.Get(doc))
                    {
                        Assert.Fail("failed: " + doc + " not in expected set");
                        return;
                    }
                    else
                    {
                        bs.Set(doc, false);
                    }
                }
                var cardinality = bs.Cardinality();
                if (cardinality > 0)
                {
                    Assert.Fail("failed: leftover cardinality: " + cardinality);
                }
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        private class MyFilteredDocSetIterator : FilteredDocSetIterator
        {
            public MyFilteredDocSetIterator(DocIdSetIterator iterator)
                : base(iterator)
            {
            }

            protected internal override bool Match(int doc)
            {
                return doc % 5 == 0;
            }
        }
    }
}
