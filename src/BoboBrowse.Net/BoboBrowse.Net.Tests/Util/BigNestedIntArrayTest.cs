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
namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.Support;
    using Lucene.Net.Support;
    using Lucene.Net.Util;
    using NUnit.Framework;
    using System;

    /// <summary>
    /// Author: ymatsuda
    /// </summary>
    [TestFixture]
    public class BigNestedIntArrayTest
    {
        [Test]
        public void TestBasic()
        {
            int maxId = 3000;
            int[] count = new int[maxId];
            var loader = new BigNestedInt32Array.BufferedLoader(maxId);
            for (int id = 0; id < maxId; id++)
            {
                for (int val = 0; val < 2000; val += (id + 1))
                {
                    if (loader.Add(id, val)) count[id]++;
                }
            }
            var nestedArray = new BigNestedInt32Array();
            nestedArray.Load(maxId, loader);

            int[] buf = new int[1024];
            for (int id = 0; id < maxId; id++)
            {
                int cnt = nestedArray.GetData(id, buf);
                Assert.AreEqual(count[id], cnt, "item count");

                if (cnt > 0)
                {
                    int val = 0;
                    for (int i = 0; i < cnt; i++)
                    {
                        Assert.AreEqual(val, buf[i], "item[" + i.ToString() + "]");
                        val += (id + 1);
                    }
                }
            }
        }

        [Test]
        public void TestSparseIds()
        {
            int maxId = 100000;
            int[] count = new int[maxId];
            var loader = new BigNestedInt32Array.BufferedLoader(maxId);
            for (int id = 0; id < maxId; id += ((id >> 2) + 1))
            {
                for (int val = 0; val < 3000; val += (id + 1))
                {
                    if (loader.Add(id, val)) count[id]++;
                }
            }
            var nestedArray = new BigNestedInt32Array();
            nestedArray.Load(maxId, loader);

            int[] buf = new int[1024];
            for (int id = 0; id < maxId; id++)
            {
                int cnt = nestedArray.GetData(id, buf);
                Assert.AreEqual(count[id], cnt, "item count");

                if (cnt > 0)
                {
                    int val = 0;
                    for (int i = 0; i < cnt; i++)
                    {
                        Assert.AreEqual(val, buf[i], "item[" + i + "]");
                        val += (id + 1);
                    }
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public void TestBufferedLoaderReuse()
        {
            int maxId = 5000;
            int[] maxNumItems = { 25, 50, 20, 100, 15, 500, 10, 1000, 5, 2000, 2 };
            int[,] count = new int[maxNumItems.Length, maxId];
            var buffer = new BigInt32Buffer();
            var loader = new BigNestedInt32Array.BufferedLoader(maxId, BigNestedInt32Array.MAX_ITEMS, buffer);
            var nestedArray = new BigNestedInt32Array[maxNumItems.Length];

            for (int i = 0; i < maxNumItems.Length; i++)
            {
                for(int id = 0; id < maxId; id++)
                {
                    int cnt = id % (maxNumItems[i] + 1);
                    for(int val = 0; val < cnt; val++)
                    {
                        if(loader.Add(id, val)) count[i, id]++;
                    }
                }
                nestedArray[i] = new BigNestedInt32Array();
                nestedArray[i].Load(maxId, loader);
      
                loader.Reset(maxId, BigNestedInt32Array.MAX_ITEMS, buffer);
            }

            for (int i = 0; i < maxNumItems.Length; i++)
            {
                int[] buf = new int[1024];
                for(int id = 0; id < maxId; id++)
                {
                    int cnt = nestedArray[i].GetData(id, buf);
                    Assert.AreEqual(count[i, id], cnt, "count[" + i + "," + id + "]");
      
                    if(cnt > 0)
                    {
                        for(int val = 0; val < cnt; val++)
                        {
                            Assert.AreEqual(val, buf[val], "item[" + i + "," + id + "," + val + "]");
                        }
                    }
                }
            }
        }

        [Test]
        public void TestMemoryReuse()
        {
            int maxId = 4096;
            int[] maxNumItems = { 1, 1, 2, 2, 3, 3, 3, 3, 1, 1 };
            int[] minNumItems = { 1, 1, 0, 1, 0, 0, 2, 3, 1, 0 };
            int[] count = new int[maxId];
            BigInt32Buffer buffer = new BigInt32Buffer();
            BigNestedInt32Array.BufferedLoader loader = null;
            BigNestedInt32Array nestedArray = new BigNestedInt32Array();
            Random rand = new Random();

            for (int i = 0; i < maxNumItems.Length; i++)
            {
                loader = new BigNestedInt32Array.BufferedLoader(maxId, BigNestedInt32Array.MAX_ITEMS, buffer);
                for (int id = 0; id < maxId; id++)
                {
                    count[id] = 0;
                    int cnt = Math.Max(rand.Next(maxNumItems[i] + 1), minNumItems[i]);
                    for (int val = 0; val < cnt; val++)
                    {
                        if (loader.Add(id, val)) count[id]++;
                    }
                }

                nestedArray.Load(maxId, loader);

                int[] buf = new int[1024];
                for (int id = 0; id < maxId; id++)
                {
                    int cnt = nestedArray.GetData(id, buf);
                    Assert.AreEqual(count[id], cnt, "count[" + i + "," + id + "]");

                    if (cnt > 0)
                    {
                        for (int val = 0; val < cnt; val++)
                        {
                            Assert.AreEqual(val, buf[val], "item[" + i + "," + id + "," + val + "]");
                        }
                    }
                }

                if (i == 0)
                {
                    maxId = maxId * 2;
                    count = new int[maxId];
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public void TestAllocThenAddData()
        {
            int maxId = 5000;
            int[] maxNumItems = { 25, 50, 20, 100, 15, 500, 10, 1000, 5, 1024, 2 };
            int[,] count = new int[maxNumItems.Length, maxId];
            AllocOnlyTestLoader loader = new AllocOnlyTestLoader(maxId);
            BigNestedInt32Array[] nestedArray = new BigNestedInt32Array[maxNumItems.Length];

            for(int i = 0 ; i < maxNumItems.Length; i++)
            {
                for(int id = 0; id < maxId; id++)
                {
                    int cnt = id % (maxNumItems[i] + 1);
                    loader.AddSize(id, cnt);
                    count[i, id] = cnt;
                }
                nestedArray[i] = new BigNestedInt32Array();
                nestedArray[i].Load(maxId, loader);
                loader.Reset();

                for(int id = 0; id < maxId; id++)
                {
                    for(int data = 0; data < count[i, id]; data++)
                    {
                        nestedArray[i].AddData(id, data);
                    }
                }
            }

            for(int i = 0 ; i < maxNumItems.Length; i++)
            {
                int[] buf = new int[1024];
                for(int id = 0; id < maxId; id++)
                {
                    int cnt = nestedArray[i].GetData(id, buf);
                    Assert.AreEqual(count[i, id], cnt, "count[" + i + "," + id + "]");

                    if(cnt > 0)
                    {
                        for(int val = 0; val < cnt; val++)
                        {
                            Assert.AreEqual(val, buf[val], "item[" + i + "," + id + "," + val + "]");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A loader that allocate data storage without loading data to BigNestedIntArray.
        /// Note that this loader supports only non-negative integer data.
        /// </summary>
        public sealed class AllocOnlyTestLoader : BigNestedInt32Array.Loader
        {
            private readonly int[] _maxNumItems;

            public AllocOnlyTestLoader(int maxdoc)
            {
                _maxNumItems = new int[maxdoc];
            }

            public void AddSize(int docid, int size)
            {
                _maxNumItems[docid] = size;
            }

            public void Reset()
            {
                Arrays.Fill(_maxNumItems, 0);
            }

            public override void Load()
            {
                for (int i = 0; i < _maxNumItems.Length; i++)
                {
                    if (_maxNumItems[i] > 0)
                    {
                        Allocate(i, _maxNumItems[i], true);
                    }
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public void TestMaxItems()
        {
            int maxId = 5000;
            int[] maxNumItems = { 25, 50, 20, 100, 15, 500, 10, 1000, 5, 1024, 2 };
            int[,] count = new int[maxNumItems.Length, maxId];
            AllocOnlyTestLoader loader = new AllocOnlyTestLoader(maxId);
            BigNestedInt32Array[] nestedArray = new BigNestedInt32Array[maxNumItems.Length];
    
            for(int i = 0 ; i < maxNumItems.Length; i++)
            {
                for(int id = 0; id < maxId; id++)
                {
                    int cnt = id % 2000;
                    loader.AddSize(id, cnt);
                    count[i, id] = cnt;
                }
                nestedArray[i] = new BigNestedInt32Array();
                nestedArray[i].MaxItems = maxNumItems[i];
                nestedArray[i].Load(maxId, loader);
                loader.Reset();

                for(int id = 0; id < maxId; id++)
                {
                    bool failed = false;
                    for(int data = 0; data < count[i, id]; data++)
                    {
                        if(nestedArray[i].AddData(id, data))
                        {
                            if(!failed && (data + 1 > maxNumItems[i]))
                            {
                                failed = true;
                                Assert.AreEqual(data, maxNumItems[i], "maxItems");
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public void TestCountNoReturnWithFilter()
        {
            int maxId = 20;
            int numVals = 10;
            int[] count = new int[numVals];

            var loader = new BigNestedInt32Array.BufferedLoader(maxId);
            for (int val = 0; val < numVals; val++)
            {
                for (int i = 0; i < maxId - val; i++)
                {
                    loader.Add(i, val);
                }
            }

            BigNestedInt32Array nestedArray = new BigNestedInt32Array();
            nestedArray.Load(maxId, loader);

            OpenBitSet filter = new OpenBitSet(numVals);
            for (int i = 0; i < numVals; i++)
            {
                if (i % 2 == 0)
                {
                    filter.Set(i);
                }
            }

            for (int i = 0; i < maxId; i++)
            {
                nestedArray.CountNoReturnWithFilter(i, count, filter);
            }

            for (int i = 0; i < numVals; i++)
            {
                if (i % 2 == 0)
                {
                    Assert.True(count[i] == maxId - i);
                }
                else
                {
                    Assert.True(count[i] == 0);
                }
            }
            return;
        }
    }
}
