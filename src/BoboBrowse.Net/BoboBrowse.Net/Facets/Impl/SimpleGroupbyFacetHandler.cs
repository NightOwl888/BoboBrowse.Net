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
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class SimpleGroupbyFacetHandler : FacetHandler<FacetDataNone>
    {
        private readonly IList<string> m_fieldsSet;
        private IList<SimpleFacetHandler> m_facetHandlers;
        private IDictionary<string, SimpleFacetHandler> m_facetHandlerMap;

        private const string SEP = ",";
        private readonly string m_sep;

        /// <summary>
        /// Initializes a new instance of <see cref="T:SimpleFacetHandler"/> with the specified name, 
        /// dependent facet handler names, and separator.
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="dependsOn">List of facet handler names that will be included in the group.</param>
        /// <param name="separator">The separator string that will be used to delineate each value in the group.</param>
        public SimpleGroupbyFacetHandler(string name, IList<string> dependsOn, string separator)
            : base(name, dependsOn)
        {
            m_fieldsSet = dependsOn;
            m_facetHandlers = null;
            m_facetHandlerMap = null;
            m_sep = separator;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:SimpleFacetHandler"/> with the specified name and 
        /// dependent facet handler names. The separator is assumed to be ",".
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="dependsOn">List of facet handler names that will be included in the group.</param>
        public SimpleGroupbyFacetHandler(string name, IList<string> dependsOn)
            : this(name, dependsOn, SEP)
        {
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> selectionProperty)
        {
            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>();
            string[] vals = value.Split(new string[] { m_sep }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < vals.Length; ++i)
            {
                SimpleFacetHandler handler = m_facetHandlers[i];
                BrowseSelection sel = new BrowseSelection(handler.Name);
                sel.AddValue(vals[i]);
                filterList.Add(handler.BuildFilter(sel));
            }
            return new RandomAccessAndFilter(filterList);
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec)
        {
            return new GroupbyFacetCountCollectorSource(m_facetHandlers, m_name, m_sep, sel, fspec);
        }

        private class GroupbyFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly IList<SimpleFacetHandler> m_facetHandlers;
            private readonly string m_name;
            private readonly string m_sep;
            private readonly BrowseSelection m_sel;
            private readonly FacetSpec m_fspec;

            public GroupbyFacetCountCollectorSource(IList<SimpleFacetHandler> facetHandlers, string name, string sep, BrowseSelection sel, FacetSpec fspec)
            {
                m_facetHandlers = facetHandlers;
                m_name = name;
                m_sep = sep;
                m_sel = sel;
                m_fspec = fspec;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                var collectorList = new List<DefaultFacetCountCollector>(m_facetHandlers.Count);
                foreach (var facetHandler in m_facetHandlers)
                {
                    collectorList.Add((DefaultFacetCountCollector)facetHandler.GetFacetCountCollectorSource(m_sel, m_fspec).GetFacetCountCollector(reader, docBase));
                }
                return new GroupbyFacetCountCollector(m_name, m_fspec, collectorList.ToArray(), reader.MaxDoc, m_sep);
            }
        }

        public override string[] GetFieldValues(BoboSegmentReader reader, int id)
        {
            List<string> valList = new List<string>();
            foreach (IFacetHandler handler in m_facetHandlers)
            {
                StringBuilder buf = new StringBuilder();
                bool firsttime = true;
                string[] vals = handler.GetFieldValues(reader, id);
                if (vals != null && vals.Length > 0)
                {
                    if (!firsttime)
                    {
                        buf.Append(",");
                    }
                    else
                    {
                        firsttime = false;
                    }
                    foreach (string val in vals)
                    {
                        buf.Append(val);
                    }
                }
                valList.Add(buf.ToString());
            }
            return valList.ToArray();
        }

        public override object[] GetRawFieldValues(BoboSegmentReader reader, int id)
        {
            return GetFieldValues(reader, id);
        }

        public override DocComparerSource GetDocComparerSource()
        {
            return new GroupbyDocComparerSource(m_fieldsSet, m_facetHandlers);
        }

        private class GroupbyDocComparerSource : DocComparerSource
        {
            private readonly IList<string> m_fieldsSet;
            private readonly IList<SimpleFacetHandler> m_facetHandlers;

            public GroupbyDocComparerSource(IList<string> fieldsSet, IList<SimpleFacetHandler> facetHandlers)
            {
                m_fieldsSet = fieldsSet;
                m_facetHandlers = facetHandlers;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                var comparerList = new List<DocComparer>(m_fieldsSet.Count);
                foreach (var handler in m_facetHandlers)
                {
                    comparerList.Add(handler.GetDocComparerSource().GetComparer(reader, docbase));
                }
                return new GroupbyDocComparer(comparerList.ToArray());
            }
        }

        public override FacetDataNone Load(BoboSegmentReader reader)
        {
            m_facetHandlers = new List<SimpleFacetHandler>(m_fieldsSet.Count);
            m_facetHandlerMap = new Dictionary<string, SimpleFacetHandler>(m_fieldsSet.Count);
            foreach (string name in m_fieldsSet)
            {
                IFacetHandler handler = reader.GetFacetHandler(name);
                if (handler == null || !(handler is SimpleFacetHandler))
                {
                    throw new InvalidOperationException("only simple facet handlers supported");
                }
                SimpleFacetHandler sfh = (SimpleFacetHandler)handler;
                m_facetHandlers.Add(sfh);
                m_facetHandlerMap.Add(name, sfh);
            }
            return FacetDataNone.Instance;
        }

        private class GroupbyDocComparer : DocComparer
        {
            private readonly DocComparer[] m_comparers;

            public GroupbyDocComparer(DocComparer[] comparers)
            {
                m_comparers = comparers;
            }

            public override sealed int Compare(ScoreDoc d1, ScoreDoc d2)
            {
                int retval = 0;
                foreach (DocComparer comparer in m_comparers)
                {
                    retval = comparer.Compare(d1, d2);
                    if (retval != 0) break;
                }
                return retval;
            }

            public override sealed IComparable Value(ScoreDoc doc)
            {
                return new GroupbyComparable(m_comparers, doc);
            }
        }

        private class GroupbyComparable : IComparable
        {
            private readonly DocComparer[] m_comparers;
            private readonly ScoreDoc m_doc;

            public GroupbyComparable(DocComparer[] comparers, ScoreDoc doc)
            {
                m_comparers = comparers;
                m_doc = doc;
            }

            public virtual int CompareTo(object o)
            {
                int retval = 0;
                foreach (DocComparer comparer in m_comparers)
                {
                    retval = comparer.Value(m_doc).CompareTo(o);
                    if (retval != 0) break;
                }
                return retval;
            }
        }

        private class GroupbyFacetCountCollector : IFacetCountCollector
        {
            private readonly DefaultFacetCountCollector[] m_subcollectors;
            private readonly string m_name;
            private readonly FacetSpec m_fspec;
            private readonly BigSegmentedArray m_count;
            private readonly int m_countlength;
            private readonly int[] m_lens;
            private readonly int m_maxdoc;
            private readonly string m_sep;

            public GroupbyFacetCountCollector(string name, FacetSpec fspec, DefaultFacetCountCollector[] subcollectors, int maxdoc, string sep)
            {
                m_name = name;
                m_fspec = fspec;
                m_subcollectors = subcollectors;
                m_sep = sep;
                int totalLen = 1;
                m_lens = new int[m_subcollectors.Length];
                for (int i = 0; i < m_subcollectors.Length; ++i)
                {
                    m_lens[i] = m_subcollectors[i].CountLength;
                    totalLen *= m_lens[i];
                }
                m_countlength = totalLen;
                m_count = new LazyBigIntArray(m_countlength);
                m_maxdoc = maxdoc;
            }

            public void Collect(int docid)
            {
                int idx = 0;
                int i = 0;
                int segsize = m_countlength;
                foreach (DefaultFacetCountCollector subcollector in m_subcollectors)
                {
                    segsize = segsize / m_lens[i++];
                    idx += (subcollector.DataCache.OrderArray.Get(docid) * segsize);
                }
                m_count.Add(idx, m_count.Get(idx) + 1);
            }

            public virtual void CollectAll()
            {
                for (int i = 0; i < m_maxdoc; ++i)
                {
                    Collect(i);
                }
            }

            public virtual BigSegmentedArray GetCountDistribution()
            {
                return m_count;
            }

            public virtual string Name
            {
                get { return m_name; }
            }

            public virtual BrowseFacet GetFacet(string value)
            {
                string[] vals = value.Split(new string[] { m_sep }, StringSplitOptions.RemoveEmptyEntries);
                if (vals.Length == 0)
                    return null;
                StringBuilder buf = new StringBuilder();
                int startIdx = 0;
                int segLen = m_countlength;

                for (int i = 0; i < vals.Length; ++i)
                {
                    if (i > 0)
                    {
                        buf.Append(m_sep);
                    }
                    int index = m_subcollectors[i].DataCache.ValArray.IndexOf(vals[i]);
                    string facetName = m_subcollectors[i].DataCache.ValArray.Get(index);
                    buf.Append(facetName);

                    segLen /= m_subcollectors[i].CountLength;
                    startIdx += index * segLen;
                }

                int count = 0;
                for (int i = startIdx; i < startIdx + segLen; ++i)
                {
                    count += m_count.Get(i);
                }

                BrowseFacet f = new BrowseFacet(buf.ToString(), count);
                return f;
            }

            public virtual int GetFacetHitsCount(object value)
            {
                string[] vals = ((string)value).Split(new string[] { m_sep }, StringSplitOptions.RemoveEmptyEntries);
                if (vals.Length == 0) return 0;
                int startIdx = 0;
                int segLen = m_countlength;

                for (int i = 0; i < vals.Length; ++i)
                {
                    int index = m_subcollectors[i].DataCache.ValArray.IndexOf(vals[i]);
                    segLen /= m_subcollectors[i].CountLength;
                    startIdx += index * segLen;
                }

                int count = 0;
                for (int i = startIdx; i < startIdx + segLen; ++i)
                    count += m_count.Get(i);

                return count;
            }

            private string GetFacetString(int idx)
            {
                StringBuilder buf = new StringBuilder();
                int i = 0;
                foreach (int len in m_lens)
                {
                    if (i > 0)
                    {
                        buf.Append(m_sep);
                    }

                    int adjusted = idx * len;

                    int bucket = adjusted / m_countlength;
                    buf.Append(m_subcollectors[i].DataCache.ValArray.Get(bucket));
                    idx = adjusted % m_countlength;
                    i++;
                }
                return buf.ToString();
            }

            private object[] GetRawFaceValue(int idx)
            {
                object[] retVal = new object[m_lens.Length];
                int i = 0;
                foreach (int len in m_lens)
                {
                    int adjusted = idx * len;
                    int bucket = adjusted / m_countlength;
                    retVal[i++] = m_subcollectors[i].DataCache.ValArray.GetRawValue(bucket);
                    idx = adjusted % m_countlength;
                }
                return retVal;
            }

            public virtual ICollection<BrowseFacet> GetFacets()
            {
                if (m_fspec != null)
                {
                    int minCount = m_fspec.MinHitCount;
                    int max = m_fspec.MaxCount;
                    if (max <= 0)
                        max = m_countlength;

                    FacetSpec.FacetSortSpec sortspec = m_fspec.OrderBy;
                    List<BrowseFacet> facetColl;
                    if (sortspec == FacetSpec.FacetSortSpec.OrderValueAsc)
                    {
                        facetColl = new List<BrowseFacet>(max);
                        for (int i = 1; i < m_countlength; ++i) // exclude zero
                        {
                            int hits = m_count.Get(i);
                            if (hits >= minCount)
                            {
                                BrowseFacet facet = new BrowseFacet(GetFacetString(i), hits);
                                facetColl.Add(facet);
                            }
                            if (facetColl.Count >= max)
                                break;
                        }
                    }
                    else
                    {
                        IComparerFactory comparerFactory;
                        if (sortspec == FacetSpec.FacetSortSpec.OrderHitsDesc)
                        {
                            comparerFactory = new FacetHitcountComparerFactory();
                        }
                        else
                        {
                            comparerFactory = m_fspec.CustomComparerFactory;
                        }

                        if (comparerFactory == null)
                        {
                            throw new System.ArgumentException("facet comparer factory not specified");
                        }

                        IComparer<int> comparer = comparerFactory.NewComparer(new GroupbyFieldValueAccessor(this.GetFacetString, this.GetRawFaceValue), m_count);
                        facetColl = new List<BrowseFacet>();
                        int forbidden = -1;
                        IntBoundedPriorityQueue pq = new IntBoundedPriorityQueue(comparer, max, forbidden);

                        for (int i = 1; i < m_countlength; ++i) // exclude zero
                        {
                            int hits = m_count.Get(i);
                            if (hits >= minCount)
                            {
                                if (!pq.Offer(i))
                                {
                                    // pq is full. we can safely ignore any facet with <=hits.
                                    minCount = hits + 1;
                                }
                            }
                        }

                        int val;
                        while ((val = pq.Poll()) != forbidden)
                        {
                            BrowseFacet facet = new BrowseFacet(GetFacetString(val), m_count.Get(val));
                            facetColl.Insert(0, facet);
                        }
                    }
                    return facetColl;
                }
                else
                {
                    return FacetCountCollector.EMPTY_FACET_LIST;
                }
            }

            private class GroupbyFieldValueAccessor : IFieldValueAccessor
            {
                private readonly Func<int, string> getFacetString;
                private readonly Func<int, object> getRawFaceValue;

                public GroupbyFieldValueAccessor(Func<int, string> getFacetString, Func<int, object> getRawFaceValue)
                {
                    this.getFacetString = getFacetString;
                    this.getRawFaceValue = getRawFaceValue;
                }

                public string GetFormatedValue(int index)
                {
                    return getFacetString(index);
                }

                public object GetRawValue(int index)
                {
                    return getRawFaceValue(index);
                }
            }

            public virtual void Dispose()
            {
            }

            public virtual FacetIterator GetIterator()
            {
                return new GroupByFacetIterator(this);
            }

            public class GroupByFacetIterator : FacetIterator
            {
                private readonly GroupbyFacetCountCollector m_parent;
                private int m_index;

                public GroupByFacetIterator(GroupbyFacetCountCollector parent)
                {
                    m_parent = parent;
                    m_index = 0;
                    m_facet = null;
                    m_count = 0;
                }

                /// <summary>
                /// (non-Javadoc)
                /// see com.browseengine.bobo.api.FacetIterator#next()
                /// </summary>
                /// <returns></returns>
                public override string Next()
                {
                    if ((m_index >= 0) && !HasNext())
                        throw new IndexOutOfRangeException("No more facets in this iteration");
                    m_index++;
                    m_facet = m_parent.GetFacetString(m_index);
                    m_count = m_parent.m_count.Get(m_index);
                    return m_facet;
                }

                /// <summary>
                /// (non-Javadoc)
                /// see java.util.Iterator#hasNext()
                /// </summary>
                /// <returns></returns>
                public override bool HasNext()
                {
                    return (m_index < (m_parent.m_countlength - 1));
                }

                /// <summary>
                /// (non-Javadoc)
                /// see java.util.Iterator#remove()
                /// </summary>
                public override void Remove()
                {
                    throw new NotSupportedException("remove() method not supported for Facet Iterators");
                }

                /// <summary>
                /// (non-Javadoc)
                /// see com.browseengine.bobo.api.FacetIterator#next(int)
                /// </summary>
                /// <param name="minHits"></param>
                /// <returns></returns>
                public override string Next(int minHits)
                {
                    if ((m_index >= 0) && !HasNext())
                    {
                        m_count = 0;
                        m_facet = null;
                        return null;
                    }
                    do
                    {
                        m_index++;
                    } while ((m_index < (m_parent.m_countlength - 1)) && (m_parent.m_count.Get(m_index) < minHits));
                    if (m_parent.m_count.Get(m_index) >= minHits)
                    {
                        m_facet = m_parent.GetFacetString(m_index);
                        m_count = m_parent.m_count.Get(m_index);
                    }
                    else
                    {
                        m_count = 0;
                        m_facet = null;
                    }
                    return m_facet;
                }

                /// <summary>
                /// The string from here should be already formatted. No need to reformat.
                /// see com.browseengine.bobo.api.FacetIterator#format(java.lang.Object)
                /// </summary>
                /// <param name="val"></param>
                /// <returns></returns>
                public override string Format(object val)
                {
                    return (string)val;
                }
            }
        }
    }
}