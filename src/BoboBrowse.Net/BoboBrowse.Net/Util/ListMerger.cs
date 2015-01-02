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

﻿// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.Facets.Impl;
    using Lucene.Net.Util;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class ListMerger
    {
        public class MergedIterator<T> : IEnumerator<T>
        {
            private class IteratorNode
            {
                public IEnumerator<T> iterator;
                public T _curVal;

                public IteratorNode(IEnumerator<T> iterator)
                {
                    this.iterator = iterator;
                    _curVal = default(T);
                }

                public bool Fetch()
                {
                    if (iterator.MoveNext())
                    {
                        _curVal = iterator.Current;
                        return true;
                    }
                    _curVal = default(T);
                    return false;
                }
            }

            private readonly MergedQueue _queue;
            private T _current;

            private MergedIterator(int length, IComparer<T> comparator)
            {
                _queue = new MergedQueue(length, comparator);
            }

            private class MergedQueue : PriorityQueue<IteratorNode>
            {
                private readonly IComparer<T> comparator;

                public MergedQueue(int length, IComparer<T> comparator)
                {
                    this.comparator = comparator;
                    this.Initialize(length);
                }

                public override bool LessThan(IteratorNode a, IteratorNode b)
                {
                    return (comparator.Compare(a._curVal, b._curVal) < 0);
                }
            }

            public MergedIterator(IEnumerable<IEnumerator<T>> iterators, IComparer<T> comparator)
                : this(iterators.Count(), comparator)
            {
                foreach (IEnumerator<T> iterator in iterators)
                {
                    IteratorNode ctx = new IteratorNode(iterator);
                    if (ctx.Fetch()) _queue.Add(ctx); // NOTE: This was InsertWithOverflow in codeplex version
                }
            }

            public MergedIterator(IEnumerator<T>[] iterators, IComparer<T> comparator)
                : this(iterators.Length, comparator)
            {
                foreach (IEnumerator<T> iterator in iterators)
                {
                    IteratorNode ctx = new IteratorNode(iterator);
                    if (ctx.Fetch()) _queue.Add(ctx); // NOTE: This was InsertWithOverflow in codeplex version
                }
            }

            //public virtual bool HasNext()
            //{
            //    return queue.Size() > 0;
            //}

            //public virtual T Next()
            //{
            //    IteratorNode ctx = (IteratorNode)queue.Top();
            //    T val = ctx.CurVal;
            //    if (ctx.Fetch())
            //    {
            //        queue.UpdateTop();
            //    }
            //    else
            //    {
            //        queue.Pop();
            //    }
            //    return val;
            //}

            //public virtual void Remove()
            //{
            //    throw new NotSupportedException();
            //}

            public T Current
            {
                get { return _current; }
            }

            public void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get { return _current; }
            }

            public bool MoveNext()
            {
                if (_queue.Size() > 0)
                {
                    IteratorNode ctx = (IteratorNode)_queue.Top();
                    T val = ctx._curVal;
                    if (ctx.Fetch())
                    {
                        _queue.UpdateTop();
                    }
                    else
                    {
                        _queue.Pop();
                    }
                    this._current = val;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

        private ListMerger()
        {
        }

        public static MergedIterator<T> MergeLists<T>(IEnumerator<T>[] iterators, IComparer<T> comparator)
        {
            return new MergedIterator<T>(iterators, comparator);
        }

        public static MergedIterator<T> MergeLists<T>(IEnumerable<IEnumerator<T>> iterators, IComparer<T> comparator)
        {
            return new MergedIterator<T>(iterators, comparator);
        }

        public static List<T> MergeLists<T>(int offset, int count, IEnumerator<T>[] iterators, IComparer<T> comparator)
        {
            return MergeLists(offset, count, new MergedIterator<T>(iterators, comparator));
        }

        public static List<T> MergeLists<T>(int offset, int count, IEnumerable<IEnumerator<T>> iterators, IComparer<T> comparator)
        {
            return MergeLists(offset, count, new MergedIterator<T>(iterators, comparator));
        }

        private static List<T> MergeLists<T>(int offset, int count, MergedIterator<T> mergedIter)
        {
            for (int c = 0; c < offset && mergedIter.MoveNext(); c++)
            {
                var x = mergedIter.Current;
            }

            List<T> mergedList = new List<T>();

            for (int c = 0; c < count && mergedIter.MoveNext(); c++)
            {
                mergedList.Add(mergedIter.Current);
            }

            return mergedList;
        }

        public static IComparer<BrowseFacet> FACET_VAL_COMPARATOR = new FacetValComparator();

        private class FacetValComparator : IComparer<BrowseFacet>
        {
            public int Compare(BrowseFacet o1, BrowseFacet o2)
            {
                int ret = string.CompareOrdinal(o1.Value, o2.Value);
                if (o1.Value.StartsWith("-") && o2.Value.StartsWith("-"))
                {
                    ret = -ret;
                }
                return ret;
            }

            public static IDictionary<string, IFacetAccessible> MergeSimpleFacetContainers(IEnumerable<IDictionary<string, IFacetAccessible>> subMaps, BrowseRequest req)
            {
                Dictionary<string, Dictionary<object, BrowseFacet>> counts = new Dictionary<string, Dictionary<object, BrowseFacet>>();
                foreach (Dictionary<string, IFacetAccessible> subMap in subMaps)
                {
                    foreach (KeyValuePair<string, IFacetAccessible> entry in subMap)
                    {
                        Dictionary<object, BrowseFacet> count = counts[entry.Key];
                        if (count == null)
                        {
                            count = new Dictionary<object, BrowseFacet>();
                            counts.Add(entry.Key, count);
                        }
                        foreach (BrowseFacet facet in entry.Value.GetFacets())
                        {
                            string val = facet.Value;
                            BrowseFacet oldValue = count[val];
                            if (oldValue == null)
                            {
                                count.Add(val, new BrowseFacet(val, facet.FacetValueHitCount));
                            }
                            else
                            {
                                oldValue.FacetValueHitCount = oldValue.FacetValueHitCount + facet.FacetValueHitCount;
                            }
                        }
                    }
                }

                Dictionary<string, IFacetAccessible> mergedFacetMap = new Dictionary<string, IFacetAccessible>();

                foreach (string facet in counts.Keys)
                {
                    FacetSpec fs = req.GetFacetSpec(facet);

                    FacetSpec.FacetSortSpec sortSpec = fs.OrderBy;

                    IComparer<BrowseFacet> comparator;
                    if (FacetSpec.FacetSortSpec.OrderValueAsc.Equals(sortSpec))
                    {
                        comparator = FACET_VAL_COMPARATOR;
                    }
                    else if (FacetSpec.FacetSortSpec.OrderHitsDesc.Equals(sortSpec))
                    {
                        comparator = FacetHitcountComparatorFactory.FACET_HITS_COMPARATOR;
                    }
                    else
                    {
                        comparator = fs.CustomComparatorFactory.NewComparator();
                    }

                    Dictionary<object, BrowseFacet> facetValueCounts = counts[facet];
                    BrowseFacet[] facetArray = facetValueCounts.Values.ToArray();
                    Array.Sort(facetArray, comparator);

                    int numToShow = facetArray.Length;
                    if (req != null)
                    {
                        FacetSpec fspec = req.GetFacetSpec(facet);
                        if (fspec != null)
                        {
                            int maxCount = fspec.MaxCount;
                            if (maxCount > 0)
                            {
                                numToShow = Math.Min(maxCount, numToShow);
                            }
                        }
                    }

                    BrowseFacet[] facets;
                    if (numToShow == facetArray.Length)
                    {
                        facets = facetArray;
                    }
                    else
                    {
                        facets = new BrowseFacet[numToShow];
                        Array.Copy(facetArray, 0, facets, 0, numToShow);
                    }

                    MappedFacetAccessible mergedFacetAccessible = new MappedFacetAccessible(facets);
                    mergedFacetMap.Add(facet, mergedFacetAccessible);
                }
                return mergedFacetMap;
            }
        }
    }
}
