
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Text;
    using NUnit.Framework;

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

            SimpleFacetHandler.SimpleFacetCountCollector collector = new SimpleFacetHandler.SimpleFacetCountCollector(name, MakeFacetDataCache(), numDocsPerSeg * segment, null, fspec);
            collector.CollectAll();
            return collector;
        }

        public class RunnerThread
        {
            private readonly AtomicLong _timeCounter;
            private readonly int _numIters;
            private readonly FacetSpec _fspec;
            private readonly IEnumerable<IFacetAccessible> _list1;

            public RunnerThread(AtomicLong timeCounter, int numIters, FacetSpec fspec, IEnumerable<IFacetAccessible> list1)
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
            //string fname2 = "facet2"; // NOT USED
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

            //List<FacetAccessible> list2 = new List<FacetAccessible>(numSegs);
            //for (int i = 0; i < numSegs; ++i)
            //{
            //    list2.add(BuildSubAccessible(fname2, i, fspec));
            //}		
            
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

        public static void Main1(string[] args)
        {
            //Comparable c = "00000000001";
            //Comparable c2 ="00000000002";
            //Comparable c = Integer.valueOf(1);
            //Comparable c2 = Integer.valueOf(2);

            int count = 500000;
            ITermValueList list = new TermIntList(count, "0000000000");
            for (int i = 0; i < count; ++i)
            {
                list.Add(i.ToString("0000000000"));
            }
            /*IntList list = new IntArrayList(count);
            for (int i=0;i<count;++i){
                list.add(i);
            }*/
            //int v1 = 1; // NOT USED
            //int v2 = 2; // NOT USED
            Console.WriteLine("start");
            long s = System.Environment.TickCount;
            for (int i = 0; i < count; ++i)
            {
                list.GetRawValue(i);
            }
            long e = System.Environment.TickCount;

            Console.WriteLine("timeL: " + (e - s));
        }

        [Test]
        [Category("LongRunning")]
        public void TestFacetMergePerf()
        {
            Main(new string[0]);
        }

        [Test]
        public void TestFacetMergePerf1()
        {
            Main1(new string[0]);
        }
    }
}
