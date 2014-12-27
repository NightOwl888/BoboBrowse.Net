// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Util
{
    using NUnit.Framework;
    using System;

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
            long start = System.Environment.TickCount;
            for (int i = 0; i < count; i++)
            {
                k = test.Get(i);
            }
            long end = System.Environment.TickCount;
            Console.WriteLine("Big array took: " + (end - start));

            start = System.Environment.TickCount;
            for (int i = 0; i < count; i++)
            {
                k = test2[i];
            }
            end = System.Environment.TickCount;
            Console.WriteLine("int[] took: " + (end - start));
        }
    }
}
