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

﻿// Version compatibility level: 4.0.2
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Search;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Support.Logging;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public class BoboSubBrowser : BoboSearcher, IBrowsable
    {
        private static readonly ILog logger = LogProvider.For<BoboSubBrowser>();
        private readonly BoboSegmentReader m_reader;
        private readonly IDictionary<string, IRuntimeFacetHandlerFactory> m_runtimeFacetHandlerFactoryMap;
        private readonly IDictionary<string, IFacetHandler> m_runtimeFacetHandlerMap;
        private IDictionary<string, IFacetHandler> m_allFacetHandlerMap;
        private IList<IRuntimeFacetHandler> m_runtimeFacetHandlers = null;

        public override IndexReader IndexReader
        {
            get { return m_reader; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">A bobo reader instance</param>
        public BoboSubBrowser(BoboSegmentReader reader)
            : base(reader)
        {
            m_reader = reader;
            m_runtimeFacetHandlerMap = new Dictionary<string, IFacetHandler>();
            m_runtimeFacetHandlerFactoryMap = reader.RuntimeFacetHandlerFactoryMap;
            m_allFacetHandlerMap = null;
        }

        private bool IsNoQueryNoFilter(BrowseRequest req)
        {
            Lucene.Net.Search.Query q = req.Query;
            Filter filter = req.Filter;
            return ((q == null || q is MatchAllDocsQuery) && filter == null && !m_reader.HasDeletions); 
        }

        public virtual object[] GetRawFieldVal(int docid, string fieldname) 
        {
            var facetHandler = GetFacetHandler(fieldname);
            if (facetHandler==null)
            {
                return GetFieldVal(docid, fieldname);
            }
            else
            {
                return facetHandler.GetRawFieldValues(m_reader, docid);
            }
        }

        /// <summary>
        /// Sets runtime facet handler. If has the same name as a preload handler, for the
        /// duration of this browser, this one will be used.
        /// </summary>
        /// <param name="facetHandler">Runtime facet handler</param>
        public virtual void SetFacetHandler(IFacetHandler facetHandler)
        {
            ICollection<string> dependsOn = facetHandler.DependsOn;
            BoboSegmentReader reader = (BoboSegmentReader)IndexReader;
            if (dependsOn.Count > 0)
            {
                foreach (var fn in dependsOn)
                {
                    var f = m_runtimeFacetHandlerMap.Get(fn);
                    if (f == null)
		            {
			            f = reader.GetFacetHandler(fn);
		            }
		            if (f == null)
		            {
			            throw new System.IO.IOException("depended on facet handler: " + fn + ", but is not found");
		            }
		            facetHandler.PutDependedFacetHandler(f);
                }
            }
            facetHandler.LoadFacetData(reader);
            m_runtimeFacetHandlerMap.Put(facetHandler.Name, facetHandler);
        }

        /// <summary>
        /// Gets a defined facet handler
        /// </summary>
        /// <param name="name">facet name</param>
        /// <returns>a facet handler</returns>
        public virtual IFacetHandler GetFacetHandler(string name)
        {
            return FacetHandlerMap.Get(name);
        }

        public virtual IDictionary<string, IFacetHandler> FacetHandlerMap
        {
            get
            {
                if (m_allFacetHandlerMap == null)
                {
                    m_allFacetHandlerMap = new Dictionary<string, IFacetHandler>(m_reader.FacetHandlerMap);
                }
                m_allFacetHandlerMap.PutAll(m_runtimeFacetHandlerMap);
                return m_allFacetHandlerMap;
            }
        }
  
        /// <summary>
        /// Gets a set of facet names
        /// </summary>
        /// <returns>set of facet names</returns>
        public virtual ICollection<string> FacetNames
        {
            get
            {
                var map = FacetHandlerMap;
                return new List<string>(map.Keys);
            }
        }

        /// <summary>
        /// browses the index.
        /// </summary>
        /// <param name="req">browse request</param>
        /// <param name="collector">collector for the hits</param>
        /// <param name="facetMap">map to gather facet data</param>
        /// <param name="start"></param>
        public virtual void Browse(
            BrowseRequest req,
            ICollector collector,
            IDictionary<string, IFacetAccessible> facetMap,
            int start)
        {

            if (m_reader == null)
                return;

    
            //      initialize all RuntimeFacetHandlers with data supplied by user at run-time.
            m_runtimeFacetHandlers = new List<IRuntimeFacetHandler>(m_runtimeFacetHandlerFactoryMap.Count);

            IEnumerable<string> runtimeFacetNames = m_runtimeFacetHandlerFactoryMap.Keys;
            foreach (string facetName in runtimeFacetNames)
            {
                var sfacetHandler = this.GetFacetHandler(facetName);
                if (sfacetHandler != null)
                {
                    logger.Warn("attempting to reset facetHandler: " + sfacetHandler);
                    continue;
                }
                IRuntimeFacetHandlerFactory factory = (IRuntimeFacetHandlerFactory)m_runtimeFacetHandlerFactoryMap.Get(facetName);
      
                try
                {

                    FacetHandlerInitializerParam data = req.GetFacetHandlerData(facetName);
                    if (data == null)
                        data = FacetHandlerInitializerParam.EMPTY_PARAM;
                    if (data != FacetHandlerInitializerParam.EMPTY_PARAM || !factory.IsLoadLazily)
                    {
                        IRuntimeFacetHandler facetHandler = factory.Get(data);
                        if (facetHandler != null)
                        {
                            m_runtimeFacetHandlers.Add(facetHandler); // add to a list so we close them after search
                            this.SetFacetHandler(facetHandler);
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new BrowseException("error trying to set FacetHandler : " + facetName + ":" + e.Message, e);
                }
            }
            // done initialize all RuntimeFacetHandlers with data supplied by user at run-time.

            IEnumerable<string> fields = FacetNames;

            List<Filter> preFilterList = new List<Filter>();
            List<FacetHitCollector> facetHitCollectorList = new List<FacetHitCollector>();
    
            Filter baseFilter = req.Filter;
            if (baseFilter != null)
            {
                preFilterList.Add(baseFilter);
            }

            int selCount = req.SelectionCount;
            bool isNoQueryNoFilter = IsNoQueryNoFilter(req);

            bool isDefaultSearch = isNoQueryNoFilter && selCount == 0;
            try
            {
      
                foreach (string name in fields)
                {
                    BrowseSelection sel = req.GetSelection(name);
                    FacetSpec ospec = req.GetFacetSpec(name);

                    var handler = GetFacetHandler(name);
        
                    if (handler == null)
                    {
        	            logger.Error("facet handler: " + name + " is not defined, ignored.");
        	            continue;
                    }
        
                    FacetHitCollector facetHitCollector = null;

                    RandomAccessFilter filter = null;
                    if (sel != null)
                    {
                        filter = handler.BuildFilter(sel);
                    }

                    if (ospec == null)
                    {
                        if (filter != null)
                        {
                            preFilterList.Add(filter);
                        }
                    }
                    else
                    {
                        /*FacetSpec fspec = new FacetSpec(); // OrderValueAsc,
                        fspec.setMaxCount(0);
                        fspec.setMinHitCount(1);
          
                        fspec.setExpandSelection(ospec.isExpandSelection());*/
                        FacetSpec fspec = ospec;

                        facetHitCollector = new FacetHitCollector();
                        facetHitCollector.FacetHandler = handler;
          
                        if (isDefaultSearch)
                        {
        	                facetHitCollector.CollectAllSource = handler.GetFacetCountCollectorSource(sel, fspec);
                        }
                        else
                        {
                            facetHitCollector.FacetCountCollectorSource = handler.GetFacetCountCollectorSource(sel, fspec);            
                            if (ospec.ExpandSelection)
                            {
                                if (isNoQueryNoFilter && sel != null && selCount == 1)
                                {
            	                    facetHitCollector.CollectAllSource = handler.GetFacetCountCollectorSource(sel, fspec);
                                    if (filter != null)
                                    {
                                        preFilterList.Add(filter);
                                    }
                                }
                                else
                                {
                                    if (filter != null)
                                    {
                	                    facetHitCollector.Filter = filter;
                                    }
                                }
                            }
                            else
                            {
                                if (filter != null)
                                {
                                    preFilterList.Add(filter);
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
                        finalFilter = preFilterList.First();
                    }
                    else
                    {
                        finalFilter = new AndFilter(preFilterList);
                    }
                }

                this.SetFacetHitCollectorList(facetHitCollectorList);

                try
                {
                    var query = req.Query;
                    Weight weight = CreateNormalizedWeight(query);
                    Search(weight, finalFilter, collector, start, req.MapReduceWrapper);
                }
                finally
                {
                    foreach (FacetHitCollector facetCollector in facetHitCollectorList)
                    {
                        string name = facetCollector.FacetHandler.Name;
                        IList<IFacetCountCollector> resultcollector = null;
                        resultcollector = facetCollector.CountCollectorList;
                        if (resultcollector == null || resultcollector.Count == 0)
                        {
        	                resultcollector = facetCollector.CollectAllCollectorList;
                        }
                        if (resultcollector != null)
                        {
        	                FacetSpec fspec = req.GetFacetSpec(name);
        	                Debug.Assert(fspec != null);
                            if(resultcollector.Count == 1)
                            {
                                facetMap.Put(name, resultcollector[0]);             
                            }
                            else
                            {
                                List<IFacetAccessible> finalList = new List<IFacetAccessible>(resultcollector.Count);
                                foreach (IFacetCountCollector fc in resultcollector)
                                {
                                    finalList.Add((IFacetAccessible)fc);
                                }
        	                    CombinedFacetAccessible combinedCollector = new CombinedFacetAccessible(fspec, finalList);
                                facetMap.Put(name, combinedCollector);
        	                }
                        }
                    }
                }
            }
            catch (Exception ioe)
            {
                throw new BrowseException(ioe.Message, ioe);
            }
        }

        public virtual SortCollector GetSortCollector(
            SortField[] sort,
            Lucene.Net.Search.Query q, 
            int offset, 
            int count, 
            bool fetchStoredFields, 
            ICollection<string> termVectorsToFetch, 
            string[] groupBy, 
            int maxPerGroup, 
            bool collectDocIdCache)
        {
            return SortCollector.BuildSortCollector(
                this, 
                q, 
                sort, 
                offset, 
                count, 
                fetchStoredFields, 
                termVectorsToFetch, 
                groupBy, 
                maxPerGroup, 
                collectDocIdCache);
        }

        /// <summary>
        /// Browses the index
        /// </summary>
        /// <param name="req">Browse request</param>
        /// <returns>Browse result</returns>
        public virtual BrowseResult Browse(BrowseRequest req)
        {
            throw new NotSupportedException();
        }

        public virtual IDictionary<string, IFacetHandler> RuntimeFacetHandlerMap
        {
            get { return m_runtimeFacetHandlerMap; }
        }

        public virtual int NumDocs
        {
            get { return m_reader.NumDocs; }
        }

        public override Document Doc(int docid)
        {
            Document doc = base.Doc(docid);
            foreach (var handler in m_runtimeFacetHandlerMap.Values)
            {
                string[] vals = handler.GetFieldValues(m_reader, docid);
                foreach (var val in vals)
                {
                    doc.Add(new StringField(handler.Name, val, Field.Store.NO));
                }
            }
            return doc;
        }

        /// <summary>
        /// Returns the field data for a given doc.
        /// </summary>
        /// <param name="docid">doc</param>
        /// <param name="fieldname">name of the field</param>
        /// <returns>field data</returns>
        public virtual string[] GetFieldVal(int docid, string fieldname)
        {
            var facetHandler = GetFacetHandler(fieldname);
            if (facetHandler != null)
            {
                return facetHandler.GetFieldValues(m_reader, docid);
            }
            else
            {
                logger.Warn("facet handler: " + fieldname
                    + " not defined, looking at stored field.");
                // this is not predefined, so it will be slow

                return m_reader.GetStoredFieldValue(docid, fieldname);
            }
        }

        // Analogous to the DoClose() method in Java
        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this)
                {
                    Exception exception = null;
                    if (m_runtimeFacetHandlers != null)
                    {
                        foreach (var handler in m_runtimeFacetHandlers)
                        {
                            try
                            {
                                handler.Dispose();
                            }
                            catch (Exception e)
                            {
                                exception = e;
                            }
                        }
                    }
                    if (m_reader != null)
                    {
                        m_reader.ClearRuntimeFacetData();
                        m_reader.ClearRuntimeFacetHandler();
                    }
                    if (exception != null)
                    {
                        throw exception;
                    }
                }
            }
        }
    }
}
