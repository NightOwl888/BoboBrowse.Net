﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
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
namespace BoboBrowse.Net.Util
{
    using Lucene.Net.Search;
    using NUnit.Framework;

    /// <summary>
    /// Test BigSegmentedArray
    /// author jko
    /// </summary>
    [TestFixture]
    public class BigSegmentedArrayTest
    {
        [Test]
        public void TestEmptyArray()
        {
            EmptyArrayTestHelper(new BigInt32Array(0));
            EmptyArrayTestHelper(new BigByteArray(0));
            EmptyArrayTestHelper(new BigInt16Array(0));
            EmptyArrayTestHelper(new LazyBigInt32Array(0));
        }

        private static void EmptyArrayTestHelper(BigSegmentedArray array)
        {
            Assert.AreEqual(0, array.Get(0));
            Assert.AreEqual(0, array.Length);
        }

        [Test]
        public void TestCountUp()
        {
            CountUpTestHelper(new BigInt32Array(short.MaxValue * 2));
            CountUpTestHelper(new LazyBigInt32Array(short.MaxValue * 2));
            CountUpTestHelper(new BigInt16Array(short.MaxValue * 2));
            CountUpTestHelper(new BigByteArray(short.MaxValue * 2));
        }

        private static void CountUpTestHelper(BigSegmentedArray array)
        {
            Initialize(array);
            Assert.AreEqual(short.MaxValue * 2, array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                Assert.AreEqual(i % array.MaxValue, array.Get(i));
            }
        }

        [Test]
        public void TestFindValues()
        {
            FindValueHelper(new BigInt32Array(short.MaxValue * 2));
            FindValueHelper(new LazyBigInt32Array(short.MaxValue * 2));
            FindValueHelper(new BigInt16Array(short.MaxValue * 2));
            FindValueHelper(new BigByteArray(short.MaxValue * 2));
        }

        private static void FindValueHelper(BigSegmentedArray array)
        {
            int a = array.MaxValue / 16;
            int b = a * 2;
            int c = a * 3;

            array.Add(1000, a);
            array.Add(2000, b);
            Assert.AreEqual(1000, array.FindValue(a, 0, 2000));
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, array.FindValue(a, 1001, 2000));
            Assert.AreEqual(2000, array.FindValue(b, 2000, 3000));

            array.Fill(c);
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, array.FindValue(b, 2000, 3000));
            Assert.AreEqual(4000, array.FindValue(c, 4000, 4000));
        }

        [Test]
        public void TestFindValueRange()
        {
            FindValueRangeHelper(new BigInt32Array(short.MaxValue * 2));
            FindValueRangeHelper(new LazyBigInt32Array(short.MaxValue * 2));
            FindValueRangeHelper(new BigInt16Array(short.MaxValue * 2));
            FindValueRangeHelper(new BigByteArray(short.MaxValue * 2));
        }

        private static void FindValueRangeHelper(BigSegmentedArray array)
        {
            int a = array.MaxValue / 16;
            int b = a * 2;
            int d = a * 4;
            int e = a * 5;

            array.Add(10000, b);
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, array.FindValueRange(d, e, 0, array.Length));
            Assert.AreEqual(10000, array.FindValueRange(a, e, 0, array.Length));
            Assert.AreEqual(10000, array.FindValueRange(a, e, 10000, array.Length));
            Assert.AreEqual(10000, array.FindValueRange(a, e, 0, 10000));

            Assert.AreEqual(10000, array.FindValueRange(a, b, 9000, 10100));
            Assert.AreEqual(10000, array.FindValueRange(b, e, 9000, 10000));
            Assert.AreEqual(10000, array.FindValueRange(b, b, 9000, 10000));
        }

        [Test]
        public void TestFill()
        {
            FillTestHelper(new BigInt32Array(short.MaxValue << 1));
            FillTestHelper(new LazyBigInt32Array(short.MaxValue << 1));
            FillTestHelper(new BigInt16Array(short.MaxValue << 1));
            FillTestHelper(new BigByteArray(short.MaxValue << 1));
        }

        private static void FillTestHelper(BigSegmentedArray array)
        {
            int a = array.MaxValue / 4;
            int b = array.MaxValue / 2;
            int c = array.MaxValue - 1;

            Assert.AreEqual(0, array.Get(20000));

            array.Fill(a);
            Assert.AreEqual(a, array.Get(20000));

            array.Add(20000, b);
            Assert.AreEqual(b, array.Get(20000));
            Assert.AreEqual(a, array.Get(20001));

            Assert.AreEqual(20000, array.FindValue(b, 0, 21000));

            array.Fill(c);
            Assert.AreEqual(c, array.Get(20000));
            Assert.AreEqual(c, array.Get(40000));
            Assert.AreEqual(c, array.Get(0));
        }

        public static BigSegmentedArray Initialize(BigSegmentedArray array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array.Add(i, i % array.MaxValue);
            }
            return array;
        }
    }
}
