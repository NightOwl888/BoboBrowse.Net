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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Util;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class BoboFacetIteratorTest
    {
        [SetUp]
        public void Init()
        {
        }

        [TearDown]
        public void Dispose()
        {
        }

        [Test]
        public void TestTermStringListAddWrongOrder()
        {
            TermStringList tsl1 = new TermStringList();
            tsl1.Add(null);
            tsl1.Add("m");
            try
            {
                tsl1.Add("a");
            }
            catch (Exception e)
            {
                Assert.True(e.Message.Contains("ascending order"), "There should be an exception and the message contains ascending order");
                return;
            }
            Assert.Fail("There should be an exception and the message contains ascending order");
        }

        [Test]
        public void TestTermStringListAddCorrectOrder()
        {
            TermStringList tsl1 = new TermStringList();
            tsl1.Add(null);
            tsl1.Add("");
            try
            {
                tsl1.Add("m");
                tsl1.Add("s");
                tsl1.Add("t");
            }
            catch (Exception e)
            {
                Assert.False(e.Message.Contains("ascending order"), "There should NOT be an exception and the message contains ascending order");
                return;
            }
            tsl1.Seal();
            Assert.AreEqual(1, tsl1.IndexOf(""), "Should skip index 0 which is used for dummy null");
        }

        [Test]
        public void TestTermIntListAddCorrectOrder()
        {
            TermInt32List tsl1 = new TermInt32List("000");
            tsl1.Add(null);
            tsl1.Add("0");
            try
            {
                tsl1.Add("1");
                tsl1.Add("2");
                tsl1.Add("3");
            }
            catch (Exception e)
            {
                Assert.False(e.Message.Contains("ascending order"), "There should NOT be an exception and the message contains ascending order");
                return;
            }
            tsl1.Seal();
            Assert.AreEqual(1, tsl1.IndexOf(0), "Should skip index 0 which is used for dummy null");
        }

        [Test]
        public void TestDefaultFacetIterator()
        {
            TermStringList tsl1 = new TermStringList();
            tsl1.Add("i");
            tsl1.Add("m");
            tsl1.Seal();
            BigInt32Array count = new BigInt32Array(2);
            count.Add(0, 1);
            count.Add(1, 2);
            DefaultFacetIterator itr1 = new DefaultFacetIterator(tsl1, count, 2, false);
            TermStringList tsl2 = new TermStringList();
            tsl2.Add("i");
            tsl2.Add("m");
            tsl2.Seal();
            BigInt32Array count2 = new BigInt32Array(2);
            count2.Add(0, 1);
            count2.Add(1, 5);
            DefaultFacetIterator itr2 = new DefaultFacetIterator(tsl2, count2, 2, true);
            List<FacetIterator> list = new List<FacetIterator>();
            list.Add(itr1);
            list.Add(itr2);
            CombinedFacetIterator ctr = new CombinedFacetIterator(list);
            string result = "";
            while (ctr.HasNext())
            {
                ctr.Next();
                result += ctr.Facet;
                result += ctr.Count;
            }
            Assert.AreEqual("i1m7", result, "result should be i1m7");
        }

        [Test]
        public void TestDefaultIntFacetIterator()
        {
            string format = "00";
            List<Int32FacetIterator> list = new List<Int32FacetIterator>();
            for (int seg = 0; seg < 5; seg++)
            {
                TermInt32List tsl1 = new TermInt32List(format);
                int limit = 25;
                BigInt32Array count = new BigInt32Array(limit);
                string[] terms = new string[limit];
                for (int i = limit - 1; i >= 0; i--)
                {
                    terms[i] = i.ToString(format);
                }
                Array.Sort(terms);
                for (int i = 0; i < limit; i++)
                {
                    tsl1.Add(terms[i]);
                    count.Add(i, i);
                }
                tsl1.Seal();
                DefaultInt32FacetIterator itr1 = new DefaultInt32FacetIterator(tsl1, count, limit, true);
                list.Add(itr1);
            }
            CombinedInt32FacetIterator ctr = new CombinedInt32FacetIterator(list);
            string result = "";
            while (ctr.HasNext())
            {
                ctr.Next();
                result += (ctr.Facet + ":" + ctr.Count + " ");
            }
            string expected = "1:5 2:10 3:15 4:20 5:25 6:30 7:35 8:40 9:45 10:50 11:55 12:60 13:65 14:70 15:75 16:80 17:85 18:90 19:95 20:100 21:105 22:110 23:115 24:120 ";
            Assert.AreEqual(expected, result);
        }
    }
}
