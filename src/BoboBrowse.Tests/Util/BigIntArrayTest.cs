namespace BoboBrowse.Net.Util
{
    using NUnit.Framework;
    using System;
    using System.Diagnostics;

    [TestFixture]
    public class BigIntArrayTest
    {
        [Test]
        public void TestBigIntArray()
        {
            int count = 5000000;
            var test = new BigIntArray(count);
            var test2 = new int[count];
            for (int i = 0; i < count; i++)
            {
                test.Add(i, i);
                test2[i] = i;
            }

            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(0, test.Get(0));
            }

            int k = 0;
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < count; i++)
            {
                k = test.Get(i);
            }
            sw.Stop();
            Console.WriteLine("Big array took: " + sw.ElapsedMilliseconds.ToString());

            sw.Reset();
            sw.Start();
            for (int i = 0; i < count; i++)
            {
                k = test2[i];
            }
            sw.Stop();
            Console.WriteLine("int[] took: " + sw.ElapsedMilliseconds.ToString());
        }
    }
}
