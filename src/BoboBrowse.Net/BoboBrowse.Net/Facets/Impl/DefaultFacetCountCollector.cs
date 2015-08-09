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
        private static ILog log = LogManager.GetLogger(typeof(DefaultFacetCountCollector));
        protected readonly FacetSpec _ospec;
        protected BigSegmentedArray _count;

        protected int _countlength;
        protected readonly FacetDataCache _dataCache;
        private readonly string _name;
        protected readonly BrowseSelection _sel;
        protected readonly BigSegmentedArray _array;
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

        /// <summary>
        /// Added in .NET version as an accessor to the _count field.
        /// </summary>
        public virtual BigSegmentedArray Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Added in .NET version as an accessor to the _dataCache field.
        /// </summary>
        public virtual FacetDataCache DataCache
        {
            get { return _dataCache; }
        }

        /// <summary>
        /// Added in .NET version as an accessor to the _countLength field.
        /// </summary>
        public virtual int CountLength
        {
            get { return _countlength; }
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
