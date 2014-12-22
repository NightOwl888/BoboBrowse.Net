// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Support;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    [TestFixture]
    public class TermLongListTest
    {
        [Test]
        public void Test1TwoNegativeValues()
        {
            TermLongList list = new TermLongList();
            list.Add(null);
            list.Add("-1");
            list.Add("-2");
            list.Add("0");
            list.Add("1");

            list.Seal();
            Assert.True(Arrays.Equals(new long[] { 0, -2, -1, 0, 1 }, list.Elements));
        }

        [Test]
        public void Test2ThreeNegativeValues()
        {
            TermLongList list = new TermLongList();
            list.Add(null);
            list.Add("-1");
            list.Add("-2");
            list.Add("-3");
            list.Add("0");
            list.Add("1");

            list.Seal();
            Assert.True(Arrays.Equals(new long[] { 0, -3, -2, -1, 0, 1 }, list.Elements));
        }

        [Test]
        public void Test2aThreeNegativeValuesInt()
        {
            TermIntList list = new TermIntList();
            list.Add(null);
            list.Add("-1");
            list.Add("-2");
            list.Add("-3");
            list.Add("0");
            list.Add("1");

            list.Seal();
            Assert.True(Arrays.Equals(new int[] { 0, -3, -2, -1, 0, 1 }, list.Elements));
        }

        [Test]
        public void Test2bThreeNegativeValuesShort()
        {
            TermShortList list = new TermShortList();
            list.Add(null);
            list.Add("-1");
            list.Add("-2");
            list.Add("-3");
            list.Add("0");
            list.Add("1");

            list.Seal();
            Assert.True(Arrays.Equals(new short[] { 0, -3, -2, -1, 0, 1 }, list.Elements));
        }

        public void Test3ThreeNegativeValuesWithoutDummy()
        {
            TermLongList list = new TermLongList();

            list.Add("-1");
            list.Add("-2");
            list.Add("-3");
            list.Add("0");
            list.Add("1");

            list.Seal();
            Assert.True(Arrays.Equals(new long[] { -3, -2, -1, 0, 1 }, list.Elements));
        }
    }
}
