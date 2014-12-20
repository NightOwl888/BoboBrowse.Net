/**
 * Bobo Browse Engine - High performance faceted/parametric search implementation 
 * that handles various types of semi-structured data.  Written in Java.
 * 
 * Copyright (C) 2005-2006  spackle
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
 * contact owner@browseengine.com.
 */

// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Util
{
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
            MutableSparseFloatArray fromEmpty = new MutableSparseFloatArray(new float[1024]);
			float density = 0.2f;
			int idx = 0;
			while (rand.NextDouble() > density) {
				idx++;
			}
            int count = 0;
			while (idx < orig.Length) 
            {
				float val = (float)rand.NextDouble();
				orig[idx] = val;
				fromEmpty.Set(idx, val);
				count++;
				idx += 1;
				while (rand.NextDouble() > density) {
					idx++;
				}
			}

            float[] copy =new float[orig.Length]; 
            Array.Copy(orig, 0, copy, 0, orig.Length);
			MutableSparseFloatArray fromPartial = new MutableSparseFloatArray(copy);

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
        public void TestSpeed()
        {
            Random r = new Random();

            float[] orig = new float[16 * 1024 * 1024];
            MutableSparseFloatArray arr = new MutableSparseFloatArray(new float[orig.Length]);

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
            arr = new MutableSparseFloatArray(new float[orig.Length]);
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

            Console.WriteLine("elapsed time on the primitive array: " + elapsedTimePrim + "; elapsed time on the mutable condensed arr: " + elapsedTimeMutable);
            Console.WriteLine("ratio of time to do it on the mutable condensed arr, to time on primitive array: " + (double)elapsedTimeMutable / elapsedTimePrim);
        }
    }
}
