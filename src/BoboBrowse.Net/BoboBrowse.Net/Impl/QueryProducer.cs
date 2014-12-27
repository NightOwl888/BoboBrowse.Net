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

// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Impl
{
    using Common.Logging;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.QueryParsers;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class QueryProducer
    {
        private static readonly ILog logger = LogManager.GetLogger<QueryProducer>();

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
