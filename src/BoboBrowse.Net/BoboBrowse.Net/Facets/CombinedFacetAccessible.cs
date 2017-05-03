//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
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
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Support.Logging;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Linq;

     ///<summary>
     ///author nnarkhed
     ///</summary>
    public class CombinedFacetAccessible : IFacetAccessible
    {
        private static readonly ILog log = LogProvider.For<CombinedFacetAccessible>();
        protected readonly ICollection<IFacetAccessible> m_list;
        protected readonly FacetSpec m_fspec;
        protected bool m_closed;

        public CombinedFacetAccessible(FacetSpec fspec, ICollection<IFacetAccessible> list)
        {
            m_list = list;
            m_fspec = fspec;
        }

        public override string ToString()
        {
            return "_list:" + m_list + " _fspec:" + m_fspec;
        }

        public virtual BrowseFacet GetFacet(string value)
        {
            if (m_closed)
            {
                throw new InvalidOperationException("This instance of count collector was already closed");
            }
            int sum = -1;
            string foundValue = null;
            if (m_list != null)
            {
                foreach (IFacetAccessible facetAccessor in m_list)
                {
                    BrowseFacet facet = facetAccessor.GetFacet(value);
                    if (facet != null)
                    {
                        foundValue = facet.Value;
                        if (sum == -1) sum = facet.FacetValueHitCount;
                        else sum += facet.FacetValueHitCount;
                    }
                }
            }
            if (sum == -1) return null;
            return new BrowseFacet(foundValue, sum);
        }

        public virtual int GetCappedFacetCount(object value, int cap)
        {
            if (m_closed)
            {
                throw new InvalidOperationException("This instance of count collector was already closed");
            }
            int sum = 0;
            if (m_list != null)
            {
                foreach (IFacetAccessible facetAccessor in m_list)
                {
                    sum += facetAccessor.GetFacetHitsCount(value);
                    if (sum >= cap)
                        return cap;
                }
            }
            return sum;
        }

        public virtual int GetFacetHitsCount(object value)
        {
            if (m_closed)
            {
                throw new InvalidOperationException("This instance of count collector was already closed");
            }
            int sum = 0;
            if (m_list != null)
            {
                foreach (IFacetAccessible facetAccessor in m_list)
                {
                    sum += facetAccessor.GetFacetHitsCount(value);
                }
            }
            return sum;
        }

        public virtual ICollection<BrowseFacet> GetFacets()
        {
            if (m_closed)
            {
                throw new InvalidOperationException("This instance of count collector was already closed");
            }
            int maxCnt = m_fspec.MaxCount;
            if (maxCnt <= 0)
                maxCnt = int.MaxValue;
            int minHits = m_fspec.MinHitCount;
            List<BrowseFacet> list = new List<BrowseFacet>();

            int cnt = 0;
            string facet = null;
            FacetIterator iter = this.GetIterator();
            IComparer<BrowseFacet> comparer;
            if (FacetSpec.FacetSortSpec.OrderValueAsc.Equals(m_fspec.OrderBy))
            {
                while (!string.IsNullOrEmpty((facet = iter.Next(minHits))))
                {
                    // find the next facet whose combined hit count obeys minHits
                    list.Add(new BrowseFacet(Convert.ToString(facet), iter.Count));
                    if (++cnt >= maxCnt) break;
                }
            }
            else if (FacetSpec.FacetSortSpec.OrderHitsDesc.Equals(m_fspec.OrderBy))
            {
                comparer = new BrowseFacetComparer();
                if (maxCnt != int.MaxValue)
                {
                    // we will maintain a min heap of size maxCnt
                    // Order by hits in descending order and max count is supplied
                    var queue = CreatePQ(maxCnt, comparer);
                    int qsize = 0;
                    while ((qsize < maxCnt) && !string.IsNullOrEmpty((facet = iter.Next(minHits))))
                    {
                        queue.Add(new BrowseFacet(facet, iter.Count));
                        qsize++;
                    }
                    if (!string.IsNullOrEmpty(facet))
                    {
                        BrowseFacet rootFacet = (BrowseFacet)queue.Top;
                        minHits = rootFacet.FacetValueHitCount + 1;
                        // facet count less than top of min heap, it will never be added 
                        while (!string.IsNullOrEmpty((facet = iter.Next(minHits))))
                        {
                            rootFacet.Value = facet;
                            rootFacet.FacetValueHitCount = iter.Count;
                            rootFacet = (BrowseFacet)queue.UpdateTop();
                            minHits = rootFacet.FacetValueHitCount + 1;
                        }
                    }
                    // at this point, queue contains top maxCnt facets that have hitcount >= minHits
                    while (qsize-- > 0)
                    {
                        // append each entry to the beginning of the facet list to order facets by hits descending
                        list.Insert(0, (BrowseFacet)queue.Pop());
                    }
                }
                else
                {
                    // no maxCnt specified. So fetch all facets according to minHits and sort them later
                    while (!string.IsNullOrEmpty((facet = iter.Next(minHits))))
                    {
                        list.Add(new BrowseFacet(facet, iter.Count));
                    }
                    list.Sort(comparer);
                }
            }
            else // FacetSortSpec.OrderByCustom.equals(_fspec.getOrderBy()
            {
                comparer = m_fspec.CustomComparerFactory.NewComparer();
                if (maxCnt != int.MaxValue)
                {
                    var queue = CreatePQ(maxCnt, comparer);
                    BrowseFacet browseFacet = new BrowseFacet();
                    int qsize = 0;
                    while ((qsize < maxCnt) && !string.IsNullOrEmpty((facet = iter.Next(minHits))))
                    {
                        queue.Add(new BrowseFacet(facet, iter.Count));
                        qsize++;
                    }
                    if (!string.IsNullOrEmpty(facet))
                    {
                        while (!string.IsNullOrEmpty((facet = iter.Next(minHits))))
                        {
                            // check with the top of min heap
                            browseFacet.FacetValueHitCount = iter.Count;
                            browseFacet.Value = facet;
                            browseFacet = (BrowseFacet)queue.InsertWithOverflow(browseFacet);
                        }
                    }
                    // remove from queue and add to the list
                    while (qsize-- > 0)
                    {
                        list.Insert(0, (BrowseFacet)queue.Pop());
                    }
                }
                else
                {
                    // order by custom but no max count supplied
                    while (!string.IsNullOrEmpty((facet = iter.Next(minHits))))
                    {
                        list.Add(new BrowseFacet(facet, iter.Count));
                    }
                    list.Sort(comparer);
                }
            }
            return list;
        }

        private class BrowseFacetComparer : IComparer<BrowseFacet>
        {
            public virtual int Compare(BrowseFacet f1, BrowseFacet f2)
            {
 	            int val = f2.FacetValueHitCount - f1.FacetValueHitCount;
                if (val==0)
                {
                    val = string.CompareOrdinal(f1.Value, f2.Value);
                }
                return val;
            }
        }

        private PriorityQueue<BrowseFacet> CreatePQ(int max, IComparer<BrowseFacet> comparer)
        {
            return new BrowseFacetPriorityQueue(max, comparer);
        }

        private class BrowseFacetPriorityQueue : PriorityQueue<BrowseFacet>
        {
            private readonly IComparer<BrowseFacet> m_comparer;

            public BrowseFacetPriorityQueue(int max, IComparer<BrowseFacet> comparer)
                : base(max)
            {
                this.m_comparer = comparer;
            }

            protected override bool LessThan(BrowseFacet a, BrowseFacet b)
            {
                BrowseFacet o1 = a;
                BrowseFacet o2 = b;
                return m_comparer.Compare(o1, o2) > 0;
            }
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
                    log.Warn("This instance of count collector was already closed. This operation is no-op.");
                    return;
                }
                m_closed = true;
                if (m_list != null)
                {
                    Exception exception = null;
                    foreach (IFacetAccessible fa in m_list)
                    {
                        try
                        {
                            fa.Dispose();
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }
                    }
                    // NOTE: This was done in the original Java source, but
                    // is not necessary in .NET. After disposing all of the child instances,
                    // memory cleanup is left up to the framework.
                    //_list.Clear();
                    if (exception != null)
                    {
                        throw exception;
                    }
                }
            }
        }

        public virtual FacetIterator GetIterator()
        {
            if (m_closed)
            {
                throw new InvalidOperationException("This instance of count collector was already closed");
            }

            List<FacetIterator> iterList = new List<FacetIterator>(m_list.Count);
            FacetIterator iter;
            foreach (IFacetAccessible facetAccessor in m_list)
            {
                iter = (FacetIterator)facetAccessor.GetIterator();
                if (iter != null)
                    iterList.Add(iter);
            }
            if (iterList.Count > 0 && iterList[0] is IntFacetIterator)
            {
                List<IntFacetIterator> il = new List<IntFacetIterator>();
                foreach (IFacetAccessible facetAccessor in m_list)
                {
                    iter = (FacetIterator)facetAccessor.GetIterator();
                    if (iter != null)
                        il.Add((IntFacetIterator)iter);
                }
                return new CombinedIntFacetIterator(il, m_fspec.MinHitCount);
            }
            if (iterList.Count > 0 && iterList[0] is LongFacetIterator)
            {
                List<LongFacetIterator> il = new List<LongFacetIterator>();
                foreach (IFacetAccessible facetAccessor in m_list)
                {
                    iter = (FacetIterator)facetAccessor.GetIterator();
                    if (iter != null)
                        il.Add((LongFacetIterator)iter);
                }
                return new CombinedLongFacetIterator(il, m_fspec.MinHitCount);
            }
            if (iterList.Count > 0 && iterList[0] is ShortFacetIterator)
            {
                List<ShortFacetIterator> il = new List<ShortFacetIterator>();
                foreach (IFacetAccessible facetAccessor in m_list)
                {
                    iter = (FacetIterator)facetAccessor.GetIterator();
                    if (iter != null)
                        il.Add((ShortFacetIterator)iter);
                }
                return new CombinedShortFacetIterator(il, m_fspec.MinHitCount);
            }
            if (iterList.Count > 0 && iterList[0] is FloatFacetIterator)
            {
                List<FloatFacetIterator> il = new List<FloatFacetIterator>();
                foreach (IFacetAccessible facetAccessor in m_list)
                {
                    iter = (FacetIterator)facetAccessor.GetIterator();
                    if (iter != null)
                        il.Add((FloatFacetIterator)iter);
                }
                return new CombinedFloatFacetIterator(il, m_fspec.MinHitCount);
            }
            if (iterList.Count > 0 && iterList[0] is DoubleFacetIterator)
            {
                List<DoubleFacetIterator> il = new List<DoubleFacetIterator>();
                foreach (IFacetAccessible facetAccessor in m_list)
                {
                    iter = (FacetIterator)facetAccessor.GetIterator();
                    if (iter != null)
                        il.Add((DoubleFacetIterator)iter);
                }
                return new CombinedDoubleFacetIterator(il, m_fspec.MinHitCount);
            }
            return new CombinedFacetIterator(iterList);
        }
    }
}
