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
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Support.Logging;
    using BoboBrowse.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public class PathFacetCountCollector : IFacetCountCollector
    {
        private static readonly ILog log = LogProvider.For<PathFacetCountCollector>();
        private readonly BrowseSelection m_sel;
        protected BigSegmentedArray m_count;
        private readonly string m_name;
        private readonly string m_sep;
        private readonly BigSegmentedArray m_orderArray;
        protected readonly FacetDataCache m_dataCache;
        private readonly IComparerFactory m_comparerFactory;
        private readonly int m_minHitCount;
	    private int m_maxCount;
	    private string[] m_stringData;
	    private readonly char[] m_sepArray;
	    private int m_patStart;
	    private int m_patEnd;

        internal PathFacetCountCollector(string name, string sep, BrowseSelection sel, FacetSpec ospec, FacetDataCache dataCache)
        {
            m_sel = sel;
            m_name = name;
            m_dataCache = dataCache;
            m_sep = sep;
            m_sepArray = sep.ToCharArray();
            m_count = new LazyBigIntArray(m_dataCache.Freqs.Length);
            log.Info(name + ": " + m_count.Length);
            m_orderArray = m_dataCache.OrderArray;
            m_minHitCount = ospec.MinHitCount;
            m_maxCount = ospec.MaxCount;
            if (m_maxCount < 1)
            {
                m_maxCount = m_count.Length;
            }
            FacetSpec.FacetSortSpec sortOption = ospec.OrderBy;
            switch (sortOption)
            {
                case FacetSpec.FacetSortSpec.OrderHitsDesc: 
                    m_comparerFactory = new FacetHitcountComparerFactory(); 
                    break;
                case FacetSpec.FacetSortSpec.OrderValueAsc: 
                    m_comparerFactory = null; 
                    break;
                case FacetSpec.FacetSortSpec.OrderByCustom: 
                    m_comparerFactory = ospec.CustomComparerFactory; 
                    break;
                default: 
                    throw new ArgumentOutOfRangeException("invalid sort option: " + sortOption);
            }
            // Doesn't make much sense to do this, so it is commented.
            // new Regex(_sep, RegexOptions.Compiled);
            m_stringData = new string[10];
            m_patStart = 0;
            m_patEnd = 0;
        }

        public virtual BigSegmentedArray GetCountDistribution()
        {
            return m_count;
        }

        public virtual string Name
        {
            get { return m_name; }
        }

        public virtual void Collect(int docid)
        {
            int i = m_orderArray.Get(docid);
            m_count.Add(i, m_count.Get(i) + 1);
        }

        public virtual void CollectAll()
        {
            m_count = BigIntArray.FromArray(m_dataCache.Freqs);
        }

        public virtual BrowseFacet GetFacet(string @value)
        {
            return null;
        }

        public virtual int GetFacetHitsCount(object value)
        {
            return 0;
        }

        private void EnsureCapacity(int minCapacity)
        {
            int oldCapacity = m_stringData.Length;
            if (minCapacity > oldCapacity)
            {
                string[] oldData = m_stringData;
                int newCapacity = (oldCapacity * 3) / 2 + 1;
                if (newCapacity < minCapacity)
                    newCapacity = minCapacity;
                // minCapacity is usually close to size, so this is a win:
                m_stringData = new string[newCapacity];
                Array.Copy(oldData, 0, m_stringData, Math.Min(oldData.Length, newCapacity), newCapacity);
            }
        }

        private int PatListSize()
        {
            return (m_patEnd - m_patStart);
        }

        public virtual bool SplitString(string input)
        {
            m_patStart = 0;
            m_patEnd = 0;
            char[] str = input.ToCharArray();
            int index = 0;
            int sepindex = 0;
            int tokStart = -1;
            int tokEnd = 0;
            while (index < input.Length)
            {
                for (sepindex = 0; (sepindex < m_sepArray.Length) 
                    && (str[index + sepindex] == m_sepArray[sepindex]); sepindex++) 
                    ;
                if (sepindex == m_sepArray.Length)
                {
                    index += m_sepArray.Length;
                    if (tokStart >= 0)
                    {
                        EnsureCapacity(m_patEnd + 1);
                        tokEnd++;
                        m_stringData[m_patEnd++] = input.Substring(tokStart, tokEnd - tokStart);
                    }
                    tokStart = -1;
                }
                else
                {
                    if (tokStart < 0)
                    {
                        tokStart = index;
                        tokEnd = index;
                    }
                    else
                    {
                        tokEnd++;
                    }
                    index++;
                }
            }

            if (m_patEnd == 0)
                return false;

            if (tokStart >= 0)
            {
                EnsureCapacity(m_patEnd + 1);
                tokEnd++;
                m_stringData[m_patEnd++] = input.Substring(tokStart, tokEnd - tokStart);
            }

            // let gc do its job 
            str = null;

            // Construct result
            while (m_patEnd > 0 && m_stringData[PatListSize() - 1].Equals(""))
            {
                m_patEnd--;
            }
            return true;
        }

        private ICollection<BrowseFacet> GetFacetsForPath(string selectedPath, int depth, bool strict, int minCount, int maxCount)
        {
            List<BrowseFacet> list = new List<BrowseFacet>();

            BoundedPriorityQueue<BrowseFacet> pq = null;
            if (m_comparerFactory != null)
            {
                IComparer<BrowseFacet> comparer = m_comparerFactory.NewComparer();

                pq = new BoundedPriorityQueue<BrowseFacet>(new PathFacetCountCollectorComparer(comparer), maxCount);
            }

            string[] startParts = null;
            int startDepth = 0;

            if (selectedPath != null && selectedPath.Length > 0)
            {
                startParts = selectedPath.Split(new string[] { m_sep }, StringSplitOptions.RemoveEmptyEntries);
                startDepth = startParts.Length;
                if (!selectedPath.EndsWith(m_sep))
                {
                    selectedPath += m_sep;
                }
            }

            string currentPath = null;
            int currentCount = 0;

            int wantedDepth = startDepth + depth;

            int index = 0;
            if (selectedPath != null && selectedPath.Length > 0)
            {
                index = m_dataCache.ValArray.IndexOf(selectedPath);
                if (index < 0)
                {
                    index = -(index + 1);
                }
            }

            StringBuilder buf = new StringBuilder();
            for (int i = index; i < m_count.Length; ++i)
            {
                if (m_count.Get(i) >= minCount)
                {
                    string path = m_dataCache.ValArray.Get(i);
                    //if (path==null || path.equals(selectedPath)) continue;						

                    int subCount = m_count.Get(i);

                    // do not use Java split string in a loop !
                    //				string[] pathParts=path.split(_sep);
                    int pathDepth = 0;
                    if (!SplitString(path))
                    {
                        pathDepth = 0;
                    }
                    else
                    {
                        pathDepth = PatListSize();
                    }

                    int tmpdepth = 0;
                    if ((startDepth == 0) || (startDepth > 0 && path.StartsWith(selectedPath)))
                    {
                        buf = new StringBuilder();

                        int minDepth = Math.Min(wantedDepth, pathDepth);
                        tmpdepth = 0;
                        for (int k = m_patStart; ((k < m_patEnd) && (tmpdepth < minDepth)); ++k, tmpdepth++)
                        {
                            buf.Append(m_stringData[k]);
                            if (!m_stringData[k].EndsWith(m_sep))
                            {
                                if (pathDepth != wantedDepth || k < (wantedDepth - 1))
                                    buf.Append(m_sep);
                            }
                        }
                        string wantedPath = buf.ToString();
                        if (currentPath == null)
                        {
                            currentPath = wantedPath;
                            currentCount = subCount;
                        }
                        else if (wantedPath.Equals(currentPath))
                        {
                            if (!strict)
                            {
                                currentCount += subCount;
                            }
                        }
                        else
                        {
                            bool directNode = false;

                            if (wantedPath.EndsWith(m_sep))
                            {
                                if (currentPath.Equals(wantedPath.Substring(0, wantedPath.Length - 1)))
                                {
                                    directNode = true;
                                }
                            }

                            if (strict)
                            {
                                if (directNode)
                                {
                                    currentCount += subCount;
                                }
                                else
                                {
                                    BrowseFacet ch = new BrowseFacet(currentPath, currentCount);
                                    if (pq != null)
                                    {
                                        pq.Add(ch);
                                    }
                                    else
                                    {
                                        if (list.Count < maxCount)
                                        {
                                            list.Add(ch);
                                        }
                                    }
                                    currentPath = wantedPath;
                                    currentCount = subCount;
                                }
                            }
                            else
                            {
                                if (!directNode)
                                {
                                    BrowseFacet ch = new BrowseFacet(currentPath, currentCount);
                                    if (pq != null)
                                    {
                                        pq.Add(ch);
                                    }
                                    else
                                    {
                                        if (list.Count < maxCount)
                                        {
                                            list.Add(ch);
                                        }
                                    }
                                    currentPath = wantedPath;
                                    currentCount = subCount;
                                }
                                else
                                {
                                    currentCount += subCount;
                                }
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (currentPath != null && currentCount > 0)
            {
                BrowseFacet ch = new BrowseFacet(currentPath, currentCount);
                if (pq != null)
                {
                    pq.Add(ch);
                }
                else
                {
                    if (list.Count < maxCount)
                    {
                        list.Add(ch);
                    }
                }
            }

            if (pq != null)
            {
                BrowseFacet val;
                while ((val = pq.Poll()) != null)
                {
                    list.Insert(0, val);
                }
            }

            return list;
        }

        private class PathFacetCountCollectorComparer : IComparer<BrowseFacet>
        {
            private readonly IComparer<BrowseFacet> m_comparer;

            public PathFacetCountCollectorComparer(IComparer<BrowseFacet> comparer)
            {
                m_comparer = comparer;
            }

            public virtual int Compare(BrowseFacet o1, BrowseFacet o2)
            {
                return -m_comparer.Compare(o1, o2);
            }
        }

        public virtual ICollection<BrowseFacet> GetFacets()
        {
            IDictionary<string, string> props = m_sel == null ? null : m_sel.SelectionProperties;
            int depth = PathFacetHandler.GetDepth(props);
            bool strict = PathFacetHandler.IsStrict(props);

            string[] paths = m_sel == null ? null : m_sel.Values;
            if (paths == null || paths.Length == 0)
            {
                return GetFacetsForPath(null, depth, strict, m_minHitCount, m_maxCount);
            }

            if (paths.Length == 1) return GetFacetsForPath(paths[0], depth, strict, m_minHitCount, m_maxCount);

            List<BrowseFacet> finalList = new List<BrowseFacet>();
            var iterList = new List<IEnumerator<BrowseFacet>>(paths.Length);
            foreach (string path in paths)
            {
                var subList = GetFacetsForPath(path, depth, strict, m_minHitCount, m_maxCount);
                if (subList.Count > 0)
                {
                    iterList.Add(subList.GetEnumerator());
                }
            }

            var finalIter = ListMerger.MergeLists(iterList.ToArray(),
                m_comparerFactory == null ? new FacetValueComparerFactory().NewComparer() : m_comparerFactory.NewComparer());
            while (finalIter.MoveNext())
            {
                BrowseFacet f = finalIter.Current;
                finalList.Insert(0, f);
            }
            return finalList;
        }

        public virtual void Dispose()
        { }

        public virtual FacetIterator GetIterator()
        {
            IDictionary<string, string> props = m_sel == null ? null : m_sel.SelectionProperties;
            int depth = PathFacetHandler.GetDepth(props);
            bool strict = PathFacetHandler.IsStrict(props);
            List<BrowseFacet> finalList;

            string[] paths = m_sel == null ? null : m_sel.Values;
            if (paths == null || paths.Length == 0)
            {
                finalList = new List<BrowseFacet>(GetFacetsForPath(null, depth, strict, int.MinValue, m_count.Length));
                return new PathFacetIterator(finalList);
            }

            if (paths.Length == 1)
            {
                finalList = new List<BrowseFacet>(GetFacetsForPath(paths[0], depth, strict, int.MinValue, m_count.Length));
                return new PathFacetIterator(finalList);
            }

            finalList = new List<BrowseFacet>();
            var iterList = new List<IEnumerator<BrowseFacet>>(paths.Length);
            foreach (string path in paths)
            {
                var subList = GetFacetsForPath(path, depth, strict, int.MinValue, m_count.Length);
                if (subList.Count > 0)
                {
                    iterList.Add(subList.GetEnumerator());
                }
            }
            var finalIter = ListMerger.MergeLists(iterList.ToArray(),
                m_comparerFactory == null ? new FacetValueComparerFactory().NewComparer() : m_comparerFactory.NewComparer());

            while (finalIter.MoveNext())
            {
                BrowseFacet f = finalIter.Current;
                finalList.Add(f);
            }
            return new PathFacetIterator(finalList);
        }
    }
}
