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
