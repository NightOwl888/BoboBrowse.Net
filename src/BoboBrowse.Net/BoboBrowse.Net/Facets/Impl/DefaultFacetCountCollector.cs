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
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Support.Logging;
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections.Generic;
    
    public abstract class DefaultFacetCountCollector : IFacetCountCollector
    {
        private static ILog m_log = LogProvider.For<DefaultFacetCountCollector>();
        protected readonly FacetSpec m_ospec;
        protected BigSegmentedArray m_count;

        protected int m_countlength;
        protected readonly FacetDataCache m_dataCache;
        private readonly string m_name;
        protected readonly BrowseSelection m_sel;
        protected readonly BigSegmentedArray m_array;
        // NOTE: Removed memory manager implementation
        //protected readonly List<BigSegmentedArray> intarraylist = new List<BigSegmentedArray>();
        private bool m_closed = false;

        public DefaultFacetCountCollector(string name, FacetDataCache dataCache, int docBase, BrowseSelection sel, FacetSpec ospec)
        {
            m_sel = sel;
            this.m_ospec = ospec;
            m_name = name;
            m_dataCache = dataCache;
            m_countlength = m_dataCache.Freqs.Length;

            if (m_dataCache.Freqs.Length <= 3096)
            {
                m_count = new LazyBigInt32Array(m_countlength);
            }
            else
            {
                m_count = new LazyBigInt32Array(m_countlength);

                // NOTE: Removed memory manager implementation
                //_count = intarraymgr.Get(_countlength);
                //intarraylist.Add(_count);
            }

            m_array = m_dataCache.OrderArray;
        }

        /// <summary>
        /// Added in .NET version as an accessor to the _count field.
        /// </summary>
        public virtual BigSegmentedArray Count
        {
            get { return m_count; }
        }

        /// <summary>
        /// Added in .NET version as an accessor to the _dataCache field.
        /// </summary>
        public virtual FacetDataCache DataCache
        {
            get { return m_dataCache; }
        }

        /// <summary>
        /// Added in .NET version as an accessor to the _countLength field.
        /// </summary>
        public virtual int CountLength
        {
            get { return m_countlength; }
        }

        public virtual string Name
        {
            get { return m_name; }
        }

        public abstract void Collect(int docid);

        public abstract void CollectAll();

        public virtual BrowseFacet GetFacet(string value)
        {
            if (m_closed)
            {
                throw new InvalidOperationException("This instance of count collector for " + m_name + " was already closed");
            }
            BrowseFacet facet = null;
            int index = m_dataCache.ValArray.IndexOf(value);
            if (index >= 0)
            {
                facet = new BrowseFacet(m_dataCache.ValArray.Get(index), m_count.Get(index));
            }
            else
            {
                facet = new BrowseFacet(m_dataCache.ValArray.Format(@value), 0);
            }
            return facet;
        }

        public virtual int GetFacetHitsCount(object value)
        {
            if (m_closed)
            {
                throw new InvalidOperationException("This instance of count collector for " + m_name + " was already closed");
            }
            int index = m_dataCache.ValArray.IndexOf(value);
            if (index >= 0)
            {
                return m_count.Get(index);
            }
            else
            {
                return 0;
            }
        }

        public virtual BigSegmentedArray GetCountDistribution()
        {
            return m_count;
        }

        public virtual FacetDataCache FacetDataCache
        {
            get { return m_dataCache; }
        }

        public static ICollection<BrowseFacet> GetFacets(FacetSpec ospec, BigSegmentedArray count, int countlength, ITermValueList valList)
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
                    IComparerFactory comparerFactory;
                    if (sortspec == FacetSpec.FacetSortSpec.OrderHitsDesc)
                    {
                        comparerFactory = new FacetHitcountComparerFactory();
                    }
                    else
                    {
                        comparerFactory = ospec.CustomComparerFactory;
                    }

                    if (comparerFactory == null)
                    {
                        throw new ArgumentException("facet comparer factory not specified");
                    }

                    IComparer<int> comparer = comparerFactory.NewComparer(new DefaultFacetCountCollectorFieldAccessor(valList), count);
                    facetColl = new LinkedList<BrowseFacet>();
                    int forbidden = -1;
                    Int32BoundedPriorityQueue pq = new Int32BoundedPriorityQueue(comparer, max, forbidden);

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
                return FacetCountCollector.EMPTY_FACET_LIST;
            }
        }

        private class DefaultFacetCountCollectorFieldAccessor : IFieldValueAccessor
        {
            private ITermValueList m_valList;

            public DefaultFacetCountCollectorFieldAccessor(ITermValueList valList)
            {
                this.m_valList = valList;
            }

            public virtual string GetFormatedValue(int index)
            {
                return m_valList.Get(index);
            }

            public virtual object GetRawValue(int index)
            {
                return m_valList.GetRawValue(index);
            }
        }

        public virtual ICollection<BrowseFacet> GetFacets()
        {
            if (m_closed)
            {
                throw new InvalidOperationException("This instance of count collector for " + m_name + " was already closed");
            }

            return GetFacets(m_ospec, m_count, m_countlength, m_dataCache.ValArray);
        }

        public virtual void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_closed)
                {
                    m_log.Warn("This instance of count collector for '" + m_name + "' was already closed. This operation is no-op.");
                    return;
                }
                m_closed = true;
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
        public virtual FacetIterator GetIterator()
        {
            if (m_closed)
            {
                throw new InvalidOperationException("This instance of count collector for '" + m_name + "' was already closed");
            }
            if (m_dataCache.ValArray.Type.Equals(typeof(int)))
            {
                return new DefaultInt32FacetIterator((TermInt32List)m_dataCache.ValArray, m_count, m_countlength, false);
            }
            else if (m_dataCache.ValArray.Type.Equals(typeof(long)))
            {
                return new DefaultInt64FacetIterator((TermInt64List)m_dataCache.ValArray, m_count, m_countlength, false);
            }
            else if (m_dataCache.ValArray.Type.Equals(typeof(short)))
            {
                return new DefaultInt16FacetIterator((TermInt16List)m_dataCache.ValArray, m_count, m_countlength, false);
            }
            else if (m_dataCache.ValArray.Type.Equals(typeof(float)))
            {
                return new DefaultSingleFacetIterator((TermSingleList)m_dataCache.ValArray, m_count, m_countlength, false);
            }
            else if (m_dataCache.ValArray.Type.Equals(typeof(double)))
            {
                return new DefaultDoubleFacetIterator((TermDoubleList)m_dataCache.ValArray, m_count, m_countlength, false);
            }
            else
                return new DefaultFacetIterator(m_dataCache.ValArray, m_count, m_countlength, false);
        }
    }
}
