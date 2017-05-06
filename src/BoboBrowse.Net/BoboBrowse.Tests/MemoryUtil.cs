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

        private class RunnerThread
        {
            private readonly int[] array;
            private readonly BigInt32Array bigarray;

            public RunnerThread(int[] a, BigInt32Array b)
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
            BigInt32Array bigArray = new BigInt32Array(max);
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
        }

        [Test]
        public void TestMemory()
        {
            Main(new string[0]);
        }
    }
}
