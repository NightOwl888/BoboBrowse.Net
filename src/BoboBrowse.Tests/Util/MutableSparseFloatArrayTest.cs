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
            try
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
            catch (Exception e)
            {
                Assert.Fail(e.ToString());
            }
        }
    }
}
