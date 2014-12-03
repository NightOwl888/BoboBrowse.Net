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

namespace BoboBrowse.Net
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Query;
    using BoboBrowse.Net.Search;
    using Common.Logging;
    using Lucene.Net.Search;
    using Lucene.Net.Documents;
    using System;
    using System.Collections.Generic;    

    ///<summary>This class implements the browsing functionality. </summary>
    public class BoboBrowser : BoboSearcher2, IBrowsable
    {
        private readonly BoboIndexReader reader;
        private Dictionary<string, FacetHandler> runtimeFacetHandlerMap;

        private static ILog logger = LogManager.GetLogger(typeof(BoboBrowser));


        ///<summary>Constructor.</summary>
        ///<param name="reader">A bobo reader instance </param>
        public BoboBrowser(BoboIndexReader reader)
            : base(reader)
        {            
            this.reader = reader;
            runtimeFacetHandlerMap = new Dictionary<string, FacetHandler>();
        }

        private static bool IsNoQueryNoFilter(BrowseRequest req)
        {
            Lucene.Net.Search.Query q = req.Query;
            Filter filter = req.Filter;
            return ((q == null || q is MatchAllDocsQuery || q is FastMatchAllDocsQuery) && filter == null);
        }

        public BoboIndexReader GetIndexReader()
        {
            return this.reader;
        }

        ///<summary>Sets runtime facet handler. If has the same name as a preload handler, for the
        ///duration of this browser, this one will be used.</summary>
        ///<param name="facetHandler">Runtime facet handler </param>
        public virtual void SetFacetHandler(FacetHandler facetHandler)
        {
            List<string> dependsOn = facetHandler.DependsOn;
            BoboIndexReader indexReader = GetIndexReader();
            if (dependsOn.Count > 0)
            {
                List<string>.Enumerator iter = dependsOn.GetEnumerator();
                while (iter.MoveNext())
                {
                    string fn = iter.Current;
                    FacetHandler f = null;
                    if (runtimeFacetHandlerMap.ContainsKey(fn))
                    {
                        f = runtimeFacetHandlerMap[fn];
                    }
                    if (f == null)
                    {
                        f = indexReader.GetFacetHandler(fn);
                    }
                    if (f == null)
                    {
                        throw new System.IO.IOException("depended on facet handler: " + fn + ", but is not found");
                    }
                    facetHandler.PutDependedFacetHandler(f);
                }
            }
            facetHandler.Load(indexReader);
            runtimeFacetHandlerMap.Add(facetHandler.Name, facetHandler);
        }

        ///<summary>Gets a defined facet handler</summary>
        ///<param name="name">facet name </param>
        ///<returns>a facet handler </returns>
        public virtual FacetHandler GetFacetHandler(string name)
        {
            FacetHandler handler;
            if (!runtimeFacetHandlerMap.TryGetValue(name, out handler))
            {
                return reader.GetFacetHandler(name);
            }
            else
            {
                return handler;
            }
        }

        ///<summary>Gets a set of facet names</summary>
        ///<returns> set of facet names </returns>
        public virtual IEnumerable<string> GetFacetNames()
        {
            Dictionary<string, FacetHandler>.KeyCollection runtimeFacetNames = runtimeFacetHandlerMap.Keys;
            IEnumerable<string> installedFacetNames = reader.GetFacetNames();
            if (runtimeFacetNames.Count > 0)
            {
                C5.HashSet<string> names = new C5.HashSet<string>();
                names.AddAll(reader.GetFacetNames());
                names.AddAll(runtimeFacetHandlerMap.Keys);
                return names;
            }
            else
            {
                return installedFacetNames;
            }
        }

        ///<summary>browses the index.</summary>
        ///<param name="req">browse request </param>
        ///<param name="collector">collector for the hits </param>
        ///<param name="facetMap">map to gather facet data </param>
        public virtual void Browse(BrowseRequest req, Collector collector, Dictionary<string, IFacetAccessible> facetMap) //  throws BrowseException
        {
            if (reader == null)
                return;

            IEnumerable<string> fields = GetFacetNames();

            LinkedList<Filter> preFilterList = new LinkedList<Filter>();
            List<FacetHitCollector> facetHitCollectorList = new List<FacetHitCollector>();
            List<IFacetCountCollector> countAllCollectorList = new List<IFacetCountCollector>();

            Filter baseFilter = req.Filter;
            if (baseFilter != null)
            {
                preFilterList.AddLast(baseFilter);
            }

            int selCount = req.SelectionCount;
            bool isNoQueryNoFilter = IsNoQueryNoFilter(req);

            bool isDefaultSearch = isNoQueryNoFilter && selCount == 0;
            try
            {

                foreach (string name in fields)
                {
                    FacetSpec ospec = req.GetFacetSpec(name);

                    FacetHandler handler = GetFacetHandler(name);

                    if (handler == null)
                    {
                        logger.Warn("facet handler: " + name + " is not defined, ignored.");
                        continue;
                    }

                    FacetHitCollector facetHitCollector = null;

                    RandomAccessFilter filter = null;
                    BrowseSelection sel = req.GetSelection(name);
                    if (sel != null)
                    {
                        filter = handler.BuildFilter(sel);
                    }

                    if (ospec == null)
                    {
                        if (filter != null)
                        {
                            preFilterList.AddLast(filter);
                        }
                    }
                    else
                    {
                        if (isDefaultSearch)
                        {
                            countAllCollectorList.Add(handler.GetFacetCountCollector(sel, ospec));
                        }
                        else
                        {
                            facetHitCollector = new FacetHitCollector();
                            facetHitCollector.FacetCountCollector = handler.GetFacetCountCollector(sel, ospec);
                            facetHitCollector.FacetHandler = handler;
                            if (ospec.ExpandSelection)
                            {
                                if (isNoQueryNoFilter && sel != null && selCount == 1)
                                {
                                    facetHitCollector = null; // don't post collect
                                    countAllCollectorList.Add(handler.GetFacetCountCollector(sel, ospec));
                                    if (filter != null)
                                    {
                                        preFilterList.AddLast(filter);
                                    }
                                }
                                else
                                {
                                    if (filter != null)
                                    {
                                        RandomAccessDocIdSet docset = filter.GetRandomAccessDocIdSet(reader);
                                        facetHitCollector.PostDocIDSetIterator = docset.Iterator();
                                        facetHitCollector.DocIdSet = docset;
                                    }
                                }
                            }
                            else
                            {
                                if (filter != null)
                                {
                                    preFilterList.AddLast(filter);
                                }
                            }
                        }
                    }
                    if (facetHitCollector != null)
                    {
                        facetHitCollectorList.Add(facetHitCollector);
                    }
                }

                Filter finalFilter = null;
                if (preFilterList.Count > 0)
                {
                    if (preFilterList.Count == 1)
                    {
                        finalFilter = preFilterList.First.Value;
                    }
                    else
                    {
                        finalFilter = new AndFilter(preFilterList);
                    }
                }

                SetFacetHitCollectorList(facetHitCollectorList);

                Lucene.Net.Search.Query q = req.Query;
                if (q == null || q is MatchAllDocsQuery)
                {
                    q = reader.GetFastMatchAllDocsQuery();
                }

                try
                {
                    Search(q, finalFilter, collector);
                }
                finally
                {
                    foreach (FacetHitCollector facetCollector in facetHitCollectorList)
                    {
                        string name = facetCollector.FacetCountCollector.Name;
                        facetMap.Add(name, facetCollector.FacetCountCollector);
                    }
                    foreach (IFacetCountCollector facetCountCollector in countAllCollectorList)
                    {
                        facetCountCollector.CollectAll();
                        facetMap.Add(facetCountCollector.Name, facetCountCollector);
                    }
                }
            }
            catch (System.IO.IOException ioe)
            {
                throw new BrowseException(ioe.Message, ioe);
            }
        }

        ///<summary>browses the index.</summary>
        ///<param name="req">browse request </param>
        ///<returns> browse result </returns>
        public virtual BrowseResult Browse(BrowseRequest req) // throws BrowseException
        {
            if (reader == null)
            {
                return new BrowseResult();
            }

            BrowseResult result = new BrowseResult();

            long start = System.Environment.TickCount;

            int offset = req.Offset;
            int count = req.Count;

            if (offset < 0 || count <= 0)
            {
                throw new ArgumentException("the offset must be >= 0 and count must be > 0: " + offset + "/" + count);
            }
            TopDocsSortedHitCollector myHC = GetSortedHitCollector(req.Sort, offset, count, req.FetchStoredFields);
            Dictionary<string, IFacetAccessible> facetCollectors = new Dictionary<string, IFacetAccessible>();

            Browse(req, myHC, facetCollectors);
            BrowseHit[] hits = null;

            try
            {
                hits = myHC.GetTopDocs();
            }
            catch (System.IO.IOException e)
            {
                logger.Error(e.Message, e);
                hits = new BrowseHit[0];
            }
            result.Hits = hits;
            result.NumHits = myHC.GetTotalHits();
            result.TotalDocs = reader.NumDocs();
            result.AddAll(facetCollectors);
            long end = System.Environment.TickCount;
            result.Time = end - start;
            return result;
        }

        public virtual Dictionary<string, FacetHandler> GetRuntimeFacetHandlerMap()
        {
            return runtimeFacetHandlerMap;
        }

        public virtual int NumDocs()
        {
            return reader.NumDocs();
        }

        public override Document Doc(int docid)
        {
            Document doc = base.Doc(docid);
            foreach (FacetHandler handler in runtimeFacetHandlerMap.Values)
            {
                string[] vals = handler.GetFieldValues(docid);
                foreach (string val in vals)
                {
                    doc.Add(new Field(handler.Name, val, Field.Store.NO, Field.Index.NOT_ANALYZED));
                }
            }
            return doc;
        }

        ///<summary>Returns the field data for a given doc.</summary>
        ///<param name="docid">doc</param>
        ///<param name="fieldname">name of the field </param>
        ///<returns> field data </returns>
        private class BoboFieldSelector : FieldSelector
        {
            private readonly string fieldname;

            public BoboFieldSelector(string fieldname)
            {
                this.fieldname = fieldname;
            }

            public FieldSelectorResult Accept(string field)
            {
                if (fieldname.Equals(field))
                {
                    return FieldSelectorResult.LOAD_AND_BREAK;
                }
                else
                {
                    return FieldSelectorResult.NO_LOAD;
                }
            }
        }

        public virtual string[] GetFieldVal(int docid, string fieldname)
        {
            FacetHandler facetHandler = GetFacetHandler(fieldname);
            if (facetHandler != null)
            {
                return facetHandler.GetFieldValues(docid);
            }
            else
            {
                logger.Warn("facet handler: " + fieldname + " not defined, looking at stored field.");
                // this is not predefined, so it will be slow
                Document doc = reader.Document(docid, new BoboFieldSelector(fieldname));
                return doc.GetValues(fieldname);
            }
        }


        public virtual object[] GetRawFieldVal(int docid, string fieldname)
        {
            FacetHandler facetHandler = GetFacetHandler(fieldname);
            if (facetHandler == null)
            {
                return GetFieldVal(docid, fieldname);
            }
            else
            {
                return facetHandler.GetRawFieldValues(docid);
            }
        }

        public virtual TopDocsSortedHitCollector GetSortedHitCollector(SortField[] sort, int offset, int count, bool fetchStoredFields)
        {
            return new InternalBrowseHitCollector(this, sort, offset, count, fetchStoredFields);
        }
                
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                this.runtimeFacetHandlerMap = null;
                this.reader.Dispose();
            }
        }
    }
}
