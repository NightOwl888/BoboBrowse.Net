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
        private readonly IEnumerable<string> _fieldsSet;
        private IList<SimpleFacetHandler> _facetHandlers;
        private IDictionary<string, SimpleFacetHandler> _facetHandlerMap;

        private const string SEP = ",";
        private readonly string _sep;

        /// <summary>
        /// Initializes a new instance of <see cref="T:SimpleFacetHandler"/> with the specified name, 
        /// dependent facet handler names, and separator.
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="dependsOn">List of facet handler names that will be included in the group.</param>
        /// <param name="separator">The separator string that will be used to delineate each value in the group.</param>
        public SimpleGroupbyFacetHandler(string name, IEnumerable<string> dependsOn, string separator)
            : base(name, dependsOn)
        {
            _fieldsSet = dependsOn;
            _facetHandlers = null;
            _facetHandlerMap = null;
            _sep = separator;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:SimpleFacetHandler"/> with the specified name and 
        /// dependent facet handler names. The separator is assumed to be ",".
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="dependsOn">List of facet handler names that will be included in the group.</param>
        public SimpleGroupbyFacetHandler(string name, IEnumerable<string> dependsOn)
            : this(name, dependsOn, SEP)
        {
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> selectionProperty)
        {
            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>();
            string[] vals = value.Split(new string[] { _sep }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < vals.Length; ++i)
            {
                SimpleFacetHandler handler = _facetHandlers[i];
                BrowseSelection sel = new BrowseSelection(handler.Name);
                sel.AddValue(vals[i]);
                filterList.Add(handler.BuildFilter(sel));
            }
            return new RandomAccessAndFilter(filterList);
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec)
        {
            return new GroupbyFacetCountCollectorSource(_facetHandlers, _name, _sep, sel, fspec);
        }

        private class GroupbyFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly IEnumerable<SimpleFacetHandler> _facetHandlers;
            private readonly string _name;
            private readonly string _sep;
            private readonly BrowseSelection _sel;
            private readonly FacetSpec _fspec;

            public GroupbyFacetCountCollectorSource(IEnumerable<SimpleFacetHandler> facetHandlers, string name, string sep, BrowseSelection sel, FacetSpec fspec)
            {
                _facetHandlers = facetHandlers;
                _name = name;
                _sep = sep;
                _sel = sel;
                _fspec = fspec;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                var collectorList = new List<DefaultFacetCountCollector>(_facetHandlers.Count());
                foreach (var facetHandler in _facetHandlers)
                {
                    collectorList.Add((DefaultFacetCountCollector)facetHandler.GetFacetCountCollectorSource(_sel, _fspec).GetFacetCountCollector(reader, docBase));
                }
                return new GroupbyFacetCountCollector(_name, _fspec, collectorList.ToArray(), reader.MaxDoc, _sep);
            }
        }

        public override string[] GetFieldValues(BoboSegmentReader reader, int id)
        {
            List<string> valList = new List<string>();
            foreach (IFacetHandler handler in _facetHandlers)
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

        public override DocComparatorSource GetDocComparatorSource()
        {
            return new GroupbyDocComparatorSource(_fieldsSet, _facetHandlers);
        }

        private class GroupbyDocComparatorSource : DocComparatorSource
        {
            private readonly IEnumerable<string> _fieldsSet;
            private readonly IEnumerable<SimpleFacetHandler> _facetHandlers;

            public GroupbyDocComparatorSource(IEnumerable<string> fieldsSet, IEnumerable<SimpleFacetHandler> facetHandlers)
            {
                _fieldsSet = fieldsSet;
                _facetHandlers = facetHandlers;
            }

            public override DocComparator GetComparator(AtomicReader reader, int docbase)
            {
                var comparatorList = new List<DocComparator>(_fieldsSet.Count());
                foreach (var handler in _facetHandlers)
                {
                    comparatorList.Add(handler.GetDocComparatorSource().GetComparator(reader, docbase));
                }
                return new GroupbyDocComparator(comparatorList.ToArray());
            }
        }

        public override FacetDataNone Load(BoboSegmentReader reader)
        {
            _facetHandlers = new List<SimpleFacetHandler>(_fieldsSet.Count());
            _facetHandlerMap = new Dictionary<string, SimpleFacetHandler>(_fieldsSet.Count());
            foreach (string name in _fieldsSet)
            {
                IFacetHandler handler = reader.GetFacetHandler(name);
                if (handler == null || !(handler is SimpleFacetHandler))
                {
                    throw new InvalidOperationException("only simple facet handlers supported");
                }
                SimpleFacetHandler sfh = (SimpleFacetHandler)handler;
                _facetHandlers.Add(sfh);
                _facetHandlerMap.Add(name, sfh);
            }
            return FacetDataNone.Instance;
        }

        private class GroupbyDocComparator : DocComparator
        {
            private readonly DocComparator[] _comparators;

            public GroupbyDocComparator(DocComparator[] comparators)
            {
                _comparators = comparators;
            }

            public override sealed int Compare(ScoreDoc d1, ScoreDoc d2)
            {
                int retval = 0;
                foreach (DocComparator comparator in _comparators)
                {
                    retval = comparator.Compare(d1, d2);
                    if (retval != 0) break;
                }
                return retval;
            }

            public override sealed IComparable Value(ScoreDoc doc)
            {
                return new GroupbyComparable(_comparators, doc);
            }
        }

        private class GroupbyComparable : IComparable
        {
            private readonly DocComparator[] _comparators;
            private readonly ScoreDoc _doc;

            public GroupbyComparable(DocComparator[] comparators, ScoreDoc doc)
            {
                _comparators = comparators;
                _doc = doc;
            }

            public virtual int CompareTo(object o)
            {
                int retval = 0;
                foreach (DocComparator comparator in _comparators)
                {
                    retval = comparator.Value(_doc).CompareTo(o);
                    if (retval != 0) break;
                }
                return retval;
            }
        }

        private class GroupbyFacetCountCollector : IFacetCountCollector
        {
            private readonly DefaultFacetCountCollector[] _subcollectors;
            private readonly string _name;
            private readonly FacetSpec _fspec;
            private readonly BigSegmentedArray _count;
            private readonly int _countlength;
            private readonly int[] _lens;
            private readonly int _maxdoc;
            private readonly string _sep;

            public GroupbyFacetCountCollector(string name, FacetSpec fspec, DefaultFacetCountCollector[] subcollectors, int maxdoc, string sep)
            {
                _name = name;
                _fspec = fspec;
                _subcollectors = subcollectors;
                _sep = sep;
                int totalLen = 1;
                _lens = new int[_subcollectors.Length];
                for (int i = 0; i < _subcollectors.Length; ++i)
                {
                    _lens[i] = _subcollectors[i].CountLength;
                    totalLen *= _lens[i];
                }
                _countlength = totalLen;
                _count = new LazyBigIntArray(_countlength);
                _maxdoc = maxdoc;
            }

            public void Collect(int docid)
            {
                int idx = 0;
                int i = 0;
                int segsize = _countlength;
                foreach (DefaultFacetCountCollector subcollector in _subcollectors)
                {
                    segsize = segsize / _lens[i++];
                    idx += (subcollector.DataCache.OrderArray.Get(docid) * segsize);
                }
                _count.Add(idx, _count.Get(idx) + 1);
            }

            public virtual void CollectAll()
            {
                for (int i = 0; i < _maxdoc; ++i)
                {
                    Collect(i);
                }
            }

            public virtual BigSegmentedArray GetCountDistribution()
            {
                return _count;
            }

            public virtual string Name
            {
                get { return _name; }
            }

            public virtual BrowseFacet GetFacet(string value)
            {
                string[] vals = value.Split(new string[] { _sep }, StringSplitOptions.RemoveEmptyEntries);
                if (vals.Length == 0)
                    return null;
                StringBuilder buf = new StringBuilder();
                int startIdx = 0;
                int segLen = _countlength;

                for (int i = 0; i < vals.Length; ++i)
                {
                    if (i > 0)
                    {
                        buf.Append(_sep);
                    }
                    int index = _subcollectors[i].DataCache.ValArray.IndexOf(vals[i]);
                    string facetName = _subcollectors[i].DataCache.ValArray.Get(index);
                    buf.Append(facetName);

                    segLen /= _subcollectors[i].CountLength;
                    startIdx += index * segLen;
                }

                int count = 0;
                for (int i = startIdx; i < startIdx + segLen; ++i)
                {
                    count += _count.Get(i);
                }

                BrowseFacet f = new BrowseFacet(buf.ToString(), count);
                return f;
            }

            public virtual int GetFacetHitsCount(object value)
            {
                string[] vals = ((string)value).Split(new string[] { _sep }, StringSplitOptions.RemoveEmptyEntries);
                if (vals.Length == 0) return 0;
                int startIdx = 0;
                int segLen = _countlength;

                for (int i = 0; i < vals.Length; ++i)
                {
                    int index = _subcollectors[i].DataCache.ValArray.IndexOf(vals[i]);
                    segLen /= _subcollectors[i].CountLength;
                    startIdx += index * segLen;
                }

                int count = 0;
                for (int i = startIdx; i < startIdx + segLen; ++i)
                    count += _count.Get(i);

                return count;
            }

            private string GetFacetString(int idx)
            {
                StringBuilder buf = new StringBuilder();
                int i = 0;
                foreach (int len in _lens)
                {
                    if (i > 0)
                    {
                        buf.Append(_sep);
                    }

                    int adjusted = idx * len;

                    int bucket = adjusted / _countlength;
                    buf.Append(_subcollectors[i].DataCache.ValArray.Get(bucket));
                    idx = adjusted % _countlength;
                    i++;
                }
                return buf.ToString();
            }

            private object[] GetRawFaceValue(int idx)
            {
                object[] retVal = new object[_lens.Length];
                int i = 0;
                foreach (int len in _lens)
                {
                    int adjusted = idx * len;
                    int bucket = adjusted / _countlength;
                    retVal[i++] = _subcollectors[i].DataCache.ValArray.GetRawValue(bucket);
                    idx = adjusted % _countlength;
                }
                return retVal;
            }

            public virtual IEnumerable<BrowseFacet> GetFacets()
            {
                if (_fspec != null)
                {
                    int minCount = _fspec.MinHitCount;
                    int max = _fspec.MaxCount;
                    if (max <= 0)
                        max = _countlength;

                    FacetSpec.FacetSortSpec sortspec = _fspec.OrderBy;
                    List<BrowseFacet> facetColl;
                    if (sortspec == FacetSpec.FacetSortSpec.OrderValueAsc)
                    {
                        facetColl = new List<BrowseFacet>(max);
                        for (int i = 1; i < _countlength; ++i) // exclude zero
                        {
                            int hits = _count.Get(i);
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
                        IComparatorFactory comparatorFactory;
                        if (sortspec == FacetSpec.FacetSortSpec.OrderHitsDesc)
                        {
                            comparatorFactory = new FacetHitcountComparatorFactory();
                        }
                        else
                        {
                            comparatorFactory = _fspec.CustomComparatorFactory;
                        }

                        if (comparatorFactory == null)
                        {
                            throw new System.ArgumentException("facet comparator factory not specified");
                        }

                        IComparer<int> comparator = comparatorFactory.NewComparator(new GroupbyFieldValueAccessor(this.GetFacetString, this.GetRawFaceValue), _count);
                        facetColl = new List<BrowseFacet>();
                        int forbidden = -1;
                        IntBoundedPriorityQueue pq = new IntBoundedPriorityQueue(comparator, max, forbidden);

                        for (int i = 1; i < _countlength; ++i) // exclude zero
                        {
                            int hits = _count.Get(i);
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
                            BrowseFacet facet = new BrowseFacet(GetFacetString(val), _count.Get(val));
                            facetColl.Insert(0, facet);
                        }
                    }
                    return facetColl;
                }
                else
                {
                    return FacetCountCollector_Fields.EMPTY_FACET_LIST;
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

            public virtual FacetIterator Iterator()
            {
                return new GroupByFacetIterator(this);
            }

            public class GroupByFacetIterator : FacetIterator
            {
                private readonly GroupbyFacetCountCollector _parent;
                private int _index;

                public GroupByFacetIterator(GroupbyFacetCountCollector parent)
                {
                    _parent = parent;
                    _index = 0;
                    facet = null;
                    count = 0;
                }

                /// <summary>
                /// (non-Javadoc)
                /// see com.browseengine.bobo.api.FacetIterator#next()
                /// </summary>
                /// <returns></returns>
                public override string Next()
                {
                    if ((_index >= 0) && !HasNext())
                        throw new IndexOutOfRangeException("No more facets in this iteration");
                    _index++;
                    facet = _parent.GetFacetString(_index);
                    count = _parent._count.Get(_index);
                    return facet;
                }

                /// <summary>
                /// (non-Javadoc)
                /// see java.util.Iterator#hasNext()
                /// </summary>
                /// <returns></returns>
                public override bool HasNext()
                {
                    return (_index < (_parent._countlength - 1));
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
                    if ((_index >= 0) && !HasNext())
                    {
                        count = 0;
                        facet = null;
                        return null;
                    }
                    do
                    {
                        _index++;
                    } while ((_index < (_parent._countlength - 1)) && (_parent._count.Get(_index) < minHits));
                    if (_parent._count.Get(_index) >= minHits)
                    {
                        facet = _parent.GetFacetString(_index);
                        count = _parent._count.Get(_index);
                    }
                    else
                    {
                        count = 0;
                        facet = null;
                    }
                    return facet;
                }

                /// <summary>
                /// The string from here should be already formatted. No need to reformat.
                /// see com.browseengine.bobo.api.FacetIterator#format(java.lang.Object)
                /// </summary>
                /// <param name="val"></param>
                /// <returns></returns>
                public override string Format(Object val)
                {
                    return (string)val;
                }
            }
        }
    }
}