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
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    [TestFixture]
    public class FacetMergePerf
    {
        static int numVals = 100000;
        static int numDocs = 5000000;
        static int numSegs = 10;
        static int numDocsPerSeg = numDocs / numSegs;
        static Random rand = new Random();

        static int percent_zero = 80;

        static FacetDataCache MakeFacetDataCache()
        {
            FacetDataCache cache = new FacetDataCache();
            cache.Freqs = new int[numVals];
            Random r = new Random();
            for (int i = 0; i < cache.Freqs.Length; ++i)
            {
                int p = r.Next(100);
                int v;
                if (p % 100 < percent_zero)
                {
                    v = 0;
                }
                else
                {
                    v = Math.Abs(rand.Next(numDocs - 1)) + 1;
                }

                cache.Freqs[i] = v;
            }
            //Arrays.Fill(cache.Freqs, 1);
            cache.MaxIDs = new int[numVals];
            cache.MinIDs = new int[numVals];
            cache.ValArray = new TermIntList(numVals, "0000000000");

            for (int i = 0; i < numVals; ++i)
            {
                cache.ValArray.Add((i + 1).ToString("0000000000"));
            }
            cache.ValArray.Seal();
            cache.OrderArray = new BigIntArray(numDocsPerSeg);
            return cache;
        }

        static IFacetAccessible BuildSubAccessible(string name, int segment, FacetSpec fspec)
        {

            SimpleFacetHandler.SimpleFacetCountCollector collector = new SimpleFacetHandler.SimpleFacetCountCollector(name, MakeFacetDataCache(), 
                numDocsPerSeg * segment, null, fspec);
            collector.CollectAll();
            return collector;
        }

        public class RunnerThread
        {
            private readonly AtomicLong _timeCounter;
            private readonly int _numIters;
            private readonly FacetSpec _fspec;
            private readonly ICollection<IFacetAccessible> _list1;

            public RunnerThread(AtomicLong timeCounter, int numIters, FacetSpec fspec, ICollection<IFacetAccessible> list1)
            {
                _timeCounter = timeCounter;
                _numIters = numIters;
                _fspec = fspec;
                _list1 = list1;
            }

            public void Run()
            {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                for (int i = 0; i < _numIters; ++i)
                {
				    long start = sw.ElapsedMilliseconds;
				    CombinedFacetAccessible combined1 = new CombinedFacetAccessible(_fspec, _list1);
				    // CombinedFacetAccessible combined2 = new CombinedFacetAccessible(_fspec, _list2);
				    IEnumerable<BrowseFacet> facets1 = combined1.GetFacets();
				    //IEnumerable<BrowseFacet> facets2 = combined2.GetFacets();
				    long end= sw.ElapsedMilliseconds;
				    _timeCounter.GetAndAdd(end-start);
			    }
            }
        }

        public static void Main(string[] args)
        {
            int nThreads = 2;
            int numIters = 200;

            string fname1 = "facet1";
            FacetSpec fspec = new FacetSpec();
            fspec.ExpandSelection = (true);
            fspec.MaxCount = (50);
            fspec.MinHitCount = (1);
            fspec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;

            List<IFacetAccessible> list1 = new List<IFacetAccessible>(numSegs);
            for (int i = 0; i < numSegs; ++i)
            {
                list1.Add(BuildSubAccessible(fname1, i, fspec));
            }
            
            AtomicLong timeCounter = new AtomicLong();
            Thread[] threads = new Thread[nThreads];
            RunnerThread[] threadStates = new RunnerThread[nThreads];
            for (int i = 0; i < threads.Length; ++i)
            {
                var threadState = new RunnerThread(timeCounter, numIters, fspec, list1);
                threadStates[i] = threadState;
                threads[i] = new Thread(new ThreadStart(threadState.Run));
            }


            //		System.out.println("press key to start load test... ");
            //		{
            //			BufferedReader br = new BufferedReader(new InputStreamReader(
            //					System.in));
            //			int ch = br.read();
            //			char c = (char) ch;
            //		}
            foreach (Thread t in threads)
            {
                t.Start();
            }

            foreach (Thread t in threads)
            {
                t.Join();
            }

            Console.WriteLine("average time: " + timeCounter.Get() / numIters / nThreads + " ms");
        }

        [Test]
        [Category("LongRunning")]
        public void TestFacetMergePerf()
        {
            Main(new string[0]);
        }
    }
}
