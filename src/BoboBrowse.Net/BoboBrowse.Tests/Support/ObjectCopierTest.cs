//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
//*
//* Copyright (C) 2015  Shad Storhaug
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

namespace BoboBrowse.Net.Support
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.DocIdSet;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;

    [TestFixture]
    public class ObjectCopierTest
    {
        [Serializable]
        private class Clonable1 : ICloneable
        {
            [NonSerialized]
            protected int growth;

            public Clonable1(object array, int count, int growth, int len)
            {
                this.Array = array;
                this.Count = count;
                this.Growth = growth;
                this.Len = len;
            }

            protected internal object Array { get; set; }

            protected internal int Count { get; set; }

            protected internal int Growth 
            {
                get { return this.growth; }
                set { this.growth = value; }
            }

            protected internal int Len { get; set; }

            public object Clone()
            {
                return ObjectCopier.Clone(this);
            }
        }

        [Test]
        public void TestCloneWithNonSerialized()
        {
            // Arrange
            var array = new Clonable1(new int[] { 5, 4, 3, 2, 1, 1, 2 }, 4, 6, 7);

            // Act
            var clone = (Clonable1)array.Clone();

            // Assert
            Assert.AreEqual(new int[] { 5, 4, 3, 2, 1, 1, 2 }, clone.Array);
            Assert.AreEqual(4, clone.Count);
            Assert.AreEqual(0, clone.Growth);
            Assert.AreEqual(7, clone.Len);
        }

        [Test]
        public void TestCloneIntArray()
        {
            // Arrange
            var array = new IntArray(7);
            array.Array = new int[] { 5, 4, 3, 2, 1, 1, 2 };
            array.Count = 4;
            array.Growth = 6;
            array.Len = 7;

            // Act
            var clone = (IntArray)array.Clone();

            // Assert
            Assert.AreEqual(new int[] { 5, 4, 3, 2, 1, 1, 2 }, clone.Array);
            Assert.AreEqual(4, clone.Count);
            Assert.AreEqual(6, clone.Growth);
            Assert.AreEqual(7, clone.Len);
        }
    }
}
