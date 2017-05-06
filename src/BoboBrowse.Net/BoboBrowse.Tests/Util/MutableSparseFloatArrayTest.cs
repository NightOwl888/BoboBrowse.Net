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

    /**
     * @author spackle
     *
     */
    [TestFixture]
    public class MutableSparseFloatArrayTest
    {
        //private const long SEED = -7862018348108294439L;

        [Test]
        public void TestMute()
        {
            var rand = new Random();

            float[] orig = new float[1024];
            MutableSparseSingleArray fromEmpty = new MutableSparseSingleArray(new float[1024]);
			float density = 0.2f;
			int idx = 0;
			while (rand.NextDouble() > density) {
				idx++;
			}
			while (idx < orig.Length) 
            {
				float val = (float)rand.NextDouble();
				orig[idx] = val;
				fromEmpty.Set(idx, val);
				idx += 1;
				while (rand.NextDouble() > density) {
					idx++;
				}
			}

            float[] copy =new float[orig.Length]; 
            Array.Copy(orig, 0, copy, 0, orig.Length);
			MutableSparseSingleArray fromPartial = new MutableSparseSingleArray(copy);

            // do 128 modifications
			int mods = 128;
			for (int i = 0; i < mods; i++) {
				float val = (float)rand.NextDouble();
				idx = rand.Next(orig.Length);
				orig[idx] = val;
				fromEmpty.Set(idx, val);
				fromPartial.Set(idx, val);				
			}

            for (int i = 0; i < orig.Length; i++)
            {
                Assert.True(orig[i] == fromEmpty.Get(i), "orig " + orig[i] + " wasn't the same as fromEmpty " + fromEmpty.Get(i) + " at i=" + i);
                Assert.True(orig[i] == fromPartial.Get(i), "orig " + orig[i] + " wasn't the same as fromPartial " + fromPartial.Get(i) + " at i=" + i);
            }

            Console.WriteLine("success!");
        }

        [Test]
        [Category("LongRunning")]
        public void TestSpeed()
        {
            Random r = new Random();

            float[] orig = new float[16 * 1024 * 1024];
            MutableSparseSingleArray arr = new MutableSparseSingleArray(new float[orig.Length]);

            for (int i = 0; i < 32 * 1024; i++)
            {
                int idx = r.Next(orig.Length);
                if (r.Next(1000) % 2 == 0)
                {
                    Assert.True(orig[idx] == arr.Get(idx), "orig " + orig[idx] + " not the same as arr " + arr.Get(idx) + " at idx=" + idx);
                }
                else
                {
                    float val = (float)r.NextDouble();
                    orig[idx] = val;
                    arr.Set(idx, val);
                }
            }

            // repeat it, but timed
            orig = new float[orig.Length];
            arr = new MutableSparseSingleArray(new float[orig.Length]);
            int[] idxs = new int[1024 * 1024];
            float[] vals = new float[idxs.Length];
            for (int i = 0; i < idxs.Length; i++)
            {
                idxs[i] = r.Next(orig.Length);
                vals[i] = (float)r.NextDouble();
            }

            long markTime = System.Environment.TickCount;
            for (int i = 0; i < idxs.Length; i++)
            {
                orig[i] = vals[i];
            }
            long elapsedTimePrim = System.Environment.TickCount - markTime;

            markTime = System.Environment.TickCount;
            for (int i = 0; i < idxs.Length; i++)
            {
                arr.Set(idxs[i], vals[i]);
            }
            long elapsedTimeMutable = System.Environment.TickCount - markTime;

            Console.WriteLine("elapsed time on the primitive array: " + elapsedTimePrim 
                + "; elapsed time on the mutable condensed arr: " + elapsedTimeMutable);
            Console.WriteLine("ratio of time to do it on the mutable condensed arr, to time on primitive array: " 
                + (double)elapsedTimeMutable / elapsedTimePrim);
        }
    }
}
