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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Data
{
    using BoboBrowse.Net.Support;
    using NUnit.Framework;

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
