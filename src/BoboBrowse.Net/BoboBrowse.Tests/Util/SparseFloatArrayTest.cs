/**
 * Bobo Browse Engine - High performance faceted/parametric search implementation 
 * that handles various types of semi-structured data.  Written in Java.
 * 
 * Copyright (C) 2005-2006  Spackle
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * 
 * To contact the project administrators for the bobo-browse project, 
 * please go to https://sourceforge.net/projects/bobo-browse/, or 
 * send mail to owner@browseengine.com.
 */

// Version compatibility level: 3.2.0
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
            Console.WriteLine("float array with " + count + " out of " + orig.Length + " non-zero values");

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
