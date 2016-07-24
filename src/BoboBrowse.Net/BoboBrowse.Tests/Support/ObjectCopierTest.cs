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
    using BoboBrowse.Net.Facets.Impl;
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
            var orig = new IntArray(7);
            orig.Array = new int[] { 5, 4, 3, 2, 1, 1, 2 };
            orig.Count = 4;
            orig.Growth = 6;
            orig.Len = 7;

            // Act
            var clone = (IntArray)orig.Clone();

            // Assert
            Assert.AreEqual(new int[] { 5, 4, 3, 2, 1, 1, 2 }, clone.Array);
            Assert.AreEqual(4, clone.Count);
            Assert.AreEqual(6, clone.Growth);
            Assert.AreEqual(7, clone.Len);
        }

        [Test]
        public void TestCloneBitSet()
        {
            // Arrange
            var orig = new BitSet(8);
            orig.Set(2);
            orig.Set(4);
            orig.Set(6);
            orig.Set(8);

            // Act
            var clone = (BitSet)orig.Clone();
            clone.Set(3);

            // Assert
            Assert.AreEqual(false, orig.Get(1));
            Assert.AreEqual(true, orig.Get(2));
            Assert.AreEqual(false, orig.Get(3));
            Assert.AreEqual(true, orig.Get(4));
            Assert.AreEqual(false, orig.Get(5));
            Assert.AreEqual(true, orig.Get(6));
            Assert.AreEqual(false, orig.Get(7));
            Assert.AreEqual(true, orig.Get(8));

            Assert.AreEqual(false, clone.Get(1));
            Assert.AreEqual(true, clone.Get(2));
            Assert.AreEqual(true, clone.Get(3));
            Assert.AreEqual(true, clone.Get(4));
            Assert.AreEqual(false, clone.Get(5));
            Assert.AreEqual(true, clone.Get(6));
            Assert.AreEqual(false, clone.Get(7));
            Assert.AreEqual(true, clone.Get(8));
        }

        [Test]
        public void TestCloneFacetSpec()
        {
            // Arrange
            var orig = new FacetSpec()
            {
                OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc,
                MinHitCount = 2,
                ExpandSelection = false,
                CustomComparatorFactory = null,
                Properties = new Dictionary<string, string>()
                {
                    { "foo", "bar" },
                    { "prop", "two" }
                }
            };
            

            // Act
            var clone = (FacetSpec)orig.Clone();
            clone.MinHitCount = 1;
            clone.Properties.Add("prop3", "args");

            // Assert
            Assert.AreEqual(FacetSpec.FacetSortSpec.OrderHitsDesc, orig.OrderBy);
            Assert.AreEqual(2, orig.MinHitCount);
            Assert.AreEqual(false, orig.ExpandSelection);
            Assert.AreEqual(null, orig.CustomComparatorFactory);
            Assert.AreEqual(2, orig.Properties.Count);
            Assert.AreEqual("bar", orig.Properties.Get("foo"));
            Assert.AreEqual("two", orig.Properties.Get("prop"));

            Assert.AreEqual(FacetSpec.FacetSortSpec.OrderHitsDesc, clone.OrderBy);
            Assert.AreEqual(1, clone.MinHitCount);
            Assert.AreEqual(false, clone.ExpandSelection);
            Assert.AreEqual(null, clone.CustomComparatorFactory);
            Assert.AreEqual(3, clone.Properties.Count);
            Assert.AreEqual("bar", clone.Properties.Get("foo"));
            Assert.AreEqual("two", clone.Properties.Get("prop"));
            Assert.AreEqual("args", clone.Properties.Get("prop3"));
        }
    }
}
