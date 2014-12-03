namespace BoboBrowse.Search
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Filter;
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
    public class BoboSearcher2Test
    {
        private Directory _ramDir;

        public BoboSearcher2Test()
        {
            _ramDir = new RAMDirectory();
        }

        private class MyWeight : Weight
        {
            public override Explanation Explain(IndexReader reader, int doc)
            {
                return null;
            }

            public override Query Query
            {
                get { return null; }
            }

            public override float Value
            {
                get { return 0; }
            }

            public override void Normalize(float norm)
            {
            }

            private class MyScorer : Scorer
            {
                private int _doc = -1;
                private int ptr = -1;
                private int[] fakedata = { 5, 6, 7, 8, 9 }; // has to be sorted in increasing order

                public MyScorer(Similarity similarity)
                    : base(similarity)
                {
                }

                // Not implemented in Lucene 3.0.3
                //public override Explanation Explain(int arg0)
                //{
                //    return null;
                //}

                public override float Score()
                {
                    return fakedata[fakedata.Length - 1] + 1 - _doc;
                }

                public override int DocID()
                {
                    return _doc;
                }

                public override int NextDoc()
                {
                    ptr++;
                    if (ptr < fakedata.Length)
                    {
                        _doc = fakedata[ptr];
                        return _doc;
                    }
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

                public override int Advance(int target)
                {
                    ptr++;
                    if (target <= _doc) target = _doc + 1;
                    while (ptr < fakedata.Length)
                    {
                        if (fakedata[ptr] >= target)
                        {
                            _doc = fakedata[ptr];
                            return _doc;
                        }
                        ptr++;
                    }
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

                //public static Scorer Scorer(IndexReader arg0)
                //{
                //    return new MyScorer(DefaultSimilarity.Default);
                //}
            }

            public override Scorer Scorer(IndexReader reader, bool scoreDocsInOrder, bool topScorer)
            {
                return new MyScorer(Similarity.Default);
            }

            public override float GetSumOfSquaredWeights()
            {
                return 0;
            }
        }

        private static int[] fakedata = { 5, 6, 7, 8, 9 }; // has to be sorted in increasing order
        private class MyIterator : DocIdSetIterator
        {
            private int _doc = -1;
            private int ptr = -1;

            public override int DocID()
            {
                return _doc;
            }

            public override int NextDoc()
            {
                ptr++;
                if (ptr < fakedata.Length)
                {
                    _doc = fakedata[ptr];
                    return _doc;
                }
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override int Advance(int target)
            {
                ptr++;
                if (target <= _doc) target = _doc + 1;
                while (ptr < fakedata.Length)
                {
                    if (fakedata[ptr] >= target)
                    {
                        _doc = fakedata[ptr];
                        return _doc;
                    }
                    ptr++;
                }
                return DocIdSetIterator.NO_MORE_DOCS;
            }
        }
        

        /// <summary>
        /// Test method for <see cref="M:BoboBrowse.Net.Search.BoboSearcher2.Search"/>
        /// </summary>
        [Test]
        public void TestSearchWeightFilterHitCollector()
        {
            Assert.AreEqual(DoTestSingle(), DoTestList());
        }

        private int DoTestList()
        {
            var bbs2 = new BoboSearcher2(null);

            var facetHitCollectors = new List<FacetHitCollector>();
            var o = new FacetHitCollector();
            o.PostDocIDSetIterator = new MyIterator();
            var radis = new MyRandomAccessDocIdSet();

            o.DocIdSet = radis;
            o.FacetCountCollector = new MyFacetCountCollector();

            //o.DocIdSet = radis;

            facetHitCollectors.Add(o);

            o = new FacetHitCollector();
            o.DocIdSet = radis;
            o.PostDocIDSetIterator = new MyIterator();
            o.FacetCountCollector = new MyFacetCountCollector();

            facetHitCollectors.Add(o);
            bbs2.SetFacetHitCollectorList(facetHitCollectors);
            var weight = new MyWeight();
            var results = TopScoreDocCollector.Create(100, true);
            bbs2.Search(weight, null, results);
            return results.TotalHits;
        }

        private int DoTestSingle()
        {
            var bbs2 = new BoboSearcher2(null);

            var facetHitCollectors = new List<FacetHitCollector>();
            var o = new FacetHitCollector();
            o.PostDocIDSetIterator = new MyIterator();
            var radis = new MyRandomAccessDocIdSet();
            o.DocIdSet = radis;
            o.FacetCountCollector = new MyFacetCountCollector();

            facetHitCollectors.Add(o);
            bbs2.SetFacetHitCollectorList(facetHitCollectors);
            var weight = new MyWeight();
            var results = TopScoreDocCollector.Create(100, true);
            bbs2.Search(weight, null, results);
            return results.TotalHits;
        }

        private class MyRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            public override bool Get(int docId)
            {
                foreach (var x in fakedata)
                {
                    if (x == docId) return true;
                }
                return false;
            }

            public override DocIdSetIterator Iterator()
            {
                return null;
            }
        }

        private class MyFacetCountCollector : DefaultFacetCountCollector
        {
            public MyFacetCountCollector()
                : base(new BrowseSelection(""), new FacetDataCache(), "", new FacetSpec())
            {
            }

            public override void Collect(int docid)
            {
            }

            public override void CollectAll()
            {
            }

            public override int[] GetCountDistribution()
            {
                return null;
            }

            public override string Name
            {
                get
                {
                    return null;
                }
            }

            public override BrowseFacet GetFacet(string value)
            {
                return null;
            }

            public override IEnumerable<BrowseFacet> GetFacets()
            {
                return null;
            }
        }
    }
}
