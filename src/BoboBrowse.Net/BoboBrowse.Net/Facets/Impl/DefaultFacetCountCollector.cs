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
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Util;
    using Common.Logging;
    using System;
    using System.Collections.Generic;
    
    public abstract class DefaultFacetCountCollector : IFacetCountCollector
    {
        private static ILog log = LogManager.GetLogger<DefaultFacetCountCollector>();

        protected internal readonly FacetSpec _ospec;
        protected internal BigSegmentedArray _count;
        protected internal int _countlength;
        protected internal readonly FacetDataCache _dataCache;
        private readonly string _name;
        protected internal readonly BrowseSelection _sel;
        protected internal readonly BigSegmentedArray _array;
        private int _docBase;
        // NOTE: Removed memory manager implementation
        //protected readonly List<BigSegmentedArray> intarraylist = new List<BigSegmentedArray>();
        //private Iterator _iterator; // NOT USED
        private bool _closed = false;

        public DefaultFacetCountCollector(string name, FacetDataCache dataCache, int docBase, BrowseSelection sel, FacetSpec ospec)
        {
            _sel = sel;
            _ospec = ospec;
            _name = name;
            _dataCache = dataCache;
            _countlength = _dataCache.Freqs.Length;

            if (_dataCache.Freqs.Length <= 3096)
            {
                _count = new LazyBigIntArray(_countlength);
            }
            else
            {
                _count = new LazyBigIntArray(_countlength);

                // NOTE: Removed memory manager implementation
                //_count = intarraymgr.Get(_countlength);
                //intarraylist.Add(_count);
            }

            _array = _dataCache.OrderArray;
            _docBase = docBase;
        }

        public virtual string Name
        {
            get { return _name; }
        }

        public abstract void Collect(int docid);

        public abstract void CollectAll();

        public virtual BrowseFacet GetFacet(string value)
        {
            BrowseFacet facet = null;
            int index = _dataCache.ValArray.IndexOf(value);
            if (index >= 0)
            {
                facet = new BrowseFacet(_dataCache.ValArray.Get(index), _count.Get(index));
            }
            else
            {
                facet = new BrowseFacet(_dataCache.ValArray.Format(@value), 0);
            }
            return facet;
        }

        public virtual int GetFacetHitsCount(object value)
        {
            if (_closed)
            {
                throw new InvalidOperationException("This instance of count collector for " + _name + " was already closed");
            }
            int index = _dataCache.ValArray.IndexOf(value);
            if (index >= 0)
            {
                return _count.Get(index);
            }
            else
            {
                return 0;
            }
        }

        public virtual BigSegmentedArray GetCountDistribution()
        {
            return _count;
        }

        public virtual FacetDataCache FacetDataCache
        {
            get { return _dataCache; }
        }

        public static IEnumerable<BrowseFacet> GetFacets(FacetSpec ospec, BigSegmentedArray count, int countlength, ITermValueList valList)
        {
            if (ospec != null)
            {
                int minCount = ospec.MinHitCount;
                int max = ospec.MaxCount;
                if (max <= 0) max = countlength;

                LinkedList<BrowseFacet> facetColl;
                FacetSpec.FacetSortSpec sortspec = ospec.OrderBy;
                if (sortspec == FacetSpec.FacetSortSpec.OrderValueAsc)
                {
                    facetColl = new LinkedList<BrowseFacet>();
                    for (int i = 1; i < countlength; ++i) // exclude zero
                    {
                        int hits = count.Get(i);
                        if (hits >= minCount)
                        {
                            BrowseFacet facet = new BrowseFacet(valList.Get(i), hits);
                            facetColl.AddLast(facet);
                        }

                        if (facetColl.Count >= max)
                            break;
                    }
                }
                else //if (sortspec == FacetSortSpec.OrderHitsDesc)
                {
                    IComparatorFactory comparatorFactory;
                    if (sortspec == FacetSpec.FacetSortSpec.OrderHitsDesc)
                    {
                        comparatorFactory = new FacetHitcountComparatorFactory();
                    }
                    else
                    {
                        comparatorFactory = ospec.CustomComparatorFactory;
                    }

                    if (comparatorFactory == null)
                    {
                        throw new ArgumentException("facet comparator factory not specified");
                    }

                    IComparer<int> comparator = comparatorFactory.NewComparator(new DefaultFacetCountCollectorFieldAccessor(valList), count);
                    facetColl = new LinkedList<BrowseFacet>();
                    int forbidden = -1;
                    IntBoundedPriorityQueue pq = new IntBoundedPriorityQueue(comparator, max, forbidden);

                    for (int i = 1; i < countlength; ++i) // exclude zero
                    {
                        int hits = count.Get(i);
                        if (hits >= minCount)
                        {
                            pq.Offer(i);
                        }
                    }

                    int val;
                    while ((val = pq.Poll()) != forbidden)
                    {
                        BrowseFacet facet = new BrowseFacet(valList[val], count.Get(val));
                        facetColl.AddFirst(facet);
                    }
                }
                return facetColl;
            }
            else
            {
                return FacetCountCollector_Fields.EMPTY_FACET_LIST;
            }
        }

        private class DefaultFacetCountCollectorFieldAccessor : IFieldValueAccessor
        {
            private ITermValueList valList;

            public DefaultFacetCountCollectorFieldAccessor(ITermValueList valList)
            {
                this.valList = valList;
            }

            public virtual string GetFormatedValue(int index)
            {
                return valList.Get(index);
            }

            public virtual object GetRawValue(int index)
            {
                return valList.GetRawValue(index);
            }
        }

        public virtual IEnumerable<BrowseFacet> GetFacets()
        {
            if (_closed)
            {
                throw new InvalidOperationException("This instance of count collector for " + _name + " was already closed");
            }

            return GetFacets(_ospec, _count, _countlength, _dataCache.ValArray);
        }

        public virtual void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_closed)
                {
                    log.Warn("This instance of count collector for '" + _name + "' was already closed. This operation is no-op.");
                    return;
                }
                _closed = true;
                // NOTE: Removed memory manager implementation
                //while (!intarraylist.isEmpty())
                //{
                //    intarraymgr.release(intarraylist.poll());
                //}
            }
        }

        /// <summary>
        /// This function returns an Iterator to visit the facets in value order
        /// </summary>
        /// <returns>The Iterator to iterate over the facets in value order</returns>
        public virtual FacetIterator Iterator()
        {
            if (_closed)
            {
                throw new InvalidOperationException("This instance of count collector for '" + _name + "' was already closed");
            }
            if (_dataCache.ValArray.Type.Equals(typeof(int)))
            {
                return new DefaultIntFacetIterator((TermIntList)_dataCache.ValArray, _count, _countlength, false);
            }
            else if (_dataCache.ValArray.Type.Equals(typeof(long)))
            {
                return new DefaultLongFacetIterator((TermLongList)_dataCache.ValArray, _count, _countlength, false);
            }
            else if (_dataCache.ValArray.Type.Equals(typeof(short)))
            {
                return new DefaultShortFacetIterator((TermShortList)_dataCache.ValArray, _count, _countlength, false);
            }
            else if (_dataCache.ValArray.Type.Equals(typeof(float)))
            {
                return new DefaultFloatFacetIterator((TermFloatList)_dataCache.ValArray, _count, _countlength, false);
            }
            else if (_dataCache.ValArray.Type.Equals(typeof(double)))
            {
                return new DefaultDoubleFacetIterator((TermDoubleList)_dataCache.ValArray, _count, _countlength, false);
            }
            else
                return new DefaultFacetIterator(_dataCache.ValArray, _count, _countlength, false);
        }
    }
}
