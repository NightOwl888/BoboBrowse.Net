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
namespace BoboBrowse.Net.Facets
{
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;
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
        RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties prop);
        RandomAccessFilter BuildRandomAccessFilter(string value, Properties selectionProperty);
        RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot);
        object Clone();
        IEnumerable<string> DependsOn { get; }


        object GetDependedFacetHandler(string name); // Generic - IFacetHandler


        DocComparatorSource GetDocComparatorSource();
        IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec fspec);
        FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec);
        FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec ospec, bool groupMode);

        //D GetFacetData(BoboIndexReader reader);// Generic
        T GetFacetData<T>(BoboIndexReader reader);

        string GetFieldValue(BoboIndexReader reader, int id);
        string[] GetFieldValues(BoboIndexReader reader, int id);
        int GetNumItems(BoboIndexReader reader, int id);
        object[] GetRawFieldValues(BoboIndexReader reader, int id);

        //TermCountSize GetTermCountSize();

        //D Load(BoboIndexReader reader);// Generic
        //D Load(BoboIndexReader reader, BoboIndexReader.WorkArea workArea);// Generic

        //object Load(BoboIndexReader reader); // TODO: Make these pass generic parameters - it appears the classes know what type to expect
        //object Load(BoboIndexReader reader, BoboIndexReader.WorkArea workArea);

        void LoadFacetData(BoboIndexReader reader);
        void LoadFacetData(BoboIndexReader reader, BoboIndexReader.WorkArea workArea);

        IFacetAccessible Merge(FacetSpec fspec, IEnumerable<IFacetAccessible> facetList);
        string Name { get; }

        //void PutDependedFacetHandler<T>(FacetHandler<T> facetHandler); // Generic - IFacetHandler
        void PutDependedFacetHandler(IFacetHandler facetHandler);

        void SetTermCountSize(string termCountSize);
        TermCountSize TermCountSize { get; set; }

        //FacetHandler<D> SetTermCountSize(TermCountSize termCountSize);//Make void
        //FacetHandler<D> SetTermCountSize(string termCountSize);//Make void
    }

    [Serializable]
    public class FacetDataNone
    {
        private static long serialVersionUID = 1L;
        public static FacetDataNone instance = new FacetDataNone();
        private FacetDataNone() { }
    }

    public abstract class FacetHandler<D> : ICloneable, IFacetHandler
    {
        protected readonly string _name;
        private readonly IEnumerable<string> _dependsOn;
        // original was <string, FacetHandler<?>>
        private readonly Dictionary<string, IFacetHandler> _dependedFacetHandlers;
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

        //public FacetHandler<D> SetTermCountSize(string termCountSize)
        //{
        //    this.SetTermCountSize((TermCountSize)Enum.Parse(typeof(TermCountSize), termCountSize, true));
        //    return this;
        //}

        //public FacetHandler<D> SetTermCountSize(TermCountSize termCountSize)
        //{
        //    _termCountSize = termCountSize;
        //    return this;
        //}

        //public TermCountSize GetTermCountSize()
        //{
        //    return _termCountSize;
        //}

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
        protected FacetHandler(string name) 
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

        // TODO: Need to revisit this design later to see if there is a better alternative
        // to using FacetHandler<T>. In Java, they used FacetHandler<?>.

        ///// <summary>
        ///// Adds a list of depended facet handlers
        ///// </summary>
        ///// <param name="name">Name of handler depended facet handler</param>
        ///// <param name="facetHandler">Handler depended facet handler</param>
        //public void PutDependedFacetHandler<T>(FacetHandler<T> facetHandler)
        //{
        //    _dependedFacetHandlers.Put(facetHandler._name, facetHandler);
        //}

        ///// <summary>
        ///// Gets a depended facet handler
        ///// </summary>
        ///// <param name="name">facet handler name</param>
        ///// <returns>facet handler instance</returns>
        //public object GetDependedFacetHandler(string name)
        //{
        //    return _dependedFacetHandlers.Get(name);
        //}

        /// <summary>
        /// Adds a list of depended facet handlers
        /// </summary>
        /// <param name="name">Name of handler depended facet handler</param>
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
        public abstract D Load(BoboIndexReader reader);

        ///// <summary>
        ///// Load information from an index reader, initialized by <see cref="T:BoboBrowse.Net.BoboIndexReader"/>.
        ///// </summary>
        ///// <param name="reader"></param>
        //public abstract object Load(BoboIndexReader reader);

        public virtual IFacetAccessible Merge(FacetSpec fspec, IEnumerable<IFacetAccessible> facetList)
        {
            return new CombinedFacetAccessible(fspec, facetList);
        }

        //public virtual D GetFacetData(BoboIndexReader reader)
        //{
        //    return (D)reader.GetFacetData(_name);
        //}

        public virtual T GetFacetData<T>(BoboIndexReader reader)
        {
            return (T)reader.GetFacetData(_name);
        }


        public virtual D Load(BoboIndexReader reader, BoboIndexReader.WorkArea workArea)
        {
            return Load(reader);
        }

        //public virtual object Load(BoboIndexReader reader, BoboIndexReader.WorkArea workArea)
        //{
        //    return Load(reader);
        //}

        public virtual void LoadFacetData(BoboIndexReader reader, BoboIndexReader.WorkArea workArea)
        {
            reader.PutFacetData(_name, Load(reader, workArea));
        }

        public virtual void LoadFacetData(BoboIndexReader reader)
        {
            reader.PutFacetData(_name, Load(reader));
        }

        public virtual RandomAccessFilter BuildFilter(BrowseSelection sel)
        {
            string[] selections = sel.Values;
            string[] notSelections = sel.NotValues;
            Properties prop = sel.SelectionProperties;

            RandomAccessFilter filter = null;
            if (selections != null && selections.Length > 0)
            {
                if (sel.SelectionOperation == BrowseSelection.ValueOperation.ValueOperationAnd)
                {
                    filter = BuildRandomAccessAndFilter(selections, prop);
                    if (filter == null)
                    {
                        filter = EmptyFilter.GetInstance();
                    }
                }
                else
                {
                    filter = BuildRandomAccessOrFilter(selections, prop, false);
                    if (filter == null)
                    {
                        return EmptyFilter.GetInstance();
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

        public abstract RandomAccessFilter BuildRandomAccessFilter(string value, Properties selectionProperty);

        public virtual RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties prop)
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
                    return EmptyFilter.GetInstance();
                }
            }

            if (filterList.Count == 1)
                return filterList.First();
            return new RandomAccessAndFilter(filterList);
        }

        public virtual RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot)
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
                finalFilter = EmptyFilter.GetInstance();
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
        public abstract string[] GetFieldValues(BoboIndexReader reader, int id);

        public int GetNumItems(BoboIndexReader reader, int id)
        {
            throw new NotImplementedException("GetNumItems is not supported for this facet handler: " + this.GetType().FullName);
        }

        public virtual object[] GetRawFieldValues(BoboIndexReader reader, int id)
        {
            return GetFieldValues(reader, id);
        }

        /// <summary>
        /// Gets a single field value 
        /// </summary>
        /// <param name="reader">index reader</param>
        /// <param name="id">doc</param>
        /// <returns>first field value</returns>
        public virtual string GetFieldValue(BoboIndexReader reader, int id)
        {
            return GetFieldValues(reader, id)[0];
        }

        /// <summary>
        /// builds a comparator to determine how sorting is done
        /// </summary>
        /// <returns>a sort comparator</returns>
        public abstract DocComparatorSource GetDocComparatorSource();


       /// <summary>
        /// Gets a FacetCountCollector 
       /// </summary>
       /// <param name="sel"></param>
       /// <param name="fspec"></param>
       /// <returns></returns>
        public abstract IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec fspec);

        public virtual object Clone()
        {
            return base.MemberwiseClone();
        }
    }
}
