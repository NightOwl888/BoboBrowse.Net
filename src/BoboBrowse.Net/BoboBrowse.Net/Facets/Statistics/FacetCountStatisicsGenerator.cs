//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Statistics
{
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Index;
    using Lucene.Net.QueryParsers;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using Lucene.Net.Util;
    using Directory = Lucene.Net.Store.Directory;

    public abstract class FacetCountStatisicsGenerator
    {
        private int _minCount = 1;

        public virtual int MinCount
        {
            get { return _minCount; }
            set { _minCount = value; }
        }

        public abstract double CalculateDistributionScore(int[] distribution, int collectedSampleCount, int numSamplesCollected, int totalSamplesCount);

        public virtual FacetCountStatistics GenerateStatistic(int[] distribution, int n)
        {
            int[] tmp = distribution;
            int totalSampleCount = distribution.Length;
            bool sorted = false;
            if (n > 0)
            {
                totalSampleCount = Math.Min(n, tmp.Length);
                // this is crappy, to be made better with a pq
                int[] tmp2 = new int[distribution.Length];
                System.Array.Copy(distribution, 0, tmp2, 0, distribution.Length);

                System.Array.Sort(tmp2);

                tmp = new int[totalSampleCount];
                System.Array.Copy(tmp2, 0, tmp, 0, tmp.Length);
                sorted = true;
            }

            int collectedSampleCount = 0;
            int numSamplesCollected = 0;

            foreach (int count in tmp)
            {
                if (count >= _minCount)
                {
                    collectedSampleCount += count;
                    numSamplesCollected++;
                }
                else
                {
                    if (sorted) break;
                }
            }

            double distScore = CalculateDistributionScore(tmp, collectedSampleCount, numSamplesCollected, totalSampleCount);

            FacetCountStatistics stats = new FacetCountStatistics();

            stats.Distribution = distScore;
            stats.NumSamplesCollected = numSamplesCollected;
            stats.CollectedSampleCount = collectedSampleCount;
            stats.TotalSampleCount = totalSampleCount;
            return stats;
        }

        public virtual FacetCountStatistics GenerateStatistic(IFacetCountCollector countHitCollector, int n)
        {
            return GenerateStatistic(BigIntArray.ToArray(countHitCollector.GetCountDistribution()), n);
        }

        //// FIXME : this is a kind of test
        //static void Main2(string[] args)
        //{
        //    Directory idxDir = FSDirectory.GetDirectory(new FileInfo(@"/Users/jwang/dataset/facet_idx_2/beef"));
        //    QueryParser qp = new QueryParser(Lucene.Net.Util.Version.LUCENE_CURRENT, "b", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_CURRENT));
        //    string q = "pc:yahoo";
        //    Query query = qp.Parse(q);


        //    BrowseRequest req = new BrowseRequest();
        //    req.Query = query;

        //    FacetSpec fspec = new FacetSpec();
        //    fspec.ExpandSelection = true;
        //    fspec.MaxCount = 5;
        //    fspec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;

        //    req.SetFacetSpec("ccid", fspec);
        //    req.SetFacetSpec("pcid", fspec);
        //    req.SetFacetSpec("education_id", fspec);
        //    req.SetFacetSpec("geo_region", fspec);
        //    req.SetFacetSpec("geo_country", fspec);
        //    req.SetFacetSpec("industry", fspec);
        //    req.SetFacetSpec("proposal_accepts", fspec);
        //    req.SetFacetSpec("num_endorsers", fspec);
        //    req.SetFacetSpec("group_id", fspec);

        //    BoboIndexReader reader = BoboIndexReader.GetInstance(IndexReader.Open(idxDir));
        //    BoboBrowser browser = new BoboBrowser(reader);

        //    BrowseResult res = browser.Browse(req);

        //    Dictionary<string, IFacetAccessible> facetMap = res.FacetMap;
        //    ICollection<IFacetAccessible> facetCountCollectors = facetMap.Values;
        //    IEnumerator<IFacetAccessible> iter = facetCountCollectors.GetEnumerator();
        //    while (iter.MoveNext())
        //    {
        //        IFacetAccessible f = iter.Current;
        //        if (f is IFacetCountCollector)
        //        {
        //            Console.WriteLine("====================================");
        //            IFacetCountCollector fc = (IFacetCountCollector)f;
        //            int[] dist = BigIntArray.ToArray(fc.GetCountDistribution());
        //            if (dist != null)
        //            {
        //                ChiSquaredFacetCountStatisticsGenerator gen = new ChiSquaredFacetCountStatisticsGenerator();
        //                gen.setMinCount(0);
        //                FacetCountStatistics stats = gen.generateStatistic(dist, 0);
        //                Console.WriteLine("stat for field " + fc.Name + ": " + stats);
        //                Console.WriteLine("Centered distribution score: " + (stats.getDistribution() - (double)(stats.getNumSamplesCollected() - 1)) / Math.Sqrt((2.0 * (double)(stats.getNumSamplesCollected() - 1))));
        //                Console.WriteLine("........................");
        //                IEnumerable<BrowseFacet> facetList = fc.GetFacets();
        //                Console.WriteLine(facetList);
        //                Console.WriteLine("........................");
        //            }
        //            Console.WriteLine("====================================");
        //        }
        //    }
        //    reader.Close();
        //}
    }
}