//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
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
namespace BoboBrowse.Net.Impl
{
    using Common.Logging;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.QueryParsers;
    using Lucene.Net.Search;

    public class QueryProducer
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(QueryProducer));

        public const string CONTENT_FIELD = "contents";

        public static Query Convert(string queryString, string defaultField)
        {
            if (string.IsNullOrEmpty(queryString))
            {
                return null;
            }
            else
            {
                var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_CURRENT);
                if (string.IsNullOrEmpty(defaultField)) defaultField = "contents";
                return new QueryParser(Lucene.Net.Util.Version.LUCENE_CURRENT, defaultField, analyzer).Parse(queryString);
            }
        }

        private readonly static SortField[] DEFAULT_SORT = new SortField[] { SortField.FIELD_SCORE };

        // NOTE: This method was commented in bobo-browse 3.1.0, so it is also commented here
        // because of incompatibility with comparator sources.
        //public static SortField[] ConvertSort(SortField[] sortSpec, BoboIndexReader idxReader)
        //{
        //    SortField[] retVal = DEFAULT_SORT;
        //    if (sortSpec != null && sortSpec.Length>0)
        //    {
        //        var sortList = new List<SortField>(sortSpec.Length + 1);
        //        bool relevanceSortAdded = false;
        //        for (int i = 0; i < sortSpec.Length; ++i)
        //        {
        //            if (SortField.FIELD_DOC.Equals(sortSpec[i]))
        //            {
        //                sortList.Add(SortField.FIELD_DOC);
        //            }
        //            else if (SortField.FIELD_SCORE.Equals(sortSpec[i]))
        //            {
        //                sortList.Add(SortField.FIELD_SCORE);
        //                relevanceSortAdded = true;
        //            }
        //            else{
        //                string fieldname = sortSpec[i].Field;
        //                if (fieldname != null)
        //                {
        //                  SortField sf = null;
        //                  var facetHandler = idxReader.GetFacetHandler(fieldname);
        //                  if (facetHandler!=null)
        //                  {
        //                      sf = new SortField(fieldname.ToLowerCase(), new SortComparatorSource()
        //                      {

        //                            /**
        //                            * 
        //                            */
        //                            private static final long serialVersionUID = 1L;

        //                            public ScoreDocComparator newComparator(
        //                                IndexReader reader, string fieldname)
        //                                throws IOException 
        //                            {
        //                                return facetHandler.getScoreDocComparator();
        //                            }
    			    		  
        //                        },sortSpec[i].getReverse());
        //                    }
        //                    else
        //                    {
        //                        sf = sortSpec[i];
        //                    }
        //                    sortList.add(sf);
        //                }
        //            }
        //        }
        //        if (!relevanceSortAdded)
        //        {
        //            sortList.add(SortField.FIELD_SCORE);
        //        }
        //        retVal = sortList.ToArray();		
        //    }
        //    return retVal;
        //}
	
        //public static DocIdSet BuildBitSet(BrowseSelection[] selections, BoboIndexReader reader) 
        //{
        //    if (selections==null || selections.Length == 0) return null;
        //    DocIdSet finalBits=null;
        //    FieldConfiguration fConf = reader.GetFieldConfiguration();
        //    FieldPlugin plugin;
        //    DocIdSet finalNotBits=null;
		
        //    for(int i = 0; i < selections.Length; ++i) 
        //    {
        //        string fieldName = selections[i].FieldName;
        //        plugin = fConf.getFieldPlugin(fieldName);
        	
        //        if (plugin==null)
        //        {
        //            throw new System.IO.IOException("Undefined field: " + fieldName + " please check your field configuration.");
        //        }
        //        BoboFilter[] f = plugin.BuildFilters(selections[i],false);        	
        //        DocIdSet bs = FieldPlugin.MergeSelectionBitset(reader, f, selections[i].SelectionOperation);
        //        if (bs!=null)
        //        {
        //            if (finalBits==null)
        //            {	        	
        //                    finalBits = bs;	        
        //            }
        //            else
        //            {
        //                finalBits.And(bs);
        //            }
        //        }        
        	
        //        if (plugin.supportNotSelections())
        //        {
        //            BoboFilter[] notF = plugin.buildFilters(selections[i], true);
        //            DocIdSet notBS = FieldPlugin.MergeSelectionBitset(reader, notF, ValueOperation.ValueOperationOr);
	        	
        //            if (notBS!=null)
        //            {
        //                if (finalNotBits==null)
        //                {
        //                    finalNotBits=notBS;
        //                }
        //                else
        //                {
        //                    finalNotBits.Or(notBS);
        //                }
        //            }
        //            /*
        //            DocSet emptyVals = new TermFilter(new Term(fieldName,"")).getDocSet(reader);
        //            if (emptyVals != null && emptyVals.cardinality() > 0 && finalNotBits != null)
        //            {
        //                finalNotBits.or(emptyVals);
        //            }*/
        //        }
        //    }

        //    if (finalNotBits != null && finalNotBits.Cardinality() > 0)	// we have "not" selections
        //    {	
        //        if (finalBits!=null){
        //            finalNotBits.Flip(0, finalBits.Length);
        //            finalBits.And(finalNotBits);
        //        }
        //        else{        		
        //            finalNotBits.Flip(0, reader.MaxDoc);
        //            finalBits=finalNotBits;
        //        }
        //    }
        
        //    return finalBits;  
        //}

        public virtual Query BuildQuery(string query)
        {
            return Convert(query, CONTENT_FIELD);
        }
    }
}
