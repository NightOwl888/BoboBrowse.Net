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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Facets
{
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;
    using Lucene.Net.Support;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public enum TermCountSize
    {
        Small,
        Medium,
        Large
    }

    public interface IFacetHandler
    {
        RandomAccessFilter BuildFilter(BrowseSelection sel);
        RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, IDictionary<string, string> prop);
        RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> selectionProperty);
        RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, IDictionary<string, string> prop, bool isNot);
        IEnumerable<string> DependsOn { get; }
        IFacetHandler GetDependedFacetHandler(string name);
        DocComparatorSource GetDocComparatorSource();
        FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec);
        FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec ospec, bool groupMode);
        T GetFacetData<T>(BoboSegmentReader reader);
        string GetFieldValue(BoboSegmentReader reader, int id);
        string[] GetFieldValues(BoboSegmentReader reader, int id);
        int GetNumItems(BoboSegmentReader reader, int id);
        object[] GetRawFieldValues(BoboSegmentReader reader, int id);
        void LoadFacetData(BoboSegmentReader reader);
        void LoadFacetData(BoboSegmentReader reader, BoboSegmentReader.WorkArea workArea);
        IFacetAccessible Merge(FacetSpec fspec, IEnumerable<IFacetAccessible> facetList);
        string Name { get; }
        void PutDependedFacetHandler(IFacetHandler facetHandler);
        void SetTermCountSize(string termCountSize);
        TermCountSize TermCountSize { get; set; }
    }

    [Serializable]
    public class FacetDataNone
    {
        //private static long serialVersionUID = 1L; // NOT USED
        private readonly static FacetDataNone instance = new FacetDataNone();
        private FacetDataNone() { }

        /// <summary>
        /// Added in .NET version as an accessor to the instance static field.
        /// </summary>
        /// <returns></returns>
        public static FacetDataNone Instance
        {
            get { return instance; }
        }
    }

    public abstract class FacetHandler<D> : IFacetHandler
    {
        protected readonly string _name;
        private readonly IEnumerable<string> _dependsOn;
        // original was <string, FacetHandler<?>>
        private readonly IDictionary<string, IFacetHandler> _dependedFacetHandlers;
        private TermCountSize _termCountSize;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dependsOn">Set of names of facet handlers this facet handler depend on for loading</param>
        protected FacetHandler(string name, IEnumerable<string> dependsOn)
        {
            _name = name;
            _dependsOn = dependsOn == null ? new List<string>() : new List<string>(dependsOn);
            _dependedFacetHandlers = new Dictionary<string, IFacetHandler>();
            _termCountSize = TermCountSize.Large;
        }

        public virtual void SetTermCountSize(string termCountSize)
        {
            _termCountSize = (TermCountSize)Enum.Parse(typeof(TermCountSize), termCountSize, true);
        }

        public virtual TermCountSize TermCountSize
        {
            get { return _termCountSize; }
            set { _termCountSize = value; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        public FacetHandler(string name) 
            : this(name, null) 
        { }

        /// <summary>
        /// Gets name of the current facet handler
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets names of the facet handler this depends on
        /// </summary>
        public IEnumerable<string> DependsOn
        {
            get { return _dependsOn; }
        }

        /// <summary>
        /// Adds a list of depended facet handlers
        /// </summary>
        /// <param name="facetHandler">Handler depended facet handler</param>
        public void PutDependedFacetHandler(IFacetHandler facetHandler)
        {
            _dependedFacetHandlers.Put(facetHandler.Name, facetHandler);
        }

        /// <summary>
        /// Gets a depended facet handler
        /// </summary>
        /// <param name="name">facet handler name</param>
        /// <returns>facet handler instance</returns>
        public IFacetHandler GetDependedFacetHandler(string name)
        {
            return _dependedFacetHandlers.Get(name);
        }

        /// <summary>
        /// Load information from an index reader, initialized by <see cref="T:BoboBrowse.Net.BoboIndexReader"/>.
        /// </summary>
        /// <param name="reader"></param>
        public abstract D Load(BoboSegmentReader reader);

        public virtual IFacetAccessible Merge(FacetSpec fspec, IEnumerable<IFacetAccessible> facetList)
        {
            return new CombinedFacetAccessible(fspec, facetList);
        }

        public virtual T GetFacetData<T>(BoboSegmentReader reader)
        {
            return (T)reader.GetFacetData(_name);
        }

        public virtual D Load(BoboSegmentReader reader, BoboSegmentReader.WorkArea workArea)
        {
            return Load(reader);
        }

        public virtual void LoadFacetData(BoboSegmentReader reader, BoboSegmentReader.WorkArea workArea)
        {
            reader.PutFacetData(_name, Load(reader, workArea));
        }

        public virtual void LoadFacetData(BoboSegmentReader reader)
        {
            reader.PutFacetData(_name, Load(reader));
        }

        public virtual RandomAccessFilter BuildFilter(BrowseSelection sel)
        {
            string[] selections = sel.Values;
            string[] notSelections = sel.NotValues;
            IDictionary<string, string> prop = sel.SelectionProperties;

            RandomAccessFilter filter = null;
            if (selections != null && selections.Length > 0)
            {
                if (sel.SelectionOperation == BrowseSelection.ValueOperation.ValueOperationAnd)
                {
                    filter = BuildRandomAccessAndFilter(selections, prop);
                    if (filter == null)
                    {
                        filter = EmptyFilter.Instance;
                    }
                }
                else
                {
                    filter = BuildRandomAccessOrFilter(selections, prop, false);
                    if (filter == null)
                    {
                        return EmptyFilter.Instance;
                    }
                }
            }

            if (notSelections != null && notSelections.Length > 0)
            {
                RandomAccessFilter notFilter = BuildRandomAccessOrFilter(notSelections, prop, true);
                if (filter == null)
                {
                    filter = notFilter;
                }
                else
                {
                    RandomAccessFilter andFilter =
                        new RandomAccessAndFilter(new RandomAccessFilter[] { filter, notFilter }.ToList());
                    filter = andFilter;
                }
            }

            return filter;
        }

        public abstract RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> selectionProperty);

        public virtual RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, IDictionary<string, string> prop)
        {
            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>(vals.Length);

            foreach (string val in vals)
            {
                RandomAccessFilter f = BuildRandomAccessFilter(val, prop);
                if (f != null)
                {
                    filterList.Add(f);
                }
                else
                {
                    return EmptyFilter.Instance;
                }
            }

            if (filterList.Count == 1)
                return filterList.First();
            return new RandomAccessAndFilter(filterList);
        }

        public virtual RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, IDictionary<string, string> prop, bool isNot)
        {
            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>(vals.Length);

            foreach (string val in vals)
            {
                RandomAccessFilter f = BuildRandomAccessFilter(val, prop);
                if (f != null && !(f is EmptyFilter))
                {
                    filterList.Add(f);
                }
            }

            RandomAccessFilter finalFilter;
            if (filterList.Count == 0)
            {
                finalFilter = EmptyFilter.Instance;
            }
            else
            {
                finalFilter = new RandomAccessOrFilter(filterList);
            }

            if (isNot)
            {
                finalFilter = new RandomAccessNotFilter(finalFilter);
            }
            return finalFilter;
        }

        /// <summary>
        /// Gets a FacetCountCollector
        /// </summary>
        /// <param name="sel">selection</param>
        /// <param name="fspec">facetSpec</param>
        /// <returns>a FacetCountCollector</returns>
        public abstract FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec);

        /// <summary>
        /// Override this method if your facet handler have a better group mode like the SimpleFacetHandler.
        /// </summary>
        /// <param name="sel">selection</param>
        /// <param name="ospec">facetSpec</param>
        /// <param name="groupMode">groupMode</param>
        /// <returns></returns>
        public virtual FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec ospec, bool groupMode)
        {
            return GetFacetCountCollectorSource(sel, ospec);
        }

        /// <summary>
        /// Gets the field value
        /// </summary>
        /// <param name="reader">index reader</param>
        /// <param name="id">doc</param>
        /// <returns>array of field values</returns>
        public abstract string[] GetFieldValues(BoboSegmentReader reader, int id);

        public virtual int GetNumItems(BoboSegmentReader reader, int id)
        {
            throw new NotImplementedException("GetNumItems is not supported for this facet handler: " + this.GetType().FullName);
        }

        public virtual object[] GetRawFieldValues(BoboSegmentReader reader, int id)
        {
            return GetFieldValues(reader, id);
        }

        /// <summary>
        /// Gets a single field value 
        /// </summary>
        /// <param name="reader">index reader</param>
        /// <param name="id">doc</param>
        /// <returns>first field value</returns>
        public virtual string GetFieldValue(BoboSegmentReader reader, int id)
        {
            return GetFieldValues(reader, id)[0];
        }

        /// <summary>
        /// builds a comparator to determine how sorting is done
        /// </summary>
        /// <returns>a sort comparator</returns>
        public abstract DocComparatorSource GetDocComparatorSource();

        // Removed clone method (differs from Java). For it to work, all facet handlers and any nested types
        // (such as TermListFactory and subclasses) would all need to be marked [Serializable].
        // There doesn't seem to be much benefit in cloning a facet handler, since it is tied to
        // a specific field at construction anyway.
        //public virtual object Clone()
        //{
        //    return ObjectCopier.Clone(this);
        //}
    }
}
