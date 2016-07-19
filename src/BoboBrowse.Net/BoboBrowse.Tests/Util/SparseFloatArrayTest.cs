//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
//*
//* Copyright (C) 2005-2015  Spackle
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
namespace BoboBrowse.Net.Util
{
    using NUnit.Framework;
    using System;

    /// <summary>
    /// author spackle
    /// </summary>
    [TestFixture]
    public class SparseFloatArrayTest
    {
        //private const long SEED = -1587797429870936371L;

        [Test]
        [Category("LongRunning")]
        public void TestSpeed()
        {
            float[] orig = new float[32 * 1024 * 1024];
            float density = 0.4f;
            var rand = new Random();
            int idx = 0;
            while ((float)rand.NextDouble() > density)
            {
                idx++;
            }
            int count = 0;
            while (idx < orig.Length)
            {
                orig[idx] = (float)rand.NextDouble();
                count++;
                idx += 1;
                while ((float)rand.NextDouble() > density)
                {
                    idx++;
                }
            }
            Assert.True(count > 100 && count < orig.Length / 2, "count was bad: " + count);
            Console.WriteLine("float array with " + count + " out of " + orig.Length 
                + " non-zero values");

            var sparse = new SparseFloatArray(orig);

            for (int i = 0; i < orig.Length; i++)
            {
                float o = orig[i];
                float s = sparse.Get(i);
                Assert.True(o == s, "orig " + o + " wasn't the same as sparse: " + s + " for i = " + i);
            }
            // things came out correct

            long markTime = System.Environment.TickCount;
            for (int i = 0; i < orig.Length; i++)
            {
                float f = orig[i];
            }
            long elapsedTimeOrig = System.Environment.TickCount - markTime;

            markTime = System.Environment.TickCount;
            for (int i = 0; i < orig.Length; i++)
            {
                float f = sparse.Get(i);
            }
            long elapsedTimeSparse = System.Environment.TickCount - markTime;

            double ratio = (double)elapsedTimeSparse / (double)elapsedTimeOrig;
            Console.Write("fyi on speed, direct array access took ");
            Console.Write(elapsedTimeOrig);
            Console.Write(" millis, while sparse float access took ");
            Console.Write(elapsedTimeSparse);
            Console.Write("; that's a ");
            Console.Write(ratio);
            Console.Write(" X slowdown by using the condensed memory model (smaller number is better)");
            Console.WriteLine();
            Console.WriteLine(" success!");
        }
    }
}
