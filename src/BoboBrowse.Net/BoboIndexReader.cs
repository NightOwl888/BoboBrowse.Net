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

// Version compatibility level: 3.1.0
// EXCEPTION: Spring XML Configuration
// TODO: This class is not compatible with .NET 3.5 as is
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Query;
    using BoboBrowse.Net.Search;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Common.Logging;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// bobo browse index reader
    /// </summary>
    public class BoboIndexReader : FilterIndexReader
    {        
        private const string SPRING_CONFIG = "bobo.spring";
        private static readonly ILog logger = LogManager.GetLogger<BoboIndexReader>();

        protected virtual Dictionary<string, IFacetHandler> _facetHandlerMap;

        protected virtual IEnumerable<IFacetHandler> _facetHandlers;
        protected virtual IEnumerable<IRuntimeFacetHandlerFactory> _runtimeFacetHandlerFactories;
        protected virtual IDictionary<string, IRuntimeFacetHandlerFactory> _runtimeFacetHandlerFactoryMap;

        protected virtual WorkArea _workArea;

        protected virtual IndexReader _srcReader;
        internal virtual BoboIndexReader[] _subReaders = null;
        protected int[] _starts = null;
        private Directory _dir = null;

        private readonly IDictionary<string, object> _facetDataMap = new Dictionary<string, object>();

        // TODO: This cannot be used in .NET 3.5
        private readonly ThreadLocal<IDictionary<string, object>> _runtimeFacetDataMap = new ThreadLocal<IDictionary<string, object>>(() =>
            {
                return new Dictionary<string, object>();
            });

        // TODO: This cannot be used in .NET 3.5
        private readonly ThreadLocal<IDictionary<string, RuntimeFacetHandler>> _runtimeFacetHandlerMap = new ThreadLocal<IDictionary<string, RuntimeFacetHandler>>(() =>
            {
                return new Dictionary<string, RuntimeFacetHandler>();
            });

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Index reader</param>
        /// <returns></returns>
        public static BoboIndexReader GetInstance(IndexReader reader)
        {
            return BoboIndexReader.GetInstance(reader, null, null, new WorkArea());
        }

        public static BoboIndexReader GetInstance(IndexReader reader, WorkArea workArea)
        {
            return BoboIndexReader.GetInstance(reader, null, null, workArea);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="reader">index reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <param name="facetHandlerFactories"></param>
        /// <returns></returns>
        public static BoboIndexReader GetInstance(IndexReader reader, IEnumerable<IFacetHandler> facetHandlers, IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories)
        {
            return BoboIndexReader.GetInstance(reader, facetHandlers, facetHandlerFactories, new WorkArea());
        }

        public static BoboIndexReader GetInstance(IndexReader reader, IEnumerable<IFacetHandler> facetHandlers)
        {
            return BoboIndexReader.GetInstance(reader, facetHandlers, new IRuntimeFacetHandlerFactory[0], new WorkArea());
        }

        public static BoboIndexReader GetInstance(IndexReader reader, IEnumerable<IFacetHandler> facetHandlers, IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories, WorkArea workArea)
        {
            BoboIndexReader boboReader = new BoboIndexReader(reader, facetHandlers, facetHandlerFactories, workArea);
            boboReader.FacetInit();
            return boboReader;
        }


        public static BoboIndexReader GetInstanceAsSubReader(IndexReader reader)
        {
            return GetInstanceAsSubReader(reader, null, null, new WorkArea());
        }

        public static BoboIndexReader GetInstanceAsSubReader(IndexReader reader,
                                                       IEnumerable<IFacetHandler> facetHandlers,
                                                       IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories)
        {
            return GetInstanceAsSubReader(reader, facetHandlers, facetHandlerFactories, new WorkArea());
        }

        public static BoboIndexReader GetInstanceAsSubReader(IndexReader reader,
                                                       IEnumerable<IFacetHandler> facetHandlers,
                                                       IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories,
                                                       WorkArea workArea)
        {
            BoboIndexReader boboReader = new BoboIndexReader(reader, facetHandlers, facetHandlerFactories, workArea, false);
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
                catch (Exception e)
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
            IndexReader newInner = null;

            SegmentInfos sinfos = new SegmentInfos();
            sinfos.Read(_dir);
            int size = sinfos.Count;

            if (this.in_Renamed is MultiReader)
            {
                // setup current reader list
                List<IndexReader> boboReaderList = new List<IndexReader>();
                ReaderUtil.GatherSubReaders(boboReaderList, this.in_Renamed);
                var readerMap = new Dictionary<string, BoboIndexReader>();

                foreach (IndexReader reader in boboReaderList)
                {
                    BoboIndexReader boboReader = (BoboIndexReader)reader;
                    SegmentReader sreader = (SegmentReader)(boboReader.in_Renamed);
                    readerMap.Put(sreader.SegmentName, boboReader);
                }

                var currentReaders = new List<BoboIndexReader>(size);
                bool isNewReader = false;
                for (int i = 0; i < size; ++i)
                {
                    SegmentInfo sinfo = (SegmentInfo)sinfos.Info(i);

                    // NOTE: Replaced the remove() call with the 2 lines below.
                    // It didn't look like the java HashMap was thread safe anyway.
                    BoboIndexReader breader = readerMap.Get(sinfo.name); 
                    readerMap.Remove(sinfo.name);
                    if (breader != null)
                    {
                        // should use SegmentReader.reopen
                        // TODO: see LUCENE-2559
                        BoboIndexReader newReader = (BoboIndexReader)breader.Reopen(true);
                        if (newReader != breader)
                        {
                            isNewReader = true;
                        }
                        if (newReader != null)
                        {
                            currentReaders.Add(newReader);
                        }
                    }
                    else
                    {
                        isNewReader = true;
                        SegmentReader newSreader = SegmentReader.Get(true, sinfo, 1);
                        breader = BoboIndexReader.GetInstanceAsSubReader(newSreader, this._facetHandlers, this._runtimeFacetHandlerFactories);
                        breader._dir = _dir;
                        currentReaders.Add(breader);
                    }
                }
                isNewReader = isNewReader || (readerMap.Count != 0);
                if (!isNewReader)
                {
                    return this;
                }
                else
                {
                    MultiReader newMreader = new MultiReader(currentReaders.ToArray(), false);
                    BoboIndexReader newReader = BoboIndexReader.GetInstanceAsSubReader(newMreader, this._facetHandlers, this._runtimeFacetHandlerFactories);
                    newReader._dir = _dir;
                    return newReader;
                }
            }
            else if (this.in_Renamed is SegmentReader)
            {
                // should use SegmentReader.reopen
                // TODO: see LUCENE-2559

                SegmentReader sreader = (SegmentReader)this.in_Renamed;
                int numDels = sreader.NumDeletedDocs;

                SegmentInfo sinfo = null;
                bool sameSeg = false;
                //get SegmentInfo instance
                for (int i = 0; i < size; ++i)
                {
                    SegmentInfo sinfoTmp = (SegmentInfo)sinfos.Info(i);
                    if (sinfoTmp.name.Equals(sreader.SegmentName))
                    {
                        int numDels2 = sinfoTmp.GetDelCount();
                        sameSeg = numDels == numDels2;
                        sinfo = sinfoTmp;
                        break;
                    }
                }

                if (sinfo == null)
                {
                    // segment no longer exists
                    return null;
                }
                if (sameSeg)
                {
                    return this;
                }
                else
                {
                    SegmentReader newSreader = SegmentReader.Get(true, sinfo, 1);
                    return BoboIndexReader.GetInstanceAsSubReader(newSreader, this._facetHandlers, this._runtimeFacetHandlerFactories);
                }
            }
            else
            {
                // should not reach here, a catch-all default case
	            IndexReader reader = this.in_Renamed.Reopen(true);
                if (this.in_Renamed != reader)
                {
	                return BoboIndexReader.GetInstance(newInner, _facetHandlers, _runtimeFacetHandlerFactories, _workArea);
	            }
	            else
                {
		            return this;
	            }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual IndexReader Reopen(bool openReadOnly)
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
            var map = _runtimeFacetDataMap.Value;
            if (map == null) return null;

            return map.Get(name);
        }

        public virtual void PutRuntimeFacetData(string name, object data)
        {
            var map = _runtimeFacetDataMap.Value;
            if (map == null)
            {
                map = new Dictionary<string, object>();
                _runtimeFacetDataMap.Value = map;
            }
            map.Put(name, data);
        }

        public virtual void ClearRuntimeFacetData()
        {
            _runtimeFacetDataMap.Value = null;
        }

        public RuntimeFacetHandler GetRuntimeFacetHandler(string name)
        {
            var map = _runtimeFacetHandlerMap.Value;
            if (map == null) return null;

            return map.Get(name);
        }

        public void PutRuntimeFacetHandler(string name, RuntimeFacetHandler data)
        {
            var map = _runtimeFacetHandlerMap.Value;
            if (map == null)
            {
                map = new Dictionary<string, RuntimeFacetHandler>();
                _runtimeFacetHandlerMap.Value = map;
            }
            map.Put(name, data);
        }

        public void ClearRuntimeFacetHandler()
        {
            _runtimeFacetHandlerMap.Value = null;
        }

        protected override void DoClose()
        {
            _facetDataMap.Clear();
            if (_srcReader != null) _srcReader.Close();
            base.DoClose();
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
                                throw new System.IO.IOException("Facet handler dependency cycle detected, facet handler: " + name + " not loaded");
                            }
                            LoadFacetHandler(f, loaded, visited, workArea);
                        }
                        if (!loaded.Contains(f))
                        {
                            throw new System.IO.IOException("unable to load facet handler: " + f);
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
                    boboReaders[i] = new BoboIndexReader(subReaders[i], null, null, workArea, false);
                }
            }
            else
            {
                boboReaders = new BoboIndexReader[] { new BoboIndexReader(reader, null, null, workArea, false) };
            }
            return boboReaders;
        }

        public override Lucene.Net.Store.Directory Directory()
        {
            return (_subReaders != null) ? _subReaders[0].Directory() : base.Directory();
        }

        private static IEnumerable<IFacetHandler> LoadFromIndex(System.IO.DirectoryInfo file, WorkArea workArea)
        {
            // File springFile = new File(file, SPRING_CONFIG);
            // FileSystemXmlApplicationContext appCtx =
            //   new FileSystemXmlApplicationContext("file:" + springFile.getAbsolutePath());
            //return (Collection<FacetHandler<?>>) appCtx.getBean("handlers");

            // TODO: Use Spring.Net.Core to configure this. It would be best to use DI to inject
            // a loader for the configuration so there doesn't have to be a dependency on Spring.Net,
            // but instead make it an optional NuGet package.
            //var entries = workArea.map.Values;
            //FileSystemXmlApplicationContext appCtx = new FileSystemXmlApplicationContext();
            //foreach (var entry in entries)
            //{
            //    object obj = entry;
            //    if (obj is ClassLoader)
            //    {
            //        appCtx.SetClassLoader((ClassLoader)obj);
            //        break;
            //    }
            //}

            //string absolutePath = file.GetAbsolutePath();
            //string partOne = absolutePath.substring(0, absolutePath.lastIndexOf(File.separator));
            //string partTwo = URLEncoder.encode(absolutePath.substring(absolutePath.lastIndexOf(File.separator) + 1), "UTF-8");
            //absolutePath = partOne + File.separator + partTwo;

            //File springFile = new File(new File(absolutePath), SPRING_CONFIG);
            //appCtx.setConfigLocation("file:" + springFile.getAbsolutePath());
            //appCtx.refresh();
      
            //return (Collection<FacetHandler<?>>) appCtx.getBean("handlers");

            throw new NotImplementedException("file configuration support has not yet been added");
        }

        private void Initialize(ref IEnumerable<IFacetHandler> facetHandlers)
        {
            if (facetHandlers == null)
            {
                var idxDir = Directory();
                if (idxDir != null && idxDir is FSDirectory)
                {
                    var fsDir = (FSDirectory)idxDir;
                    var file = fsDir.Directory;
                    var springFile = System.IO.Path.Combine(file.FullName, SPRING_CONFIG);
                    if (System.IO.File.Exists(springFile))
                    {
                        facetHandlers = LoadFromIndex(file, _workArea);
                    }
                    else
                    {
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
        /// <param name="reader"></param>
        /// <param name="facetHandlers"></param>
        /// <param name="workArea"></param>
        protected BoboIndexReader(IndexReader reader, 
            IEnumerable<IFacetHandler> facetHandlers, 
            IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories, 
            WorkArea workArea)
            : this(reader, facetHandlers, facetHandlerFactories, workArea, true)
        {
            _srcReader = reader;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="facetHandlers"></param>
        /// <param name="facetHandlerFactories"></param>
        /// <param name="workArea"></param>
        /// <param name="useSubReaders">true => we create a MultiReader of all the leaf sub-readers as 
        /// the inner reader. false => we use the given reader as the inner reader.</param>
        protected BoboIndexReader(IndexReader reader,
            IEnumerable<IFacetHandler> facetHandlers,
            IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories,
            WorkArea workArea,
            bool useSubReaders)
            : base(useSubReaders ? new MultiReader(CreateSubReaders(reader, workArea)) : reader)
        {
            if (useSubReaders)
            {
                _dir = reader.Directory();
                BoboIndexReader[] subReaders = (BoboIndexReader[])this.in_Renamed.GetSequentialSubReaders();
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
        public virtual void DumpFields(System.IO.Stream outStream)
        {
            using (var writer = new System.IO.StreamWriter(outStream))
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

        ///<summary>Gets a facet handler</summary>
        ///<param name="fieldname">name </param>
        ///<returns>facet handler </returns>
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
                int mid = (int)((uint)(lo + hi) >> 1);
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
