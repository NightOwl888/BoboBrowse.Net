/**
 * Bobo Browse Engine - High performance faceted/parametric search implementation 
 * that handles various types of semi-structured data.  Written in Java.
 * 
 * Copyright (C) 2005-2006  John Wang
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * 
 * To contact the project administrators for the bobo-browse project, 
 * please go to https://sourceforge.net/projects/bobo-browse/, or 
 * send mail to owner@browseengine.com.
 */

namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Index;
    using BoboBrowse.Net.Index.Digest;
    using BoboBrowse.Net.Query.Scoring;
    using BoboBrowse.Net.Search;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Analysis;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Analysis.Tokenattributes;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.QueryParsers;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using Lucene.Net.Util;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    [TestFixture]
    public class BoboTestCase
    {
        private Directory indexDir;
        private IEnumerable<IFacetHandler> fconf;
	    private static Term tagSizePayloadTerm = new Term("tagSizePayload", "size");

        private class TestDataDigester : DataDigester
        {
            private IEnumerable<IFacetHandler> fconf;
            private Document[] data;

            public TestDataDigester(IEnumerable<IFacetHandler> fConf, Document[] data)
                : base()
            {
                this.fconf = fConf;
                this.data = data;
            }

            public override void Digest(IDataHandler handler)
            {
                foreach (var dataItem in this.data)
                {
                    handler.HandleDocument(dataItem);
                }
            }
        }

        //public BoboTestCase()
        //{
        //    this.fconf = BuildFieldConf();
        //    this.indexDir = CreateIndex();
        //}

        [SetUp]
        public void Init()
        {
            this.fconf = BuildFieldConf();
            this.indexDir = CreateIndex();
        }

        [TearDown]
        public void Dispose()
        {
            this.fconf = null;
            this.indexDir = null;
        }


        private BoboIndexReader NewIndexReader(bool readOnly)
        {
            IndexReader srcReader = IndexReader.Open(indexDir, readOnly);
            try
            {
                return BoboIndexReader.GetInstance(srcReader, this.fconf);
            }
            finally
            {
                if (srcReader != null)
                {
                    srcReader.Close();
                }
            }
        }

        private BoboBrowser NewBrowser(bool readOnly)
        {
            return new BoboBrowser(NewIndexReader(readOnly));
        }

        public static Field BuildMetaField(string name, string val)
        {
            Field f = new Field(name, val, Field.Store.NO, Field.Index.NOT_ANALYZED_NO_NORMS);
            f.OmitTermFreqAndPositions = true;
            return f;
        }

        public static Field BuildMetaSizePayloadField(Term term, int size)
        {
            var ts = new MetaSizeTokenStream(term, size);
            Field f = new Field(term.Field, ts);
            return f;
        }

        // From Bobo 3.1.0 MetaTokenStream class
        private class MetaSizeTokenStream : TokenStream
        {
            private bool returnToken = false;
            private PayloadAttribute payloadAttr;
            private TermAttribute termAttr;

            public MetaSizeTokenStream(Term term, int size)
            {
                byte[] buffer = new byte[4];
                buffer[0] = (byte) (size);
                buffer[1] = (byte) (size >> 8);
                buffer[2] = (byte) (size >> 16);
                buffer[3] = (byte) (size >> 24);

                payloadAttr = new PayloadAttribute();
                payloadAttr.Payload = new Payload(buffer);
                // NOTE: Calling the AddAttribute<T> method failed, so 
                // switched to using AddAttributeImpl.
                AddAttributeImpl(payloadAttr);
                
                termAttr = new TermAttribute();
                termAttr.SetTermBuffer(term.Text);
                // NOTE: Calling the AddAttribute<T> method failed, so 
                // switched to using AddAttributeImpl.
                AddAttributeImpl(termAttr);
                
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

            protected override void Dispose(bool disposing)
            {
            }
        }

        //// From Bobo 2.0.7
        //private class MetaSizeTokenStream : TokenStream
        //{
        //    private bool returnToken = true;
        //    private readonly Term term;
        //    private readonly int size;

        //    public MetaSizeTokenStream(Term term, int size)
        //    {
        //        this.term = term;
        //        this.size = size;
        //    }

        //    private Payload GetSizePayload()
        //    {
        //        byte[] buffer = new byte[4];
        //        buffer[0] = (byte)(size);
        //        buffer[1] = (byte)(size >> 8);
        //        buffer[2] = (byte)(size >> 16);
        //        buffer[3] = (byte)(size >> 24);
        //        return new Payload(buffer);
        //    }

        //    public Lucene.Net.Analysis.Token Next(Lucene.Net.Analysis.Token token)
        //    {
        //        if (returnToken)
        //        {
        //            returnToken = false;
        //            token.SetTermBuffer(term.Text);
        //            token.StartOffset = 0;
        //            token.EndOffset = 0;
        //            token.Payload = GetSizePayload();
        //            return token;
        //        }
        //        return null;
        //    }

        //}

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
            d1.Add(BuildMetaField("date_range_start", "200001"));
            d1.Add(BuildMetaField("date_range_end", "200003"));
            d1.Add(BuildMetaField("multinum", "001"));
            d1.Add(BuildMetaField("multinum", "003"));
            d1.Add(BuildMetaField("compactnum", "001"));
            d1.Add(BuildMetaField("compactnum", "003"));
            d1.Add(BuildMetaField("numendorsers", "000003"));

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
            d2.Add(BuildMetaField("date_range_start", "200005"));
            d2.Add(BuildMetaField("date_range_end", "200102"));
            d2.Add(BuildMetaField("multinum", "002"));
            d2.Add(BuildMetaField("multinum", "004"));
            d2.Add(BuildMetaField("compactnum", "002"));
            d2.Add(BuildMetaField("compactnum", "004"));
            d2.Add(BuildMetaField("numendorsers", "000010"));

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
            d3.Add(BuildMetaField("date_range_start", "200101"));
            d3.Add(BuildMetaField("date_range_end", "200112"));
            d3.Add(BuildMetaField("multinum", "007"));
            d3.Add(BuildMetaField("multinum", "012"));
            d3.Add(BuildMetaField("compactnum", "007"));
            d3.Add(BuildMetaField("compactnum", "012"));
            d3.Add(BuildMetaField("numendorsers", "000015"));

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
            d4.Add(BuildMetaField("date_range_start", "200105"));
            d4.Add(BuildMetaField("date_range_end", "200205"));
            d4.Add(BuildMetaField("multinum", "007"));
            d4.Add(BuildMetaField("date_range_end", "200205"));
            d4.Add(BuildMetaField("multinum", "007"));
            d4.Add(BuildMetaField("compactnum", "007"));
            d4.Add(BuildMetaField("numendorsers", "000019"));

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
            d5.Add(BuildMetaField("date_range_start", "200212"));
            d5.Add(BuildMetaField("date_range_end", "200312"));
            d5.Add(BuildMetaField("multinum", "001"));
            d5.Add(BuildMetaField("multinum", "001"));
            d5.Add(BuildMetaField("compactnum", "001"));
            d5.Add(BuildMetaField("compactnum", "001"));
            d5.Add(BuildMetaField("numendorsers", "000002"));

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
            d6.Add(BuildMetaField("date_range_start", "200106"));
            d6.Add(BuildMetaField("date_range_end", "200301"));
            d6.Add(BuildMetaField("multinum", "001"));
            d6.Add(BuildMetaField("multinum", "002"));
            d6.Add(BuildMetaField("multinum", "003"));
            d6.Add(BuildMetaField("compactnum", "001"));
            d6.Add(BuildMetaField("compactnum", "002"));
            d6.Add(BuildMetaField("compactnum", "003"));
            d6.Add(BuildMetaField("numendorsers", "000009"));

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
            d7.Add(BuildMetaField("date_range_start", "200011"));
            d7.Add(BuildMetaField("date_range_end", "200212"));
            d7.Add(BuildMetaField("multinum", "008"));
            d7.Add(BuildMetaField("multinum", "003"));
            d7.Add(BuildMetaField("compactnum", "008"));
            d7.Add(BuildMetaField("compactnum", "003"));
            d7.Add(BuildMetaField("numendorsers", "000013"));

            dataList.Add(d1);
            dataList.Add(d2);
            dataList.Add(d3);
            dataList.Add(d4);
            dataList.Add(d5);
            dataList.Add(d6);
            dataList.Add(d7);
            dataList.Add(d7);

            return dataList.ToArray();
        }

        private Directory CreateIndex()
        {
            RAMDirectory idxDir = new RAMDirectory();

            Document[] data = BuildData();

            TestDataDigester testDigester = new TestDataDigester(this.fconf, data);
            BoboIndexer indexer = new BoboIndexer(testDigester, idxDir);
            indexer.Index();
            IndexReader r = IndexReader.Open(idxDir, false);
            try
            {
                r.DeleteDocument(r.MaxDoc - 1);
                //r.Flush();
            }
            finally
            {
                r.Close();
            }

            return idxDir;
        }

        public static IEnumerable<IFacetHandler> BuildFieldConf()
        {
            List<IFacetHandler> facetHandlers = new List<IFacetHandler>();
		    facetHandlers.Add(new SimpleFacetHandler("id"));
		    SimpleFacetHandler colorHandler = new SimpleFacetHandler("color");
		    colorHandler.TermCountSizeFlag = TermCountSize.Small;
		    facetHandlers.Add(colorHandler);

		    SimpleFacetHandler shapeHandler = new SimpleFacetHandler("shape");
		    colorHandler.TermCountSizeFlag = TermCountSize.Medium;
		    facetHandlers.Add(new SimpleFacetHandler("shape"));
		    facetHandlers.Add(new RangeFacetHandler("size", true));
		    String[] ranges = new String[]{"[000000 TO 000005]", "[000006 TO 000010]", "[000011 TO 000020]"};
		    facetHandlers.Add(new RangeFacetHandler("numendorsers", new PredefinedTermListFactory<int>("000000"), ranges));
		
		    var numTermFactory = new PredefinedTermListFactory<int>("0000");

		    facetHandlers.Add(new PathFacetHandler("location"));
		
		    facetHandlers.Add(new SimpleFacetHandler("number", numTermFactory));
		    facetHandlers.Add(new RangeFacetHandler("date", new PredefinedTermListFactory<DateTime>("yyyy/MM/dd"), new String[]{"[2000/01/01 TO 2003/05/05]", "[2003/05/06 TO 2005/04/04]"}));
		    facetHandlers.Add(new SimpleFacetHandler("char", (TermListFactory)null));
		    facetHandlers.Add(new RangeFacetHandler("date_range_start", new PredefinedTermListFactory<DateTime>("yyyyMM"), true));
		    facetHandlers.Add(new RangeFacetHandler("date_range_end", new PredefinedTermListFactory<DateTime>("yyyyMM"), true));
            facetHandlers.Add(new MultiValueFacetHandler("tag", (String)null, (TermListFactory)null, tagSizePayloadTerm));
		    facetHandlers.Add(new MultiValueFacetHandler("multinum", new PredefinedTermListFactory<int>("000")));
		    facetHandlers.Add(new CompactMultiValueFacetHandler("compactnum", new PredefinedTermListFactory<int>("000")));
		    facetHandlers.Add(new SimpleFacetHandler("storenum", new PredefinedTermListFactory<long>(null)));
		

            // TODO: Test the SimpleGroupbyFacetHandler
            //HashSet<String> dependsNames=new HashSet<String>();
            //dependsNames.Add("color");
            //dependsNames.Add("shape");
            //dependsNames.Add("number");
            //facetHandlers.Add(new SimpleGroupbyFacetHandler("groupby", dependsNames));
	    		
		    return facetHandlers;
        }

        public static bool Check(BrowseResult res, int numHits, IDictionary<string, IEnumerable<BrowseFacet>> choiceMap, string[] ids)
        {
            bool match = false;
		    if (numHits == res.NumHits){
		        if (choiceMap != null)
                {
                    var entries = res.FacetMap;
    			
    			    if (entries.Count == choiceMap.Count){
                        foreach (var entry in entries)
                        {
                            string name = entry.Key;
                            IFacetAccessible c1 = entry.Value;
                            var l1 = c1.GetFacets();
                            var l2 = choiceMap.ContainsKey(name) ? choiceMap[name] : new List<BrowseFacet>();

                            if (l1.Count() == l2.Count())
                            {
                                for (int i = 0; i < l1.Count(); i++)
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
                            String id = hits[i].GetField("id");
                            if (!ids[i].Equals(id)) return false;
                        }
                    }
                    catch(Exception e)
                    {
                        return false;
                    }
			    }
			    match = true; 
		    }
		    return match;
        }

        private void DoTest(BrowseResult result, BrowseRequest req, int numHits, IDictionary<string, IEnumerable<BrowseFacet>> choiceMap, string[] ids)
        {
            if (!Check(result, numHits, choiceMap, ids))
            {
				StringBuilder buffer = new StringBuilder();
                buffer.Append("Test: ").Append(new StackFrame(1).GetMethod().Name).Append("\n");
				buffer.Append("Result check failed: \n");
				buffer.Append("expected: \n");
				buffer.Append(numHits).Append(" hits\n");
				buffer.Append(choiceMap).Append('\n');
				buffer.Append("gotten: \n");
				buffer.Append(result.NumHits).Append(" hits\n");
				

				var entries = result.FacetMap;
			
				buffer.Append("{");
                foreach (var entry in entries)
                {
                    String name = entry.Key;
					IFacetAccessible facetAccessor = entry.Value;
					buffer.Append("name=").Append(name).Append(",");
					buffer.Append("facets=").Append(facetAccessor.GetFacets()).Append(";");
                }
				buffer.Append("}").Append('\n');
				
				BrowseHit[] hits = result.Hits;
				for (int i = 0 ; i < hits.Length; ++i)
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
                String name = entry.Key;
                IFacetAccessible facetAccessor = entry.Value;
                buffer.Append("name=").Append(name).Append(",");
                buffer.Append("facets=").Append(facetAccessor.GetFacets()).Append(";");
            }
            buffer.Append("}").Append('\n');
            return buffer.ToString();
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

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("blue", 2), new BrowseFacet("green", 2),new BrowseFacet("red", 3) } },
                { "shape", new BrowseFacet[] { new BrowseFacet("rectangle", 1),new BrowseFacet("square", 2) } }
            };

            DoTest(br, 3, answer, new String[] { "1", "2", "7" });

            sel = new BrowseSelection("shape");
            sel.AddValue("square");
            br.AddSelection(sel);

            answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("blue", 1),new BrowseFacet("red", 2) } },
                { "shape", new BrowseFacet[] { new BrowseFacet("rectangle", 1),new BrowseFacet("square", 2) } }
            };

            DoTest(br, 2, answer, new String[] { "1", "7" });
        }

        public void TestRollup()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("location");
            Properties prop = sel.SelectionProperties;
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

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "location", new BrowseFacet[] { new BrowseFacet("toy/lego/block",3) } },
                { "tag", new BrowseFacet[] { new BrowseFacet("pet", 2), new BrowseFacet("animal", 1), new BrowseFacet("dog", 1), new BrowseFacet("funny", 1), new BrowseFacet("humor", 1), new BrowseFacet("joke", 1), new BrowseFacet("poodle", 1), new BrowseFacet("rabbit", 1) } }
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
            DoTest(br, 1, null, new String[] { "3" });

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
            br.AddSortField(new SortField("date", SortField.STRING, true)); // NOTE: Added string sorting for lack of a better option - not sure what the default in lucene was

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "char", new BrowseFacet[] { new BrowseFacet("a", 1), new BrowseFacet("i", 1), new BrowseFacet("k", 1) } }
            };

            DoTest(br, 3, answer, new String[] { "7", "2", "1" });
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

            br.AddSortField(new SortField("date", SortField.STRING, true)); // NOTE: Added string sorting for lack of a better option - not sure what the default in lucene was

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("blue", 2), new BrowseFacet("green", 1), new BrowseFacet("red", 1) } }
            };
            DoTest(br, 4, answer, new String[] { "4", "2", "5", "3" });
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
            ospec.ExpandSelection  = false;
            br.SetFacetSpec("color", ospec);

            br.AddSortField(new SortField("date", SortField.STRING, true)); // NOTE: Added string sorting for lack of a better option - not sure what the default in lucene was

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("green", 1), new BrowseFacet("red", 1) } }
            };
            DoTest(br, 2, answer, new String[] { "6", "7" });
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

            br.AddSortField(new SortField("date", SortField.STRING, true)); // NOTE: Added string sorting for lack of a better option - not sure what the default in lucene was

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("green", 1), new BrowseFacet("red", 1) } }
            };
            DoTest(br, 2, answer, new String[] { "3", "1" });
        }

        private BrowseResult DoTest(BrowseRequest req, int numHits, IDictionary<string, IEnumerable<BrowseFacet>> choiceMap, string[] ids)
        {
            return DoTest((BoboBrowser)null, req, numHits, choiceMap, ids);
        }

        private BrowseResult DoTest(BoboBrowser boboBrowser, BrowseRequest req, int numHits, IDictionary<string, IEnumerable<BrowseFacet>> choiceMap, string[] ids)
        {
            BrowseResult result;
            try
            {
                if (boboBrowser == null)
                {
                    boboBrowser = NewBrowser(readOnly: true);
                }
                result = boboBrowser.Browse(req);
                DoTest(result, req, numHits, choiceMap, ids);
                return result;
            }
            catch (BrowseException e)
            {
                Assert.Fail(e.Message);
            }
            catch (System.IO.IOException ioe)
            {
                Assert.Fail(ioe.Message);
            }
            finally
            {
                if (boboBrowser != null)
                {
                    try
                    {
                        boboBrowser.Close();
                    }
                    catch (System.IO.IOException e)
                    {
                        Assert.Fail(e.Message);
                    }
                }
            }
            return null;
        }

        [Test]
        public void TestLuceneSort()
        {
            IndexReader srcReader = IndexReader.Open(indexDir, true);
            try
            {
                var facetHandlers = new List<FacetHandler>();
                facetHandlers.Add(new SimpleFacetHandler("id"));

                BoboIndexReader reader = BoboIndexReader.GetInstance(srcReader, facetHandlers);       // not facet handlers to help
                BoboBrowser browser = new BoboBrowser(reader);

                BrowseRequest browseRequest = new BrowseRequest();
                browseRequest.Count = 10;
                browseRequest.Offset = 0;
                browseRequest.AddSortField(new SortField("date", SortField.STRING)); // NOTE: Added string sorting for lack of a better option - not sure what the default in lucene was


                DoTest(browser, browseRequest, 7, null, new String[] { "1", "3", "5", "2", "4", "7", "6" });

            }
            finally
            {
                if (srcReader != null)
                {
                    srcReader.Close();
                }
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

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("red", 3), new BrowseFacet("blue", 2), new BrowseFacet("green", 2) } },
                { "shape", new BrowseFacet[] { new BrowseFacet("circle", 2), new BrowseFacet("rectangle", 2), new BrowseFacet("square", 3) } }
            };

            DoTest(br, 7, answer, null);
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

            br.AddSortField(new SortField("date", SortField.STRING, false)); // NOTE: Added string sorting for lack of a better option - not sure what the default in lucene was

            DoTest(br, 5, null, new String[] { "1", "3", "5", "2", "4" });
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

            br.AddSortField(new SortField("date", SortField.STRING, false)); // NOTE: Added string sorting for lack of a better option - not sure what the default in lucene was

            DoTest(br, 7, null, new String[] { "1", "3", "5", "2", "4", "7", "6" });
        }

        [Test]
        public void TestSort()
        {
            // no sel
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            br.Sort = new SortField[] { new SortField("number", SortField.STRING, true) };
            DoTest(br, 7, null, new String[] { "6", "5", "4", "3", "2", "1", "7" });
            br.Sort = new SortField[] { new SortField("name", SortField.STRING, false) };
            DoTest(br, 7, null, new String[] { "7", "4", "6", "2", "3", "1", "5" });

            BrowseSelection sel = new BrowseSelection("color");
            sel.AddValue("red");
            br.AddSelection(sel);
            br.Sort = new SortField[] { new SortField("number", SortField.STRING, true) };
            DoTest(br, 3, null, new String[] { "2", "1", "7" });
            br.Sort = new SortField[] { new SortField("name", SortField.STRING, false) };
            DoTest(br, 3, null, new String[] { "7", "2", "1" });

            sel.AddValue("blue");
            br.Query = new TermQuery(new Term("shape", "square"));
            br.Sort = new SortField[] { new SortField("number", SortField.STRING, true) };
            DoTest(br, 3, null, new String[] { "5", "1", "7" });
            br.Sort = new SortField[] { new SortField("name", SortField.STRING, false) };
            DoTest(br, 3, null, new String[] { "7", "1", "5" });
        }

        [Test]
        public void TestCustomSort()
        {
            Assert.Fail("Not Implemented");
            // TODO: Implement
        }

        [Test]
        public void TestDefaultBrowse()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            FacetSpec spec = new FacetSpec();
            spec.MaxCount = 2;
            spec.OrderBy = FacetSpec.FacetSortSpec.OrderHitsDesc;
            br.SetFacetSpec("color", spec);

            br.Sort = new SortField[] { new SortField("number", SortField.STRING, false) };

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("red", 3), new BrowseFacet("blue", 2) } }
            };

            DoTest(br, 7, answer, new String[] { "7", "1", "2", "3", "4", "5", "6" });
        }

        [Test]
        public void TestRandomAccessFacet()
        {
            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;
            br.SetFacetSpec("number", new FacetSpec());

            BoboBrowser browser = NewBrowser(readOnly: true);

            BrowseResult res = browser.Browse(br);
            IFacetAccessible facetAccessor = res.GetFacetAccessor("number");
            BrowseFacet facet = facetAccessor.GetFacet("5");

            Assert.AreEqual(facet.Value, "0005");
            Assert.AreEqual(facet.HitCount, 1);
        }

        [Test]
        public void TestBrowseWithQuery()
        {
            try
            {
                BrowseRequest br = new BrowseRequest();
                QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_CURRENT, "shape", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_CURRENT));
                br.Query = parser.Parse("square");
                br.Count = 10;
                br.Offset = 0;

                BrowseSelection sel = new BrowseSelection("color");
                sel.AddValue("red");
                br.AddSelection(sel);

                br.Sort = new SortField[] { new SortField("number", SortField.STRING, false) };
                DoTest(br, 2, null, new String[] { "7", "1" });


                FacetSpec ospec = new FacetSpec();
                ospec.ExpandSelection = true;
                br.SetFacetSpec("color", ospec);
                var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
                {
                    { "color", new BrowseFacet[] { new BrowseFacet("blue", 1), new BrowseFacet("red", 2) } }
                };
                DoTest(br, 2, answer, new String[] { "7", "1" });

                answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
                {
                    { "color", new BrowseFacet[] { new BrowseFacet("blue", 1), new BrowseFacet("red", 2) } }
                };
                DoTest(br, 3, answer, new String[] { "7", "1", "5" });
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

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

            br.Sort = new SortField[] { new SortField("compactnum", SortField.STRING, true) };

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "compactnum", new BrowseFacet[] { new BrowseFacet("001", 3), new BrowseFacet("002", 1), new BrowseFacet("003", 3), new BrowseFacet("007", 2), new BrowseFacet("008", 1), new BrowseFacet("012", 1) } }
            };

            DoTest(br, 6, answer, new String[] { "3", "7", "4", "6", "1", "5" });


            br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;
            sel = new BrowseSelection("compactnum");
            sel.AddValue("001");
            sel.AddValue("002");
            sel.AddValue("003");
            br.AddSelection(sel);
            sel.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationAnd;
            DoTest(br, 1, null, new String[] { "6" });

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
            answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "compactnum", new BrowseFacet[] { new BrowseFacet("001", 1), new BrowseFacet("003", 2), new BrowseFacet("008", 1) } },
                { "color", new BrowseFacet[] { new BrowseFacet("red", 2) } }
            };

            DoTest(br, 2, answer, new String[] { "1", "7" });

            // NOTE: Original source did test twice - not sure if we really need to.
            DoTest(br, 2, answer, new String[] { "1", "7" });
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
            br.Sort = new SortField[] { new SortField("multinum", SortField.STRING, true) };
            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "multinum", new BrowseFacet[] { new BrowseFacet("001", 3), new BrowseFacet("002", 1), new BrowseFacet("003", 3), new BrowseFacet("007", 2), new BrowseFacet("008", 1), new BrowseFacet("012", 1) } }
            };

            DoTest(br, 6, answer, new String[] { "3", "4", "7", "1", "6", "5" });




            br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;
            sel = new BrowseSelection("multinum");
            sel.AddValue("001");
            sel.AddValue("002");
            sel.AddValue("003");
            br.AddSelection(sel);
            sel.SelectionOperation = BrowseSelection.ValueOperation.ValueOperationAnd;
            DoTest(br, 1, null, new String[] { "6" });

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
            answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "multinum", new BrowseFacet[] { new BrowseFacet("001", 1), new BrowseFacet("003", 2), new BrowseFacet("008", 1) } },
                { "color", new BrowseFacet[] { new BrowseFacet("red", 2) } }
            };

            DoTest(br, 2, answer, new String[] { "1", "7" });
        }

        [Test]
        public void TestBrowseWithDeletes()
        {
            BoboIndexReader reader = null;

            BrowseRequest br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            BrowseSelection sel = new BrowseSelection("color");
            sel.AddValue("red");
            br.AddSelection(sel);

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>();

            DoTest(br, 3, answer, new String[] { "1", "2", "7" });


            try
            {
                reader = NewIndexReader(false);
                reader.DeleteDocuments(new Term("id", "1"));
                reader.DeleteDocuments(new Term("id", "2"));

                br = new BrowseRequest();
                br.Count = 10;
                br.Offset = 0;

                sel = new BrowseSelection("color");
                sel.AddValue("red");
                br.AddSelection(sel);
                answer = new Dictionary<string, IEnumerable<BrowseFacet>>();

                DoTest(new BoboBrowser(reader), br, 1, answer, null);
            }
            catch (System.IO.IOException ioe)
            {
                Assert.Fail(ioe.Message);
            }
            finally
            {
                if (reader != null)
                {
                    try
                    {
                        reader.Close();
                    }
                    catch (System.IO.IOException e)
                    {
                        Assert.Fail(e.Message);
                    }
                }
            }

            br = new BrowseRequest();
            br.Count = 10;
            br.Offset = 0;

            sel = new BrowseSelection("color");
            sel.AddValue("red");
            br.AddSelection(sel);
            answer = new Dictionary<string, IEnumerable<BrowseFacet>>();


            DoTest(br, 1, answer, null);
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

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "shape", new BrowseFacet[] { new BrowseFacet("circle", 2), new BrowseFacet("rectangle", 1), new BrowseFacet("square", 1) } }
            };

            DoTest(br, 4, answer, new String[] { "3", "4", "5", "6" });

            sel.AddNotValue("green");

            answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "shape", new BrowseFacet[] { new BrowseFacet("circle", 1), new BrowseFacet("square", 1) } }
            };

            DoTest(br, 2, answer, new String[] { "4", "5" });

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
            DoTest(br, 3, null, new String[] { "3", "4", "5" });

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

            DoTest(br, 3, null, new String[] { "3", "4", "5" });
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

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "date", new BrowseFacet[] { new BrowseFacet("[2000/01/01 TO 2003/05/05]", 4), new BrowseFacet("[2003/05/06 TO 2005/04/04]", 1) } }
            };

            BrowseResult result = DoTest(br, 7, answer, null);
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

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "numendorsers", new BrowseFacet[] { new BrowseFacet("[000000 TO 000005]", 2), new BrowseFacet("[000006 TO 000010]", 2), new BrowseFacet("[000011 TO 000020]", 3) } }
            };

            BrowseResult result = DoTest(br, 7, answer, null);
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

            Properties prop = sel.SelectionProperties;
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

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] { new BrowseFacet("green", 1), new BrowseFacet("red", 2) } },
                { "size", new BrowseFacet[] { new BrowseFacet("[4 TO 4]", 1), new BrowseFacet("[7 TO 7]", 1) } },
                { "shape", new BrowseFacet[] { new BrowseFacet("square", 2) } },
                { "location", new BrowseFacet[] { new BrowseFacet("toy/lego/", 1), new BrowseFacet("toy/lego/block", 1) } },
                { "tag", new BrowseFacet[] { new BrowseFacet("rabbit", 2), new BrowseFacet("animal", 1), new BrowseFacet("dog", 1), new BrowseFacet("humane", 1), new BrowseFacet("pet", 1) } }
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
            browseRequest.AddSortField(new SortField("date", SortField.STRING));

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

            BoboBrowser boboBrowser = NewBrowser(readOnly: true);

            browseRequest.Sort = new SortField[] { new SortField("compactnum", SortField.STRING, true) };

            MultiBoboBrowser multiBoboBrowser = new MultiBoboBrowser(new IBrowsable[] { boboBrowser, boboBrowser });
            BrowseResult mergedResult = multiBoboBrowser.Browse(browseRequest);

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "color", new BrowseFacet[] {  new BrowseFacet("red", 4), new BrowseFacet("green", 2) } },
                { "tag", new BrowseFacet[] { new BrowseFacet("animal", 2), new BrowseFacet("dog", 2), new BrowseFacet("humane", 2), new BrowseFacet("pet", 2), new BrowseFacet("rabbit", 4) } },
                { "shape", new BrowseFacet[] {  new BrowseFacet("square", 4) } },
                { "date", new BrowseFacet[] { new BrowseFacet("[2000/01/01 TO 2003/05/05]", 2) } }              
            };

            DoTest(mergedResult, browseRequest, 4, answer, new String[] { "7", "7", "1", "1" });

            browseRequest.Sort = new SortField[] { new SortField("multinum", SortField.STRING, true) };
            mergedResult = multiBoboBrowser.Browse(browseRequest);
            DoTest(mergedResult, browseRequest, 4, answer, new String[] { "7", "7", "1", "1" });
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
            br.Count = 10;
            br.Offset = 0;

            DoTest(br, 5, null, new String[] { "1", "2", "7", "4", "5" });

            BoboBrowser b = NewBrowser(readOnly: true);
		    Explanation expl = b.Explain(colorQ, 0);
            Console.WriteLine(expl.ToString());
		
		    br.Query = tagQ;
		    DoTest(br,4,null,new String[]{"7","1","3","2"});
		    expl = b.Explain(tagQ, 6);
            Console.WriteLine(expl.ToString());
        }

        [Test]
        public void TestRuntimeFilteredDateRange()
        {
            BoboBrowser browser = NewBrowser(readOnly: true);
            String[] ranges = new String[] { "[2001/01/01 TO 2001/12/30]", "[2007/01/01 TO 2007/12/30]" };
            FilteredRangeFacetHandler handler = new FilteredRangeFacetHandler("filtered_date", "date", ranges);
            browser.SetFacetHandler(handler);

            BrowseRequest req = new BrowseRequest();
            req.SetFacetSpec("filtered_date", new FacetSpec());

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "filtered_date", new BrowseFacet[] { new BrowseFacet("[2001/01/01 TO 2001/12/30]", 1), new BrowseFacet("[2007/01/01 TO 2007/12/30]", 1) } }          
            };

            DoTest(browser, req, 7, answer, null);
        }

        [Test]
        public void TestCustomFacetSort()
        {
            BrowseRequest req = new BrowseRequest();
		    FacetSpec numberSpec = new FacetSpec();
            numberSpec.CustomComparatorFactory = new MyComparatorFactory();

            numberSpec.OrderBy = FacetSpec.FacetSortSpec.OrderByCustom;
            numberSpec.MaxCount = 3;
            req.SetFacetSpec("number", numberSpec);

            var answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "number", new BrowseFacet[] { new BrowseFacet("2130", 1), new BrowseFacet("1013", 1), new BrowseFacet("0913", 1) } }          
            };

            DoTest(req, 7, answer, null);

            answer = new Dictionary<string, IEnumerable<BrowseFacet>>()
            {
                { "number", new BrowseFacet[] { new BrowseFacet("0005", 1), new BrowseFacet("0010", 1), new BrowseFacet("0011", 1) } }          
            };

            DoTest(req, 7, answer, null);
        }

        private class MyComparatorFactory : IComparatorFactory
        {

            public IComparer<int> NewComparator(IFieldValueAccessor fieldValueAccessor, int[] counts)
            {
                return new MyIntComparer(fieldValueAccessor, counts);
            }

            public IComparer<BrowseFacet> NewComparator()
            {
                return new MyBrowseFacetComparer();
            }

            public class MyIntComparer : IComparer<int>
            {
                private readonly IFieldValueAccessor fieldValueAccessor;
                private readonly int[] counts;

                public MyIntComparer(IFieldValueAccessor fieldValueAccessor, int[] counts)
                {
                    this.fieldValueAccessor = fieldValueAccessor;
                    this.counts = counts;
                }

                public int Compare(int x, int y)
                {
                    var size1 = (int)this.fieldValueAccessor.GetRawValue(x);
                    var size2 = (int)this.fieldValueAccessor.GetRawValue(y);

                    int val = size1 - size2;
                    if (val == 0)
                    {
                        val = counts[x] - counts[y];
                    }
                    return val;
                }
            }

            public class MyBrowseFacetComparer : IComparer<BrowseFacet>
            {
                public int Compare(BrowseFacet x, BrowseFacet y)
                {
                    int v1 = Convert.ToInt32(x.Value);
                    int v2 = Convert.ToInt32(y.Value);
                    int val = v1 - v2;
                    if (val == 0)
                    {
                        val = x.HitCount - y.HitCount;
                    }
                    return val;
                }
            }
        }

        [Test]
        public void TestSimpleGroupByFacetHandler()
        {
            Assert.Fail("Not Implemented");
            // TODO: Implement
        }
    }
}
