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

// Version compatibility level: 4.2.0
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Support.Logging;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Support;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// bobo browse index reader
    /// </summary>
    public class BoboSegmentReader : FilterAtomicReader
    {
        private static readonly ILog logger = LogProvider.For<BoboSegmentReader>();

        protected IDictionary<string, IFacetHandler> m_facetHandlerMap;

        protected IEnumerable<IFacetHandler> m_facetHandlers;
        protected IEnumerable<IRuntimeFacetHandlerFactory> m_runtimeFacetHandlerFactories;
        protected IDictionary<string, IRuntimeFacetHandlerFactory> m_runtimeFacetHandlerFactoryMap;

        protected WorkArea m_workArea;

        private readonly IDictionary<string, object> m_facetDataMap = new Dictionary<string, object>();
        private readonly DisposableThreadLocal<IDictionary<string, object>> m_runtimeFacetDataMap = new RuntimeFacetDataMapWrapper();
        private readonly DisposableThreadLocal<IDictionary<string, IRuntimeFacetHandler>> m_runtimeFacetHandlerMap = new RuntimeFacetHandlerMapWrapper();

        private class RuntimeFacetDataMapWrapper : DisposableThreadLocal<IDictionary<string, object>>
        {
            protected override IDictionary<string, object> InitialValue()
            {
                return new Dictionary<string, object>();
            }
        }

        private class RuntimeFacetHandlerMapWrapper : DisposableThreadLocal<IDictionary<string, IRuntimeFacetHandler>>
        {
            protected override IDictionary<string, IRuntimeFacetHandler> InitialValue()
            {
                return new Dictionary<string, IRuntimeFacetHandler>();
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Atomic reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <param name="facetHandlerFactories">List of factories to create facet handler instances at runtime.</param>
        /// <returns>A new BoboSegmentReader instance.</returns>
        public static BoboSegmentReader GetInstance(AtomicReader reader, IEnumerable<IFacetHandler> facetHandlers, IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories)
        {
            return BoboSegmentReader.GetInstance(reader, facetHandlers, facetHandlerFactories, new WorkArea());
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Atomic reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <param name="facetHandlerFactories">List of factories to create facet handler instances at runtime.</param>
        /// <param name="workArea">A service locator that can be used to inject custom objects.</param>
        /// <returns>A new BoboSegmentReader instance.</returns>
        public static BoboSegmentReader GetInstance(AtomicReader reader, IEnumerable<IFacetHandler> facetHandlers, IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories, WorkArea workArea)
        {
            BoboSegmentReader boboReader = new BoboSegmentReader(reader, facetHandlers, facetHandlerFactories, workArea);
            boboReader.FacetInit();
            return boboReader;
        }

        public virtual object GetFacetData(string name)
        {
            return m_facetDataMap.Get(name);
        }

        public virtual void PutFacetData(string name, object data)
        {
            m_facetDataMap.Put(name, data);
        }

        public virtual object GetRuntimeFacetData(string name)
        {
            var map = m_runtimeFacetDataMap.Get();
            if (map == null) return null;

            return map.Get(name);
        }

        public virtual void PutRuntimeFacetData(string name, object data)
        {
            var map = m_runtimeFacetDataMap.Get();
            if (map == null)
            {
                map = new Dictionary<string, object>();
                m_runtimeFacetDataMap.Set(map);
            }
            map.Put(name, data);
        }

        public virtual void ClearRuntimeFacetData()
        {
            m_runtimeFacetDataMap.Set(null);
        }

        public virtual IRuntimeFacetHandler GetRuntimeFacetHandler(string name)
        {
            var map = m_runtimeFacetHandlerMap.Get();
            if (map == null) return null;

            return map.Get(name);
        }

        public virtual void PutRuntimeFacetHandler(string name, IRuntimeFacetHandler data)
        {
            var map = m_runtimeFacetHandlerMap.Get();
            if (map == null)
            {
                map = new Dictionary<string, IRuntimeFacetHandler>();
                m_runtimeFacetHandlerMap.Set(map);
            }
            map.Put(name, data);
        }

        public virtual void ClearRuntimeFacetHandler()
        {
            m_runtimeFacetHandlerMap.Set(null);
        }

        protected override void DoClose()
        {
            // do nothing
        }

        private void LoadFacetHandler(string name, IList<string> loaded, IList<string> visited, WorkArea workArea)
        {
            IFacetHandler facetHandler = m_facetHandlerMap.Get(name);
            if (facetHandler != null && !loaded.Contains(name))
            {
                visited.Add(name);
                var dependsOn = facetHandler.DependsOn;
                if (dependsOn.Count > 0)
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
                        facetHandler.PutDependedFacetHandler(m_facetHandlerMap[f]);
                    }
                }

                long start = System.Environment.TickCount;
                facetHandler.LoadFacetData(this, workArea);
                long end = System.Environment.TickCount;
                if (logger.IsDebugEnabled())
                {
                    StringBuilder buf = new StringBuilder();
                    buf.Append("facetHandler loaded: ").Append(name).Append(", took: ").Append(end - start).Append(" ms");
                    logger.Debug(buf.ToString());
                }
                loaded.Add(name);
            }
        }

        private void LoadFacetHandlers(WorkArea workArea)
        {
            var loaded = new List<string>();
            var visited = new List<string>();

            foreach (string name in m_facetHandlerMap.Keys)
            {
                this.LoadFacetHandler(name, loaded, visited, workArea);
            }
        }

        private void Initialize(ref IEnumerable<IFacetHandler> facetHandlers)
        {
            m_facetHandlers = facetHandlers;
            m_facetHandlerMap = new Dictionary<string, IFacetHandler>();
            foreach (var facetHandler in facetHandlers)
            {
                m_facetHandlerMap.Put(facetHandler.Name, facetHandler);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Atomic reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <param name="facetHandlerFactories">List of factories to create facet handler instances at runtime.</param>
        /// <param name="workArea">A service locator that can be used to inject custom objects.</param>
        protected internal BoboSegmentReader(AtomicReader reader, IEnumerable<IFacetHandler> facetHandlers, IEnumerable<IRuntimeFacetHandlerFactory> facetHandlerFactories, WorkArea workArea)
            : base(reader)
        {
            m_runtimeFacetHandlerFactories = facetHandlerFactories;
            m_runtimeFacetHandlerFactoryMap = new Dictionary<string, IRuntimeFacetHandlerFactory>();
            if (m_runtimeFacetHandlerFactories != null)
            {
                foreach (IRuntimeFacetHandlerFactory factory in m_runtimeFacetHandlerFactories)
                {
                    m_runtimeFacetHandlerFactoryMap.Put(factory.Name, factory);
                }
            }
            m_facetHandlers = facetHandlers;
            m_workArea = workArea;
        }

        protected internal virtual void FacetInit()
        {
            Initialize(ref m_facetHandlers);
            LoadFacetHandlers(m_workArea);
        }

        ///<summary>Gets all the facet field names</summary>
        ///<returns> Set of facet field names </returns>
        public virtual IEnumerable<string> FacetNames
        {
            get { return m_facetHandlerMap.Keys; }
        }

        /// <summary>Gets a facet handler</summary>
        /// <param name="fieldName">name</param>
        /// <returns>facet handler</returns>
        public virtual IFacetHandler GetFacetHandler(string fieldName)
        {
            IFacetHandler f = m_facetHandlerMap.Get(fieldName);
            if (f == null)
                f = (IFacetHandler)this.GetRuntimeFacetHandler(fieldName);
            return f;
        }

        ///<summary>Gets the facet handler map</summary>
        ///<returns>facet handler map </returns>
        public virtual IDictionary<string, IFacetHandler> FacetHandlerMap
        {
            get { return m_facetHandlerMap; }
        }

        /// <summary>
        /// Gets the map of RuntimeFacetHandlerFactories
        /// </summary>
        public virtual IDictionary<string, IRuntimeFacetHandlerFactory> RuntimeFacetHandlerFactoryMap
        {
            get { return m_runtimeFacetHandlerFactoryMap; }
        }

        /// <summary>
        /// Gets or sets the map of RuntimeFacetHandlers
        /// </summary>
        public virtual IDictionary<string, IRuntimeFacetHandler> RuntimeFacetHandlerMap
        {
            get { return m_runtimeFacetHandlerMap.Get(); }
            set { m_runtimeFacetHandlerMap.Set(value); }
        }

        /// <summary>
        /// Gets or sets the map of RuntimeFacetData
        /// </summary>
        public virtual IDictionary<string, object> RuntimeFacetDataMap
        {
            get { return m_runtimeFacetDataMap.Get(); }
            set { m_runtimeFacetDataMap.Set(value); }
        }

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            base.Document(docID, visitor);
            if (!(visitor is DocumentStoredFieldVisitor))
            {
                return;
            }

            Document doc = ((DocumentStoredFieldVisitor)visitor).Document;

            IEnumerable<IFacetHandler> facetHandlers = m_facetHandlerMap.Values;
            foreach (IFacetHandler facetHandler in facetHandlers)
            {
                string[] vals = facetHandler.GetFieldValues(this, docID);
                if (vals != null)
                {
                    string[] values = doc.GetValues(facetHandler.Name);
                    var storedVals = new HashSet<string>(values);

                    foreach (string val in vals)
                    {
                        storedVals.Add(val);
                    }
                    doc.RemoveField(facetHandler.Name);

                    foreach (string val in storedVals)
                    {
                        doc.Add(new StringField(facetHandler.Name, val, Field.Store.NO));
                    }
                }
            }
        }

        public virtual string[] GetStoredFieldValue(int docid, string fieldname)
        {
            DocumentStoredFieldVisitor visitor = new DocumentStoredFieldVisitor(fieldname);
            base.Document(docid, visitor);
            Document doc = visitor.Document;
            return doc.GetValues(fieldname);
        }

        /// <summary>
        /// Work area for loading
        /// </summary>
        public class WorkArea
        {
            internal Dictionary<Type, object> m_map = new Dictionary<Type, object>();

            public virtual T Get<T>()
            {
                T obj = (T)m_map.Get(typeof(T));
                return obj;
            }

            public virtual void Put(object obj)
            {
                m_map.Put(obj.GetType(), obj);
            }

            public virtual void Clear()
            {
                m_map.Clear();
            }
        }

        private BoboSegmentReader(AtomicReader input)
            : base(input)
        { }

        public virtual BoboSegmentReader Copy(AtomicReader index)
        {
            BoboSegmentReader copy = new BoboSegmentReader(index);
            copy.m_facetHandlerMap = this.m_facetHandlerMap;
            copy.m_facetHandlers = this.m_facetHandlers;
            copy.m_runtimeFacetHandlerFactories = this.m_runtimeFacetHandlerFactories;
            copy.m_runtimeFacetHandlerFactoryMap = this.m_runtimeFacetHandlerFactoryMap;
            copy.m_workArea = this.m_workArea;
            copy.m_facetDataMap.PutAll(this.m_facetDataMap);
            return copy;
        }

        public virtual AtomicReader InnerReader
        {
            get { return m_input; }
        }
    }
}
