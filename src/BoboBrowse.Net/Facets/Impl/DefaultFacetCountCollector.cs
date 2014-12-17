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
        protected internal int[] _count;
        protected internal int _countlength;
        protected internal readonly IFacetDataCache _dataCache;
        private readonly string _name;
        protected internal readonly BrowseSelection _sel;
        protected internal readonly BigSegmentedArray _array;
        private int _docBase;
        //protected readonly List<int[]> intarraylist = new List<int[]>();
        private Iterator _iterator;
        private bool _closed = false;

        // TODO: Need to determine if the memory manger is necessary

        public DefaultFacetCountCollector(String name, IFacetDataCache dataCache, int docBase, BrowseSelection sel, FacetSpec ospec)
        {
            _sel = sel;
            _ospec = ospec;
            _name = name;
            _dataCache = dataCache;
            if (_dataCache.Freqs.Length <= 3096)
            {
                _countlength = _dataCache.Freqs.Length;
                _count = new int[_countlength];
            }
            else
            {
                _countlength = _dataCache.Freqs.Length;
                _count = new int[_countlength];

                //_count = intarraymgr.get(_countlength);//new int[_dataCache.freqs.length];
                //intarraylist.add(_count);
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
                facet = new BrowseFacet(_dataCache.ValArray.Get(index), _count[index]);
            }
            else
            {
                facet = new BrowseFacet(_dataCache.ValArray.Format(@value), 0);
            }
            return facet;
        }

        public virtual int GetFacetsHitCount(object value)
        {
            if (_closed)
            {
                throw new InvalidOperationException("This instance of count collector for " + _name + " was already closed");
            }
            int index = _dataCache.ValArray.IndexOf(value);
            if (index >= 0)
            {
                return _count[index];
            }
            else
            {
                return 0;
            }
        }

        public virtual int[] GetCountDistribution()
        {
            return _count;
        }

        public virtual IEnumerable<BrowseFacet> GetFacets(FacetSpec ospec, int[] count, int countLength, ITermValueList valList)
        {
            if (_ospec != null)
            {
                int minCount = _ospec.MinHitCount;
                int max = _ospec.MaxCount;
                if (max <= 0) max = _count.Length;

                IList<BrowseFacet> facetColl;
                FacetSpec.FacetSortSpec sortspec = _ospec.OrderBy;
                if (sortspec == FacetSpec.FacetSortSpec.OrderValueAsc)
                {
                    facetColl = new List<BrowseFacet>(max);
                    for (int i = 1; i < _count.Length; ++i) // exclude zero
                    {
                        int hits = _count[i];
                        if (hits >= minCount)
                        {
                            BrowseFacet facet = new BrowseFacet(valList[i], hits);
                            facetColl.Add(facet);
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
                        comparatorFactory = _ospec.CustomComparatorFactory;
                    }

                    if (comparatorFactory == null)
                    {
                        throw new ArgumentException("facet comparator factory not specified");
                    }

                    IComparer<int> comparator = comparatorFactory.NewComparator(new DefaultFacetCountCollectorFieldAccessor(valList), count);
                    facetColl = new List<BrowseFacet>();
                    int forbidden = -1;
                    IntBoundedPriorityQueue pq = new IntBoundedPriorityQueue(comparator, max, forbidden);

                    for (int i = 1; i < countLength; ++i) // exclude zero
                    {
                        int hits = _count[i];
                        if (hits >= minCount)
                        {
                            pq.Offer(i);
                        }
                    }

                    int val;
                    while ((val = pq.Poll()) != forbidden)
                    {
                        BrowseFacet facet = new BrowseFacet(valList[val], _count[val]);
                        facetColl.Insert(0, facet);
                    }
                }
                return facetColl;
            }
            else
            {
                return IFacetCountCollector_Fields.EMPTY_FACET_LIST;
            }
        }

        private class DefaultFacetCountCollectorFieldAccessor : IFieldValueAccessor
        {
            private ITermValueList valList;

            public DefaultFacetCountCollectorFieldAccessor(ITermValueList valList)
            {
                this.valList = valList;
            }

            public string GetFormatedValue(int index)
            {
                return valList.Get(index);
            }

            public object GetRawValue(int index)
            {
                return valList.GetRawValue(index);
            }
        }

        public IEnumerable<BrowseFacet> GetFacets()
        {
            if (_closed)
            {
                throw new InvalidOperationException("This instance of count collector for " + _name + " was already closed");
            }

            return GetFacets(_ospec, _count, _countlength, _dataCache.ValArray);
        }

        // TODO: Implement dispose?
        public void Close()
        {
            if (_closed)
            {
                log.Warn("This instance of count collector for '" + _name + "' was already closed. This operation is no-op.");
                return;
            }
            _closed = true;
            // TODO: memory manager implmentation
            //while (!intarraylist.isEmpty())
            //{
            //    intarraymgr.release(intarraylist.poll());
            //}
        }

        /// <summary>
        /// This function returns an Iterator to visit the facets in value order
        /// </summary>
        /// <returns>The Iterator to iterate over the facets in value order</returns>
        public FacetIterator Iterator()
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
