// TODO: Work out how to make this function with Lucene.Net 3.0.3.

//namespace BoboBrowse.Net.Facets.Impl
//{
//    using BoboBrowse.Net;
//    using BoboBrowse.Net.Facets.Filter;
//    using BoboBrowse.Net.Util;
//    using C5;
//    using Lucene.Net.Search;
//    using System;
//    using System.Collections.Generic;
//    using System.Text;

//    public class SimpleGroupbyFacetHandler : FacetHandler, IFacetHandlerFactory
//    {
//        private readonly C5.HashedLinkedList<string> _fieldsSet;
//        private List<SimpleFacetHandler> _facetHandlers;
//        private Dictionary<string, SimpleFacetHandler> _facetHandlerMap;

//        private const string SEP = ",";
//        private int _maxdoc;
//        private readonly string _sep;

//        public SimpleGroupbyFacetHandler(string name, HashedLinkedList<string> dependsOn, string separator)
//            : base(name, dependsOn)
//        {
//            _fieldsSet = dependsOn;
//            _facetHandlers = null;
//            _facetHandlerMap = null;
//            _maxdoc = 0;
//            _sep = separator;
//        }

//        public SimpleGroupbyFacetHandler(string name, HashedLinkedList<string> dependsOn)
//            : this(name, dependsOn, SEP)
//        {
//        }

//        public override RandomAccessFilter BuildRandomAccessFilter(string @value, Properties selectionProperty)
//        {
//            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>();
//            string[] vals = @value.Split(new string[] {_sep}, StringSplitOptions.RemoveEmptyEntries);
//            for (int i = 0; i < vals.Length; ++i)
//            {
//                SimpleFacetHandler handler = _facetHandlers[i];
//                BrowseSelection sel = new BrowseSelection(handler.Name);
//                sel.AddValue(vals[i]);
//                filterList.Add(handler.BuildFilter(sel));
//            }
//            return new RandomAccessAndFilter(filterList);
//        }

//        public override IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec fspec)
//        {
//            List<DefaultFacetCountCollector> collectorList = new List<DefaultFacetCountCollector>(_facetHandlers.Count);
//            foreach (SimpleFacetHandler facetHandler in _facetHandlers)
//            {
//                collectorList.Add((DefaultFacetCountCollector)facetHandler.GetFacetCountCollector(sel, fspec));
//            }
//            return new GroupbyFacetCountCollector(Name, fspec, collectorList.ToArray(), _maxdoc, _sep);
//        }

//        public override string[] GetFieldValues(int id)
//        {
//            List<string> valList = new List<string>();
//            foreach (FacetHandler handler in _facetHandlers)
//            {
//                StringBuilder buf = new StringBuilder();
//                bool firsttime = true;
//                string[] vals = handler.GetFieldValues(id);
//                if (vals != null && vals.Length > 0)
//                {
//                    if (!firsttime)
//                    {
//                        buf.Append(",");
//                    }
//                    else
//                    {
//                        firsttime = false;
//                    }
//                    foreach (string val in vals)
//                    {
//                        buf.Append(val);
//                    }
//                }
//                valList.Add(buf.ToString());
//            }
//            return valList.ToArray();
//        }

//        public override object[] GetRawFieldValues(int id)
//        {
//            return GetFieldValues(id);
//        }

//        //public override ScoreDocComparator GetScoreDocComparator()
//        //{
//        //    List<ScoreDocComparator> comparatorList = new List<ScoreDocComparator>(_fieldsSet.Count);
//        //    foreach (FacetHandler handler in _facetHandlers)
//        //    {
//        //        comparatorList.Add(handler.GetScoreDocComparator());
//        //    }
//        //    return new GroupbyScoreDocComparator(comparatorList.ToArray());
//        //}

//        public override FieldComparator GetComparator(int numDocs, SortField field)
//        {
//            var comparatorList = new List<FieldComparator>(_fieldsSet.Count);
//            foreach (var handler in _facetHandlers)
//            {
//                comparatorList.Add(handler.GetComparator(numDocs, field));
//            }
//            return new GroupbyScoreDocComparator(
//        }

//        public override void Load(BoboIndexReader reader)
//        {
//            _facetHandlers = new List<SimpleFacetHandler>(_fieldsSet.Count);
//            _facetHandlerMap = new Dictionary<string, SimpleFacetHandler>(_fieldsSet.Count);
//            foreach (string name in _fieldsSet)
//            {
//                FacetHandler handler = reader.GetFacetHandler(name);
//                if (handler == null || !(handler is SimpleFacetHandler))
//                {
//                    throw new InvalidOperationException("only simple facet handlers supported");
//                }
//                SimpleFacetHandler sfh = (SimpleFacetHandler)handler;
//                _facetHandlers.Add(sfh);
//                _facetHandlerMap.Add(name, sfh);
//            }
//            _maxdoc = reader.MaxDoc;
//        }

//        public virtual FacetHandler NewInstance()
//        {
//            return new SimpleGroupbyFacetHandler(Name, _fieldsSet);
//        }

//        private class GroupByFieldComparator : FieldComparator
//        {
//            private FieldComparator[] _comparators;

//            public GroupByFieldComparator(FieldComparator[] comparators)
//            {
//                _comparators = comparators;
//            }

//            public override int Compare(int slot1, int slot2)
//            {
//                int retval = 0;
//                foreach (var comparator in _comparators)
//                {
//                    retval = comparator.Compare(slot1, slot2);
//                    if (retval != 0)
//                        break;
//                }
//                return retval;
//            }

//            public int SortType()
//            {
//                return SortField.CUSTOM;
//            }

//            private class GroupbyScoreFieldComparatorComparable : IComparable
//            {
//                private ScoreDoc doc;
//                private GroupByFieldComparator parent;

//                public GroupbyScoreFieldComparatorComparable(GroupByFieldComparator parent, ScoreDoc doc)
//                {
//                    this.parent = parent;
//                    this.doc = doc;
//                }

                

//                public int CompareTo(object obj)
//                {
//                    int retval = 0;
//                    foreach (var comparator in parent._comparators)
//                    {
//                        retval = comparator.SortValue(doc).CompareTo(obj);

//                        if (retval != 0)
//                            break;
//                    }
//                    return retval;
//                }

//                //public int CompareTo(ScoreDoc other)
//                //{
//                //    int retval = 0;
//                //    foreach (var comparator in parent._comparators)
//                //    {
//                //        //retval = comparator.SortValue(doc).CompareTo(obj);
//                //        retval = 
//                //        if (retval != 0)
//                //            break;
//                //    }
//                //    return retval
//                //}
//            }

//            public IComparable SortValue(ScoreDoc doc)
//            {
//                return new GroupbyScoreFieldComparatorComparable(this, doc);
//            }
//        }

//        //private class GroupbyScoreDocComparator : ScoreDocComparator
//        //{
//        //    private ScoreDocComparator[] _comparators;

//        //    public GroupbyScoreDocComparator(ScoreDocComparator[] comparators)
//        //    {
//        //        _comparators = comparators;
//        //    }

//        //    public int Compare(ScoreDoc d1, ScoreDoc d2)
//        //    {
//        //        int retval = 0;
//        //        foreach (ScoreDocComparator comparator in _comparators)
//        //        {
//        //            retval = comparator.Compare(d1, d2);
//        //            if (retval != 0)
//        //                break;
//        //        }
//        //        return retval;
//        //    }

//        //    public int SortType()
//        //    {
//        //        return SortField.CUSTOM;
//        //    }

//        //    private class GroupbyScoreDocComparatorComparable : IComparable
//        //    {
//        //        private ScoreDoc doc;
//        //        private GroupbyScoreDocComparator parent;

//        //        public GroupbyScoreDocComparatorComparable(GroupbyScoreDocComparator parent, ScoreDoc doc)
//        //        {
//        //            this.parent = parent;
//        //        }

//        //        public int CompareTo(object obj)
//        //        {
//        //            int retval = 0;
//        //            foreach (ScoreDocComparator comparator in parent._comparators)
//        //            {
//        //                retval = comparator.SortValue(doc).CompareTo(obj);
//        //                if (retval != 0)
//        //                    break;
//        //            }
//        //            return retval;
//        //        }
//        //    }


//        //    public IComparable SortValue(ScoreDoc doc)
//        //    {
//        //        return new GroupbyScoreDocComparatorComparable(this, doc);
//        //    }
//        //}

//        private class GroupbyFacetCountCollector : IFacetCountCollector
//        {
//            private readonly DefaultFacetCountCollector[] _subcollectors;
//            private readonly string _name;
//            private readonly FacetSpec _fspec;
//            private readonly int[] _count;
//            private readonly int[] _lens;
//            private readonly int _maxdoc;
//            private readonly string _sep;

//            public GroupbyFacetCountCollector(string name, FacetSpec fspec, DefaultFacetCountCollector[] subcollectors, int maxdoc, string sep)
//            {
//                _name = name;
//                _fspec = fspec;
//                _subcollectors = subcollectors;
//                _sep = sep;
//                int totalLen = 1;
//                _lens = new int[_subcollectors.Length];
//                for (int i = 0; i < _subcollectors.Length; ++i)
//                {
//                    _lens[i] = _subcollectors[i]._count.Length;
//                    totalLen *= _lens[i];
//                }
//                _count = new int[totalLen];
//                _maxdoc = maxdoc;
//            }

//            public void Collect(int docid)
//            {
//                int idx = 0;
//                int i = 0;
//                int segsize = _count.Length;
//                foreach (DefaultFacetCountCollector subcollector in _subcollectors)
//                {
//                    segsize = segsize / _lens[i++];
//                    idx += (subcollector._dataCache.orderArray.Get(docid) * segsize);
//                }
//                _count[idx]++;
//            }

//            public virtual void CollectAll()
//            {
//                for (int i = 0; i < _maxdoc; ++i)
//                {
//                    Collect(i);
//                }
//            }

//            public virtual int[] GetCountDistribution()
//            {
//                return _count;
//            }

//            public virtual string Name
//            {
//                get
//                {
//                    return _name;
//                }
//            }

//            public virtual BrowseFacet GetFacet(string @value)
//            {
//                string[] vals = @value.Split(new string[] {_sep}, StringSplitOptions.RemoveEmptyEntries);
//                if (vals.Length == 0)
//                    return null;
//                StringBuilder buf = new StringBuilder();
//                int startIdx = 0;
//                int segLen = _count.Length;

//                for (int i = 0; i < vals.Length; ++i)
//                {
//                    if (i > 0)
//                    {
//                        buf.Append(_sep);
//                    }
//                    int index = _subcollectors[i]._dataCache.valArray.IndexOf(vals[i]);
//                    string facetName = _subcollectors[i]._dataCache.valArray.Get(index);
//                    buf.Append(facetName);

//                    segLen /= _subcollectors[i]._count.Length;
//                    startIdx += index * segLen;
//                }

//                int count = _count[startIdx];
//                for (int i = startIdx; i < startIdx + segLen; ++i)
//                {
//                    count += _count[i];
//                }

//                BrowseFacet f = new BrowseFacet(buf.ToString(), count);
//                return f;
//            }

//            private string getFacetString(int idx)
//            {
//                StringBuilder buf = new StringBuilder();
//                int i = 0;
//                foreach (int len in _lens)
//                {
//                    if (i > 0)
//                    {
//                        buf.Append(_sep);
//                    }

//                    int adjusted = idx * len;

//                    int bucket = adjusted / _count.Length;
//                    buf.Append(_subcollectors[i]._dataCache.valArray.Get(bucket));
//                    idx = adjusted % _count.Length;
//                    i++;
//                }
//                return buf.ToString();
//            }

//            private object[] getRawFaceValue(int idx)
//            {
//                object[] retVal = new object[_lens.Length];
//                int i = 0;
//                foreach (int len in _lens)
//                {
//                    int adjusted = idx * len;
//                    int bucket = adjusted / _count.Length;
//                    retVal[i++] = _subcollectors[i]._dataCache.valArray.GetRawValue(bucket);
//                    idx = adjusted % _count.Length;
//                }
//                return retVal;
//            }

//            private class GroupByFieldValueAccessor : IFieldValueAccessor
//            {
//                private GroupbyFacetCountCollector parent;

//                public GroupByFieldValueAccessor(GroupbyFacetCountCollector parent)
//                {
//                    this.parent = parent;
//                }

//                public string GetFormatedValue(int index)
//                {
//                    return parent.getFacetString(index);
//                }

//                public object GetRawValue(int index)
//                {
//                    return parent.getRawFaceValue(index);
//                }
//            }

//            public virtual IEnumerable<BrowseFacet> GetFacets()
//            {
//                if (_fspec != null)
//                {
//                    int minCount = _fspec.MinHitCount;
//                    int max = _fspec.MaxCount;
//                    if (max <= 0)
//                        max = _count.Length;

//                    FacetSpec.FacetSortSpec sortspec = _fspec.OrderBy;
//                    List<BrowseFacet> facetColl;
//                    if (sortspec == FacetSpec.FacetSortSpec.OrderValueAsc)
//                    {
//                        facetColl = new List<BrowseFacet>(max);
//                        for (int i = 1; i < _count.Length; ++i) // exclude zero
//                        {
//                            int hits = _count[i];
//                            if (hits >= minCount)
//                            {
//                                BrowseFacet facet = new BrowseFacet(getFacetString(i), hits);
//                                facetColl.Add(facet);
//                            }
//                            if (facetColl.Count >= max)
//                                break;
//                        }
//                    }
//                    else
//                    {
//                        IComparatorFactory comparatorFactory;
//                        if (sortspec == FacetSpec.FacetSortSpec.OrderHitsDesc)
//                        {
//                            comparatorFactory = new FacetHitcountComparatorFactory();
//                        }
//                        else
//                        {
//                            comparatorFactory = _fspec.CustomComparatorFactory;
//                        }

//                        if (comparatorFactory == null)
//                        {
//                            throw new System.ArgumentException("facet comparator factory not specified");
//                        }

//                        IComparer<int> comparator = comparatorFactory.NewComparator(new GroupByFieldValueAccessor(this), _count);
//                        facetColl = new List<BrowseFacet>();
//                        BoundedPriorityQueue<int> pq = new BoundedPriorityQueue<int>(comparator, max);

//                        for (int i = 1; i < _count.Length; ++i) // exclude zero
//                        {
//                            int hits = _count[i];
//                            if (hits >= minCount)
//                            {
//                                if (!pq.Offer(i))
//                                {
//                                    // pq is full. we can safely ignore any facet with <=hits.
//                                    minCount = hits + 1;
//                                }
//                            }
//                        }

//                        while (!pq.IsEmpty) 
//                        {
//                            int val = pq.DeleteMax();
//                            BrowseFacet facet = new BrowseFacet(getFacetString(val), _count[val]);
//                            facetColl.Add(facet);
//                        }
//                    }
//                    return facetColl;
//                }
//                else
//                {
//                    return IFacetCountCollector_Fields.EMPTY_FACET_LIST;
//                }
//            }
//        }
//    }
//}