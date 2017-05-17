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
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Index;
    using BoboBrowse.Net.Index.Digest;
    using BoboBrowse.Net.Query;
    using BoboBrowse.Net.Query.Scoring;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Analysis;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Analysis.TokenAttributes;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using Lucene.Net.Support;
    using Lucene.Net.Util;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
#if FEATURE_SERIALIZABLE
    using System.Runtime.Serialization.Formatters.Binary;
#endif
    using System.Text;

    [TestFixture]
    public class BoboTestCase
    {
        //private static readonly ILog log = LogProvider.For<BoboTestCase>(); // NOT USED
        private Lucene.Net.Store.Directory _indexDir;
        private ICollection<IFacetHandler> _fconf;
        private static Term tagSizePayloadTerm = new Term("tagSizePayload", "size");

        private class TestDataDigester : DataDigester
        {
            private readonly Document[] _data;

            public TestDataDigester(IEnumerable<IFacetHandler> fConf, Document[] data)
                : base()
            {
                _data = data;
            }

            public override void Digest(IDataHandler handler)
            {
                foreach (var dataItem in this._data)
                {
                    handler.HandleDocument(dataItem);
                }
            }
        }

        [SetUp]
        public void Init()
        {
            //string confdir = System.getProperty("conf.dir");
            //if (confdir == null) confdir = "./resource";
            //System.setProperty("log.home", ".");
            //org.apache.log4j.PropertyConfigurator.configure(confdir + "/log4j.properties");
            this._fconf = BuildFieldConf();
            this._indexDir = CreateIndex();
        }

        [TearDown]
        public void Dispose()
        {
            this._fconf = null;
            this._indexDir = null;
        }

        private BoboMultiReader NewIndexReader()
        {
            DirectoryReader srcReader = DirectoryReader.Open(_indexDir);
            return BoboMultiReader.GetInstance(srcReader, this._fconf);
        }

        private BoboBrowser NewBrowser()
        {
            return new BoboBrowser(NewIndexReader());
        }

        public static Field BuildMetaField(string name, string val)
        {
            Field f = new StringField(name, val, Field.Store.NO);
            return f;
        }

        // From Bobo 3.1.0 MetaTokenStream class
        private sealed class MetaSizeTokenStream : TokenStream
        {
            private bool returnToken = false;
            private readonly IPayloadAttribute payloadAttr;
            private readonly ICharTermAttribute termAttr;

            public MetaSizeTokenStream(Term term, int size)
            {
                byte[] buffer = new byte[4];
                buffer[0] = (byte)(size);
                buffer[1] = (byte)(size >> 8);
                buffer[2] = (byte)(size >> 16);
                buffer[3] = (byte)(size >> 24);

                payloadAttr = base.AddAttribute<IPayloadAttribute>();
                payloadAttr.Payload = new BytesRef(buffer);
                termAttr = base.AddAttribute<ICharTermAttribute>();
                termAttr.Append(term.Text());

                returnToken = true;
            }

            public override bool IncrementToken()
            {
                if (returnToken)
                {
                    returnToken = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static Field BuildMetaSizePayloadField(Term term, int size)
        {
            Field f = new TextField(term.Field, new MetaSizeTokenStream(term, size));
            return f;
        }

        public static Document[] BuildData()
        {
            var dataList = new List<Document>();

            Document d1 = new Document();
            d1.Add(BuildMetaField("id", "1"));
            d1.Add(BuildMetaField("shape", "square"));
            d1.Add(BuildMetaField("color", "red"));
            d1.Add(BuildMetaField("size", "4"));
            d1.Add(BuildMetaField("location", "toy/lego/block/"));
            d1.Add(BuildMetaField("tag", "rabbit"));
            d1.Add(BuildMetaField("tag", "pet"));
            d1.Add(BuildMetaField("tag", "animal"));
            d1.Add(BuildMetaSizePayloadField(tagSizePayloadTerm, 3));
            d1.Add(BuildMetaField("number", "0010"));
            d1.Add(BuildMetaField("date", "2000/01/01"));
            d1.Add(BuildMetaField("name", "ken"));
            d1.Add(BuildMetaField("char", "k"));
            d1.Add(BuildMetaField("multinum", "001"));
            d1.Add(BuildMetaField("multinum", "003"));
            d1.Add(BuildMetaField("multiwithweight", "cool\u0000200"));
            d1.Add(BuildMetaField("multiwithweight", "good\u0000100"));
            d1.Add(BuildMetaField("compactnum", "001"));
            d1.Add(BuildMetaField("compactnum", "003"));
            d1.Add(BuildMetaField("numendorsers", "000003"));
            d1.Add(BuildMetaField("path", "a-b"));
            d1.Add(BuildMetaField("multipath", "a-b"));
            d1.Add(BuildMetaField("custom", "000003"));
            d1.Add(BuildMetaField("latitude", "60"));
            d1.Add(BuildMetaField("longitude", "120"));
            d1.Add(BuildMetaField("salary", "04500"));

            Field sf = new StringField("testStored", "stored", Field.Store.YES);
            d1.Add(sf);

            FieldType ft = new FieldType();
            ft.IsStored = (false);
            ft.IsIndexed = (true);
            ft.IsTokenized = (true);
            ft.StoreTermVectors = (true);
            ft.StoreTermVectorPositions = (false);
            ft.StoreTermVectorOffsets = (false);
            Field tvf = new Field("tv", "bobo bobo lucene lucene lucene test", ft);
            d1.Add(tvf);

            FieldType ftPositions = new FieldType();
            ftPositions.IsStored = (false);
            ftPositions.IsIndexed = (true);
            ftPositions.IsTokenized = (true);
            ftPositions.StoreTermVectors = (true);
            ftPositions.StoreTermVectorPositions = (true);
            ftPositions.StoreTermVectorOffsets = (false);
            tvf = new Field("tvPositions", "bobo bobo lucene lucene lucene test", ftPositions);
            d1.Add(tvf);

            FieldType ftOffsets = new FieldType();
            ftOffsets.IsStored = (false);
            ftOffsets.IsIndexed = (true);
            ftOffsets.IsTokenized = (true);
            ftOffsets.StoreTermVectors = (true);
            ftOffsets.StoreTermVectorPositions = (false);
            ftOffsets.StoreTermVectorOffsets = (true);
            tvf = new Field("tvOffsets", "bobo bobo lucene lucene lucene test", ftOffsets);
            d1.Add(tvf);

            FieldType ftPositionsAndOffsets = new FieldType();
            ftPositionsAndOffsets.IsStored = (false);
            ftPositionsAndOffsets.IsIndexed = (true);
            ftPositionsAndOffsets.IsTokenized = (true);
            ftPositionsAndOffsets.StoreTermVectors = (true);
            ftPositionsAndOffsets.StoreTermVectorPositions = (true);
            ftPositionsAndOffsets.StoreTermVectorOffsets = (true);
            tvf = new Field("tvPositionsAndOffsets", "bobo bobo lucene lucene lucene test",
                ftPositionsAndOffsets);
            d1.Add(tvf);

            Document d2 = new Document();
            d2.Add(BuildMetaField("id", "2"));
            d2.Add(BuildMetaField("shape", "rectangle"));
            d2.Add(BuildMetaField("color", "red"));
            d2.Add(BuildMetaField("size", "2"));
            d2.Add(BuildMetaField("location", "toy/lego/block/"));
            d2.Add(BuildMetaField("tag", "dog"));
            d2.Add(BuildMetaField("tag", "pet"));
            d2.Add(BuildMetaField("tag", "poodle"));
            d2.Add(BuildMetaSizePayloadField(tagSizePayloadTerm, 3));
            d2.Add(BuildMetaField("number", "0011"));
            d2.Add(BuildMetaField("date", "2003/02/14"));
            d2.Add(BuildMetaField("name", "igor"));
            d2.Add(BuildMetaField("char", "i"));
            d2.Add(BuildMetaField("multinum", "002"));
            d2.Add(BuildMetaField("multinum", "004"));
            d2.Add(BuildMetaField("multiwithweight", "cool\u0000300"));
            d2.Add(BuildMetaField("multiwithweight", "good\u0000200"));
            d2.Add(BuildMetaField("compactnum", "002"));
            d2.Add(BuildMetaField("compactnum", "004"));
            d2.Add(BuildMetaField("numendorsers", "000010"));
            d2.Add(BuildMetaField("path", "a-c-d"));
            d2.Add(BuildMetaField("multipath", "a-c-d"));
            d2.Add(BuildMetaField("multipath", "a-b"));
            d2.Add(BuildMetaField("custom", "000010"));
            d2.Add(BuildMetaField("latitude", "50"));
            d2.Add(BuildMetaField("longitude", "110"));
            d2.Add(BuildMetaField("salary", "08500"));

            Document d3 = new Document();
            d3.Add(BuildMetaField("id", "3"));
            d3.Add(BuildMetaField("shape", "circle"));
            d3.Add(BuildMetaField("color", "green"));
            d3.Add(BuildMetaField("size", "3"));
            d3.Add(BuildMetaField("location", "toy/lego/"));
            d3.Add(BuildMetaField("tag", "rabbit"));
            d3.Add(BuildMetaField("tag", "cartoon"));
            d3.Add(BuildMetaField("tag", "funny"));
            d3.Add(BuildMetaSizePayloadField(tagSizePayloadTerm, 3));
            d3.Add(BuildMetaField("number", "0230"));
            d3.Add(BuildMetaField("date", "2001/12/25"));
            d3.Add(BuildMetaField("name", "john"));
            d3.Add(BuildMetaField("char", "j"));
            d3.Add(BuildMetaField("multinum", "007"));
            d3.Add(BuildMetaField("multinum", "012"));
            d3.Add(BuildMetaField("multiwithweight", "cool\u0000200"));
            d3.Add(BuildMetaField("compactnum", "007"));
            d3.Add(BuildMetaField("compactnum", "012"));
            d3.Add(BuildMetaField("numendorsers", "000015"));
            d3.Add(BuildMetaField("path", "a-e"));
            d3.Add(BuildMetaField("multipath", "a-e"));
            d3.Add(BuildMetaField("multipath", "a-b"));
            d3.Add(BuildMetaField("custom", "000015"));
            d3.Add(BuildMetaField("latitude", "35"));
            d3.Add(BuildMetaField("longitude", "70"));
            d3.Add(BuildMetaField("salary", "06500"));

            Document d4 = new Document();
            d4.Add(BuildMetaField("id", "4"));
            d4.Add(BuildMetaField("shape", "circle"));
            d4.Add(BuildMetaField("color", "blue"));
            d4.Add(BuildMetaField("size", "1"));
            d4.Add(BuildMetaField("location", "toy/"));
            d4.Add(BuildMetaField("tag", "store"));
            d4.Add(BuildMetaField("tag", "pet"));
            d4.Add(BuildMetaField("tag", "animal"));
            d4.Add(BuildMetaSizePayloadField(tagSizePayloadTerm, 3));
            d4.Add(BuildMetaField("number", "0913"));
            d4.Add(BuildMetaField("date", "2004/11/24"));
            d4.Add(BuildMetaField("name", "cathy"));
            d4.Add(BuildMetaField("char", "c"));
            d4.Add(BuildMetaField("multinum", "007"));
            d4.Add(BuildMetaField("multinum", "007"));
            d4.Add(BuildMetaField("compactnum", "007"));
            d4.Add(BuildMetaField("numendorsers", "000019"));
            d4.Add(BuildMetaField("path", "a-c"));
            d4.Add(BuildMetaField("multipath", "a-c"));
            d4.Add(BuildMetaField("multipath", "a-b"));
            d4.Add(BuildMetaField("custom", "000019"));
            d4.Add(BuildMetaField("latitude", "30"));
            d4.Add(BuildMetaField("longitude", "75"));
            d4.Add(BuildMetaField("salary", "11200"));

            Document d5 = new Document();
            d5.Add(BuildMetaField("id", "5"));
            d5.Add(BuildMetaField("shape", "square"));
            d5.Add(BuildMetaField("color", "blue"));
            d5.Add(BuildMetaField("size", "5"));
            d5.Add(BuildMetaField("location", "toy/lego/"));
            d5.Add(BuildMetaField("tag", "cartoon"));
            d5.Add(BuildMetaField("tag", "funny"));
            d5.Add(BuildMetaField("tag", "disney"));
            d5.Add(BuildMetaSizePayloadField(tagSizePayloadTerm, 3));
            d5.Add(BuildMetaField("number", "1013"));
            d5.Add(BuildMetaField("date", "2002/03/08"));
            d5.Add(BuildMetaField("name", "mike"));
            d5.Add(BuildMetaField("char", "m"));
            d5.Add(BuildMetaField("multinum", "001"));
            d5.Add(BuildMetaField("multinum", "001"));
            d5.Add(BuildMetaField("compactnum", "001"));
            d5.Add(BuildMetaField("compactnum", "001"));
            d5.Add(BuildMetaField("numendorsers", "000002"));
            d5.Add(BuildMetaField("path", "a-e-f"));
            d5.Add(BuildMetaField("multipath", "a-e-f"));
            d5.Add(BuildMetaField("multipath", "a-b"));
            d5.Add(BuildMetaField("custom", "000002"));
            d5.Add(BuildMetaField("latitude", "60"));
            d5.Add(BuildMetaField("longitude", "120"));
            d5.Add(BuildMetaField("salary", "10500"));

            Document d6 = new Document();
            d6.Add(BuildMetaField("id", "6"));
            d6.Add(BuildMetaField("shape", "rectangle"));
            d6.Add(BuildMetaField("color", "green"));
            d6.Add(BuildMetaField("size", "6"));
            d6.Add(BuildMetaField("location", "toy/lego/block/"));
            d6.Add(BuildMetaField("tag", "funny"));
            d6.Add(BuildMetaField("tag", "humor"));
            d6.Add(BuildMetaField("tag", "joke"));
            d6.Add(BuildMetaSizePayloadField(tagSizePayloadTerm, 3));
            d6.Add(BuildMetaField("number", "2130"));
            d6.Add(BuildMetaField("date", "2007/08/01"));
            d6.Add(BuildMetaField("name", "doug"));
            d6.Add(BuildMetaField("char", "d"));
            d6.Add(BuildMetaField("multinum", "001"));
            d6.Add(BuildMetaField("multinum", "002"));
            d6.Add(BuildMetaField("multinum", "003"));
            d6.Add(BuildMetaField("compactnum", "001"));
            d6.Add(BuildMetaField("compactnum", "002"));
            d6.Add(BuildMetaField("compactnum", "003"));
            d6.Add(BuildMetaField("numendorsers", "000009"));
            d6.Add(BuildMetaField("path", "a-c-d"));
            d6.Add(BuildMetaField("multipath", "a-c-d"));
            d6.Add(BuildMetaField("multipath", "a-b"));
            d6.Add(BuildMetaField("custom", "000009"));
            d6.Add(BuildMetaField("latitude", "80"));
            d6.Add(BuildMetaField("longitude", "-90"));
            d6.Add(BuildMetaField("salary", "08900"));

            Document d7 = new Document();
            d7.Add(BuildMetaField("id", "7"));
            d7.Add(BuildMetaField("shape", "square"));
            d7.Add(BuildMetaField("color", "red"));
            d7.Add(BuildMetaField("size", "7"));
            d7.Add(BuildMetaField("location", "toy/lego/"));
            d7.Add(BuildMetaField("tag", "humane"));
            d7.Add(BuildMetaField("tag", "dog"));
            d7.Add(BuildMetaField("tag", "rabbit"));
            d7.Add(BuildMetaSizePayloadField(tagSizePayloadTerm, 3));
            d7.Add(BuildMetaField("number", "0005"));
            d7.Add(BuildMetaField("date", "2006/06/01"));
            d7.Add(BuildMetaField("name", "abe"));
            d7.Add(BuildMetaField("char", "a"));
            d7.Add(BuildMetaField("multinum", "008"));
            d7.Add(BuildMetaField("multinum", "003"));
            d7.Add(BuildMetaField("compactnum", "008"));
            d7.Add(BuildMetaField("compactnum", "003"));
            d7.Add(BuildMetaField("numendorsers", "000013"));
            d7.Add(BuildMetaField("path", "a-c"));
            d7.Add(BuildMetaField("multipath", "a-c"));
            d7.Add(BuildMetaField("multipath", "a-b"));
            d7.Add(BuildMetaField("custom", "000013"));
            d7.Add(BuildMetaField("latitude", "70"));
            d7.Add(BuildMetaField("longitude", "-60"));
            d7.Add(BuildMetaField("salary", "28500"));

            dataList.Add(d1);
            dataList.Add(d2);
            dataList.Add(d3);
            dataList.Add(d4);
            dataList.Add(d5);
            dataList.Add(d6);
            dataList.Add(d7);

            return dataList.ToArray();
        }

        private Lucene.Net.Store.Directory CreateIndex()
        {
            RAMDirectory idxDir = new RAMDirectory();

            Document[] data = BuildData();

            TestDataDigester testDigester = new TestDataDigester(this._fconf, data);
            BoboIndexer indexer = new BoboIndexer(testDigester, idxDir);
            indexer.Index();
            using (DirectoryReader r = DirectoryReader.Open(idxDir))
            {
            }

            return idxDir;
        }

        public static IList<IFacetHandler> BuildFieldConf()
        {
            List<IFacetHandler> facetHandlers = new List<IFacetHandler>();
            facetHandlers.Add(new SimpleFacetHandler("id"));
            SimpleFacetHandler colorHandler = new SimpleFacetHandler("color");
            colorHandler.TermCountSize = TermCountSize.Small;
            facetHandlers.Add(colorHandler);

            SimpleFacetHandler shapeHandler = new SimpleFacetHandler("shape");
            colorHandler.TermCountSize = TermCountSize.Medium;
            facetHandlers.Add(new SimpleFacetHandler("shape"));
            facetHandlers.Add(new RangeFacetHandler("size", new string[] { "[* TO 4]", 
                "[5 TO 8]", "[9 TO *]" }));
            string[] ranges = new string[] { "[000000 TO 000005]", "[000006 TO 000010]", 
                "[000011 TO 000020]" };
            facetHandlers.Add(new RangeFacetHandler("numendorsers", new PredefinedTermListFactory<int>(
                "000000"), ranges));

            var numTermFactory = new PredefinedTermListFactory<int>("0000");

            facetHandlers.Add(new PathFacetHandler("location"));

            PathFacetHandler pathHandler = new PathFacetHandler("path");
            pathHandler.Separator = "-";
            facetHandlers.Add(pathHandler);

            PathFacetHandler multipathHandler = new PathFacetHandler("multipath", true);
            multipathHandler.Separator = "-";
            facetHandlers.Add(multipathHandler);


            facetHandlers.Add(new SimpleFacetHandler("number", numTermFactory));
            facetHandlers.Add(new VirtualSimpleFacetHandler("virtual", numTermFactory, 
                new VirtualFacetDataFetcher(), new string[] { "number" }));
            facetHandlers.Add(new SimpleFacetHandler("testStored"));

            facetHandlers.Add(new SimpleFacetHandler("name"));
            facetHandlers.Add(new RangeFacetHandler("date", new PredefinedTermListFactory<DateTime>(
                "yyyy/MM/dd"), new string[] { "[2000/01/01 TO 2003/05/05]", "[2003/05/06 TO 2005/04/04]" }));
            facetHandlers.Add(new SimpleFacetHandler("char", (TermListFactory)null));
            facetHandlers.Add(new MultiValueFacetHandler("tag", (string)null, (TermListFactory)null, 
                tagSizePayloadTerm));
            facetHandlers.Add(new MultiValueFacetHandler("multinum", new PredefinedTermListFactory<int>(
                "000")));
            facetHandlers.Add(new MultiValueFacetHandler("diffname", "multinum", 
                new PredefinedTermListFactory<int>("000")));
            facetHandlers.Add(new MultiValueWithWeightFacetHandler("multiwithweight"));
            facetHandlers.Add(new CompactMultiValueFacetHandler("compactnum", 
                new PredefinedTermListFactory<int>("000")));
            facetHandlers.Add(new SimpleFacetHandler("storenum", new PredefinedTermListFactory<long>(null)));
            /* New FacetHandler for geographic locations. Depends on two RangeFacetHandlers on latitude and longitude */
            facetHandlers.Add(new RangeFacetHandler("latitude", new string[] { "[* TO 30]", 
                "[35 TO 60]", "[70 TO 120]" }));
            facetHandlers.Add(new RangeFacetHandler("longitude", new string[] { "[* TO 30]", 
                "[35 TO 60]", "[70 TO 120]" }));
            facetHandlers.Add(new GeoSimpleFacetHandler("distance", "latitude", "longitude"));
            facetHandlers.Add(new GeoFacetHandler("correctDistance", "latitude", "longitude"));
            /* Underlying time facet for DynamicTimeRangeFacetHandler */
            facetHandlers.Add(new RangeFacetHandler("timeinmillis", new PredefinedTermListFactory<long>(
                DynamicTimeRangeFacetHandler.NUMBER_FORMAT), null));

            string[] predefinedSalaryRanges = new string[4];
            predefinedSalaryRanges[0] = "[04000 TO 05999]";
            predefinedSalaryRanges[1] = "[06000 TO 07999]";
            predefinedSalaryRanges[2] = "[08000 TO 09999]";
            predefinedSalaryRanges[3] = "[10000 TO *]";
            RangeFacetHandler dependedRangeFacet = new RangeFacetHandler("salary", 
                predefinedSalaryRanges);
            facetHandlers.Add(dependedRangeFacet);

            string[][] predefinedBuckets = new string[4][];
            predefinedBuckets[0] = new string[] { "ken", "igor", "abe" };
            predefinedBuckets[1] = new string[] { "ken", "john", "mike" };
            predefinedBuckets[2] = new string[] { "john", "cathy" };
            predefinedBuckets[3] = new string[] { "doug" };

            IDictionary<string, string[]> predefinedGroups = new Dictionary<string, string[]>();
            predefinedGroups.Put("g1", predefinedBuckets[0]);
            predefinedGroups.Put("g2", predefinedBuckets[1]);
            predefinedGroups.Put("g3", predefinedBuckets[2]);
            predefinedGroups.Put("g4", predefinedBuckets[3]);

            facetHandlers.Add(new BucketFacetHandler("groups", predefinedGroups, "name"));

            string[][] predefinedBuckets2 = new string[3][];
            predefinedBuckets2[0] = new string[] { "2", "3" };
            predefinedBuckets2[1] = new string[] { "1", "4" };
            predefinedBuckets2[2] = new string[] { "7", "8" };

            IDictionary<string, string[]> predefinedNumberSets = new Dictionary<string, string[]>();
            predefinedNumberSets.Put("s1", predefinedBuckets2[0]);
            predefinedNumberSets.Put("s2", predefinedBuckets2[1]);
            predefinedNumberSets.Put("s3", predefinedBuckets2[2]);

            facetHandlers.Add(new BucketFacetHandler("sets", predefinedNumberSets, "multinum"));


            // histogram

            HistogramFacetHandler<int> histoHandler = new HistogramFacetHandler<int>("numberhisto", 
                "number", 0, 5000, 100);

            facetHandlers.Add(histoHandler);

            List<string> dependsNames = new List<string>();
            dependsNames.Add("color");
            dependsNames.Add("shape");
            dependsNames.Add("number");
            facetHandlers.Add(new SimpleGroupbyFacetHandler("groupby", dependsNames));



            ComboFacetHandler colorShape = new ComboFacetHandler("colorShape", 
                new string[] { "color", "shape" });
            ComboFacetHandler colorShapeMultinum = new ComboFacetHandler("colorShapeMultinum", 
                new string[] { "color", "shape", "multinum" });

            facetHandlers.Add(colorShape);
            facetHandlers.Add(colorShapeMultinum);

            return facetHandlers;
        }

        // NOTE: This was an anonymous class in Java.
        private class VirtualFacetDataFetcher : IFacetDataFetcher
        {
            public object Fetch(BoboSegmentReader reader, int doc)
            {
                FacetDataCache sourceCache = (FacetDataCache)reader.GetFacetData("number");
                if (sourceCache == null)
                    return null;

                return sourceCache.ValArray.GetRawValue(sourceCache.OrderArray.Get(doc));
            }

            public void Cleanup(BoboSegmentReader reader)
            {
                // do nothing here.
            }
        }

        public static bool Check(BrowseResult res, int numHits, 
            IDictionary<string, IList<BrowseFacet>> choiceMap, string[] ids)
        {
            bool match = false;
            if (numHits == res.NumHits)
            {
                if (choiceMap != null)
                {
                    var entries = res.FacetMap;

                    if (entries.Count == choiceMap.Count)
                    {
                        foreach (var entry in entries)
                        {
                            string name = entry.Key;
                            IFacetAccessible c1 = entry.Value;
                            var l1 = c1.GetFacets();
                            var l2 = choiceMap.Get(name);

                            if (l1.Count == l2.Count)
                            {
                                for (int i = 0; i < l1.Count; i++)
                                {
                                    if (!l1.ElementAt(i).Equals(l2.ElementAt(i)))
                                    {
                                        return false;
                                    }
                                }
                                match = true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                if (ids != null)
                {
                    BrowseHit[] hits = res.Hits;
                    try
                    {
                        if (hits.Length != ids.Length) return false;
                        for (int i = 0; i < hits.Length; ++i)
                        {
                            string id = hits[i].GetField("id");
                            if (!ids[i].Equals(id)) return false;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
                match = true;
            }
            return match;
        }

        /// <summary>
        /// Check results
        /// </summary>
        /// <param name="result"></param>
        /// <param name="req"></param>
        /// <param name="numHits"></param>
        /// <param name="choiceMap"></param>
        /// <param name="ids"></param>
        private void DoTest(BrowseResult result, BrowseRequest req, int numHits, 
            IDictionary<string, IList<BrowseFacet>> choiceMap, string[] ids)
        {
            if (!Check(result, numHits, choiceMap, ids))
            {
                //Format the choice map using LINQ
                string choices = (choiceMap == null) ? "" : string.Join("\n", choiceMap.Select(kvp => string.Format("{0} = {1}", kvp.Key, string.Join(", ", kvp.Value.Select(x => x.ToString()).ToArray()))).ToArray());

                StringBuilder buffer = new StringBuilder();
                //buffer.Append("Test: ").Append(new StackFrame(1).GetMethod().Name).Append("\n");
                buffer.Append("Test: ").Append(NUnit.Framework.TestContext.CurrentContext.Test.Name).Append("\n");
                buffer.Append("Result check failed: \n");
                buffer.Append("expected: \n");
                buffer.Append(numHits).Append(" hits\n");
                buffer.Append(Arrays.ToString(ids)).AppendLine().AppendLine();
                buffer.Append("Choices: \n");
                buffer.Append(choices).Append('\n');
                buffer.Append("gotten: \n");
                buffer.Append(result.NumHits).Append(" hits\n");
                buffer.Append(Arrays.ToString(result.Hits.Select(x => x.GetField("id")).ToArray())).AppendLine();

                var entries = result.FacetMap;

                buffer.Append("choiceMap: \n");
                buffer.Append("{");
                foreach (var entry in entries)
                {
                    string name = entry.Key;
                    IFacetAccessible facetAccessor = entry.Value;
                    buffer.Append("name=").Append(name).Append(",");
                    buffer.Append("facets=").Append(facetAccessor.GetFacets()).Append(";");
                }
                buffer.Append("}").Append('\n');

                BrowseHit[] hits = result.Hits;
                for (int i = 0; i < hits.Length; ++i)
                {
                    if (i != 0)
                    {
                        buffer.Append('\n');
                    }
                    buffer.Append(hits[i]);
                }
                Assert.Fail(buffer.ToString());
            }
        }

        public static string ToString(IDictionary<string, IFacetAccessible> map)
        {
            var buffer = new StringBuilder();

            buffer.Append("{");
            foreach (var entry in map)
            {
                string name = entry.Key;
                IFacetAccessible facetAccessor = entry.Value;
                buffer.Append("name=").Append(name).Append(",");
                buffer.Append("facets=").Append(facetAccessor.GetFacets()).Append(";");
            }
            buffer.Append("}").Append('\n');
            return buffer.ToString();
        }

        [Test]
        public void TestStoredFacetField()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection colorSel = new BrowseSelection("testStored");
            colorSel.AddValue("stored");
            br.AddSelection(colorSel);
            br.FetchStoredFields = true;

            using (BoboBrowser boboBrowser = NewBrowser())
            {
                using (BrowseResult result = boboBrowser.Browse(br))
                {
                    Assert.AreEqual(1, result.NumHits);
                    BrowseHit hit = result.Hits[0];
                    IList<BrowseHit.SerializableField> storedFields = hit.StoredFields;
                    Assert.NotNull(storedFields);

                    var fieldValues = new List<string>();
                    foreach (var field in storedFields)
                    {
                        if (field.Name.Equals("testStored") && field.StringValue != null)
                        {
                            fieldValues.Add(field.StringValue);
                        }
                    }

                    string[] values = fieldValues.ToArray();
                    Assert.NotNull(values);
                    Assert.AreEqual(1, values.Length);
                    Assert.True("stored".Equals(values[0]));
                }
            }
        }

        [Test]
        public void TestStoredField()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection colorSel = new BrowseSelection("color");
            colorSel.AddValue("red");
            br.AddSelection(colorSel);

            BrowseSelection shapeSel = new BrowseSelection("shape");
            shapeSel.AddValue("square");
            br.AddSelection(shapeSel);

            BrowseSelection sizeSel = new BrowseSelection("size");
            sizeSel.AddValue("[4 TO 4]");
            br.AddSelection(sizeSel);


            using (BoboBrowser boboBrowser = NewBrowser())
            {
                using (BrowseResult result = boboBrowser.Browse(br))
                {
                    Assert.AreEqual(1, result.NumHits);
                    BrowseHit hit = result.Hits[0];
                    Assert.Null(hit.StoredFields);
                }
                br.FetchStoredFields = true;
                using (BrowseResult result = boboBrowser.Browse(br))
                {
                    Assert.AreEqual(1, result.NumHits);
                    BrowseHit hit = result.Hits[0];
                    string stored = hit.GetFieldStringValue("testStored");
                    Assert.True("stored".Equals(stored));
                }
            }
        }

        [Test]
        public void TestRetrieveTermVector()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection colorSel = new BrowseSelection("color");
            colorSel.AddValue("red");
            br.AddSelection(colorSel);

            BrowseSelection shapeSel = new BrowseSelection("shape");
            shapeSel.AddValue("square");
            br.AddSelection(shapeSel);

            BrowseSelection sizeSel = new BrowseSelection("size");
            sizeSel.AddValue("[4 TO 4]");
            br.AddSelection(sizeSel);

            br.TermVectorsToFetch = new string[] { "tv", "tvPositions",
                "tvOffsets", "tvPositionsAndOffsets" };

            using (BoboBrowser boboBrowser = NewBrowser())
            {
                using (BrowseResult result = boboBrowser.Browse(br))
                {
                    Assert.AreEqual(1, result.NumHits);
                    BrowseHit hit = result.Hits[0];
                    Assert.Null(hit.StoredFields);
                }
                br.FetchStoredFields = true;
                using (BrowseResult result = boboBrowser.Browse(br))
                {
                    Assert.AreEqual(1, result.NumHits);
                    BrowseHit hit = result.Hits[0];
                    var tvMap = hit.TermVectorMap;
                    Assert.NotNull(tvMap);

                    Assert.AreEqual(4, tvMap.Count);

                    IList<BrowseHit.BoboTerm> tv = tvMap.Get("tv");
                    Assert.NotNull(tv);

                    //Assert.AreEqual("bobo", tv.Get(0).Term);
                    //Assert.AreEqual(2, tv.Get(0).Freq);
                    //Assert.AreEqual(null, tv.Get(0).Positions);
                    //Assert.AreEqual(null, tv.Get(0).StartOffsets);
                    //Assert.AreEqual(null, tv.Get(0).EndOffsets);

                    //Assert.AreEqual("lucene", tv.Get(1).Term);
                    //Assert.AreEqual(3, tv.Get(1).Freq);
                    //Assert.AreEqual(null, tv.Get(1).Positions);
                    //Assert.AreEqual(null, tv.Get(1).StartOffsets);
                    //Assert.AreEqual(null, tv.Get(1).EndOffsets);

                    //Assert.AreEqual("test", tv.Get(2).Term);
                    //Assert.AreEqual(1, tv.Get(2).Freq);
                    //Assert.AreEqual(null, tv.Get(2).Positions);
                    //Assert.AreEqual(null, tv.Get(2).StartOffsets);
                    //Assert.AreEqual(null, tv.Get(2).EndOffsets);


                    // NOTE: For a cleaner API, we are using empty IEnumerable instances
                    // instead of allowing null as was done in the original version.
                    IEnumerable<int> positions = new List<int>();
                    IEnumerable<int> startOffsets = new List<int>();
                    IEnumerable<int> endOffsets = new List<int>();

                    Assert.AreEqual("bobo", tv.Get(0).Term);
                    Assert.AreEqual(2, tv.Get(0).Freq);
                    Assert.AreEqual(positions, tv.Get(0).Positions);
                    Assert.AreEqual(startOffsets, tv.Get(0).StartOffsets);
                    Assert.AreEqual(endOffsets, tv.Get(0).EndOffsets);

                    Assert.AreEqual("lucene", tv.Get(1).Term);
                    Assert.AreEqual(3, tv.Get(1).Freq);
                    Assert.AreEqual(positions, tv.Get(1).Positions);
                    Assert.AreEqual(startOffsets, tv.Get(1).StartOffsets);
                    Assert.AreEqual(endOffsets, tv.Get(1).EndOffsets);

                    Assert.AreEqual("test", tv.Get(2).Term);
                    Assert.AreEqual(1, tv.Get(2).Freq);
                    Assert.AreEqual(positions, tv.Get(2).Positions);
                    Assert.AreEqual(startOffsets, tv.Get(2).StartOffsets);
                    Assert.AreEqual(endOffsets, tv.Get(2).EndOffsets);

                    tv = tvMap.Get("tvPositions");
                    Assert.NotNull(tv);

                    Assert.AreEqual("bobo", tv.Get(0).Term);
                    Assert.AreEqual(2, tv.Get(0).Freq);
                    positions = new List<int>() { 0, 1 };
                    Assert.AreEqual(positions, tv.Get(0).Positions);
                    startOffsets = new List<int>() { -1, -1 };
                    Assert.AreEqual(startOffsets, tv.Get(0).StartOffsets);
                    endOffsets = new List<int>() { -1, -1 };
                    Assert.AreEqual(endOffsets, tv.Get(0).EndOffsets);

                    Assert.AreEqual("lucene", tv.Get(1).Term);
                    Assert.AreEqual(3, tv.Get(1).Freq);
                    positions = new List<int>() { 2, 3, 4 };
                    Assert.AreEqual(positions, tv.Get(1).Positions);
                    startOffsets = new List<int>() { -1, -1, -1 };
                    Assert.AreEqual(startOffsets, tv.Get(1).StartOffsets);
                    endOffsets = new List<int>() { -1, -1, -1 };
                    Assert.AreEqual(endOffsets, tv.Get(1).EndOffsets);

                    Assert.AreEqual("test", tv.Get(2).Term);
                    Assert.AreEqual(1, tv.Get(2).Freq);
                    positions = new List<int>() { 5 };
                    Assert.AreEqual(positions, tv.Get(2).Positions);
                    startOffsets = new List<int>() { -1 };
                    Assert.AreEqual(startOffsets, tv.Get(2).StartOffsets);
                    endOffsets = new List<int>() { -1 };
                    Assert.AreEqual(endOffsets, tv.Get(2).EndOffsets);

                    tv = tvMap.Get("tvOffsets");
                    Assert.NotNull(tv);

                    Assert.AreEqual("bobo", tv.Get(0).Term);
                    Assert.AreEqual(2, tv.Get(0).Freq);
                    positions = new List<int>() { -1, -1 };
                    Assert.AreEqual(positions, tv.Get(0).Positions);
                    startOffsets = new List<int>() { 0, 5 };
                    Assert.AreEqual(startOffsets, tv.Get(0).StartOffsets);
                    endOffsets = new List<int>() { 4, 9 };
                    Assert.AreEqual(endOffsets, tv.Get(0).EndOffsets);

                    Assert.AreEqual("lucene", tv.Get(1).Term);
                    Assert.AreEqual(3, tv.Get(1).Freq);
                    positions = new List<int>() { -1, -1, -1 };
                    Assert.AreEqual(positions, tv.Get(1).Positions);
                    startOffsets = new List<int>() { 10, 17, 24 };
                    Assert.AreEqual(startOffsets, tv.Get(1).StartOffsets);
                    endOffsets = new List<int>() { 16, 23, 30 };
                    Assert.AreEqual(endOffsets, tv.Get(1).EndOffsets);

                    Assert.AreEqual("test", tv.Get(2).Term);
                    Assert.AreEqual(1, tv.Get(2).Freq);
                    positions = new List<int>() { -1 };
                    Assert.AreEqual(positions, tv.Get(2).Positions);
                    startOffsets = new List<int>() { 31 };
                    Assert.AreEqual(startOffsets, tv.Get(2).StartOffsets);
                    endOffsets = new List<int>() { 35 };
                    Assert.AreEqual(endOffsets, tv.Get(2).EndOffsets);

                    tv = tvMap.Get("tvPositionsAndOffsets");
                    Assert.NotNull(tv);
                    Assert.AreEqual("bobo", tv.Get(0).Term);
                    Assert.AreEqual(2, tv.Get(0).Freq);
                    positions = new List<int>() { 0, 1 };
                    Assert.AreEqual(positions, tv.Get(0).Positions);
                    startOffsets = new List<int>() { 0, 5 };
                    Assert.AreEqual(startOffsets, tv.Get(0).StartOffsets);
                    endOffsets = new List<int>() { 4, 9 };
                    Assert.AreEqual(endOffsets, tv.Get(0).EndOffsets);

                    Assert.AreEqual("lucene", tv.Get(1).Term);
                    Assert.AreEqual(3, tv.Get(1).Freq);
                    positions = new List<int>() { 2, 3, 4 };
                    Assert.AreEqual(positions, tv.Get(1).Positions);
                    startOffsets = new List<int>() { 10, 17, 24 };
                    Assert.AreEqual(startOffsets, tv.Get(1).StartOffsets);
                    endOffsets = new List<int>() { 16, 23, 30 };
                    Assert.AreEqual(endOffsets, tv.Get(1).EndOffsets);

                    Assert.AreEqual("test", tv.Get(2).Term);
                    Assert.AreEqual(1, tv.Get(2).Freq);
                    positions = new List<int>() { 5 };
                    Assert.AreEqual(positions, tv.Get(2).Positions);
                    startOffsets = new List<int>() { 31 };
                    Assert.AreEqual(startOffsets, tv.Get(2).StartOffsets);
                    endOffsets = new List<int>() { 35 };
                    Assert.AreEqual(endOffsets, tv.Get(2).EndOffsets);
                }
            }
        }

        [Test]
        public void TestRawDataRetrieval()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;
            br.Sort = new SortField[] { new SortField("date", SortFieldType.CUSTOM, false) };

            using (BoboBrowser boboBrowser = NewBrowser())
            {
                using (BrowseResult result = boboBrowser.Browse(br))
                {
                    Assert.AreEqual(7, result.NumHits);
                    BrowseHit hit = result.Hits[0];
                    Assert.AreEqual(0, hit.DocId);
                    Object lowDate = hit.GetRawField("date");
                    DateTime date = new DateTime(2000, 1, 1);
                    Assert.True(lowDate.Equals(date.ToBinary()));

                    hit = result.Hits[6];
                    Assert.AreEqual(5, hit.DocId);
                    Object highDate = hit.GetRawField("date");
                    date = new DateTime(2007, 8, 1);
                    Assert.True(highDate.Equals(date.ToBinary()));
                }
            }
        }



        [Test]
        public void TestExpandSelection()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("color");
            sel.AddValue("red");
            br.AddSelection(sel);


            FacetSpec output = new FacetSpec();
            output.ExpandSelection = true;
            br.SetFacetSpec("color", output);
            br.SetFacetSpec("shape", output);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("blue", 2), new BrowseFacet("green", 2),
                    new BrowseFacet("red", 3) } },
                { "shape", new BrowseFacet[] { new BrowseFacet("rectangle", 1),
                    new BrowseFacet("square", 2) } }
            };

            DoTest(br, 3, answer, new string[] { "1", "2", "7" });

            sel = new BrowseSelection("shape");
            sel.AddValue("square");
            br.AddSelection(sel);

            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("blue", 1), new BrowseFacet("red", 2) } },
                { "shape", new BrowseFacet[] { new BrowseFacet("rectangle", 1),
                    new BrowseFacet("square", 2) } }
            };

            DoTest(br, 2, answer, new String[] { "1", "7" });
        }

        [Test]
        public void TestPath()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("path");
            sel.AddValue("a");
            IDictionary<string, string> prop = sel.SelectionProperties;
            PathFacetHandler.SetDepth(prop, 1);
            br.AddSelection(sel);

            FacetSpec pathSpec = new FacetSpec();
            pathSpec.OrderBy = FacetSpec.FacetSortSpec.OrderValueAsc;
            br.SetFacetSpec("path", pathSpec);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "path", new BrowseFacet[] { new BrowseFacet("a-b", 1) ,new BrowseFacet("a-c", 4), 
                    new BrowseFacet("a-e", 2) } }
            };
            DoTest(br, 7, answer, null);

            pathSpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "path", new BrowseFacet[] { new BrowseFacet("a-c", 4), new BrowseFacet("a-e", 2), 
                    new BrowseFacet("a-b", 1) } }
            };
            DoTest(br, 7, answer, null);

            pathSpec.MaxCount = 2;
            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "path", new BrowseFacet[] { new BrowseFacet("a-c", 4), new BrowseFacet("a-e", 2) } }
            };
            DoTest(br, 7, answer, null);
        }

        [Test]
        public void TestComboFacetHandlerSelectionOnly()
        {

            BrowseRequest br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            BrowseSelection sel = new BrowseSelection("colorShape");
            sel.AddValue("color:green");
            sel.AddValue("shape:rectangle");
            sel.AddValue("shape:square");
            sel.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationOr;
            br.AddSelection(sel);

            DoTest(br, 6, null, new String[] { "1", "2", "3", "5", "6", "7" });

            br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            sel = new BrowseSelection("colorShape");
            sel.AddValue("color:green");
            sel.AddValue("shape:rectangle");
            sel.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationAnd;
            br.AddSelection(sel);

            DoTest(br, 1, null, new String[] { "6" });

            br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            sel = new BrowseSelection("colorShapeMultinum");
            sel.AddValue("color:red");
            sel.AddValue("shape:square");
            sel.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationOr;
            sel.AddNotValue("multinum:001");
            sel.AddNotValue("multinum:003");
            br.AddSelection(sel);

            DoTest(br, 1, null, new String[] { "2" });

            br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            sel = new BrowseSelection("colorShapeMultinum");
            sel.AddValue("color:red");
            sel.AddValue("shape:square");
            sel.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationOr;
            sel.AddNotValue("multinum:003");
            br.AddSelection(sel);

            DoTest(br, 2, null, new String[] { "2", "5" });
        }


        /**
	     * This tests GeoSimpleFacetHandler
	     * @throws Exception
	     */
        [Test]
        public void TestSimpleGeo()
        {
            // testing facet counts for two distance facets - <30,70,5>, <60,120,1>
            BrowseRequest br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            BrowseSelection sel = new BrowseSelection("distance");
            sel.AddValue("30,70:5");
            sel.AddValue("60,120:1");
            br.AddSelection(sel);

            FacetSpec geoSpec = new FacetSpec();
            geoSpec.OrderBy = FacetSpec.FacetSortSpec.OrderValueAsc;
            br.SetFacetSpec("distance", geoSpec);
            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "distance", new BrowseFacet[] { new BrowseFacet("30,70:5", 2), 
                    new BrowseFacet("60,120:1", 2) } }
            };
            DoTest(br, 4, answer, new string[] { "1", "3", "4", "5" });

            // testing for selection of facet <60,120,1> and verifying that 2 documents match this facet.
            BrowseRequest br2 = new BrowseRequest();
            br2.Count = (10);
            br2.Offset = (0);

            BrowseSelection sel2 = new BrowseSelection("distance");
            sel2.AddValue("60,120:1");
            var map = new Dictionary<string, float>()
            {
                { "0,120:1", 3.0f }
            };
            FacetTermQuery geoQ = new FacetTermQuery(sel2, map);

            br2.Query = (geoQ);
            DoTest(br2, 2, null, new string[] { "1", "5" });

            // facet query for color "red" and getting facet counts for the distance facet.
            BrowseRequest br3 = new BrowseRequest();
            br3.Count = (10);
            br3.Offset = (0);

            BrowseSelection sel3 = new BrowseSelection("color");
            sel3.AddValue("red");
            var map3 = new Dictionary<string, float>()
            {
                { "red", 3.0f }
            };
            FacetTermQuery colorQ = new FacetTermQuery(sel3, map3);

            br3.SetFacetSpec("distance", geoSpec);
            geoSpec.MinHitCount = (0);
            br3.Query = (colorQ);             // query is color=red
            br3.AddSelection(sel);			  // count facets <30,70,5> and <60,120,1>
            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "distance", new BrowseFacet[] { new BrowseFacet("30,70:5", 0), 
                    new BrowseFacet("60,120:1", 1) } }
            };
            DoTest(br3, 1, answer, null);
        }

        /**
         * This tests GeoFacetHandler
         * @throws Exception
         */
        [Test]
        public void TestGeo()
        {
            // testing facet counts for two distance facets - <30,70,5>, <60,120,1>
            BrowseRequest br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            BrowseSelection sel = new BrowseSelection("correctDistance");
            sel.AddValue("30,75:100");
            sel.AddValue("60,120:1");
            br.AddSelection(sel);

            FacetSpec geoSpec = new FacetSpec();
            geoSpec.MinHitCount = (0);
            geoSpec.OrderBy = FacetSpec.FacetSortSpec.OrderValueAsc;
            br.SetFacetSpec("correctDistance", geoSpec);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "correctDistance", new BrowseFacet[] { new BrowseFacet("30,75:100", 1), 
                    new BrowseFacet("60,120:1", 2) } }
            };
            DoTest(br, 3, answer, null);

            // testing for selection of facet <60,120,1> and verifying that 2 documents match this facet.
            BrowseRequest br2 = new BrowseRequest();
            br2.Count = (10);
            br2.Offset = (0);

            BrowseSelection sel2 = new BrowseSelection("correctDistance");
            sel2.AddValue("60,120:1");
            var map = new Dictionary<string, float>()
            {
                { "60,120:1", 3.0f }
            };
            FacetTermQuery geoQ = new FacetTermQuery(sel2, map);

            br2.Query = (geoQ);
            DoTest(br2, 2, null, new string[] { "1", "5" });

            // facet query for color "red" and getting facet counts for the distance facet.
            BrowseRequest br3 = new BrowseRequest();
            br3.Count = (10);
            br3.Offset = (0);

            BrowseSelection sel3 = new BrowseSelection("color");
            sel3.AddValue("red");
            var map3 = new Dictionary<string, float>()
            {
                { "red", 3.0f }
            };
            FacetTermQuery colorQ = new FacetTermQuery(sel3, map3);

            br3.SetFacetSpec("correctDistance", geoSpec);
            geoSpec.MinHitCount = (1);
            br3.Query = (colorQ);             // query is color=red
            br3.AddSelection(sel);			  // count facets <30,70,5> and <60,120,1>
            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "correctDistance", new BrowseFacet[] { new BrowseFacet("60,120:1", 1) } }
            };
            DoTest(br3, 1, answer, null);
        }

        [Test]
        public void TestMultiPath()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            BrowseSelection sel = new BrowseSelection("multipath");
            sel.AddValue("a");
            IDictionary<string, string> prop = sel.SelectionProperties;
            PathFacetHandler.SetDepth(prop, 1);
            br.AddSelection(sel);

            FacetSpec pathSpec = new FacetSpec();
            pathSpec.MaxCount = (3);

            pathSpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            br.SetFacetSpec("multipath", pathSpec);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "multipath", new BrowseFacet[] { new BrowseFacet("a-b", 7), new BrowseFacet("a-c", 4),
                    new BrowseFacet("a-e", 2) } }
            };
            DoTest(br, 7, answer, null);
        }

        [Test]
        public void TestMultiSelectedPaths()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            BrowseSelection sel = new BrowseSelection("path");
            sel.AddValue("a-c");
            sel.AddValue("a-e");
            IDictionary<string, string> prop = sel.SelectionProperties;
            PathFacetHandler.SetDepth(prop, 1);
            PathFacetHandler.SetStrict(prop, true);
            br.AddSelection(sel);

            FacetSpec pathSpec = new FacetSpec();
            pathSpec.MaxCount = (3);

            pathSpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            br.SetFacetSpec("path", pathSpec);
            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "path", new BrowseFacet[] { new BrowseFacet("a-c-d",2),
                    new BrowseFacet("a-e-f",1) } }
            };
            DoTest(br, 3, answer, null);

            pathSpec.OrderBy = FacetSpec.FacetSortSpec.OrderByCustom;
            pathSpec.CustomComparerFactory = new TestMultiSelectedPathsComparerFactory();

            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "path", new BrowseFacet[] { new BrowseFacet("a-c-d",2),
                    new BrowseFacet("a-e-f",1) } }
            };
            DoTest(br, 3, answer, null);
        }

        private class TestMultiSelectedPathsComparerFactory : IComparerFactory
        {

            public IComparer<int> NewComparer(IFieldValueAccessor fieldValueAccessor, BigSegmentedArray counts)
            {
                return new TestMultiSelectedPathsIntComparer(counts);
            }

            public IComparer<BrowseFacet> NewComparer()
            {
                return new TestMultiSelectedPathsBrowseFacetComparer();
            }
        }

        private class TestMultiSelectedPathsIntComparer : IComparer<int>
        {
            private readonly BigSegmentedArray counts;

            public TestMultiSelectedPathsIntComparer(BigSegmentedArray counts)
            {
                this.counts = counts;
            }

            public int Compare(int f1, int f2)
            {
                int val = counts.Get(f2) - counts.Get(f1);
                if (val == 0)
                {
                    val = f2 - f1;
                }
                return val;
            }
        }

        private class TestMultiSelectedPathsBrowseFacetComparer : IComparer<BrowseFacet>
        {
            public int Compare(BrowseFacet f1, BrowseFacet f2)
            {
                int val = f2.FacetValueHitCount - f1.FacetValueHitCount;
                if (val == 0)
                {
                    val = string.CompareOrdinal(f1.Value, f2.Value);
                }
                return val;
            }
        }


        [Test]
        public void TestTagRollup()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("location");
            IDictionary<string, string> prop = sel.SelectionProperties;
            PathFacetHandler.SetDepth(prop, 1);
            PathFacetHandler.SetStrict(prop, true);
            sel.AddValue("toy/lego");
            br.AddSelection(sel);

            FacetSpec locationOutput = new FacetSpec();

            br.SetFacetSpec("location", locationOutput);

            FacetSpec tagOutput = new FacetSpec();
            tagOutput.MaxCount = 50;
            tagOutput.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;

            br.SetFacetSpec("tag", tagOutput);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "location", new BrowseFacet[] { new BrowseFacet("toy/lego/block",3) } },
                { "tag", new BrowseFacet[] { new BrowseFacet("pet", 2), 
                    new BrowseFacet("animal", 1), new BrowseFacet("dog", 1), new BrowseFacet("funny", 1), 
                    new BrowseFacet("humor", 1), new BrowseFacet("joke", 1), 
                    new BrowseFacet("poodle", 1), new BrowseFacet("rabbit", 1) } }
            };
            DoTest(br, 3, answer, null);
        }

        [Test]
        public void TestChar()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("char");
            sel.AddValue("j");
            br.AddSelection(sel);
            DoTest(br, 1, null, new string[] { "3" });

            br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            sel = new BrowseSelection("color");
            sel.AddValue("red");
            br.AddSelection(sel);

            FacetSpec charOutput = new FacetSpec();
            charOutput.MaxCount = 50;
            charOutput.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;


            br.SetFacetSpec("char", charOutput);
            br.AddSortField(new SortField("date", SortFieldType.CUSTOM, true));

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "char", new BrowseFacet[] { new BrowseFacet("a", 1), new BrowseFacet("i", 1), 
                    new BrowseFacet("k", 1) } }
            };

            DoTest(br, 3, answer, new string[] { "7", "2", "1" });
        }

        [Test]
        public void TestDate()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("date");
            sel.AddValue("[2001/01/01 TO 2005/01/01]");
            br.AddSelection(sel);

            FacetSpec ospec = new FacetSpec();
            ospec.ExpandSelection = false;
            br.SetFacetSpec("color", ospec);

            br.AddSortField(new SortField("date", SortFieldType.CUSTOM, true));

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("blue", 2), new BrowseFacet("green", 1), 
                    new BrowseFacet("red", 1) } }
            };
            DoTest(br, 4, answer, new string[] { "4", "2", "5", "3" });
        }

        [Test]
        public void TestDate2()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("date");
            sel.AddValue("[2005/01/01 TO *]");
            br.AddSelection(sel);

            FacetSpec ospec = new FacetSpec();
            ospec.ExpandSelection = false;
            br.SetFacetSpec("color", ospec);

            br.AddSortField(new SortField("date", SortFieldType.CUSTOM, true));

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("green", 1), new BrowseFacet("red", 1) } }
            };
            DoTest(br, 2, answer, new string[] { "6", "7" });
        }

        [Test]
        public void TestDate3()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("date");
            sel.AddValue("[* TO 2002/01/01]");
            br.AddSelection(sel);

            FacetSpec ospec = new FacetSpec();
            ospec.ExpandSelection = false;
            br.SetFacetSpec("color", ospec);

            br.AddSortField(new SortField("date", SortFieldType.CUSTOM, true));

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("green", 1), new BrowseFacet("red", 1) } }
            };
            DoTest(br, 2, answer, new string[] { "3", "1" });
        }

        /// <summary>
        /// Do the test and check result. 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="numHits"></param>
        /// <param name="choiceMap"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        private BrowseResult DoTest(BrowseRequest req, int numHits, 
            IDictionary<string, IList<BrowseFacet>> choiceMap, string[] ids)
        {
            return DoTest((BoboBrowser)null, req, numHits, choiceMap, ids);
        }

        private BrowseResult DoTest(BoboBrowser boboBrowser, BrowseRequest req, int numHits, 
            IDictionary<string, IList<BrowseFacet>> choiceMap, string[] ids)
        {
            BrowseResult result;

            if (boboBrowser == null)
            {
                using (boboBrowser = NewBrowser())
                {
                    result = boboBrowser.Browse(req);
                }
            }
            else
            {
                result = boboBrowser.Browse(req);
            }

            DoTest(result, req, numHits, choiceMap, ids);
            return result;
        }

        [Test]
        public void TestLuceneSort()
        {
            using (DirectoryReader srcReader = DirectoryReader.Open(_indexDir))
            {
                var facetHandlers = new List<IFacetHandler>();
                facetHandlers.Add(new SimpleFacetHandler("id"));

                BoboMultiReader reader = BoboMultiReader.GetInstance(srcReader, facetHandlers);       // not facet handlers to help
                BoboBrowser browser = new BoboBrowser(reader);

                BrowseRequest browseRequest = new BrowseRequest();
                browseRequest.Count = 10;
                browseRequest.Offset = 0;
                browseRequest.AddSortField(new SortField("date", SortFieldType.STRING));

                DoTest(browser, browseRequest, 7, null, new string[] { "1", "3", "5", "2", "4", "7", "6" });
            }
        }

        [Test]
        public void TestFacetSort()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            FacetSpec colorSpec = new FacetSpec();
            colorSpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            br.SetFacetSpec("color", colorSpec);

            FacetSpec shapeSpec = new FacetSpec();
            shapeSpec.OrderBy = FacetSpec.FacetSortSpec.OrderValueAsc;
            br.SetFacetSpec("shape", shapeSpec);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("red", 3), new BrowseFacet("blue", 2), 
                    new BrowseFacet("green", 2) } },
                { "shape", new BrowseFacet[] { new BrowseFacet("circle", 2), 
                    new BrowseFacet("rectangle", 2), new BrowseFacet("square", 3) } }
            };

            DoTest(br, 7, answer, null);

            var valComp = new FacetValueComparerFactory().NewComparer();

            int v = valComp.Compare(new BrowseFacet("red", 3), new BrowseFacet("blue", 2));
            Assert.True(v > 0);

            valComp = new FacetHitcountComparerFactory().NewComparer();
            v = valComp.Compare(new BrowseFacet("red", 3), new BrowseFacet("blue", 2));
            Assert.True(v < 0);

            v = valComp.Compare(new BrowseFacet("red", 3), new BrowseFacet("blue", 3));
            Assert.True(v > 0);

            // Test for a non-match (was throwing null reference exception - see #14).
            br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            colorSpec = new FacetSpec();
            colorSpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            colorSpec.MaxCount = 5;
            br.SetFacetSpec("color", colorSpec);

            shapeSpec = new FacetSpec();
            shapeSpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            shapeSpec.MaxCount = 5;
            br.SetFacetSpec("shape", shapeSpec);

            BrowseSelection sel = new BrowseSelection("color");
            sel.AddValue("red");
            br.AddSelection(sel);

            sel = new BrowseSelection("shape");
            sel.AddValue("circle");
            br.AddSelection(sel);

            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { } },
                { "shape", new BrowseFacet[] { } }
            };

            DoTest(br, 0, answer, null);
        }

        [Test]
        public void TestMultiDate()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("date");
            sel.AddValue("[2000/01/01 TO 2002/07/07]");
            sel.AddValue("[2003/01/01 TO 2005/01/01]");
            br.AddSelection(sel);

            br.AddSortField(new SortField("date", SortFieldType.CUSTOM, false));

            DoTest(br, 5, null, new string[] { "1", "3", "5", "2", "4" });
        }

        [Test]
        public void TestNoCount()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 0;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("color");
            sel.AddValue("red");
            br.AddSelection(sel);

            FacetSpec ospec = new FacetSpec();
            ospec.ExpandSelection = false;
            br.SetFacetSpec("color", ospec);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("red", 3) } }
            };
            DoTest(br, 3, null, new string[0]);
        }

        [Test]
        public void TestDate4()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("date");
            sel.AddValue("[* TO *]");
            br.AddSelection(sel);

            FacetSpec ospec = new FacetSpec();
            ospec.ExpandSelection = false;
            br.SetFacetSpec("color", ospec);

            br.AddSortField(new SortField("date", SortFieldType.CUSTOM, false));

            DoTest(br, 7, null, new string[] { "1", "3", "5", "2", "4", "7", "6" });
        }

        [Test]
        public void TestMultiSort()
        {
            // no sel
            BrowseRequest br = new BrowseRequest();
            br.Count=(10);
            br.Offset=(0);

            br.Sort = new SortField[] { new SortField("color", SortFieldType.CUSTOM, false), 
                new SortField("number", SortFieldType.CUSTOM, true) };

            DoTest(br, 7, null, new String[] { "5", "4", "6", "3", "2", "1", "7" });

            // now test with serialization
            using (BoboBrowser boboBrowser = NewBrowser())
            {
                BrowseResult result = boboBrowser.Browse(br);
                try
                {
#if FEATURE_SERIALIZABLE
                    BinaryFormatter formatter = new BinaryFormatter();
                    using (var stream = new MemoryStream())
                    {
                        // Serialize to a stream
                        formatter.Serialize(stream, result);

                        // Return to the beginning of the stream
                        stream.Seek(0, SeekOrigin.Begin);

                        // Deserialize from the stream
                        result = (BrowseResult)formatter.Deserialize(stream);
                    }
#endif

                    DoTest(result, br, 7, null, new string[] { "5", "4", "6", "3", "2", "1", "7" });
                }
                finally
                {
                    if (result != null) result.Dispose();
                }
            }
        }

        [Test]
        public void TestSort()
        {
            // no sel
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            br.Sort = new SortField[] { new SortField("number", SortFieldType.CUSTOM, true) };
            DoTest(br, 7, null, new string[] { "6", "5", "4", "3", "2", "1", "7" });
            br.Sort = new SortField[] { new SortField("name", SortFieldType.STRING, false) };
            DoTest(br, 7, null, new string[] { "7", "4", "6", "2", "3", "1", "5" });

            BrowseSelection sel = new BrowseSelection("color");
            sel.AddValue("red");
            br.AddSelection(sel);
            br.Sort = new SortField[] { new SortField("number", SortFieldType.CUSTOM, true) };
            DoTest(br, 3, null, new string[] { "2", "1", "7" });
            br.Sort = new SortField[] { new SortField("name", SortFieldType.STRING, false) };
            DoTest(br, 3, null, new string[] { "7", "2", "1" });

            sel.AddValue("blue");
            br.Query = new TermQuery(new Term("shape", "square"));
            br.Sort = new SortField[] { new SortField("number", SortFieldType.CUSTOM, true) };
            DoTest(br, 3, null, new string[] { "5", "1", "7" });
            br.Sort = new SortField[] { new SortField("name", SortFieldType.STRING, false) };
            DoTest(br, 3, null, new string[] { "7", "1", "5" });
        }

        [Test]
        public void TestCustomSort()
        {
            // no sel
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            br.Sort = new SortField[] { new BoboCustomSortField("custom", false, 
                new CustomSortComparerSource()) };
            DoTest(br, 7, null, new string[] { "5", "4", "6", "3", "7", "2", "1" });
        }

        private class CustomSortComparerSource : DocComparerSource
        {
            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                return new CustomSortDocComparer();
            }

            public class CustomSortDocComparer : DocComparer
            {
                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    int id1 = Math.Abs(doc1.Doc - 4);
                    int id2 = Math.Abs(doc2.Doc - 4);
                    int val = id1 - id2;
                    if (val == 0)
                    {
                        return doc1.Doc - doc2.Doc;
                    }
                    return val;
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    return (int)Math.Abs(doc.Doc - 4);
                }
            }
        }


        [Test]
        public void TestDefaultBrowse()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 3;
            br.Offset = 0;

            FacetSpec spec = new FacetSpec();
            spec.MaxCount = 2;
            spec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            br.SetFacetSpec("color", spec);

            br.Sort = new SortField[] { new SortField("number", SortFieldType.CUSTOM, false) };

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("red", 3), new BrowseFacet("blue", 2) } }
            };

            DoTest(br, 7, answer, new string[] { "7", "1", "2" });
        }

        [Test]
        public void TestMinHit()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 3;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("shape");
            sel.AddValue("square");
            br.AddSelection(sel);

            FacetSpec spec = new FacetSpec();
            spec.MinHitCount = 0;
            spec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            br.SetFacetSpec("color", spec);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] {  new BrowseFacet("red", 2), new BrowseFacet("blue", 1), 
                    new BrowseFacet("green", 0) } }
            };

            DoTest(br, 3, answer, null);
        }

        [Test]
        public void TestRandomAccessFacet()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;
            br.SetFacetSpec("number", new FacetSpec());

            BoboBrowser browser = NewBrowser();

            using (BrowseResult res = browser.Browse(br))
            {
                IFacetAccessible facetAccessor = res.GetFacetAccessor("number");
                BrowseFacet facet = facetAccessor.GetFacet("5");

                Assert.AreEqual(facet.Value, "0005");
                Assert.AreEqual(facet.FacetValueHitCount, 1);
            }
        }

        /*
        [Test]
        public void TestQueryWithScore()
        {
            BrowseRequest br = new BrowseRequest();
            br.ShowExplanation = (false);	// default
            QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "color", new StandardAnalyzer(
                LuceneVersion.LUCENE_48));
            br.Query = parser.Parse("color:red OR shape:square");
            br.Count = (10);
            br.Offset = (0);

            br.Sort = (new SortField[] { SortField.FIELD_SCORE });
            BrowseResult res = DoTest(br, 4, null, new string[] { "1", "7", "2", "5" });

            BrowseHit[] hits = res.Hits;
            foreach (BrowseHit hit in hits)
            {
                Assert.Null(hit.Explanation);
            }

            br.ShowExplanation = (true);
            res = DoTest(br, 4, null, new string[] { "1", "7", "2", "5" });
            hits = res.Hits;
            foreach (BrowseHit hit in hits)
            {
                Assert.NotNull(hit.Explanation);
            }

            var rawQuery = br.Query;

            long fromTime = new DateTime(2006, 6, 1).ToBinary();

            RecencyBoostScorerBuilder recencyBuilder = new RecencyBoostScorerBuilder("date", 2.0f, 
                TimeUnit.DAYS.Convert(fromTime, TimeUnit.MILLISECONDS), 30L, TimeUnit.DAYS);
            ScoreAdjusterQuery sq = new ScoreAdjusterQuery(rawQuery, recencyBuilder);
            br.Query = (sq);

            res = DoTest(br, 4, null, new string[] { "7", "1", "2", "5" });

            hits = res.Hits;
            foreach (BrowseHit hit in hits)
            {
                Assert.NotNull(hit.Explanation);
                Console.WriteLine(hit.Explanation);
            }
        }

        [Test]
        public void TestBrowseWithQuery()
        {
            try
            {
                BrowseRequest br = new BrowseRequest();
                QueryParser parser = new QueryParser(LuceneVersion.LUCENE_48, "shape", new StandardAnalyzer(
                    LuceneVersion.LUCENE_48));
                br.Query = parser.Parse("square OR circle");
                br.Count = 10;
                br.Offset = 0;

                BrowseSelection sel = new BrowseSelection("color");
                sel.AddValue("red");
                br.AddSelection(sel);

                br.Sort = new SortField[] { new SortField("number", SortField.Type_e.CUSTOM, false) };
                DoTest(br, 2, null, new string[] { "7", "1" });

                FacetSpec ospec = new FacetSpec();
                ospec.ExpandSelection = true;
                br.SetFacetSpec("color", ospec);
                var answer = new Dictionary<string, IList<BrowseFacet>>()
                {
                    { "color", new BrowseFacet[] { new BrowseFacet("blue", 2), new BrowseFacet("green", 1), 
                        new BrowseFacet("red", 2) } }
                };
                DoTest(br, 2, answer, new string[] { "7", "1" });

                br.ClearSelections();
                answer = new Dictionary<string, IList<BrowseFacet>>()
                {
                    { "color", new BrowseFacet[] { new BrowseFacet("blue", 2), new BrowseFacet("green", 1), 
                        new BrowseFacet("red", 2) } }
                };
                DoTest(br, 5, answer, new string[] { "7", "1", "3", "4", "5" });
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }
        */

        [Test]
        public void TestBrowseCompactMultiVal()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;
            BrowseSelection sel = new BrowseSelection("compactnum");
            sel.AddValue("001");
            sel.AddValue("003");
            sel.AddValue("007");
            br.AddSelection(sel);

            FacetSpec ospec = new FacetSpec();
            br.SetFacetSpec("compactnum", ospec);

            br.Sort = new SortField[] { new SortField("compactnum", SortFieldType.CUSTOM, true) };

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "compactnum", new BrowseFacet[] { new BrowseFacet("001", 3), new BrowseFacet("002", 1), 
                    new BrowseFacet("003", 3), new BrowseFacet("007", 2), new BrowseFacet("008", 1), 
                    new BrowseFacet("012", 1) } }
            };

            DoTest(br, 6, answer, new string[] { "3", "7", "4", "6", "1", "5" });

            br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;
            sel = new BrowseSelection("compactnum");
            sel.AddValue("001");
            sel.AddValue("002");
            sel.AddValue("003");
            br.AddSelection(sel);
            sel.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationAnd;
            DoTest(br, 1, null, new string[] { "6" });

            br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;
            sel = new BrowseSelection("compactnum");
            sel.AddValue("001");
            sel.AddValue("003");
            sel.AddValue("008");
            sel.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationOr;
            br.AddSelection(sel);

            sel = new BrowseSelection("color");
            sel.AddValue("red");
            br.AddSelection(sel);

            ospec = new FacetSpec();
            br.SetFacetSpec("color", ospec);

            ospec = new FacetSpec();
            br.SetFacetSpec("compactnum", ospec);
            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "compactnum", new BrowseFacet[] { new BrowseFacet("001", 1), new BrowseFacet("003", 2), 
                    new BrowseFacet("008", 1) } },
                { "color", new BrowseFacet[] { new BrowseFacet("red", 2) } }
            };

            DoTest(br, 2, answer, new string[] { "1", "7" });

            // NOTE: Original source did test twice - not sure if we really need to.
            DoTest(br, 2, answer, new string[] { "1", "7" });
        }

        [Test]
        public void TestBrowseMultiValWithWeight()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;
            BrowseSelection sel = new BrowseSelection("multiwithweight");
            sel.AddValue("cool");
            br.AddSelection(sel);


            FacetSpec ospec = new FacetSpec();
            br.SetFacetSpec("multiwithweight", ospec);
            br.Sort = new SortField[] { new SortField("multiwithweight", SortFieldType.CUSTOM, true) };

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "multiwithweight", new BrowseFacet[] { new BrowseFacet("cool", 3), new BrowseFacet("good", 2) } }
            };

            DoTest(br, 3, answer, new string[] { "1", "2", "3" });
        }

        [Test]
        public void TestMultiWithDiffName()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("diffname");
            sel.AddValue("001");

            br.AddSelection(sel);

            DoTest(br, 3, null, new string[] { "1", "5", "6" });
        }

        [Test]
        public void TestBrowseMultiVal()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;
            BrowseSelection sel = new BrowseSelection("multinum");
            sel.AddValue("001");
            sel.AddValue("003");
            sel.AddValue("007");
            br.AddSelection(sel);

            FacetSpec ospec = new FacetSpec();
            br.SetFacetSpec("multinum", ospec);
            br.Sort = new SortField[] { new SortField("multinum", SortFieldType.CUSTOM, true) };
            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "multinum", new BrowseFacet[] { new BrowseFacet("001", 3), new BrowseFacet("002", 1), 
                    new BrowseFacet("003", 3), new BrowseFacet("007", 2), new BrowseFacet("008", 1), 
                    new BrowseFacet("012", 1) } }
            };

            DoTest(br, 6, answer, new string[] { "3", "4", "7", "1", "6", "5" });

            br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;
            sel = new BrowseSelection("multinum");
            sel.AddValue("001");
            sel.AddValue("002");
            sel.AddValue("003");
            br.AddSelection(sel);
            sel.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationAnd;
            DoTest(br, 1, null, new string[] { "6" });

            br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;
            sel = new BrowseSelection("multinum");
            sel.AddValue("001");
            sel.AddValue("003");
            sel.AddValue("008");
            sel.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationOr;
            br.AddSelection(sel);

            sel = new BrowseSelection("color");
            sel.AddValue("red");
            br.AddSelection(sel);

            ospec = new FacetSpec();
            br.SetFacetSpec("color", ospec);

            ospec = new FacetSpec();
            br.SetFacetSpec("multinum", ospec);
            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "multinum", new BrowseFacet[] { new BrowseFacet("001", 1), new BrowseFacet("003", 2), 
                    new BrowseFacet("008", 1) } },
                { "color", new BrowseFacet[] { new BrowseFacet("red", 2) } }
            };

            DoTest(br, 2, answer, new string[] { "1", "7" });
        }

        [Test]
        public void TestBrowseWithDeletes()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("color");
            sel.AddValue("red");
            br.AddSelection(sel);

            var answer = new Dictionary<string, IList<BrowseFacet>>();

            DoTest(br, 3, answer, new string[] { "1", "2", "7" });

            Analyzer analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
            IndexWriterConfig config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);
            using (IndexWriter idxWriter = new IndexWriter(_indexDir, config))
            {
                idxWriter.DeleteDocuments(new Term("id", "1"));
                idxWriter.DeleteDocuments(new Term("id", "2"));
                idxWriter.Commit();
                using (BoboMultiReader reader = NewIndexReader())
                {
                    br = new BrowseRequest();
                    br.Count = 10;
                    br.Offset = 0;

                    sel = new BrowseSelection("color");
                    sel.AddValue("red");
                    br.AddSelection(sel);
                    answer = new Dictionary<string, IList<BrowseFacet>>();

                    DoTest(new BoboBrowser(reader), br, 1, answer, new string[] { "7" });
                }
            }

            br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            sel = new BrowseSelection("color");
            sel.AddValue("red");
            br.AddSelection(sel);
            answer = new Dictionary<string, IList<BrowseFacet>>();

            DoTest(br, 1, answer, new string[] { "7" });
        }

        [Test]
        public void TestNotSupport()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("color");
            sel.AddNotValue("red");
            br.AddSelection(sel);

            FacetSpec simpleOutput = new FacetSpec();
            br.SetFacetSpec("shape", simpleOutput);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "shape", new BrowseFacet[] { new BrowseFacet("circle", 2), 
                    new BrowseFacet("rectangle", 1), new BrowseFacet("square", 1) } }
            };

            DoTest(br, 4, answer, new string[] { "3", "4", "5", "6" });

            sel.AddNotValue("green");

            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "shape", new BrowseFacet[] { new BrowseFacet("circle", 1), 
                    new BrowseFacet("square", 1) } }
            };

            DoTest(br, 2, answer, new string[] { "4", "5" });

            br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;
            sel = new BrowseSelection("compactnum");
            sel.AddNotValue("3");
            sel.AddNotValue("4");
            sel.AddValue("1");
            sel.AddValue("2");
            sel.AddValue("7");

            br.AddSelection(sel);
            DoTest(br, 3, null, new string[] { "3", "4", "5" });

            br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;
            sel = new BrowseSelection("multinum");
            sel.AddNotValue("3");
            sel.AddNotValue("4");
            sel.AddValue("1");
            sel.AddValue("2");
            sel.AddValue("7");

            br.AddSelection(sel);

            DoTest(br, 3, null, new string[] { "3", "4", "5" });
        }

        [Test]
        public void TestMissedSelection()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;
            BrowseSelection sel = new BrowseSelection("location");
            sel.AddValue("something/stupid");
            br.AddSelection(sel);
            DoTest(br, 0, null, null);
        }

        [Test]
        public void TestDateRange()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            FacetSpec simpleOutput = new FacetSpec();
            simpleOutput.ExpandSelection = true;
            br.SetFacetSpec("date", simpleOutput);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "date", new BrowseFacet[] { new BrowseFacet("[2000/01/01 TO 2003/05/05]", 4), 
                    new BrowseFacet("[2003/05/06 TO 2005/04/04]", 1) } }
            };

            DoTest(br, 7, answer, null);
        }

        [Test]
        public void TestNewRangeFacet()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            FacetSpec simpleOutput = new FacetSpec();
            simpleOutput.ExpandSelection = (true);
            br.SetFacetSpec("date", simpleOutput);

            //	  d1.add(buildMetaField("date","2000/01/01"));
            //	  d3.add(buildMetaField("date","2001/12/25"));
            //	  d5.add(buildMetaField("date","2002/03/08"));
            //	  d2.add(buildMetaField("date","2003/02/14"));
            //	  d4.add(buildMetaField("date","2004/11/24"));
            //	  d7.add(buildMetaField("date","2006/06/01"));
            //	  d6.add(buildMetaField("date","2007/08/01"));

            BrowseSelection sel1 = new BrowseSelection("date");
            sel1.Values = new string[] { "(2000/01/01 TO 2003/02/14]" };
            BrowseSelection sel2 = new BrowseSelection("date");
            sel2.Values = new string[] { "(2000/01/01 TO 2003/02/14)" };


            br.AddSelection(sel1);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "date", new BrowseFacet[] { new BrowseFacet("[2000/01/01 TO 2003/02/14]", 4), 
                    new BrowseFacet("[2003/05/06 TO 2005/04/04]", 1) } }
            };
            DoTest(br, 3, null, null);

            br.ClearSelections();
            br.AddSelection(sel2);
            DoTest(br, 2, null, null);
        }


        [Test(Description = "Verifies the range facet numbers are returned correctly (as they were passed in)")]
        public void TestNumEndorsers()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            FacetSpec simpleOutput = new FacetSpec();
            simpleOutput.ExpandSelection = true;
            br.SetFacetSpec("numendorsers", simpleOutput);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "numendorsers", new BrowseFacet[] { new BrowseFacet("[000000 TO 000005]", 2), 
                    new BrowseFacet("[000006 TO 000010]", 2), new BrowseFacet("[000011 TO 000020]", 3) } }
            };
            DoTest(br, 7, answer, null);
        }

        [Test]
        public void TestBrowse()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("color");
            sel.AddValue("red");
            br.AddSelection(sel);

            sel = new BrowseSelection("location");
            sel.AddValue("toy/lego");

            IDictionary<string, string> prop = sel.SelectionProperties;
            PathFacetHandler.SetDepth(prop, 1);
            br.AddSelection(sel);

            sel = new BrowseSelection("size");
            sel.AddValue("[* TO 4]");

            sel = new BrowseSelection("tag");
            sel.AddValue("rabbit");
            br.AddSelection(sel);

            FacetSpec output = new FacetSpec();
            output.MaxCount = 5;

            FacetSpec simpleOutput = new FacetSpec();
            simpleOutput.ExpandSelection = true;


            br.SetFacetSpec("color", simpleOutput);
            br.SetFacetSpec("size", output);
            br.SetFacetSpec("shape", simpleOutput);
            br.SetFacetSpec("location", output);

            FacetSpec tagOutput = new FacetSpec();
            tagOutput.MaxCount = 5;
            tagOutput.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;

            br.SetFacetSpec("tag", tagOutput);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("green", 1), new BrowseFacet("red", 2) } },
                { "size", new BrowseFacet[] { new BrowseFacet("[* TO 4]", 1), 
                    new BrowseFacet("[5 TO 8]", 1) } },
                { "shape", new BrowseFacet[] { new BrowseFacet("square", 2) } },
                { "location", new BrowseFacet[] { new BrowseFacet("toy/lego/", 1), 
                    new BrowseFacet("toy/lego/block", 1) } },
                { "tag", new BrowseFacet[] { new BrowseFacet("rabbit", 2), new BrowseFacet("animal", 1), 
                    new BrowseFacet("dog", 1), new BrowseFacet("humane", 1), new BrowseFacet("pet", 1) } }
            };

            DoTest(br, 2, answer, null);
        }

        /// <summary>
        /// Tests the MultiBoboBrowser functionality by creating a BoboBrowser and 
        /// submitting the same browserequest 2 times generating 2 BrowseResults.
        /// The 2 BoboBrowsers are instantiated with the MultiBoboBrowser and the browse method is called.
        /// The BrowseResult generated is submitted to the doTest method which compares the result
        /// </summary>
        [Test]
        public void TestMultiBrowser()
        {
            BrowseRequest browseRequest = new BrowseRequest();
            browseRequest.Count = 10;
            browseRequest.Offset = 0;
            browseRequest.AddSortField(new SortField("date", SortFieldType.CUSTOM));

            BrowseSelection colorSel = new BrowseSelection("color");
            colorSel.AddValue("red");
            browseRequest.AddSelection(colorSel);

            BrowseSelection tageSel = new BrowseSelection("tag");
            tageSel.AddValue("rabbit");
            browseRequest.AddSelection(tageSel);

            FacetSpec colorFacetSpec = new FacetSpec();
            colorFacetSpec.ExpandSelection = true;
            colorFacetSpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;

            FacetSpec tagFacetSpec = new FacetSpec();

            browseRequest.SetFacetSpec("color", colorFacetSpec);
            browseRequest.SetFacetSpec("tag", tagFacetSpec);

            FacetSpec shapeSpec = new FacetSpec();
            shapeSpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            browseRequest.SetFacetSpec("shape", shapeSpec);

            FacetSpec dateSpec = new FacetSpec();
            dateSpec.ExpandSelection = true;
            browseRequest.SetFacetSpec("date", dateSpec);

            BoboBrowser boboBrowser = NewBrowser();

            browseRequest.Sort = new SortField[] { new SortField("compactnum", SortFieldType.CUSTOM, true) };

            using (MultiBoboBrowser multiBoboBrowser = new MultiBoboBrowser(new IBrowsable[] { boboBrowser, 
                boboBrowser }))
            {
                var answer = new Dictionary<string, IList<BrowseFacet>>()
                {
                    { "color", new BrowseFacet[] {  new BrowseFacet("red", 4), new BrowseFacet("green", 2) } },
                    { "tag", new BrowseFacet[] { new BrowseFacet("animal", 2), new BrowseFacet("dog", 2), 
                        new BrowseFacet("humane", 2), new BrowseFacet("pet", 2), new BrowseFacet("rabbit", 4) } },
                    { "shape", new BrowseFacet[] {  new BrowseFacet("square", 4) } },
                    { "date", new BrowseFacet[] { new BrowseFacet("[2000/01/01 TO 2003/05/05]", 2) } }              
                };

                using (BrowseResult mergedResult = multiBoboBrowser.Browse(browseRequest))
                {
                    DoTest(mergedResult, browseRequest, 4, answer, new string[] { "7", "7", "1", "1" });
                }

                browseRequest.Sort = new SortField[] { new SortField("multinum", SortFieldType.CUSTOM, true) };
                using (BrowseResult mergedResult = multiBoboBrowser.Browse(browseRequest))
                {
                    DoTest(mergedResult, browseRequest, 4, answer, new string[] { "7", "7", "1", "1" });
                }
            }
        }

        [Test]
        public void TestFacetQueryBoost()
        {
            BrowseSelection sel = new BrowseSelection("color");
            sel.AddValue("red");
            sel.AddValue("blue");

            var map = new Dictionary<string, float>()
            {
                { "red", 5.0f },
                { "blue", 4.0f }
            };
            FacetTermQuery colorQ = new FacetTermQuery(sel, map);

            BrowseSelection sel2 = new BrowseSelection("shape");
            sel2.AddValue("circle");
            sel2.AddValue("square");
            var map2 = new Dictionary<string, float>()
            {
                { "circle", 3.0f },
                { "square", 2.0f }
            };
            FacetTermQuery shapeQ = new FacetTermQuery(sel2, map2);
            shapeQ.Boost = 3.0f;

            BooleanQuery bq = new BooleanQuery();
            bq.Add(shapeQ, Occur.SHOULD);
            bq.Add(colorQ, Occur.SHOULD);

            BrowseRequest br = new BrowseRequest();
            br.Sort = new SortField[] { SortField.FIELD_SCORE };
            br.Query = bq;
            br.Offset = 0;
            br.Count = 10;

            BrowseResult res = DoTest(br, 6, null, new string[] { "4", "1", "7", "5", "3", "2" });
            BrowseHit[] hits = res.Hits;
            float[] scores = new float[] { 13, 11, 11, 10, 4.5f, 2.5f };  // default coord = 1/2
            for (int i = 0; i < hits.Length; ++i)
            {
                Assert.AreEqual(scores[i], hits[i].Score);
            }
        }

        [Test]
        public void TestFacetQuery()
        {
            BrowseSelection sel = new BrowseSelection("color");
            sel.AddValue("red");
            sel.AddValue("blue");
            var map = new Dictionary<string, float>();
            map.Add("red", 3.0f);
            map.Add("blue", 2.0f);
            FacetTermQuery colorQ = new FacetTermQuery(sel, map);

            BrowseSelection sel2 = new BrowseSelection("tag");
            sel2.AddValue("rabbit");
            sel2.AddValue("dog");
            var map2 = new Dictionary<string, float>();
            map2.Add("rabbit", 100.0f);
            map2.Add("dog", 50.0f);
            FacetTermQuery tagQ = new FacetTermQuery(sel2, map2);

            BrowseRequest br = new BrowseRequest();
            br.Query = colorQ;
            br.Offset = 0;
            br.Count = 10;

            DoTest(br, 5, null, new string[] { "1", "2", "7", "4", "5" });

            //BoboBrowser b = NewBrowser();
            //Explanation expl = b.Explain(colorQ, 0);

            br.Query = tagQ;
            DoTest(br, 4, null, new string[] { "7", "1", "3", "2" });
            //	expl = b.Explain(tagQ, 6);
        }

        [Test]
        public void TestFacetQueryBoolean()
        {
            BrowseSelection sel = new BrowseSelection("color");
            sel.AddValue("red");
            sel.AddValue("blue");
            var map = new Dictionary<String, float>();
            map.Put("red", 3.0f);
            map.Put("blue", 2.0f);
            FacetTermQuery colorQ = new FacetTermQuery(sel, map);

            BrowseSelection sel2 = new BrowseSelection("tag");
            sel2.AddValue("rabbit");
            sel2.AddValue("dog");
            var map2 = new Dictionary<String, float>();
            map2.Put("rabbit", 100.0f);
            map2.Put("dog", 50.0f);
            FacetTermQuery tagQ = new FacetTermQuery(sel2, map2);

            BrowseRequest br = new BrowseRequest();

            br.Offset = 0;
            br.Count = 10;

            BooleanQuery bq = new BooleanQuery(true);
            bq.Add(colorQ, Occur.SHOULD);
            bq.Add(tagQ, Occur.SHOULD);

            br.Query = bq;
            DoTest(br, 6, null, new String[] { "7", "1", "3", "2", "4", "5" });
        }

        [Test]
        public void TestFacetRangeQuery()
        {
            BrowseSelection sel = new BrowseSelection("numendorsers");
            sel.AddValue("[* TO 000010]");

            var map = new Dictionary<string, float>();
            map.Put("000002", 100.0f);
            map.Put("000010", 50.0f);
            FacetTermQuery numberQ = new FacetTermQuery(sel, map);

            BrowseRequest br = new BrowseRequest();
            br.Query = numberQ;
            br.Offset = 0;
            br.Count = 10;

            DoTest(br, 4, null, new string[] { "5", "2", "1", "6" });
        }

        [Test]
        public void TestFacetBoost()
        {
            var boostMaps = new Dictionary<string, IDictionary<string, float>>();

            var map = new Dictionary<string, float>();
            map.Put("red", 3.0f);
            map.Put("blue", 2.0f);
            boostMaps.Put("color", map);

            map = new Dictionary<string, float>();
            map.Put("rabbit", 5.0f);
            map.Put("dog", 7.0f);
            boostMaps.Put("tag", map);

            var q = new ScoreAdjusterQuery(new MatchAllDocsQuery(), new FacetBasedBoostScorerBuilder(
                boostMaps));

            BrowseRequest br = new BrowseRequest();
            br.Query = q;
            br.Offset = 0;
            br.Count = 10;
            br.Sort = new SortField[] { SortField.FIELD_SCORE };
            BoboBrowser b = NewBrowser();

            BrowseResult r = b.Browse(br);

            DoTest(r, br, 7, null, new string[] { "7", "2", "1", "3", "4", "5", "6" });
        }

        [Test]
        public void TestRuntimeFilteredDateRange()
        {
            BoboBrowser browser = NewBrowser();
            string[] ranges = new string[] { "[2001/01/01 TO 2001/12/30]", "[2007/01/01 TO 2007/12/30]" };
            FilteredRangeFacetHandler handler = new FilteredRangeFacetHandler("filtered_date", "date", ranges);
            browser.SetFacetHandler(handler);

            BrowseRequest req = new BrowseRequest();
            req.SetFacetSpec("filtered_date", new FacetSpec());

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "filtered_date", new BrowseFacet[] { new BrowseFacet("[2001/01/01 TO 2001/12/30]", 1), 
                    new BrowseFacet("[2007/01/01 TO 2007/12/30]", 1) } }          
            };

            DoTest(browser, req, 7, answer, null);
        }

        [Test]
        public void TestCustomFacetSort()
        {
            BrowseRequest req = new BrowseRequest();
            FacetSpec numberSpec = new FacetSpec();
            numberSpec.CustomComparerFactory = new TestCustomFacetSortComparerFactory();

            numberSpec.OrderBy = FacetSpec.FacetSortSpec.OrderByCustom;
            numberSpec.MaxCount = 3;
            req.SetFacetSpec("number", numberSpec);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "number", new BrowseFacet[] { new BrowseFacet("2130", 1), new BrowseFacet("1013", 1), 
                    new BrowseFacet("0913", 1) } }          
            };

            DoTest(req, 7, answer, null);

            numberSpec.OrderBy = FacetSpec.FacetSortSpec.OrderValueAsc;
            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "number", new BrowseFacet[] { new BrowseFacet("0005", 1), new BrowseFacet("0010", 1), 
                    new BrowseFacet("0011", 1) } }          
            };

            DoTest(req, 7, answer, null);
        }

        private class TestCustomFacetSortComparerFactory : IComparerFactory
        {

            public IComparer<int> NewComparer(IFieldValueAccessor fieldValueAccessor, BigSegmentedArray counts)
            {
                return new TestCustomFacetSortIntComparer(fieldValueAccessor, counts);
            }

            public IComparer<BrowseFacet> NewComparer()
            {
                return new TestCustomFacetSortBrowseFacetComparer();
            }

            public class TestCustomFacetSortIntComparer : IComparer<int>
            {
                private readonly IFieldValueAccessor fieldValueAccessor;
                private readonly BigSegmentedArray counts;

                public TestCustomFacetSortIntComparer(IFieldValueAccessor fieldValueAccessor, BigSegmentedArray counts)
                {
                    this.fieldValueAccessor = fieldValueAccessor;
                    this.counts = counts;
                }

                public int Compare(int v1, int v2)
                {
                    var size1 = (int)this.fieldValueAccessor.GetRawValue(v1);
                    var size2 = (int)this.fieldValueAccessor.GetRawValue(v2);

                    int val = size1 - size2;
                    if (val == 0)
                    {
                        val = counts.Get(v1) - counts.Get(v2);
                    }
                    return val;
                }
            }

            public class TestCustomFacetSortBrowseFacetComparer : IComparer<BrowseFacet>
            {
                public int Compare(BrowseFacet o1, BrowseFacet o2)
                {
                    int v1 = Convert.ToInt32(o1.Value);
                    int v2 = Convert.ToInt32(o2.Value);
                    int val = v2 - v1;
                    if (val == 0)
                    {
                        val = o2.FacetValueHitCount - o1.FacetValueHitCount;
                    }
                    return val;
                }
            }
        }

        [Test]
        public void TestSimpleGroupbyFacetHandler()
        {
            BrowseRequest req = new BrowseRequest();
            FacetSpec fspec = new FacetSpec();
            req.SetFacetSpec("groupby", fspec);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "groupby", new BrowseFacet[] { new BrowseFacet("red,rectangle,0011", 1), 
                    new BrowseFacet("red,square,0005", 1), new BrowseFacet("red,square,0010", 1) } }          
            };

            BrowseSelection sel = new BrowseSelection("groupby");
            sel.AddValue("red");
            req.AddSelection(sel);

            DoTest(req, 3, answer, null);

            sel.Values = new string[] { "red,square" };
            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "groupby", new BrowseFacet[] { new BrowseFacet("red,square,0005", 1), 
                    new BrowseFacet("red,square,0010", 1) } }          
            };

            DoTest(req, 2, answer, null);

            sel.Values = new string[] { "red,square,0005" };
            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "groupby", new BrowseFacet[] { new BrowseFacet("red,square,0005",1) } }          
            };

            DoTest(req, 1, answer, null);

            req.RemoveSelection("groupby");
            fspec.MaxCount = 2;
            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "groupby", new BrowseFacet[] { new BrowseFacet("blue,circle,0913",1),
                    new BrowseFacet("blue,square,1013",1) } }          
            };

            DoTest(req, 7, answer, null);
        }

        [Test]
        public void TestTime()
        {
            var facetHandlers = new List<IFacetHandler>();
            /* Underlying time facet for DynamicTimeRangeFacetHandler */
            facetHandlers.Add(new RangeFacetHandler("timeinmillis", new PredefinedTermListFactory<long>(
                DynamicTimeRangeFacetHandler.NUMBER_FORMAT), null));
            Lucene.Net.Store.Directory idxDir = new RAMDirectory();
            Analyzer analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
            IndexWriterConfig config = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);

            // This method returns the difference, measured in milliseconds, between the current 
            // time and midnight, January 1, 1970 UTC(coordinated universal time).
            // long now = System.currentTimeMillis();
            long now = DateTime.Now.GetTime();
            using (IndexWriter idxWriter = new IndexWriter(idxDir, config))
            {
                for (long l = 0; l < 53; l++)
                {
                    Document d = new Document();
                    d.Add(BuildMetaField("timeinmillis", (now - l * 3500000).ToString(DynamicTimeRangeFacetHandler.NUMBER_FORMAT)));
                    idxWriter.AddDocument(d);
                    idxWriter.ForceMerge(1);
                    idxWriter.Commit();
                }
            }

            DirectoryReader idxReader = DirectoryReader.Open(idxDir);
            BoboMultiReader boboReader = BoboMultiReader.GetInstance(idxReader, facetHandlers);
            BoboBrowser browser = new BoboBrowser(boboReader);
            List<string> ranges = new List<string>();
            ranges.Add("000000001");
            ranges.Add("000010000");// one hour
            ranges.Add("000020000");// two hours
            ranges.Add("000030000");
            ranges.Add("000040000");
            ranges.Add("001000000");// one day
            ranges.Add("002000000");// two days
            ranges.Add("003000000");
            ranges.Add("004000000");
            IFacetHandler facetHandler = new DynamicTimeRangeFacetHandler("timerange", "timeinmillis", 
                now, ranges);
            browser.SetFacetHandler(facetHandler);
            //  
            BrowseRequest req = new BrowseRequest();
            BrowseFacet facet = null;
            FacetSpec facetSpec = new FacetSpec();
            req.SetFacetSpec("timerange", facetSpec);
            BrowseResult result = browser.Browse(req);
            IFacetAccessible facetholder = result.GetFacetAccessor("timerange");
            ICollection<BrowseFacet> facets = facetholder.GetFacets();
            facet = facets.Get(0);
            Assert.AreEqual("000000001", facet.Value, "order by value");
            Assert.AreEqual(1, facet.FacetValueHitCount, "order by value");
            facet = facets.Get(1);
            Assert.AreEqual("000010000", facet.Value, "order by value");
            Assert.AreEqual(1, facet.FacetValueHitCount, "order by value");
            facet = facets.Get(5);
            Assert.AreEqual("001000000", facet.Value, "order by value");
            Assert.AreEqual(20, facet.FacetValueHitCount, "order by value");
            facet = facets.Get(7);
            Assert.AreEqual("003000000", facet.Value, "order by value");
            Assert.AreEqual(3, facet.FacetValueHitCount, "order by value");
            //  
            req = new BrowseRequest();
            facetSpec = new FacetSpec();
            facetSpec.MinHitCount = (0);
            facetSpec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            req.SetFacetSpec("timerange", facetSpec);
            result = browser.Browse(req);
            facetholder = result.GetFacetAccessor("timerange");
            facets = facetholder.GetFacets();
            facet = facets.Get(0);
            Assert.AreEqual("002000000", facet.Value);
            Assert.AreEqual(25, facet.FacetValueHitCount);
            facet = facets.Get(1);
            Assert.AreEqual("001000000", facet.Value);
            Assert.AreEqual(20, facet.FacetValueHitCount);
            facet = facets.Get(2);
            Assert.AreEqual("003000000", facet.Value);
            Assert.AreEqual(3, facet.FacetValueHitCount);
            facet = facets.Get(8);
            Assert.AreEqual("004000000", facet.Value, "minCount=0");
            Assert.AreEqual(0, facet.FacetValueHitCount, "minCount=0");
            //  
            req = new BrowseRequest();
            facetSpec = new FacetSpec();
            BrowseSelection sel = new BrowseSelection("timerange");
            sel.AddValue("001000000");
            req.AddSelection(sel);
            facetSpec.ExpandSelection = (true);
            req.SetFacetSpec("timerange", facetSpec);
            result = browser.Browse(req);
            facetholder = result.GetFacetAccessor("timerange");
            facets = facetholder.GetFacets();
            facet = facets.Get(0);
            Assert.AreEqual("000000001", facet.Value);
            Assert.AreEqual(1, facet.FacetValueHitCount);
            facet = facets.Get(6);
            Assert.AreEqual("002000000", facet.Value);
            Assert.AreEqual(25, facet.FacetValueHitCount);
            facet = facets.Get(7);
            Assert.AreEqual("003000000", facet.Value);
            Assert.AreEqual(3, facet.FacetValueHitCount);
            //  
            req = new BrowseRequest();
            facetSpec = new FacetSpec();
            sel = new BrowseSelection("timerange");
            sel.AddValue("001000000");
            sel.AddValue("003000000");
            sel.AddValue("004000000");
            req.AddSelection(sel);
            facetSpec.ExpandSelection = (false);
            req.SetFacetSpec("timerange", facetSpec);
            result = browser.Browse(req);
            facetholder = result.GetFacetAccessor("timerange");
            facet = facetholder.GetFacet("001000000");
            Assert.AreEqual(20, facet.FacetValueHitCount, "001000000");
            facet = facetholder.GetFacet("003000000");
            Assert.AreEqual(3, facet.FacetValueHitCount, "003000000");
            facet = facetholder.GetFacet("004000000");
            Assert.AreEqual(0, facet.FacetValueHitCount, "004000000");
            Assert.AreEqual(23, result.NumHits, "");
        }

        [Test]
        public void TestHistogramFacetHandler()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = (0);
            br.Offset = (0);

            FacetSpec output = new FacetSpec();
            output.MaxCount = (100);
            output.MinHitCount = (1);
            br.SetFacetSpec("numberhisto", output);

            BrowseFacet[] answerBucketFacets = new BrowseFacet[5];
            answerBucketFacets[0] = new BrowseFacet("0000000000", 3);
            answerBucketFacets[1] = new BrowseFacet("0000000002", 1);
            answerBucketFacets[2] = new BrowseFacet("0000000009", 1);
            answerBucketFacets[3] = new BrowseFacet("0000000010", 1);
            answerBucketFacets[4] = new BrowseFacet("0000000021", 1);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "numberhisto", answerBucketFacets }          
            };

            DoTest(br, 7, answer, null);

            // now with selection

            BrowseSelection sel = new BrowseSelection("color");
            sel.AddValue("green");
            br.AddSelection(sel);

            answerBucketFacets = new BrowseFacet[2];
            answerBucketFacets[0] = new BrowseFacet("0000000002", 1);
            answerBucketFacets[1] = new BrowseFacet("0000000021", 1);

            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "numberhisto", answerBucketFacets }          
            };

            DoTest(br, 2, answer, null);
        }

        [Test]
        public void TestBucketFacetHandlerForNumbers()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            FacetSpec output = new FacetSpec();
            output.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            br.SetFacetSpec("sets", output);

            BrowseFacet[] answerBucketFacets = new BrowseFacet[3];
            answerBucketFacets[0] = new BrowseFacet("s1", 5);
            answerBucketFacets[1] = new BrowseFacet("s2", 4);
            answerBucketFacets[2] = new BrowseFacet("s3", 3);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "sets", answerBucketFacets }          
            };

            DoTest(br, 7, answer, null);

            br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            BrowseSelection sel = new BrowseSelection("sets");
            sel.AddValue("s1");
            br.AddSelection(sel);

            output = new FacetSpec();
            output.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            br.SetFacetSpec("sets", output);

            answerBucketFacets = new BrowseFacet[3];
            answerBucketFacets[0] = new BrowseFacet("s1", 5);
            answerBucketFacets[1] = new BrowseFacet("s2", 3);
            answerBucketFacets[2] = new BrowseFacet("s3", 1);

            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "sets", answerBucketFacets }          
            };

            DoTest(br, 4, answer, null);
        }

        [Test]
        public void TestBucketFacetHandlerForStrings()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            BrowseSelection sel = new BrowseSelection("groups");
            sel.AddValue("g2");
            br.AddSelection(sel);

            FacetSpec output = new FacetSpec();
            output.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            br.SetFacetSpec("groups", output);

            BrowseFacet[] answerBucketFacets = new BrowseFacet[3];
            answerBucketFacets[0] = new BrowseFacet("g2", 3);
            answerBucketFacets[1] = new BrowseFacet("g1", 1);
            answerBucketFacets[2] = new BrowseFacet("g3", 1);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "groups", answerBucketFacets }          
            };
            DoTest(br, 3, answer, null);

            br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            sel = new BrowseSelection("groups");
            sel.AddValue("g2");
            sel.AddValue("g1");
            sel.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationAnd;
            br.AddSelection(sel);

            output = new FacetSpec();
            output.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            br.SetFacetSpec("groups", output);

            answerBucketFacets = new BrowseFacet[2];
            answerBucketFacets[0] = new BrowseFacet("g1", 1);
            answerBucketFacets[1] = new BrowseFacet("g2", 1);

            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "groups", answerBucketFacets }          
            };
            DoTest(br, 1, answer, null);

            br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            sel = new BrowseSelection("groups");
            sel.AddValue("g2");
            sel.AddValue("g1");
            sel.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationOr;
            br.AddSelection(sel);

            output = new FacetSpec();
            output.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            br.SetFacetSpec("groups", output);

            answerBucketFacets = new BrowseFacet[3];
            answerBucketFacets[0] = new BrowseFacet("g1", 3);
            answerBucketFacets[1] = new BrowseFacet("g2", 3);
            answerBucketFacets[2] = new BrowseFacet("g3", 1);

            answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "groups", answerBucketFacets }          
            };
            DoTest(br, 5, answer, null);
        }

        [Test]
        public void TestVirtual()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = (10);
            br.Offset = (0);

            BrowseSelection sel = new BrowseSelection("virtual");
            sel.AddValue("10");
            sel.AddValue("11");
            br.AddSelection(sel);

            FacetSpec spec = new FacetSpec();
            spec.OrderBy = FacetSpec.FacetSortSpec.OrderValueAsc;
            br.SetFacetSpec("virtual", spec);

            var answer = new Dictionary<string, IList<BrowseFacet>>()
            {
                { "virtual", new BrowseFacet[]{new BrowseFacet("0010", 1), new BrowseFacet("0011", 1)} }          
            };
            DoTest(br, 2, answer, new String[] { "1", "2" });
        }
    }
}
