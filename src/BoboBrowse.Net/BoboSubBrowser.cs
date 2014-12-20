﻿// Version compatibility level: 3.1.0
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Search;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Common.Logging;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class BoboSubBrowser : BoboSearcher2, IBrowsable
    {
        private static ILog logger = LogManager.GetLogger<BoboSubBrowser>();
        private readonly BoboIndexReader _reader;
        private readonly IDictionary<string, IRuntimeFacetHandlerFactory> _runtimeFacetHandlerFactoryMap;
        private readonly IDictionary<string, IFacetHandler> _runtimeFacetHandlerMap;
        private IDictionary<string, IFacetHandler> _allFacetHandlerMap;
        private IList<IRuntimeFacetHandler> _runtimeFacetHandlers = null;

        new public BoboIndexReader IndexReader
        {
            get { return _reader; }
        }

        public BoboSubBrowser(BoboIndexReader reader)
            : base(reader)
        {
            _reader = reader;
            _runtimeFacetHandlerMap = new Dictionary<string, IFacetHandler>();
            _runtimeFacetHandlerFactoryMap = reader.RuntimeFacetHandlerFactoryMap;
            _allFacetHandlerMap = null;
        }

        private bool IsNoQueryNoFilter(BrowseRequest req)
        {
            Lucene.Net.Search.Query q = req.Query;
            Filter filter = req.Filter;
            return ((q == null || q is MatchAllDocsQuery) && filter == null && !_reader.HasDeletions); 
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
                return facetHandler.GetRawFieldValues(_reader, docid);
            }
        }

        /// <summary>
        /// Sets runtime facet handler. If has the same name as a preload handler, for the
        /// duration of this browser, this one will be used.
        /// </summary>
        /// <param name="facetHandler">Runtime facet handler</param>
        public virtual void SetFacetHandler(IFacetHandler facetHandler)
        {
            IEnumerable<string> dependsOn = facetHandler.DependsOn;
            BoboIndexReader reader = (BoboIndexReader)IndexReader;
            if (dependsOn.Count() > 0)
            {
                foreach (var fn in dependsOn)
                {
                    var f = _runtimeFacetHandlerMap.Get(fn);
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
            _runtimeFacetHandlerMap.Put(facetHandler.Name, facetHandler);
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
                if (_allFacetHandlerMap == null)
                {
                    _allFacetHandlerMap = new Dictionary<string, IFacetHandler>(_reader.FacetHandlerMap);
                }
                _allFacetHandlerMap.PutAll(_runtimeFacetHandlerMap);
                return _allFacetHandlerMap;
            }
        }
  
        /// <summary>
        /// Gets a set of facet names
        /// </summary>
        /// <returns>set of facet names</returns>
        public IEnumerable<string> FacetNames
        {
            get
            {
                var map = FacetHandlerMap;
                return map.Keys.ToArray();
            }
        }

        /// <summary>
        /// browses the index.
        /// </summary>
        /// <param name="req">browse request</param>
        /// <param name="collector">collector for the hits</param>
        /// <param name="facetMap">map to gather facet data</param>
        public virtual void Browse(
            BrowseRequest req,
            Collector collector,
            IDictionary<string, IFacetAccessible> facetMap)
        {
            Browse(req, collector, facetMap, 0);
        }

        public virtual void Browse(
            BrowseRequest req,
            Collector collector,
            IDictionary<string, IFacetAccessible> facetMap,
            int start)
        {
            Weight w = null;
            try
            {
                var q = req.Query;
                if (q == null)
                {
                    q = new MatchAllDocsQuery();
                }
                w = CreateWeight(q);
            }
            catch (Exception ioe)
            {
            throw new BrowseException(ioe.Message, ioe);
            }
            Browse(req, w, collector, facetMap, start);
        }

        public virtual void Browse(
            BrowseRequest req,
            Weight weight,
            Collector collector,
            IDictionary<string, IFacetAccessible> facetMap,
            int start)
        {

            if (_reader == null)
                return;

    
            //      initialize all RuntimeFacetHandlers with data supplied by user at run-time.
            _runtimeFacetHandlers = new List<IRuntimeFacetHandler>(_runtimeFacetHandlerFactoryMap.Count());

            IEnumerable<string> runtimeFacetNames = _runtimeFacetHandlerFactoryMap.Keys;
            foreach (string facetName in runtimeFacetNames)
            {
                var sfacetHandler = this.GetFacetHandler(facetName);
                if (sfacetHandler!=null)
                {
                    logger.Warn("attempting to reset facetHandler: " + sfacetHandler);
                    continue;
                }
                IRuntimeFacetHandlerFactory factory = (IRuntimeFacetHandlerFactory)_runtimeFacetHandlerFactoryMap.Get(facetName);
      
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
                            _runtimeFacetHandlers.Add(facetHandler); // add to a list so we close them after search
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
                        facetHitCollector.facetHandler = handler;
          
                        if (isDefaultSearch)
                        {
        	                facetHitCollector._collectAllSource = handler.GetFacetCountCollectorSource(sel, fspec);
                        }
                        else
                        {
                            facetHitCollector._facetCountCollectorSource = handler.GetFacetCountCollectorSource(sel, fspec);            
                            if (ospec.ExpandSelection)
                            {
                                if (isNoQueryNoFilter && sel != null && selCount == 1)
                                {
            	                    facetHitCollector._collectAllSource = handler.GetFacetCountCollectorSource(sel, fspec);
                                    if (filter != null)
                                    {
                                        preFilterList.Add(filter);
                                    }
                                }
                                else
                                {
                                    if (filter != null)
                                    {
                	                    facetHitCollector._filter = filter;
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

                this.FacetHitCollectorList = facetHitCollectorList;

                try
                {
                    if (weight == null)
                    {
                        var q = req.Query;
                        if (q == null)
                        {
                            q = new MatchAllDocsQuery();
                        }
                        weight = CreateWeight(q);
                    }
                    // TODO: Reduce wrapper not supported
                    //Search(weight, finalFilter, collector, start, req.GetMapReduceWrapper());
                    Search(weight, finalFilter, collector, start);
                }
                finally
                {
                    foreach (FacetHitCollector facetCollector in facetHitCollectorList)
                    {
                        string name = facetCollector.facetHandler.Name;
                        List<IFacetCountCollector> resultcollector = null;
                        resultcollector = facetCollector._countCollectorList;
                        if (resultcollector == null || resultcollector.Count == 0)
                        {
        	                resultcollector = facetCollector._collectAllCollectorList;
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
            IEnumerable<string> termVectorsToFetch, 
            bool forceScoring, 
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
                forceScoring, 
                fetchStoredFields, 
                termVectorsToFetch, 
                groupBy, 
                maxPerGroup, 
                collectDocIdCache);
        }

        public virtual BrowseResult Browse(BrowseRequest req)
        {
            if (_reader == null)
                return new BrowseResult();

            BrowseResult result = new BrowseResult();

            long start = System.Environment.TickCount;

            SortCollector collector = GetSortCollector(req.Sort, req.Query, req.Offset, req.Count, req.FetchStoredFields, req.TermVectorsToFetch, false, req.GroupBy, req.MaxPerGroup, req.CollectDocIdCache);
    
            IDictionary<string, IFacetAccessible> facetCollectors = new Dictionary<string, IFacetAccessible>();
            Browse(req, collector, facetCollectors);
            BrowseHit[] hits = null;

            try
            {
                hits = collector.TopDocs;
            }
            catch (Exception e)
            {
                logger.Error(e.Message, e);
                hits = new BrowseHit[0];
            }
    
            var q = req.Query;
            if (q == null)
            {
    	        q = new MatchAllDocsQuery();
            }
            if (req.ShowExplanation)
            {
    	        foreach (BrowseHit hit in hits)
                {
    		        try 
                    {
				        Explanation expl = Explain(q, hit.DocId);
				        hit.Explanation = expl;
			        } 
                    catch (Exception e) 
                    {
				        logger.Error(e.Message, e);
			        }
    	        }
            }
            result.Hits = hits;
            result.NumHits = collector.TotalHits;
            result.NumGroups = collector.TotalGroups;
            result.GroupAccessibles = collector.GroupAccessibles;
            result.SortCollector = collector;
            result.TotalDocs = _reader.NumDocs();
            result.AddAll(facetCollectors);
            long end = System.Environment.TickCount;
            result.Time = (end - start);
            return result;
        }

        public virtual IDictionary<string, IFacetHandler> RuntimeFacetHandlerMap
        {
            get { return _runtimeFacetHandlerMap; }
        }

        public virtual int NumDocs()
        {
            return _reader.NumDocs();
        }

        public override Document Doc(int docid)
        {
            Document doc = base.Doc(docid);
            foreach (var handler in _runtimeFacetHandlerMap.Values)
            {
                string[] vals = handler.GetFieldValues(_reader, docid);
                foreach (var val in vals)
                {
                    doc.Add(new Field(handler.Name,
                          val,
                          Field.Store.NO,
                          Field.Index.NOT_ANALYZED));
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
                return facetHandler.GetFieldValues(_reader, docid);
            }
            else
            {
                logger.Warn("facet handler: " + fieldname
                    + " not defined, looking at stored field.");
                // this is not predefined, so it will be slow
                Document doc = _reader.Document(docid, new BoboSubBrowserFieldSelector(fieldname));
      
                return doc.GetValues(fieldname);
            }
        }

        private class BoboSubBrowserFieldSelector : Lucene.Net.Documents.FieldSelector
        {
            private static long serialVersionUID = 1L;
            private readonly string _field;

            public BoboSubBrowserFieldSelector(string fieldName)
            {
                this._field = fieldName;
            }

            public Lucene.Net.Documents.FieldSelectorResult Accept(string fieldName)
            {
 	            if (fieldName.Equals(_field))
                {
                    return Lucene.Net.Documents.FieldSelectorResult.LOAD_AND_BREAK;
                }
                else
                {
                    return Lucene.Net.Documents.FieldSelectorResult.NO_LOAD;
                }
            }
        }

        // TODO: Should this be dispose?
        public void Close()
        {
            if (_runtimeFacetHandlers != null)
            {
                foreach (var handler in _runtimeFacetHandlers)
                {
                    handler.Close();
                }
            }
            if (_reader != null)
            {
                _reader.ClearRuntimeFacetData();
                _reader.ClearRuntimeFacetHandler();
            }
            base.Close();
        }
    }
}
