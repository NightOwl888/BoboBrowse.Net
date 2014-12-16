// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;
    using Lucene.Net.Index;
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

        public SimpleGroupbyFacetHandler(string name, IEnumerable<string> dependsOn, string separator)
            : base(name, dependsOn)
        {
            _fieldsSet = dependsOn;
            _facetHandlers = null;
            _facetHandlerMap = null;
            _sep = separator;
        }

        public SimpleGroupbyFacetHandler(string name, IEnumerable<string> dependsOn)
            : this(name, dependsOn, SEP)
        {
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, Properties selectionProperty)
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

            public override IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase)
            {
                var collectorList = new List<DefaultFacetCountCollector>(_facetHandlers.Count());
                foreach (var facetHandler in _facetHandlers)
                {
                    collectorList.Add((DefaultFacetCountCollector)facetHandler.GetFacetCountCollectorSource(_sel, _fspec).GetFacetCountCollector(reader, docBase));
                }
                return new GroupbyFacetCountCollector(_name, _fspec, collectorList.ToArray(), reader.MaxDoc, _sep);
            }
        }

        public override string[] GetFieldValues(BoboIndexReader reader, int id)
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

        public override object[] GetRawFieldValues(BoboIndexReader reader, int id)
        {
            return GetFieldValues(reader, id);
        }

        public override DocComparatorSource GetDocComparatorSource()
        {
            return new GroupbyDocComparatorSource(_fieldsSet, _facetHandlers);
        }

        public class GroupbyDocComparatorSource : DocComparatorSource
        {
            private readonly IEnumerable<string> _fieldsSet;
            private readonly IEnumerable<SimpleFacetHandler> _facetHandlers;

            public GroupbyDocComparatorSource(IEnumerable<string> fieldsSet, IEnumerable<SimpleFacetHandler> facetHandlers)
            {
                _fieldsSet = fieldsSet;
                _facetHandlers = facetHandlers;
            }

            public override DocComparator GetComparator(IndexReader reader, int docbase)
            {
                var comparatorList = new List<DocComparator>(_fieldsSet.Count());
                foreach (var handler in _facetHandlers)
                {
                    comparatorList.Add(handler.GetDocComparatorSource().GetComparator(reader, docbase));
                }
                return new GroupbyDocComparator(comparatorList.ToArray());
            }
        }

        public override FacetDataNone Load(BoboIndexReader reader)
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
            return FacetDataNone.instance;
        }

        private class GroupbyDocComparator : DocComparator
        {
            private readonly DocComparator[] _comparators;

            public GroupbyDocComparator(DocComparator[] comparators)
            {
                _comparators = comparators;
            }

            public override int Compare(ScoreDoc d1, ScoreDoc d2)
            {
                int retval = 0;
                foreach (DocComparator comparator in _comparators)
                {
                    retval = comparator.Compare(d1, d2);
                    if (retval != 0) break;
                }
                return retval;
            }

            public override IComparable Value(ScoreDoc doc)
            {
                return new GroupbyComparable(_comparators, doc);
            }
        }

        public class GroupbyComparable : IComparable
        {
            private readonly DocComparator[] _comparators;
            private readonly ScoreDoc _doc;

            public GroupbyComparable(DocComparator[] comparators, ScoreDoc doc)
            {
                _comparators = comparators;
                _doc = doc;
            }

            public int CompareTo(object o)
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
            private readonly int[] _count;
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
                    _lens[i] = _subcollectors[i]._count.Length;
                    totalLen *= _lens[i];
                }
                _countlength = totalLen;
                _count = new int[_countlength];
                _maxdoc = maxdoc;
            }

            public sealed void Collect(int docid)
            {
                int idx = 0;
                int i = 0;
                int segsize = _countlength;
                foreach (DefaultFacetCountCollector subcollector in _subcollectors)
                {
                    segsize = segsize / _lens[i++];
                    idx += (subcollector._dataCache.OrderArray.Get(docid) * segsize);
                }
                _count[idx]++;
            }

            public virtual void CollectAll()
            {
                for (int i = 0; i < _maxdoc; ++i)
                {
                    Collect(i);
                }
            }

            public virtual int[] GetCountDistribution()
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
                    int index = _subcollectors[i]._dataCache.ValArray.IndexOf(vals[i]);
                    string facetName = _subcollectors[i]._dataCache.ValArray.Get(index);
                    buf.Append(facetName);

                    segLen /= _subcollectors[i]._countlength;
                    startIdx += index * segLen;
                }

                int count = 0;
                for (int i = startIdx; i < startIdx + segLen; ++i)
                {
                    count += _count[i];
                }

                BrowseFacet f = new BrowseFacet(buf.ToString(), count);
                return f;
            }

            public int GetFacetHitsCount(object value)
            {
                string[] vals = ((string)value).Split(new string[] { _sep }, StringSplitOptions.RemoveEmptyEntries);
                if (vals.Length == 0) return 0;
                int startIdx = 0;
                int segLen = _countlength;

                for (int i = 0; i < vals.Length; ++i)
                {
                    int index = _subcollectors[i]._dataCache.ValArray.IndexOf(vals[i]);
                    segLen /= _subcollectors[i]._countlength;
                    startIdx += index * segLen;
                }

                int count = 0;
                for (int i = startIdx; i < startIdx + segLen; ++i)
                    count += _count[i];

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
                    buf.Append(_subcollectors[i]._dataCache.ValArray.Get(bucket));
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
                    retVal[i++] = _subcollectors[i]._dataCache.ValArray.GetRawValue(bucket);
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
                            int hits = _count[i];
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

                        IComparer<int> comparator = comparatorFactory.NewComparator(new GroupbyFieldValueAccessor(this), _count);
                        facetColl = new List<BrowseFacet>();
                        int forbidden = -1;
                        BoundedPriorityQueue<int> pq = new BoundedPriorityQueue<int>(comparator, max, forbidden);

                        for (int i = 1; i < _countlength; ++i) // exclude zero
                        {
                            int hits = _count[i];
                            if (hits >= minCount)
                            {
                                if (!pq.Offer(i))
                                {
                                    // pq is full. we can safely ignore any facet with <=hits.
                                    minCount = hits + 1;
                                }
                            }
                        }

                        //// NOTE: The code below is the equivalent
                        //int val;
                        //while ((val = pq.pollInt()) != forbidden)
                        //{
                        //    BrowseFacet facet = new BrowseFacet(getFacetString(val), _count[val]);
                        //    ((LinkedList<BrowseFacet>)facetColl).addFirst(facet);
                        //}

                        while (!pq.IsEmpty)
                        {
                            int val = pq.DeleteMax();
                            BrowseFacet facet = new BrowseFacet(GetFacetString(val), _count[val]);
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

            public virtual void Close()
            { }

            public FacetIterator Iterator()
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
                    _stringFacet = null;
                    _count = 0;
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
                    _stringFacet = _parent.GetFacetString(_index);
                    _count = _parent._count[_index];
                    return _stringFacet;
                }

                /// <summary>
                /// (non-Javadoc)
                /// see java.util.Iterator#hasNext()
                /// </summary>
                /// <returns></returns>
                public bool HasNext()
                {
                    return (_index < (_parent._countlength - 1));
                }

                /// <summary>
                /// (non-Javadoc)
                /// see java.util.Iterator#remove()
                /// </summary>
                public void Remove()
                {
                    throw new NotSupportedException("remove() method not supported for Facet Iterators");
                }

                /// <summary>
                /// (non-Javadoc)
                /// see com.browseengine.bobo.api.FacetIterator#next(int)
                /// </summary>
                /// <param name="minHits"></param>
                /// <returns></returns>
                public string Next(int minHits)
                {
                    if ((_index >= 0) && !HasNext())
                    {
                        _count = 0;
                        _stringFacet = null;
                        return null;
                    }
                    do
                    {
                        _index++;
                    } while ((_index < (_parent._countlength - 1)) && (_parent._count[_index] < minHits));
                    if (_parent._count[_index] >= minHits)
                    {
                        _stringFacet = _parent.GetFacetString(_index);
                        _count = _parent._count[_index];
                    }
                    else
                    {
                        _count = 0;
                        _stringFacet = null;
                    }
                    return _stringFacet;
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