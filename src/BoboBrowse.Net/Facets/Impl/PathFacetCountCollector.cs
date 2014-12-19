// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Common.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public class PathFacetCountCollector : IFacetCountCollector
    {
        private static ILog log = LogManager.GetLogger<PathFacetCountCollector>();
        private readonly BrowseSelection _sel;
        private readonly FacetSpec _ospec;
        protected int[] _count;
        private readonly string _name;
        private readonly string _sep;
        private readonly BigSegmentedArray _orderArray;
        protected readonly IFacetDataCache _dataCache;
        private readonly IComparatorFactory _comparatorFactory;
        private readonly int _minHitCount;
	    private int _maxCount;
	    private static Regex _splitPat;
	    private string[] _stringData;
	    private char[] _sepArray;
	    private int _patStart;
	    private int _patEnd;

        internal PathFacetCountCollector(string name, string sep, BrowseSelection sel, FacetSpec ospec, IFacetDataCache dataCache)
        {
            _sel = sel;
            _ospec = ospec;
            _name = name;
            _dataCache = dataCache;
            _sep = sep;
            _sepArray = sep.ToCharArray();
            _count = new int[_dataCache.Freqs.Length];
            log.Info(name + ": " + _count.Length);
            _orderArray = _dataCache.OrderArray;
            _minHitCount = ospec.MinHitCount;
            _maxCount = ospec.MaxCount;
            if (_maxCount < 1)
            {
                _maxCount = _count.Length;
            }
            FacetSpec.FacetSortSpec sortOption = ospec.OrderBy;
            switch (sortOption)
            {
                case FacetSpec.FacetSortSpec.OrderHitsDesc: _comparatorFactory = new FacetHitcountComparatorFactory(); break;
                case FacetSpec.FacetSortSpec.OrderValueAsc: _comparatorFactory = null; break;
                case FacetSpec.FacetSortSpec.OrderByCustom: _comparatorFactory = ospec.CustomComparatorFactory; break;
                default: throw new ArgumentOutOfRangeException("invalid sort option: " + sortOption);
            }
            _splitPat = new Regex(_sep, RegexOptions.Compiled);
            _stringData = new string[10];
            _patStart = 0;
            _patEnd = 0;
        }

        public int[] GetCountDistribution()
        {
            return _count;
        }

        public string Name
        {
            get { return _name; }
        }

        public void Collect(int docid)
        {
            _count[_orderArray.Get(docid)]++;
        }

        public void CollectAll()
        {
            _count = _dataCache.Freqs;
        }

        public BrowseFacet GetFacet(string @value)
        {
            return null;
        }

        public int GetFacetHitsCount(object value)
        {
            return 0;
        }

        private void EnsureCapacity(int minCapacity)
        {
            int oldCapacity = _stringData.Length;
            if (minCapacity > oldCapacity)
            {
                string[] oldData = _stringData;
                int newCapacity = (oldCapacity * 3) / 2 + 1;
                if (newCapacity < minCapacity)
                    newCapacity = minCapacity;
                // minCapacity is usually close to size, so this is a win:
                _stringData = new string[newCapacity];
                Array.Copy(oldData, 0, _stringData, Math.Min(oldData.Length, newCapacity), newCapacity);
            }
        }

        private int PatListSize()
        {
            return (_patEnd - _patStart);
        }

        public bool SplitString(string input)
        {
            _patStart = 0;
            _patEnd = 0;
            char[] str = input.ToCharArray();
            int index = 0;
            int sepindex = 0;
            int tokStart = -1;
            int tokEnd = 0;
            while (index < input.Length)
            {
                for (sepindex = 0; (sepindex < _sepArray.Length) && (str[index + sepindex] == _sepArray[sepindex]); sepindex++) ;
                if (sepindex == _sepArray.Length)
                {
                    index += _sepArray.Length;
                    if (tokStart >= 0)
                    {
                        EnsureCapacity(_patEnd + 1);
                        tokEnd++;
                        _stringData[_patEnd++] = input.Substring(tokStart, tokEnd);
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

            if (_patEnd == 0)
                return false;

            if (tokStart >= 0)
            {
                EnsureCapacity(_patEnd + 1);
                tokEnd++;
                _stringData[_patEnd++] = input.Substring(tokStart, tokEnd);
            }

            // let gc do its job 
            str = null;

            // Construct result
            while (_patEnd > 0 && _stringData[PatListSize() - 1].Equals(""))
            {
                _patEnd--;
            }
            return true;
        }

        private IEnumerable<BrowseFacet> GetFacetsForPath(string selectedPath, int depth, bool strict, int minCount, int maxCount)
        {
            List<BrowseFacet> list = new List<BrowseFacet>();

            BoundedPriorityQueue<BrowseFacet> pq = null;
            if (_comparatorFactory != null)
            {
                IComparer<BrowseFacet> comparator = _comparatorFactory.NewComparator();

                pq = new BoundedPriorityQueue<BrowseFacet>(new PathFacetCountCollectorComparator(comparator), maxCount);
            }

            string[] startParts = null;
            int startDepth = 0;

            if (selectedPath != null && selectedPath.Length > 0)
            {
                startParts = selectedPath.Split(new string[] { _sep }, StringSplitOptions.RemoveEmptyEntries);
                startDepth = startParts.Length;
                if (!selectedPath.EndsWith(_sep))
                {
                    selectedPath += _sep;
                }
            }

            string currentPath = null;
            int currentCount = 0;

            int wantedDepth = startDepth + depth;

            int index = 0;
            if (selectedPath != null && selectedPath.Length > 0)
            {
                index = _dataCache.ValArray.IndexOf(selectedPath);
                if (index < 0)
                {
                    index = -(index + 1);
                }
            }

            //string[] pathParts; // NOT USED
            StringBuilder buf = new StringBuilder();
            for (int i = index; i < _count.Length; ++i)
            {
                if (_count[i] >= minCount)
                {
                    string path = _dataCache.ValArray.Get(i);
                    //if (path==null || path.equals(selectedPath)) continue;						

                    int subCount = _count[i];

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
                        //buf.Delete(0, buf.length());
                        buf.Clear();
                        int minDepth = Math.Min(wantedDepth, pathDepth);
                        tmpdepth = 0;
                        for (int k = _patStart; ((k < _patEnd) && (tmpdepth < minDepth)); ++k, tmpdepth++)
                        {
                            buf.Append(_stringData[k]);
                            if (!_stringData[k].EndsWith(_sep))
                            {
                                if (pathDepth != wantedDepth || k < (wantedDepth - 1))
                                    buf.Append(_sep);
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

                            if (wantedPath.EndsWith(_sep))
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

        private class PathFacetCountCollectorComparator : IComparer<BrowseFacet>
        {
            private readonly IComparer<BrowseFacet> _comparator;

            public PathFacetCountCollectorComparator(IComparer<BrowseFacet> comparator)
            {
                _comparator = comparator;
            }

            public int Compare(BrowseFacet o1, BrowseFacet o2)
            {
                return -_comparator.Compare(o1, o2);
            }
        }

        public IEnumerable<BrowseFacet> GetFacets()
        {
            Properties props = _sel == null ? null : _sel.SelectionProperties;
            int depth = PathFacetHandler.GetDepth(props);
            bool strict = PathFacetHandler.IsStrict(props);

            string[] paths = _sel == null ? null : _sel.Values;
            if (paths == null || paths.Length == 0)
            {
                return GetFacetsForPath(null, depth, strict, _minHitCount, _maxCount);
            }

            if (paths.Length == 1) return GetFacetsForPath(paths[0], depth, strict, _minHitCount, _maxCount);

            List<BrowseFacet> finalList = new List<BrowseFacet>();
            var iterList = new List<IEnumerator<BrowseFacet>>(paths.Length);
            foreach (string path in paths)
            {
                var subList = GetFacetsForPath(path, depth, strict, _minHitCount, _maxCount);
                if (subList.Count() > 0)
                {
                    iterList.Add(subList.GetEnumerator());
                }
            }

            var finalIter = ListMerger.MergeLists(iterList.ToArray(),
                _comparatorFactory == null ? new FacetValueComparatorFactory().NewComparator() : _comparatorFactory.NewComparator());
            while (finalIter.MoveNext())
            {
                BrowseFacet f = finalIter.Current;
                finalList.Insert(0, f);
            }
            return finalList;
        }

        public void Close()
        { }

        public FacetIterator Iterator()
        {
            Properties props = _sel == null ? null : _sel.SelectionProperties;
            int depth = PathFacetHandler.GetDepth(props);
            bool strict = PathFacetHandler.IsStrict(props);
            List<BrowseFacet> finalList;

            string[] paths = _sel == null ? null : _sel.Values;
            if (paths == null || paths.Length == 0)
            {
                finalList = new List<BrowseFacet>(GetFacetsForPath(null, depth, strict, int.MinValue, _count.Length));
                return new PathFacetIterator(finalList);
            }

            if (paths.Length == 1)
            {
                finalList = new List<BrowseFacet>(GetFacetsForPath(paths[0], depth, strict, int.MinValue, _count.Length));
                return new PathFacetIterator(finalList);
            }

            finalList = new List<BrowseFacet>();
            var iterList = new List<IEnumerator<BrowseFacet>>(paths.Length);
            foreach (string path in paths)
            {
                var subList = GetFacetsForPath(path, depth, strict, int.MinValue, _count.Length);
                if (subList.Count() > 0)
                {
                    iterList.Add(subList.GetEnumerator());
                }
            }
            var finalIter = ListMerger.MergeLists(iterList.ToArray(),
                _comparatorFactory == null ? new FacetValueComparatorFactory().NewComparator() : _comparatorFactory.NewComparator());

            while (finalIter.MoveNext())
            {
                BrowseFacet f = finalIter.Current;
                finalList.Add(f);
            }
            return new PathFacetIterator(finalList);
        }
    }
}
