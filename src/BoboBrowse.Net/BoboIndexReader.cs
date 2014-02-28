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

namespace BoboBrowse.Net
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Common.Logging;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Search;  

    /// <summary>
    /// bobo browse index reader
    /// </summary>
    public class BoboIndexReader : FilterIndexReader
    {        
        private const string FIELD_CONFIG = "field.xml";
        private const string SPRING_CONFIG = "bobo.spring";

        private static readonly ILog logger = LogManager.GetLogger<BoboIndexReader>();

        private readonly ICollection<FacetHandler> facetHandlers;
        private readonly WorkArea workArea;
        private C5.TreeSet<int> deletedDocs;
        private volatile int[] deletedDocsArray;
        private Dictionary<string, FacetHandler> facetHandlerMap;
        //private Dictionary<SortFieldEntry, FieldComparator> defaultSortFieldCache;
        protected IndexReader _IN_Reader;
        private bool _deleted;        

        #region ctors
        protected BoboIndexReader(IndexReader reader, ICollection<FacetHandler> facetHandlers, WorkArea workArea)
            : base(reader)
        {
            this.facetHandlers = facetHandlers ?? new List<FacetHandler>();
            this.workArea = workArea;
            this._IN_Reader = reader;
        }
        #endregion

        public static BoboIndexReader GetInstance(IndexReader reader, ICollection<FacetHandler> facetHandlers)
        {
            return BoboIndexReader.GetInstance(reader, facetHandlers, new WorkArea());
        }

        public static BoboIndexReader GetInstance(IndexReader reader,ICollection<FacetHandler> facetHandlers, WorkArea workArea)
        {
            BoboIndexReader boboReader = new BoboIndexReader(reader, facetHandlers, workArea);
            boboReader.FacetInit();
            return boboReader;
        }

        protected internal virtual void FacetInit()
        {
            Initialize(facetHandlers, workArea);
        }

        private void Initialize(ICollection<FacetHandler> facetHandlers, WorkArea workArea)
        {
            this.facetHandlerMap = new Dictionary<string, FacetHandler>();
            foreach (FacetHandler facetHandler in facetHandlers)
            {
                facetHandlerMap.Add(facetHandler.Name, facetHandler);
            }
            LoadFacetHandlers(workArea);

            //defaultSortFieldCache = new Dictionary<SortFieldEntry, FieldComparator>();
        }

        private void LoadFacetHandlers(WorkArea workArea)
        {
            Dictionary<string, FacetHandler>.KeyCollection facethandlers = facetHandlerMap.Keys;
            Dictionary<string, FacetHandler>.KeyCollection.Enumerator iter = facethandlers.GetEnumerator();
            List<string> loaded = new List<string>();
            List<string> visited = new List<string>();
            List<string> tobeRemoved = new List<string>();

            while (iter.MoveNext())
            {
                string name = iter.Current;
                try
                {
                    LoadFacetHandler(name, loaded, visited, workArea);
                }
                catch (Exception ioe)
                {
                    tobeRemoved.Add(name);
                    logger.Error("facet load failed: " + name + ": " + ioe.Message, ioe);
                }
            }

            IEnumerator<string> iter2 = tobeRemoved.GetEnumerator();
            while (iter2.MoveNext())
            {
                facetHandlerMap.Remove(iter2.Current);
            }
        }

        private void LoadFacetHandler(string name, List<string> loaded, List<string> visited, WorkArea workArea)
        {
            FacetHandler facetHandler = facetHandlerMap[name];
            if (facetHandler != null && !loaded.Contains(name))
            {
                visited.Add(name);
                IList<string> dependsOn = facetHandler.DependsOn;
                if (dependsOn.Count > 0)
                {                   
                    foreach(var f in dependsOn)
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
                        facetHandler.PutDependedFacetHandler(facetHandlerMap[f]);
                    }
                }

                long start = System.Environment.TickCount;
                facetHandler.Load(this, workArea);
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

        public virtual Query GetFastMatchAllDocsQuery()
        {
            this.InitDeletedDocumentsIndex();
            int[] deldocs = deletedDocsArray;
            if (deldocs == null)
            {
                lock (deletedDocs)
                {
                    deldocs = deletedDocs.ToArray();
                    deletedDocsArray = deldocs;
                }
            }
            return new FastMatchAllDocsQuery(deldocs, this.MaxDoc);
        }       

        ///<summary>Gets all the facet field names</summary>
        ///<returns> Set of facet field names </returns>
        public virtual IEnumerable<string> GetFacetNames()
        {
            return facetHandlerMap.Keys;
        }

        ///<summary>Gets a facet handler</summary>
        ///<param name="fieldname">name </param>
        ///<returns>facet handler </returns>
        public virtual FacetHandler GetFacetHandler(string fieldname)
        {
            FacetHandler result;
            return
                facetHandlerMap.TryGetValue(fieldname, out result)
                ? result
                : null;
        }

        ///<summary>Gets the facet handler map</summary>
        ///<returns>facet handler map </returns>
        public virtual Dictionary<string, FacetHandler> GetFacetHandlerMap()
        {
            return facetHandlerMap;
        }

        public override Document Document(int docid)
        {
            Document doc = base.Document(docid);
            System.Collections.Generic.ICollection<FacetHandler> facetHandlers = facetHandlerMap.Values;
            foreach (FacetHandler facetHandler in facetHandlers)
            {
                string[] vals = facetHandler.GetFieldValues(docid);
                if (vals != null)
                {
                    foreach (string val in vals)
                    {
                        if (null != val)
                        {
                            doc.Add(new Field(facetHandler.Name, val, Field.Store.NO, Field.Index.NOT_ANALYZED));
                        }
                    }
                }
            }
            return doc;
        }

        public override void DeleteDocument(int docid)
        {
            this.InitDeletedDocumentsIndex();
            base.DeleteDocument(docid);
            lock (deletedDocs)
            {
                deletedDocs.Add(docid);
                // remove the array but do not recreate the array at this point
                // there may be more deleteDocument calls
                deletedDocsArray = null;
            }
        }

        public virtual FieldComparator GetDefaultScoreDocComparator(int numDocs,SortField f)
        {            
            int type = f.Type;
            if (type == SortField.DOC)
            {
                return f.GetComparator(numDocs, 0);
            }
            if (type == SortField.SCORE)
            {
                return f.GetComparator(numDocs, 0);
            }

            FieldComparatorSource factory = f.ComparatorSource;
            SortFieldEntry entry = factory == null ? new SortFieldEntry(f.Field, type, f.Locale) : new SortFieldEntry(f.Field, factory);

           // FieldComparator comparator;
            //if (!defaultSortFieldCache.TryGetValue(entry, out comparator))
            //{
            //    lock (defaultSortFieldCache)
            //    {
            //        if (!defaultSortFieldCache.TryGetValue(entry, out comparator))
            //        {
            //            comparator = LuceneSortDocComparatorFactory.BuildScoreDocComparator(this, numDocs, entry);
            //            if (comparator != null)
            //            {
            //                defaultSortFieldCache.Add(entry, comparator);
            //            }
            //        }
            //    }
            //    return comparator;
            //}
            //else
            //{
            //    return comparator;
            //}
            return LuceneSortDocComparatorFactory.BuildScoreDocComparator(this, numDocs, entry);
        }

        private void InitDeletedDocumentsIndex()
        {
            if (!this._deleted)
            {
                this.deletedDocs = new C5.TreeSet<int>();
                long start = System.Environment.TickCount;
                if (this._IN_Reader.HasDeletions)
                {
                    for (int i = 0; i < this.MaxDoc; i++)
                    {
                        if (this._IN_Reader.IsDeleted(i))
                        {
                            deletedDocs.Add(i);
                        }
                    }
                }
                long end = System.Environment.TickCount;
                if (logger.IsDebugEnabled)
                {
                    StringBuilder buf = new StringBuilder();
                    buf.Append("BoboIndexReader loaded deleted documents: ").Append(deletedDocs.Count).Append(", took: ").Append(end - start).Append(" ms");
                    logger.Debug(buf.ToString());
                }
            }
            this._deleted = true;
        }

        public class WorkArea
        {
            private Dictionary<Type, object> map = new Dictionary<Type, object>();

            public virtual T Get<T>()
            {
                object @value;
                return
                    map.TryGetValue(typeof(T), out @value)
                        ? (T)@value
                        : default(T);
            }

            public virtual void Put(object obj)
            {
                map.Add(obj.GetType(), obj);
            }

            public virtual void Clear()
            {
                map.Clear();
            }
        }
    }
}
