//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Written in Java.
//* 
//* Copyright (C) 2005-2006  John Wang
//*
//* This library is free software; you can redistribute it and/or
//* modify it under the terms of the GNU Lesser General Public
//* License as published by the Free Software Foundation; either
//* version 2.1 of the License, or (at your option) any later version.
//*
//* This library is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

namespace BoboBrowse.Net.Search
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using BoboBrowse.Net.Facets;

    ///<summary> * Provides implementation of Browser for multiple Browser instances </summary>
    public class MultiBoboBrowser : MultiSearcher, IBrowsable
    {
        private static ILog logger = LogManager.GetLogger(typeof(MultiBoboBrowser));

        ///   <summary> *  </summary>
        ///   * <param name="browsers">
        ///   *          Browsers to search on </param>
        ///   * <exception cref="IOException"> </exception>
        public MultiBoboBrowser(IBrowsable[] browsers)
            : base(browsers)
        {
        }

        private class MultiBoboBrowserHitCollector : Collector
        {
            internal Collector hitCollector;
            internal int start;

            /*public override void Collect(int doc, float score)
            {
                hitCollector.Collect(doc + start, score);
            }*/

            public override void SetScorer(Scorer scorer)
            {
                hitCollector.SetScorer(scorer);
            }

            public override void Collect(int doc)
            {
                hitCollector.Collect(doc);
            }

            public override void SetNextReader(IndexReader reader, int docBase)
            {
                hitCollector.SetNextReader(reader, docBase);
            }

            public override bool AcceptsDocsOutOfOrder
            {
                get
                {
                    return hitCollector.AcceptsDocsOutOfOrder;
                }
            }
        }

        ///  
        ///   <summary> * Implementation of the browse method using a Lucene HitCollector
        ///   *  </summary>
        ///   * <param name="req">
        ///   *          BrowseRequest </param>
        ///   * <param name="hitCollector">
        ///   *          HitCollector for the hits generated during a search
        ///   *           </param>
        ///   
        public virtual void Browse(BrowseRequest req, Collector hitCollector, Dictionary<string, IFacetAccessible> facetMap) // throws BrowseException
        {
            IBrowsable[] browsers = getSubBrowsers();
            int[] starts = GetStarts();

            Dictionary<string, List<IFacetAccessible>> mergedMap = new Dictionary<string, List<IFacetAccessible>>();
            try
            {
                Dictionary<string, IFacetAccessible> facetColMap = new Dictionary<string, IFacetAccessible>();
                for (int i = 0; i < browsers.Length; i++)
                {
                    int start = starts[i];
                    try
                    {
                        browsers[i].Browse(req,
                                           new MultiBoboBrowserHitCollector { hitCollector = hitCollector, start = start },
                                           facetColMap);
                    }
                    finally
                    {
                        foreach (KeyValuePair<string, IFacetAccessible> entry in facetColMap)
                        {
                            string name = entry.Key;
                            IFacetAccessible facetAccessor = entry.Value;
                            List<IFacetAccessible> list = mergedMap[name];
                            if (list == null)
                            {
                                list = new List<IFacetAccessible>(browsers.Length);
                                mergedMap.Add(name, list);
                            }
                            list.Add(facetAccessor);
                        }
                        facetColMap.Clear();
                    }
                }
            }
            finally
            {
                foreach (KeyValuePair<string, List<IFacetAccessible>> entry in mergedMap)
                {
                    string name = entry.Key;
                    FacetHandler handler = GetFacetHandler(name);
                    try
                    {
                        List<IFacetAccessible> subList = entry.Value;
                        if (subList != null)
                        {
                            IFacetAccessible merged = handler.Merge(req.GetFacetSpec(name), subList);
                            facetMap.Add(name, merged);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error(e.Message, e);
                    }
                }
            }
        }

        ///   <summary> * Generate a merged BrowseResult from the given BrowseRequest </summary>
        ///   * <param name="req">
        ///   *          BrowseRequest for generating the facets </param>
        ///   * <returns> BrowseResult of the results of the BrowseRequest </returns>
        public virtual BrowseResult Browse(BrowseRequest req) // throws BrowseException
        {           
            long start = System.Environment.TickCount;

            int offset = req.Offset;
            int count = req.Count;

            if (offset < 0 || count <= 0)
            {
                throw new System.ArgumentException("both offset and count must be >= 0: " + offset + "/" + count);
            }

            TopDocsSortedHitCollector hitCollector = GetSortedHitCollector(req.Sort, offset, count, req.FetchStoredFields);

            Dictionary<string, IFacetAccessible> mergedMap = new Dictionary<string, IFacetAccessible>();
            Browse(req, hitCollector, mergedMap);

            BrowseResult finalResult = new BrowseResult();

            finalResult.NumHits = hitCollector.GetTotalHits();
            finalResult.TotalDocs = NumDocs();
            finalResult.AddAll(mergedMap);

            BrowseHit[] hits;
            try
            {
                hits = hitCollector.GetTopDocs();
            }
            catch (System.IO.IOException e)
            {
                logger.Error(e.Message, e);
                hits = new BrowseHit[0];
            }

            finalResult.Hits = hits;

            long end = System.Environment.TickCount;

            finalResult.Time = end - start;

            return finalResult;
        }

        ///  
        ///   <summary> * Return the values of a field for the given doc
        ///   *  </summary>
        ///   
        public virtual string[] GetFieldVal(int docid, string fieldname)
        {
            int i = SubSearcher(docid);
            IBrowsable browser = getSubBrowsers()[i];
            return browser.GetFieldVal(SubDoc(docid), fieldname);
        }


        public virtual object[] GetRawFieldVal(int docid, string fieldname)
        {
            int i = SubSearcher(docid);
            IBrowsable browser = getSubBrowsers()[i];
            return browser.GetRawFieldVal(SubDoc(docid), fieldname);
        }
        ///  
        ///   <summary> * Gets the array of sub-browsers
        ///   *  </summary>
        ///   * <returns> sub-browsers </returns>
        ///   * <seealso cref= MultiSearcher#getSearchables() </seealso>
        ///   
        public virtual IBrowsable[] getSubBrowsers()
        {
            return (IBrowsable[])GetSearchables();
        }

        public int[] getStarts()
        {
            // TODO Auto-generated method stub
            return base.GetStarts();
        }

        /// <summary> * Compare BrowseFacets by their value </summary>
        public class BrowseFacetValueComparator : IComparer<BrowseFacet>
        {
            // FIXME: we need to reorganize all that stuff with comparators
            private IComparer valueComparer = new Comparer(System.Globalization.CultureInfo.InvariantCulture);

            public int Compare(BrowseFacet o1, BrowseFacet o2)
            {
                return valueComparer.Compare(o1.Value, o2.Value);
            }
        }

        ///  
        ///   <summary> * Gets the sub-browser for a given docid
        ///   *  </summary>
        ///   * <param name="docid"> </param>
        ///   * <returns> sub-browser instance </returns>
        ///   * <seealso cref= MultiSearcher#subSearcher(int) </seealso>
        ///   
        public virtual IBrowsable subBrowser(int docid)
        {
            return ((IBrowsable)(getSubBrowsers()[SubSearcher(docid)]));
        }

        public void SetSimilarity(Similarity similarity)
        {
            throw new NotImplementedException();
        }

        public Similarity GetSimilarity()
        {
            throw new NotImplementedException();
        }

        public virtual int NumDocs()
        {
            int count = 0;
            IBrowsable[] subBrowsers = getSubBrowsers();
            foreach (IBrowsable subBrowser in subBrowsers)
            {
                count += subBrowser.NumDocs();
            }
            return count;
        }

        public virtual FacetHandler GetFacetHandler(string name)
        {
            IBrowsable[] subBrowsers = getSubBrowsers();
            foreach (IBrowsable subBrowser in subBrowsers)
            {
                FacetHandler subHandler = subBrowser.GetFacetHandler(name);
                if (subHandler != null)
                    return subHandler;
            }
            return null;
        }


        public virtual void SetFacetHandler(FacetHandler facetHandler)
        {
            IBrowsable[] subBrowsers = getSubBrowsers();
            foreach (IBrowsable subBrowser in subBrowsers)
            {
                try
                {
                    subBrowser.SetFacetHandler((FacetHandler)facetHandler.Clone());
                }
                catch (NotSupportedException e)
                {
                    throw new RuntimeException(e.Message, e);
                }
            }
        }

        public virtual TopDocsSortedHitCollector GetSortedHitCollector(SortField[] sort, int offset, int count, bool fetchStoredFields)
        {
            return new MultiTopDocsSortedHitCollector(this, sort, offset, count, fetchStoredFields);
        }
    }
}
