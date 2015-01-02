// Version compatibility level: 3.1.0
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Util;
    using NUnit.Framework;
    using System;
    using System.Threading;

    [TestFixture]
    public class MemoryUtil
    {
        static int max = 5000000;

        static int[] GetIndex(int count)
        {
            Random rand = new Random();
            int[] array = new int[count];

            for (int i = 0; i < count; ++i)
            {
                array[i] = rand.Next(max);
            }
            //Arrays.sort(array);
            return array;
        }

        private class RunnerThread2
        {
            private int[] array;
            private BigIntArray bigarray;

            public RunnerThread2(int[] a, BigIntArray b)
            {
                array = a;
                bigarray = b;
            }

            public void Run()
            {
                long start = System.Environment.TickCount;
                foreach (int val in array)
                {
                    int x = bigarray.Get(val);
                }

                long end = System.Environment.TickCount;
                Console.WriteLine("time: " + (end - start));
            }
        }

        static void Time1(int[][] array)
        {
            int iter = array.Length;
            BigIntArray bigArray = new BigIntArray(max);
            Thread[] threads = new Thread[iter];
            RunnerThread2[] threadStates = new RunnerThread2[iter];

            for (int i = 0; i < iter; ++i)
            {
                threadStates[i] = new RunnerThread2(array[i], bigArray); 
                threads[i] = new Thread(new ThreadStart(threadStates[i].Run));
            }

            foreach (Thread t in threads)
            {
                t.Start();
            }
        }

        private class RunnerThread
        {
            private int[] array;
            private int[] bigarray;

            public RunnerThread(int[] a, int[] b)
            {
                array = a;
                bigarray = b;
            }

            public void Run()
            {
                long start = System.Environment.TickCount;
                foreach (int val in array)
                {
                    int x = bigarray[val];
                }

                long end = System.Environment.TickCount;
                Console.WriteLine("time: " + (end - start));
            }
        }

        static void Time2(int[][] array)
        {
            int iter = array.Length;
            int[] bigArray = new int[max];
            Thread[] threads = new Thread[iter];
            RunnerThread[] threadStates = new RunnerThread[iter];

            for (int i = 0; i < iter; ++i)
            {
                threadStates[i] = new RunnerThread(array[i], bigArray);
                threads[i] = new Thread(new ThreadStart(threadStates[i].Run));
            }

            foreach (Thread t in threads)
            {
                t.Start();
            }
        }

        public static void Main(string[] args)
        {
            int threadCount = 10;
            int numIter = 1000000;
            int[][] indexesPerThread = new int[threadCount][];
            for (int i = 0; i < threadCount; ++i)
            {
                indexesPerThread[i] = GetIndex(numIter);
            }

            Time1(indexesPerThread);

            //Time2(indexesPerThread);
        }

        [Test]
        public void TestMemory()
        {
            Main(new string[0]);
        }
    }
}
