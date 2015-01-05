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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Support;
    using Common.Logging;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Directory = Lucene.Net.Store.Directory;

    /// <summary>
    /// bobo browse index reader
    /// </summary>
    public class BoboIndexReader : FilterIndexReader
    {
        private const string SPRING_CONFIG = "bobo.spring";
        private static readonly ILog logger = LogManager.GetLogger(typeof(BoboIndexReader));

        protected IDictionary<string, IFacetHandler> _facetHandlerMap;

        protected IEnumerable<IFacetHandler> _facetHandlers;
        protected IEnumerable<IRuntimeFacetHandlerFactory> _runtimeFacetHandlerFactories;
        protected IDictionary<string, IRuntimeFacetHandlerFactory> _runtimeFacetHandlerFactoryMap;

        protected WorkArea _workArea;

        protected IndexReader _srcReader;
        internal readonly BoboIndexReader[] _subReaders = null;
        protected int[] _starts = null;
        private Directory _dir = null;

        private readonly IDictionary<string, object> _facetDataMap = new Dictionary<string, object>();

        private readonly CloseableThreadLocal<IDictionary<string, object>> _runtimeFacetDataMap = new CloseableThreadLocal<IDictionary<string, object>>();
        
        private readonly CloseableThreadLocal<IDictionary<string, IRuntimeFacetHandler>> _runtimeFacetHandlerMap = new CloseableThreadLocal<IDictionary<string, IRuntimeFacetHandler>>();

        private readonly bool _autoClose = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstance(IndexReader reader)
        {
            return BoboIndexReader.GetInstance(reader, null, null, new WorkArea(), false);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="autoClose">True to close the underlying IndexReader when this instance is closed.</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstance(IndexReader reader, bool autoClose)
        {
            return BoboIndexReader.GetInstance(reader, null, null, new WorkArea(), autoClose);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="workArea">A service locator that can be used to inject custom objects.</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstance(IndexReader reader, WorkArea workArea)
        {
            return BoboIndexReader.GetInstance(reader, null, null, workArea, false);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="workArea">A service locator that can be used to inject custom objects.</param>
        /// <param name="autoClose">True to close the underlying IndexReader when this instance is closed.</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstance(IndexReader reader, WorkArea workArea, bool autoClose)
        {
            return BoboIndexReader.GetInstance(reader, null, null, workArea, autoClose);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <param name="facetHandlerFactories">List of factories to create facet handler instances at runtime.</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstance(IndexReader reader, IEnumerable<IFacetHandler> facetHandlers, IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories)
        {
            return BoboIndexReader.GetInstance(reader, facetHandlers, facetHandlerFactories, new WorkArea(), false);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <param name="facetHandlerFactories"></param>
        /// <param name="autoClose">True to close the underlying IndexReader when this instance is closed.</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstance(IndexReader reader, IEnumerable<IFacetHandler> facetHandlers, IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories, bool autoClose)
        {
            return BoboIndexReader.GetInstance(reader, facetHandlers, facetHandlerFactories, new WorkArea(), autoClose);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstance(IndexReader reader, IEnumerable<IFacetHandler> facetHandlers)
        {
            return BoboIndexReader.GetInstance(reader, facetHandlers, new IRuntimeFacetHandlerFactory[0], new WorkArea(), false);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <param name="autoClose">True to close the underlying IndexReader when this instance is closed.</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstance(IndexReader reader, IEnumerable<IFacetHandler> facetHandlers, bool autoClose)
        {
            return BoboIndexReader.GetInstance(reader, facetHandlers, new IRuntimeFacetHandlerFactory[0], new WorkArea(), autoClose);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <param name="facetHandlerFactories">List of factories to create facet handler instances at runtime.</param>
        /// <param name="workArea">A service locator that can be used to inject custom objects.</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstance(IndexReader reader, IEnumerable<IFacetHandler> facetHandlers, IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories, WorkArea workArea)
        {
            return BoboIndexReader.GetInstance(reader, facetHandlers, facetHandlerFactories, workArea, false);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <param name="facetHandlerFactories">List of factories to create facet handler instances at runtime.</param>
        /// <param name="workArea">A service locator that can be used to inject custom objects.</param>
        /// <param name="autoClose">True to close the underlying IndexReader when this instance is closed.</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstance(IndexReader reader, IEnumerable<IFacetHandler> facetHandlers, IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories, WorkArea workArea, bool autoClose)
        {
            BoboIndexReader boboReader = new BoboIndexReader(reader, facetHandlers, facetHandlerFactories, workArea, autoClose);
            boboReader.FacetInit();
            return boboReader;
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstanceAsSubReader(IndexReader reader)
        {
            return GetInstanceAsSubReader(reader, null, null, new WorkArea(), false);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="autoClose">True to close the underlying IndexReader when this instance is closed.</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstanceAsSubReader(IndexReader reader, bool autoClose)
        {
            return GetInstanceAsSubReader(reader, null, null, new WorkArea(), autoClose);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <param name="facetHandlerFactories">List of factories to create facet handler instances at runtime.</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstanceAsSubReader(IndexReader reader,
                                                       IEnumerable<IFacetHandler> facetHandlers,
                                                       IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories)
        {
            return GetInstanceAsSubReader(reader, facetHandlers, facetHandlerFactories, new WorkArea(), false);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <param name="facetHandlerFactories">List of factories to create facet handler instances at runtime.</param>
        /// <param name="autoClose">True to close the underlying IndexReader when this instance is closed.</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstanceAsSubReader(IndexReader reader,
                                                       IEnumerable<IFacetHandler> facetHandlers,
                                                       IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories,
                                                       bool autoClose)
        {
            return GetInstanceAsSubReader(reader, facetHandlers, facetHandlerFactories, new WorkArea(), autoClose);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <param name="facetHandlerFactories">List of factories to create facet handler instances at runtime.</param>
        /// <param name="workArea">A service locator that can be used to inject custom objects.</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstanceAsSubReader(IndexReader reader,
                                                       IEnumerable<IFacetHandler> facetHandlers,
                                                       IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories,
                                                       WorkArea workArea)
        {
            return GetInstanceAsSubReader(reader, facetHandlers, facetHandlerFactories, workArea, false);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <param name="facetHandlerFactories">List of factories to create facet handler instances at runtime.</param>
        /// <param name="workArea">A service locator that can be used to inject custom objects.</param>
        /// <param name="autoClose">True to close the underlying IndexReader when this instance is closed.</param>
        /// <returns>A new BoboIndexReader instance.</returns>
        public static BoboIndexReader GetInstanceAsSubReader(IndexReader reader,
                                                       IEnumerable<IFacetHandler> facetHandlers,
                                                       IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories,
                                                       WorkArea workArea,
                                                       bool autoClose)
        {
            BoboIndexReader boboReader = new BoboIndexReader(reader, facetHandlers, facetHandlerFactories, workArea, autoClose, false);
            boboReader.FacetInit();
            return boboReader;
        }


        public override long Version
        {
            get
            {
                try
                {
                    SegmentInfos sinfos = new SegmentInfos();
                    sinfos.Read(_dir);
                    return sinfos.Version;
                }
                catch
                {
                    return 0L;
                }
            }
        }

        public virtual IndexReader InnerReader
        {
            get { return this.in_Renamed; }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override IndexReader Reopen()
        {
            throw new NotSupportedException();

            //SegmentInfos sinfos = new SegmentInfos();
            //sinfos.Read(_dir);
            //int size = sinfos.Count;

            //if (this.in_Renamed is MultiReader)
            //{
            //    // setup current reader list
            //    List<IndexReader> boboReaderList = new List<IndexReader>();
            //    ReaderUtil.GatherSubReaders(boboReaderList, this.in_Renamed);
            //    var readerMap = new Dictionary<string, BoboIndexReader>();

            //    foreach (IndexReader reader in boboReaderList)
            //    {
            //        BoboIndexReader boboReader = (BoboIndexReader)reader;
            //        SegmentReader sreader = (SegmentReader)(boboReader.in_Renamed);
            //        readerMap.Put(sreader.SegmentName, boboReader);
            //    }

            //    var currentReaders = new List<BoboIndexReader>(size);
            //    bool isNewReader = false;
            //    for (int i = 0; i < size; ++i)
            //    {
            //        SegmentInfo sinfo = (SegmentInfo)sinfos.Info(i);

            //        // NOTE: Replaced the remove() call with the 2 lines below.
            //        // It didn't look like the java HashMap was thread safe anyway.
            //        BoboIndexReader breader = readerMap.Get(sinfo.name); 
            //        readerMap.Remove(sinfo.name);
            //        if (breader != null)
            //        {
            //            // should use SegmentReader.reopen
            //            // TODO: see LUCENE-2559
            //            BoboIndexReader newReader = (BoboIndexReader)breader.Reopen(true);
            //            if (newReader != breader)
            //            {
            //                isNewReader = true;
            //            }
            //            if (newReader != null)
            //            {
            //                currentReaders.Add(newReader);
            //            }
            //        }
            //        else
            //        {
            //            isNewReader = true;
            //            SegmentReader newSreader = SegmentReader.Get(true, sinfo, 1);
            //            breader = BoboIndexReader.GetInstanceAsSubReader(newSreader, this._facetHandlers, this._runtimeFacetHandlerFactories);
            //            breader._dir = _dir;
            //            currentReaders.Add(breader);
            //        }
            //    }
            //    isNewReader = isNewReader || (readerMap.Count != 0);
            //    if (!isNewReader)
            //    {
            //        return this;
            //    }
            //    else
            //    {
            //        MultiReader newMreader = new MultiReader(currentReaders.ToArray(), false);
            //        BoboIndexReader newReader = BoboIndexReader.GetInstanceAsSubReader(newMreader, this._facetHandlers, this._runtimeFacetHandlerFactories);
            //        newReader._dir = _dir;
            //        return newReader;
            //    }
            //}
            //else if (this.in_Renamed is SegmentReader)
            //{
            //    // should use SegmentReader.reopen
            //    // TODO: see LUCENE-2559

            //    SegmentReader sreader = (SegmentReader)this.in_Renamed;
            //    int numDels = sreader.NumDeletedDocs;

            //    SegmentInfo sinfo = null;
            //    bool sameSeg = false;
            //    //get SegmentInfo instance
            //    for (int i = 0; i < size; ++i)
            //    {
            //        SegmentInfo sinfoTmp = (SegmentInfo)sinfos.Info(i);
            //        if (sinfoTmp.name.Equals(sreader.SegmentName))
            //        {
            //            int numDels2 = sinfoTmp.GetDelCount();
            //            sameSeg = numDels == numDels2;
            //            sinfo = sinfoTmp;
            //            break;
            //        }
            //    }

            //    if (sinfo == null)
            //    {
            //        // segment no longer exists
            //        return null;
            //    }
            //    if (sameSeg)
            //    {
            //        return this;
            //    }
            //    else
            //    {
            //        SegmentReader newSreader = SegmentReader.Get(true, sinfo, 1);
            //        return BoboIndexReader.GetInstanceAsSubReader(newSreader, this._facetHandlers, this._runtimeFacetHandlerFactories);
            //    }
            //}
            //else
            //{
            //    // should not reach here, a catch-all default case
            //    IndexReader reader = this.in_Renamed.Reopen(true);
            //    if (this.in_Renamed != reader)
            //    {
            //        return BoboIndexReader.GetInstance(newInner, _facetHandlers, _runtimeFacetHandlerFactories, _workArea);
            //    }
            //    else
            //    {
            //        return this;
            //    }
            //}
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override IndexReader Reopen(bool openReadOnly)
        {
            // bobo readers are always readonly 
            return Reopen();
        }

        public virtual object GetFacetData(string name)
        {
            return _facetDataMap.Get(name);
        }

        public virtual void PutFacetData(string name, object data)
        {
            _facetDataMap.Put(name, data);
        }

        public virtual object GetRuntimeFacetData(string name)
        {
            var map = _runtimeFacetDataMap.Get();
            if (map == null) return null;

            return map.Get(name);
        }

        public virtual void PutRuntimeFacetData(string name, object data)
        {
            var map = _runtimeFacetDataMap.Get();
            if (map == null)
            {
                map = new Dictionary<string, object>();
                _runtimeFacetDataMap.Set(map);
            }
            map.Put(name, data);
        }

        public virtual void ClearRuntimeFacetData()
        {
            _runtimeFacetDataMap.Set(null);
        }

        public virtual IRuntimeFacetHandler GetRuntimeFacetHandler(string name)
        {
            var map = _runtimeFacetHandlerMap.Get();
            if (map == null) return null;

            return map.Get(name);
        }

        public virtual void PutRuntimeFacetHandler(string name, IRuntimeFacetHandler data)
        {
            var map = _runtimeFacetHandlerMap.Get();
            if (map == null)
            {
                map = new Dictionary<string, IRuntimeFacetHandler>();
                _runtimeFacetHandlerMap.Set(map);
            }
            map.Put(name, data);
        }

        public virtual void ClearRuntimeFacetHandler()
        {
            _runtimeFacetHandlerMap.Set(null);
        }

        protected override void Dispose(bool disposing)
        {
            _facetDataMap.Clear();
            // We don't want to close the underlying reader here - that should
            // be up to the caller.
        }

        protected override void DoClose()
        {
            //We can not clean up the facetDataMap, as it might be used by other BoboIndexReaders created by the copy method
            //_facetDataMap.Clear();
            // BUG: DoClose() already calls close on the inner reader
            //if (_srcReader != null) _srcReader.Close();

            // We don't want to close the underlying index reader by default
            // because it should be closed in its own try block or using statement
            // and closing it twice causes an exception.
            if (_autoClose)
            {
                base.DoClose();
            }
        }

        protected override void DoCommit(IDictionary<string, string> commitUserData)
        {
            if (_srcReader != null) _srcReader.Flush(commitUserData);
        }

        protected override void DoDelete(int n)
        {
            if (_srcReader != null) _srcReader.DeleteDocument(n);
        }

        private void LoadFacetHandler(string name, IList<string> loaded, IList<string> visited, WorkArea workArea)
        {
            IFacetHandler facetHandler = _facetHandlerMap.Get(name);
            if (facetHandler != null && !loaded.Contains(name))
            {
                visited.Add(name);
                var dependsOn = facetHandler.DependsOn;
                if (dependsOn.Count() > 0)
                {
                    foreach (var f in dependsOn)
                    {
                        if (name.Equals(f))
                            continue;
                        if (!loaded.Contains(f))
                        {
                            if (visited.Contains(f))
                            {
                                throw new IOException("Facet handler dependency cycle detected, facet handler: " + name + " not loaded");
                            }
                            LoadFacetHandler(f, loaded, visited, workArea);
                        }
                        if (!loaded.Contains(f))
                        {
                            throw new IOException("unable to load facet handler: " + f);
                        }
                        facetHandler.PutDependedFacetHandler(_facetHandlerMap[f]);
                    }
                }

                long start = System.Environment.TickCount;
                facetHandler.LoadFacetData(this, workArea);
                long end = System.Environment.TickCount;
                if (logger.IsDebugEnabled)
                {
                    StringBuilder buf = new StringBuilder();
                    buf.Append("facetHandler loaded: ").Append(name).Append(", took: ").Append(end - start).Append(" ms");
                    logger.Debug(buf.ToString());
                }
                loaded.Add(name);
            }
        }

        private void LoadFacetHandlers(WorkArea workArea, IEnumerable<string> toBeRemoved)
        {
            var loaded = new List<string>();
            var visited = new List<string>();

            foreach (string name in _facetHandlerMap.Keys)
            {
                this.LoadFacetHandler(name, loaded, visited, workArea);
            }

            foreach (string name in toBeRemoved)
            {
                _facetHandlerMap.Remove(name);
            }
        }

        /// <summary>
        /// Find all the leaf sub-readers and wrap each in BoboIndexReader.
        /// </summary>
        /// <param name="reader">reader</param>
        /// <param name="workArea">workArea</param>
        /// <returns></returns>
        private static IndexReader[] CreateSubReaders(IndexReader reader, WorkArea workArea)
        {
            List<IndexReader> readerList = new List<IndexReader>();
            ReaderUtil.GatherSubReaders(readerList, reader);
            IndexReader[] subReaders = (IndexReader[])readerList.ToArray();
            BoboIndexReader[] boboReaders;
            var subReaderCount = subReaders.Count();

            if (subReaders != null && subReaderCount > 0)
            {
                boboReaders = new BoboIndexReader[subReaderCount];
                for (int i = 0; i < subReaderCount; i++)
                {
                    boboReaders[i] = new BoboIndexReader(subReaders[i], null, null, workArea, true, false);
                }
            }
            else
            {
                boboReaders = new BoboIndexReader[] { new BoboIndexReader(reader, null, null, workArea, true, false) };
            }
            return boboReaders;
        }

        public override Directory Directory()
        {
            return (_subReaders != null) ? _subReaders[0].Directory() : base.Directory();
        }

        private void Initialize(ref IEnumerable<IFacetHandler> facetHandlers)
        {
            if (facetHandlers == null)
            {
                var idxDir = Directory();
                if (idxDir != null && idxDir is FSDirectory)
                {
                    // Look for the bobo.spring file in the same directory as the Lucene index
                    var dir = ((FSDirectory)idxDir).Directory;
                    var springConfigFile = Path.Combine(dir.FullName, SPRING_CONFIG);
                    Type loaderType = Type.GetType("BoboBrowse.Net.Spring.FacetHandlerLoader, BoboBrowse.Net.Spring");

                    if (loaderType != null)
                    {
                        var loaderInstance = Activator.CreateInstance(loaderType);
                        
                        MethodInfo methodInfo = loaderType.GetMethod("LoadFacetHandlers");
                        facetHandlers = (IEnumerable<IFacetHandler>)methodInfo.Invoke(loaderInstance, new object[] { springConfigFile, _workArea });
                    }
                    else
                    {
                        if (File.Exists(springConfigFile))
                        {
                            throw new RuntimeException(string.Format(
                                "There is a file named '{0}' in the Lucene.Net index directory '{1}', but you don't have " + 
                                "the BoboBrowse.Net.Spring assembly in your project to resolve the references. You can " + 
                                "download BoboBrowse.Net.Spring as a separate optional package from NuGet or you can provide " +
								"facet handlers using an alternate BoboBrowseIndex.GetInstance overload", SPRING_CONFIG, dir));
                        }

                        facetHandlers = new List<IFacetHandler>();
                    }
                }
                else
                {
                    facetHandlers = new List<IFacetHandler>();
                }
            }

            _facetHandlers = facetHandlers;
            _facetHandlerMap = new Dictionary<string, IFacetHandler>();
            foreach (var facetHandler in facetHandlers)
            {
                _facetHandlerMap.Put(facetHandler.Name, facetHandler);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <param name="facetHandlerFactories">List of factories to create facet handler instances at runtime.</param>
        /// <param name="workArea">A service locator that can be used to inject custom objects.</param>
        /// <param name="autoClose">True to close the underlying IndexReader when this instance is closed.</param>
        protected BoboIndexReader(IndexReader reader, 
            IEnumerable<IFacetHandler> facetHandlers, 
            IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories, 
            WorkArea workArea,
            bool autoClose)
            : this(reader, facetHandlers, facetHandlerFactories, workArea, autoClose, true)
        {
            _srcReader = reader;
            _autoClose = autoClose;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <param name="facetHandlerFactories">List of factories to create facet handler instances at runtime.</param>
        /// <param name="workArea">A service locator that can be used to inject custom objects.</param>
        /// <param name="autoClose">True to close the underlying IndexReader when this instance is closed.</param>
        /// <param name="useSubReaders">true => we create a MultiReader of all the leaf sub-readers as 
        /// the inner reader. false => we use the given reader as the inner reader.</param>
        protected BoboIndexReader(IndexReader reader,
            IEnumerable<IFacetHandler> facetHandlers,
            IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories,
            WorkArea workArea,
            bool autoClose,
            bool useSubReaders)
            : base(useSubReaders ? new MultiReader(CreateSubReaders(reader, workArea)) : reader)
        {
            if (useSubReaders)
            {
                _dir = reader.Directory();
                BoboIndexReader[] subReaders = this.in_Renamed.GetSequentialSubReaders().Cast<BoboIndexReader>().ToArray();
                if (subReaders != null && subReaders.Length > 0)
                {
                    _subReaders = subReaders;

                    int maxDoc = 0;
                    _starts = new int[_subReaders.Length + 1];
                    for (int i = 0; i < _subReaders.Length; i++)
                    {
                        _subReaders[i]._dir = _dir;
                        if (facetHandlers != null) _subReaders[i].FacetHandlers = facetHandlers;
                        _starts[i] = maxDoc;
                        maxDoc += _subReaders[i].MaxDoc;
                    }
                    _starts[_subReaders.Length] = maxDoc;
                }
            }
            _runtimeFacetHandlerFactories = facetHandlerFactories;
            _runtimeFacetHandlerFactoryMap = new Dictionary<string, IRuntimeFacetHandlerFactory>();
            if (_runtimeFacetHandlerFactories != null)
            {
                foreach (var factory in _runtimeFacetHandlerFactories)
                {
                    _runtimeFacetHandlerFactoryMap.Put(factory.Name, factory);
                }
            }
            _facetHandlers = facetHandlers;
            _workArea = workArea;
            _autoClose = autoClose;
        }

        protected virtual void FacetInit()
        {
            FacetInit(new List<string>());
        }

        protected virtual void FacetInit(IEnumerable<string> toBeRemoved)
        {
            Initialize(ref _facetHandlers);
            if (_subReaders == null)
            {
                LoadFacetHandlers(_workArea, toBeRemoved);
            }
            else
            {
                foreach (var r in _subReaders)
                {
                    r.FacetInit(toBeRemoved);
                }

                foreach (var name in toBeRemoved)
                {
                    _facetHandlerMap.Remove(name);
                }
            }
        }

        protected virtual IEnumerable<IFacetHandler> FacetHandlers
        {
            set { _facetHandlers = value; }
        }

        [Obsolete("use MatchAllDocsQuery instead.")]
        public virtual Lucene.Net.Search.Query GetFastMatchAllDocsQuery()
        {
            return new MatchAllDocsQuery();
        }

        /// <summary>
        /// Utility method to dump out all fields (name and terms) for a given index.
        /// </summary>
        /// <param name="outStream">Stream to dump to.</param>
        public virtual void DumpFields(Stream outStream)
        {
            using (var writer = new StreamWriter(outStream))
            {
                var fieldNames = this.FacetNames;
                foreach (var fieldName in fieldNames)
                {
                    var te = Terms(new Term(fieldName, ""));
                    writer.WriteLine(fieldName + ":");
                    while (te.Next())
                    {
                        Term term = te.Term;
                        if (!fieldName.Equals(term.Field))
                        {
                            break;
                        }
                        writer.WriteLine(term.Text);
                    }
                    writer.WriteLine();
                    writer.WriteLine();
                }
            }
        }

        ///<summary>Gets all the facet field names</summary>
        ///<returns> Set of facet field names </returns>
        public virtual IEnumerable<string> FacetNames
        {
            get { return _facetHandlerMap.Keys; }
        }

        /// <summary>Gets a facet handler</summary>
        /// <param name="fieldName">name</param>
        /// <returns>facet handler</returns>
        public virtual IFacetHandler GetFacetHandler(string fieldName)
        {
            IFacetHandler f = _facetHandlerMap.Get(fieldName);
            if (f == null)
                f = (IFacetHandler)this.GetRuntimeFacetHandler(fieldName);
            return f;
        }

        public override IndexReader[] GetSequentialSubReaders()
        {
            return _subReaders;
        }

        ///<summary>Gets the facet handler map</summary>
        ///<returns>facet handler map </returns>
        public virtual IDictionary<string, IFacetHandler> FacetHandlerMap
        {
            get { return _facetHandlerMap; }
        }

        /// <summary>
        /// Gets the map of RuntimeFacetHandlerFactories
        /// </summary>
        public virtual IDictionary<string, IRuntimeFacetHandlerFactory> RuntimeFacetHandlerFactoryMap
        {
            get { return _runtimeFacetHandlerFactoryMap; }
        }

        /// <summary>
        /// Gets or sets the map of RuntimeFacetHandlers
        /// </summary>
        public virtual IDictionary<string, IRuntimeFacetHandler> RuntimeFacetHandlerMap
        {
            get { return _runtimeFacetHandlerMap.Get(); }
            set { _runtimeFacetHandlerMap.Set(value); }
        }

        /// <summary>
        /// Gets or sets the map of RuntimeFacetData
        /// </summary>
        public virtual IDictionary<string, object> RuntimeFacetDataMap
        {
            get { return _runtimeFacetDataMap.Get(); }
            set { _runtimeFacetDataMap.Set(value); }
        }

        public override Document Document(int docid)
        {
            if (_subReaders != null)
            {
                int readerIndex = ReaderIndex(docid, _starts, _subReaders.Length);
                BoboIndexReader subReader = _subReaders[readerIndex];
                return subReader.Document(docid - _starts[readerIndex]);
            }
            else
            {
                Document doc = base.Document(docid);
                var facetHandlers = _facetHandlerMap.Values;
                foreach (var facetHandler in facetHandlers)
                {
                    string[] vals = facetHandler.GetFieldValues(this, docid);
                    if (vals != null)
                    {
                        string[] values = doc.GetValues(facetHandler.Name);
                        var storedVals = new HashSet<string>(values);

                        foreach (string val in vals)
                        {
                            storedVals.Add(val);
                            
                        }
                        doc.RemoveField(facetHandler.Name);

                        foreach (var val in storedVals)
                        {
                            doc.Add(new Field(facetHandler.Name, 
                                val, 
                                Field.Store.NO, 
                                Field.Index.NOT_ANALYZED));
                        }
                    }
                }
                return doc;
            }
        }

        private static int ReaderIndex(int n, int[] starts, int numSubReaders)
        {
            int lo = 0;
            int hi = numSubReaders - 1;

            while (hi >= lo)
            {
                int mid = (int)(((uint)(lo + hi)) >> 1);
                int midValue = starts[mid];
                if (n < midValue)
                    hi = mid - 1;
                else if (n > midValue)
                    lo = mid + 1;
                else
                {
                    while (mid + 1 < numSubReaders && starts[mid + 1] == midValue)
                    {
                        mid++;
                    }
                    return mid;
                }
            }
            return hi;
        }

        /// <summary>
        /// Work area for loading
        /// </summary>
        public class WorkArea
        {
            internal Dictionary<Type, object> map = new Dictionary<Type, object>();

            public virtual T Get<T>()
            {
                T obj = (T)map.Get(typeof(T));
                return obj;
            }

            public virtual void Put(object obj)
            {
                map.Put(obj.GetType(), obj);
            }

            public virtual void Clear()
            {
                map.Clear();
            }
        }

        private BoboIndexReader(IndexReader in_Renamed)
            : base(in_Renamed)
        { }

        public virtual BoboIndexReader Copy(IndexReader index)
        {
            if (_subReaders != null)
            {
                throw new InvalidOperationException("this BoboIndexReader has subreaders");
            }
            BoboIndexReader copy = new BoboIndexReader(index);
            copy._facetHandlerMap = this._facetHandlerMap;
            copy._facetHandlers = this._facetHandlers;
            copy._runtimeFacetHandlerFactories = this._runtimeFacetHandlerFactories;
            copy._runtimeFacetHandlerFactoryMap = this._runtimeFacetHandlerFactoryMap;
            copy._workArea = this._workArea;
            copy._facetDataMap.PutAll(this._facetDataMap);
            copy._srcReader = index;
            copy._starts = this._starts;
            return copy;
        }
    }
}
