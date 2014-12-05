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

namespace BoboBrowse.Net.Facets
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Lucene.Net.Search;
    using BoboBrowse.Net.Facets.Filter;

    public abstract class FacetHandler : ICloneable
    {
        public enum TermCountSize
        {
            Small,
            Medium,
            Large
        }

        private readonly Dictionary<string, FacetHandler> _dependedFacetHandlers;       

        protected FacetHandler(string name) : this(name, null) { }

        protected FacetHandler(string name, IEnumerable<string> dependsOn)
        {
            this.Name = name;
            this.DependsOn = dependsOn == null ? new List<string>() : new List<string>(dependsOn);
            this._dependedFacetHandlers = new Dictionary<string, FacetHandler>();
            this.TermCountSizeFlag = TermCountSize.Large;
        }

        public abstract void Load(BoboIndexReader reader);

        public abstract RandomAccessFilter BuildRandomAccessFilter(string value, Properties selectionProperty);

       /// <summary>
        /// Gets a FacetCountCollector 
       /// </summary>
       /// <param name="sel"></param>
       /// <param name="fspec"></param>
       /// <returns></returns>
        public abstract IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec fspec);

       /// <summary>
        /// Gets the field value
       /// </summary>
       /// <param name="id"></param>
       /// <returns></returns>
        public abstract string[] GetFieldValues(int id);

        public abstract object[] GetRawFieldValues(int id);

        /// <summary>
        /// builds a comparator to determine how sorting is done
        /// </summary>
        /// <returns></returns>
        public abstract FieldComparator GetComparator(int numDocs, SortField field);

        /// <summary>
        /// Gets a single field value 
        /// </summary>
         /// <param name="id"></param>
        /// <returns></returns>
        public virtual string GetFieldValue(int id)
        {
            return GetFieldValues(id)[0];
        }

        /// <summary>
        /// Adds a list of depended facet handlers
        /// </summary>
        /// <param name="facetHandler"></param>
        public void PutDependedFacetHandler(FacetHandler facetHandler)
        {
            this._dependedFacetHandlers[facetHandler.Name] = facetHandler;
        }

        public FacetHandler GetDependedFacetHandler(string name)
        {
            FacetHandler facetHandler = null;
            if (this._dependedFacetHandlers.TryGetValue(name, out facetHandler))
            {
                return facetHandler;
            }
            return null;
        }

        public virtual IFacetAccessible Merge(FacetSpec fspec, IEnumerable<IFacetAccessible> facetList)
        {
            return new CombinedFacetAccessible(fspec, facetList);
        }

        public virtual void Load(BoboIndexReader reader, BoboIndexReader.WorkArea workArea)
        {
            Load(reader);
        }

        public RandomAccessFilter BuildFilter(BrowseSelection sel)
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
                    // there is no hit in this AND filter because this value has no hit
                    return null;
                }
            }
            if (filterList.Count == 0)
                return null;
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

        #region Properties
        /// <summary>
        /// Gets name of the current facet handler.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets names of the facet handler this depends on
        /// </summary>
        public List<string> DependsOn
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a term count length.
        /// </summary>
        public TermCountSize TermCountSizeFlag
        {
            get;
            set;
        }
        #endregion

        public virtual object Clone()
        {
            throw new NotImplementedException("implement Clone");            
        }

        private class CombinedFacetAccessible : IFacetAccessible
        {
            private readonly IEnumerable<IFacetAccessible> list;
            private readonly FacetSpec fspec;

            internal CombinedFacetAccessible(FacetSpec fspec, IEnumerable<IFacetAccessible> list)
            {
                this.list = list;
                this.fspec = fspec;
            }

            public override string ToString()
            {
                return "_list:" + list + " _fspec:" + fspec;
            }

            public virtual BrowseFacet GetFacet(string @value)
            {
                int sum = -1;
                object foundValue = null;
                if (list != null)
                {
                    foreach (IFacetAccessible facetAccessor in list)
                    {
                        BrowseFacet facet = facetAccessor.GetFacet(@value);
                        if (facet != null)
                        {
                            foundValue = facet.Value;
                            if (sum == -1)
                                sum = facet.HitCount;
                            else
                                sum += facet.HitCount;
                        }
                    }
                }
                if (sum == -1)
                    return null;
                return new BrowseFacet(foundValue, sum);
            }

            public virtual IEnumerable<BrowseFacet> GetFacets()
            {
                C5.IDictionary<object, BrowseFacet> facetMap;
                if (FacetSpec.FacetSortSpec.OrderValueAsc.Equals(fspec.OrderBy))
                {
                    facetMap = new C5.TreeDictionary<object, BrowseFacet>();
                }
                else
                {
                    facetMap = new C5.HashDictionary<object, BrowseFacet>();
                }

                foreach (IFacetAccessible facetAccessor in this.list)
                {
                    IEnumerator<BrowseFacet> iter = facetAccessor.GetFacets().GetEnumerator();
                    if (facetMap.Count == 0)
                    {
                        while (iter.MoveNext())
                        {
                            BrowseFacet facet = iter.Current;
                            facetMap.Add(facet.Value, facet);
                        }
                    }
                    else
                    {
                        while (iter.MoveNext())
                        {
                            BrowseFacet facet = iter.Current;
                            BrowseFacet existing = facetMap[facet.Value];
                            if (existing == null)
                            {
                                facetMap.Add(facet.Value, facet);
                            }
                            else
                            {
                                existing.HitCount = existing.HitCount + facet.HitCount;
                            }
                        }
                    }
                }

                List<BrowseFacet> list = new List<BrowseFacet>(facetMap.Values);
                // FIXME: we need to reorganize all that stuff with comparators
                Comparer comparer = new Comparer(System.Globalization.CultureInfo.InvariantCulture);
                if (FacetSpec.FacetSortSpec.OrderHitsDesc.Equals(fspec.OrderBy))
                {
                    list.Sort(
                        delegate(BrowseFacet f1, BrowseFacet f2)
                        {
                            int val = f2.HitCount - f1.HitCount;
                            if (val == 0)
                            {
                                val = -(comparer.Compare(f1.Value, f2.Value));
                            }
                            return val;
                        }
                        );
                }
                return list;
            }
        }
    }
}
