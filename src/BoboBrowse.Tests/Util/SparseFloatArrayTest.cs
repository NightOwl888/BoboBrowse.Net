namespace BoboBrowse.Net.Util
{
    using NUnit.Framework;
    using System;
    using System.Diagnostics;

    [TestFixture]
    public class SparseFloatArrayTest
    {
        //private const long SEED = -1587797429870936371L;

        [Test]
        public void TestSpeed()
        {
            try
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

                var sw = new Stopwatch();
                sw.Start();

                for (int i = 0; i < orig.Length; i++)
                {
                    float f = orig[i];
                }

                sw.Stop();
                long elapsedTimeOrig = sw.ElapsedMilliseconds;

                sw.Reset();
                sw.Start();
                for (int i = 0; i < orig.Length; i++)
                {
                    float f = sparse.Get(i);
                }
                sw.Stop();
                long elapsedTimeSparse = sw.ElapsedMilliseconds;

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
            catch (Exception e)
            {
                Assert.Fail(e.ToString());
            }
        }
    }
}
